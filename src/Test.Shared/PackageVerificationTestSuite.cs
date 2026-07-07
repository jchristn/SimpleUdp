namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Touchstone.Core;

    public static class PackageVerificationTestSuite
    {
        public const string SuiteId = "package-verification";

        public const string SuiteName = "Package Verification";

        public static TestSuiteDescriptor Create()
        {
            return TouchstoneDescriptorFactory.Suite(
                SuiteId,
                SuiteName,
                new List<TestCaseDescriptor>
                {
                    TouchstoneDescriptorFactory.Case(SuiteId, "packed-nupkg-consumer-smoke", "Packed NuGet package works from a temporary consumer project", PackedNuGetPackageWorksFromTemporaryConsumerProjectAsync)
                });
        }

        private static async Task PackedNuGetPackageWorksFromTemporaryConsumerProjectAsync()
        {
            string repoRoot = UdpTestHelpers.FindRepositoryRoot();
            string tempRoot = Path.Combine(Path.GetTempPath(), "SimpleUdpPackageTest-" + Guid.NewGuid().ToString("N"));
            string packageDir = Path.Combine(tempRoot, "packages");
            string consumerDir = Path.Combine(tempRoot, "consumer");

            Directory.CreateDirectory(packageDir);
            Directory.CreateDirectory(consumerDir);

            try
            {
                string projectPath = Path.Combine(repoRoot, "src", "SimpleUdp", "SimpleUdp.csproj");

                await UdpTestHelpers.RunProcessAsync(
                    "dotnet",
                    "build \"" + projectPath + "\" -c Release",
                    repoRoot,
                    TimeSpan.FromSeconds(60)).ConfigureAwait(false);

                await UdpTestHelpers.RunProcessAsync(
                    "dotnet",
                    "pack \"" + projectPath + "\" -c Release --no-build -o \"" + packageDir + "\"",
                    repoRoot,
                    TimeSpan.FromSeconds(60)).ConfigureAwait(false);

                string packagePath = Path.Combine(packageDir, "SimpleUdp.3.1.1.nupkg");
                AssertEx.True(File.Exists(packagePath), "dotnet pack should produce SimpleUdp.3.1.1.nupkg.");

                File.WriteAllText(
                    Path.Combine(consumerDir, "NuGet.config"),
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine
                    + "<configuration>" + Environment.NewLine
                    + "  <packageSources>" + Environment.NewLine
                    + "    <clear />" + Environment.NewLine
                    + "    <add key=\"local\" value=\"" + packageDir + "\" />" + Environment.NewLine
                    + "    <add key=\"nuget\" value=\"https://api.nuget.org/v3/index.json\" />" + Environment.NewLine
                    + "  </packageSources>" + Environment.NewLine
                    + "</configuration>" + Environment.NewLine);

                File.WriteAllText(
                    Path.Combine(consumerDir, "Consumer.csproj"),
                    "<Project Sdk=\"Microsoft.NET.Sdk\">" + Environment.NewLine
                    + "  <PropertyGroup>" + Environment.NewLine
                    + "    <OutputType>Exe</OutputType>" + Environment.NewLine
                    + "    <TargetFramework>net10.0</TargetFramework>" + Environment.NewLine
                    + "    <ImplicitUsings>enable</ImplicitUsings>" + Environment.NewLine
                    + "    <Nullable>enable</Nullable>" + Environment.NewLine
                    + "  </PropertyGroup>" + Environment.NewLine
                    + "  <ItemGroup>" + Environment.NewLine
                    + "    <PackageReference Include=\"SimpleUdp\" Version=\"3.1.1\" />" + Environment.NewLine
                    + "  </ItemGroup>" + Environment.NewLine
                    + "</Project>" + Environment.NewLine);

                File.WriteAllText(Path.Combine(consumerDir, "Program.cs"), CreateConsumerProgram());

                ProcessResult result = await UdpTestHelpers.RunProcessAsync(
                    "dotnet",
                    "run --framework net10.0",
                    consumerDir,
                    TimeSpan.FromSeconds(90)).ConfigureAwait(false);

                AssertEx.Contains("PACKAGE_SMOKE_OK", result.OutputLines, "The temporary consumer should run a SimpleUdp send/receive through the packed package.");
            }
            finally
            {
                try
                {
                    Directory.Delete(tempRoot, true);
                }
                catch
                {
                }
            }
        }

        private static string CreateConsumerProgram()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Net;");
            sb.AppendLine("using System.Net.Sockets;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using SimpleUdp;");
            sb.AppendLine();
            sb.AppendLine("static int GetPort()");
            sb.AppendLine("{");
            sb.AppendLine("    using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);");
            sb.AppendLine("    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));");
            sb.AppendLine("    return ((IPEndPoint)socket.LocalEndPoint!).Port;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("int senderPort = GetPort();");
            sb.AppendLine("int receiverPort = GetPort();");
            sb.AppendLine("while (receiverPort == senderPort) receiverPort = GetPort();");
            sb.AppendLine("TaskCompletionSource<Datagram> received = new TaskCompletionSource<Datagram>(TaskCreationOptions.RunContinuationsAsynchronously);");
            sb.AppendLine("using UdpEndpoint sender = new UdpEndpoint(\"127.0.0.1\", senderPort);");
            sb.AppendLine("using UdpEndpoint receiver = new UdpEndpoint(\"127.0.0.1\", receiverPort);");
            sb.AppendLine("receiver.DatagramReceived += (_, datagram) => received.TrySetResult(datagram);");
            sb.AppendLine("await sender.SendAsync(\"127.0.0.1\", receiverPort, \"package-smoke\");");
            sb.AppendLine("Task completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));");
            sb.AppendLine("if (completed != received.Task) throw new TimeoutException(\"Package consumer did not receive datagram.\");");
            sb.AppendLine("Datagram datagram = await received.Task;");
            sb.AppendLine("if (Encoding.UTF8.GetString(datagram.Data) != \"package-smoke\") throw new InvalidOperationException(\"Unexpected payload.\");");
            sb.AppendLine("Console.WriteLine(\"PACKAGE_SMOKE_OK\");");
            return sb.ToString();
        }
    }
}
