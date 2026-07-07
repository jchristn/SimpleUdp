namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class UdpTestHelpers
    {
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        internal static int GetAvailableUdpPort()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            return ((IPEndPoint)socket.LocalEndPoint!).Port;
        }

        internal static (int First, int Second) GetAvailableUdpPortPair()
        {
            int first = GetAvailableUdpPort();
            int second = GetAvailableUdpPort();

            while (second == first)
            {
                second = GetAvailableUdpPort();
            }

            return (first, second);
        }

        internal static TaskCompletionSource<T> CreateCompletionSource<T>()
        {
            return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        internal static async Task<T> WithTimeout<T>(Task<T> task, string operation, TimeSpan? timeout = null)
        {
            timeout ??= DefaultTimeout;
            Task delay = Task.Delay(timeout.Value);
            Task completed = await Task.WhenAny(task, delay).ConfigureAwait(false);
            if (completed != task) throw new TimeoutException(operation + " timed out after " + timeout.Value.TotalSeconds + "s.");
            return await task.ConfigureAwait(false);
        }

        internal static async Task WithTimeout(Task task, string operation, TimeSpan? timeout = null)
        {
            timeout ??= DefaultTimeout;
            Task delay = Task.Delay(timeout.Value);
            Task completed = await Task.WhenAny(task, delay).ConfigureAwait(false);
            if (completed != task) throw new TimeoutException(operation + " timed out after " + timeout.Value.TotalSeconds + "s.");
            await task.ConfigureAwait(false);
        }

        internal static async Task WaitUntil(Func<bool> condition, string operation, TimeSpan? timeout = null)
        {
            timeout ??= DefaultTimeout;
            DateTimeOffset expires = DateTimeOffset.UtcNow.Add(timeout.Value);

            while (DateTimeOffset.UtcNow < expires)
            {
                if (condition()) return;
                await Task.Delay(25).ConfigureAwait(false);
            }

            throw new TimeoutException(operation + " timed out after " + timeout.Value.TotalSeconds + "s.");
        }

        internal static int SendRawLoopback(int receiverPort, byte[] payload)
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            int localPort = ((IPEndPoint)socket.LocalEndPoint!).Port;
            socket.SendTo(payload, new IPEndPoint(IPAddress.Loopback, receiverPort));
            return localPort;
        }

        internal static Socket CreateBoundLoopbackDatagramSocket(out int port)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            port = ((IPEndPoint)socket.LocalEndPoint!).Port;
            return socket;
        }

        internal static string FindRepositoryRoot()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "src", "SimpleUdp", "SimpleUdp.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Unable to find the repository root from " + AppContext.BaseDirectory + ".");
        }

        internal static string GetProcessHelperPath()
        {
            string repoRoot = FindRepositoryRoot();
            DirectoryInfo baseDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string configuration = baseDirectory.Parent?.Name ?? "Debug";
            string framework = baseDirectory.Name;
            return Path.Combine(repoRoot, "src", "Test.Process", "bin", configuration, framework, "Test.Process.dll");
        }

        internal static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, string workingDirectory, TimeSpan timeout)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            List<string> outputLines = new List<string>();
            List<string> errorLines = new List<string>();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) lock (outputLines) outputLines.Add(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) lock (errorLines) errorLines.Add(e.Data);
            };

            if (!process.Start()) throw new InvalidOperationException("Unable to start process " + fileName + ".");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Task waitForExit = process.WaitForExitAsync();
            Task completed = await Task.WhenAny(waitForExit, Task.Delay(timeout)).ConfigureAwait(false);
            if (completed != waitForExit)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }

                throw new TimeoutException(fileName + " " + arguments + " timed out after " + timeout.TotalSeconds + "s.");
            }

            await waitForExit.ConfigureAwait(false);

            ProcessResult result = new ProcessResult(process.ExitCode, outputLines, errorLines);
            if (result.ExitCode != 0)
            {
                throw new TestAssertionException(
                    fileName
                    + " "
                    + arguments
                    + " exited with code "
                    + result.ExitCode
                    + ". Stdout: "
                    + String.Join(" | ", result.OutputLines)
                    + ". Stderr: "
                    + String.Join(" | ", result.ErrorLines));
            }

            return result;
        }
    }

    internal sealed class ProcessResult
    {
        internal ProcessResult(int exitCode, IReadOnlyList<string> outputLines, IReadOnlyList<string> errorLines)
        {
            ExitCode = exitCode;
            OutputLines = outputLines;
            ErrorLines = errorLines;
        }

        internal int ExitCode { get; }

        internal IReadOnlyList<string> OutputLines { get; }

        internal IReadOnlyList<string> ErrorLines { get; }
    }
}
