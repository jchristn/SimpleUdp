namespace Test.Shared
{
    using System;
    using System.Net;
    using System.Net.Sockets;
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
    }
}
