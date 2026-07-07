namespace Test.Process
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using SimpleUdp;

    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Missing role.");
                return 2;
            }

            if (args[0].Equals("send", StringComparison.OrdinalIgnoreCase))
            {
                return await SendAsync(args).ConfigureAwait(false);
            }

            if (args[0].Equals("send-before-peer-and-receive", StringComparison.OrdinalIgnoreCase))
            {
                return await SendBeforePeerAndReceiveAsync(args).ConfigureAwait(false);
            }

            Console.Error.WriteLine("Unknown role: " + args[0]);
            return 2;
        }

        private static async Task<int> SendAsync(string[] args)
        {
            if (args.Length != 4)
            {
                Console.Error.WriteLine("Usage: send <localPort> <remotePort> <payload>");
                return 2;
            }

            int localPort = Int32.Parse(args[1]);
            int remotePort = Int32.Parse(args[2]);
            string payload = args[3];

            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", localPort);
            await endpoint.SendAsync("127.0.0.1", remotePort, payload).ConfigureAwait(false);
            Console.WriteLine("SENT");
            return 0;
        }

        private static async Task<int> SendBeforePeerAndReceiveAsync(string[] args)
        {
            if (args.Length != 5)
            {
                Console.Error.WriteLine("Usage: send-before-peer-and-receive <localPort> <closedPeerPort> <expectedPayload> <timeoutMs>");
                return 2;
            }

            int localPort = Int32.Parse(args[1]);
            int closedPeerPort = Int32.Parse(args[2]);
            string expectedPayload = args[3];
            int timeoutMs = Int32.Parse(args[4]);

            TaskCompletionSource<Datagram> received = new TaskCompletionSource<Datagram>(TaskCreationOptions.RunContinuationsAsynchronously);
            int serverStoppedCount = 0;

            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", localPort);
            endpoint.ServerStopped += (_, _) => serverStoppedCount++;
            endpoint.DatagramReceived += (_, datagram) => received.TrySetResult(datagram);

            await endpoint.SendAsync("127.0.0.1", closedPeerPort, "sent-before-peer-started").ConfigureAwait(false);
            Console.WriteLine("READY");

            Task completed = await Task.WhenAny(received.Task, Task.Delay(timeoutMs)).ConfigureAwait(false);
            if (completed != received.Task)
            {
                Console.Error.WriteLine("Timed out waiting for payload. ServerStopped=" + serverStoppedCount);
                return 3;
            }

            Datagram datagram = await received.Task.ConfigureAwait(false);
            string actualPayload = Encoding.UTF8.GetString(datagram.Data);
            if (!String.Equals(expectedPayload, actualPayload, StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Expected payload '" + expectedPayload + "' but received '" + actualPayload + "'.");
                return 4;
            }

            if (serverStoppedCount != 0)
            {
                Console.Error.WriteLine("ServerStopped was raised " + serverStoppedCount + " times.");
                return 5;
            }

            Console.WriteLine("RECEIVED " + datagram.Ip + ":" + datagram.Port);
            return 0;
        }
    }
}
