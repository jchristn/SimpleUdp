namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using SimpleUdp;
    using Touchstone.Core;

    public static class UdpEndpointTestSuite
    {
        public const string SuiteId = "udp-endpoint-api";

        public const string SuiteName = "UdpEndpoint API";

        public static TestSuiteDescriptor Create()
        {
            return TouchstoneDescriptorFactory.Suite(
                SuiteId,
                SuiteName,
                new List<TestCaseDescriptor>
                {
                    TouchstoneDescriptorFactory.Case(SuiteId, "constructor-validation", "Constructor validates port range and IP format", ConstructorValidatesPortRangeAndIpFormatAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "constructor-null-ip-loopback", "Constructor accepts a null IP and still receives loopback traffic", ConstructorAcceptsNullIpAndReceivesLoopbackTrafficAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "endpoints-start-empty", "Endpoints starts empty", EndpointsStartsEmptyAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "max-datagram-size-validation", "MaxDatagramSize validates range and stores a valid value", MaxDatagramSizeValidatesRangeAndStoresValueAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "enable-broadcast-toggle", "EnableBroadcast defaults to disabled and can be toggled", EnableBroadcastDefaultsToDisabledAndCanBeToggledAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "enable-broadcast-send", "EnableBroadcast allows broadcast sends that otherwise fail", EnableBroadcastAllowsBroadcastSendsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-string-validation", "Send string overload validates arguments", SendStringOverloadValidatesArgumentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-bytes-validation", "Send byte overload validates arguments", SendByteOverloadValidatesArgumentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-string-validation", "SendAsync string overload validates arguments", SendAsyncStringOverloadValidatesArgumentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-bytes-validation", "SendAsync byte overload validates arguments", SendAsyncByteOverloadValidatesArgumentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-string-source-port", "Send string reports the configured source port", SendStringReportsConfiguredSourcePortAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-bytes-exact-payload", "Send byte overload delivers the exact payload", SendByteOverloadDeliversExactPayloadAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-single-endpoint-detection", "SendAsync detects the same endpoint only once", SendAsyncDetectsTheSameEndpointOnlyOnceAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-reply-to-detected-port", "SendAsync supports replying to the detected port", SendAsyncSupportsReplyingToDetectedPortAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "max-datagram-size-oversize-guards", "MaxDatagramSize blocks oversize sends across all overloads", MaxDatagramSizeBlocksOversizeSendsAcrossAllOverloadsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "dispose-idempotent", "Dispose is idempotent", DisposeIsIdempotentAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "dispose-raises-server-stopped", "Dispose raises ServerStopped for the receive loop", DisposeRaisesServerStoppedForTheReceiveLoopAsync)
                });
        }

        private static Task ConstructorValidatesPortRangeAndIpFormatAsync()
        {
            AssertEx.Throws<ArgumentException>(() => new UdpEndpoint("127.0.0.1", -1), "UdpEndpoint should reject negative ports.");
            AssertEx.Throws<ArgumentException>(() => new UdpEndpoint("127.0.0.1", 65536), "UdpEndpoint should reject ports above 65535.");
            AssertEx.Throws<FormatException>(() => new UdpEndpoint("not-an-ip-address", 5000), "UdpEndpoint should reject invalid IP strings.");
            return Task.CompletedTask;
        }

        private static async Task ConstructorAcceptsNullIpAndReceivesLoopbackTrafficAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();

            using UdpEndpoint receiver = new UdpEndpoint(null!, receiverPort);
            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);

            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);

            sender.Send("127.0.0.1", receiverPort, "null-ip");

            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Receiving loopback datagram on an Any-bound endpoint").ConfigureAwait(false);

            AssertEx.Equal("127.0.0.1", datagram.Ip, "Receiver should report the sender IP.");
            AssertEx.Equal(senderPort, datagram.Port, "Receiver should report the sender's configured local port.");
            AssertEx.Equal("null-ip", Encoding.UTF8.GetString(datagram.Data), "Receiver should get the original payload.");
        }

        private static Task EndpointsStartsEmptyAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());
            AssertEx.NotNull(endpoint.Endpoints, "Endpoints should never be null.");
            AssertEx.Equal(0, endpoint.Endpoints.Count, "Endpoints should start empty.");
            return Task.CompletedTask;
        }

        private static Task MaxDatagramSizeValidatesRangeAndStoresValueAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());

            AssertEx.Throws<ArgumentException>(() => endpoint.MaxDatagramSize = 0, "MaxDatagramSize should reject zero.");
            AssertEx.Throws<ArgumentException>(() => endpoint.MaxDatagramSize = 65508, "MaxDatagramSize should reject values above the UDP payload limit.");

            endpoint.MaxDatagramSize = 2048;
            AssertEx.Equal(2048, endpoint.MaxDatagramSize, "MaxDatagramSize should store valid values.");
            return Task.CompletedTask;
        }

        private static Task EnableBroadcastDefaultsToDisabledAndCanBeToggledAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint(null!, UdpTestHelpers.GetAvailableUdpPort());

            AssertEx.False(endpoint.EnableBroadcast, "EnableBroadcast should default to disabled.");

            endpoint.EnableBroadcast = true;
            AssertEx.True(endpoint.EnableBroadcast, "EnableBroadcast should report enabled after being set.");

            endpoint.EnableBroadcast = false;
            AssertEx.False(endpoint.EnableBroadcast, "EnableBroadcast should report disabled after being reset.");
            return Task.CompletedTask;
        }

        private static Task EnableBroadcastAllowsBroadcastSendsAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint(null!, UdpTestHelpers.GetAvailableUdpPort());
            byte[] payload = new byte[] { 1, 2, 3 };
            int targetPort = UdpTestHelpers.GetAvailableUdpPort();

            AssertEx.Throws<SocketException>(() => endpoint.Send("255.255.255.255", targetPort, payload), "Broadcast sends should fail while EnableBroadcast is disabled.");

            endpoint.EnableBroadcast = true;
            endpoint.Send("255.255.255.255", targetPort, payload);
            return Task.CompletedTask;
        }

        private static Task SendStringOverloadValidatesArgumentsAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());

            AssertEx.Throws<ArgumentNullException>(() => endpoint.Send(null!, 9000, "hello"), "Send(string) should reject null IP values.");
            AssertEx.Throws<ArgumentException>(() => endpoint.Send("127.0.0.1", -1, "hello"), "Send(string) should reject negative ports.");
            AssertEx.Throws<ArgumentException>(() => endpoint.Send("127.0.0.1", 65536, "hello"), "Send(string) should reject ports above 65535.");
            AssertEx.Throws<ArgumentNullException>(() => endpoint.Send("127.0.0.1", 9000, (string)null!), "Send(string) should reject null text.");
            AssertEx.Throws<ArgumentNullException>(() => endpoint.Send("127.0.0.1", 9000, String.Empty), "Send(string) should reject empty text.");
            AssertEx.Throws<ArgumentOutOfRangeException>(() => endpoint.Send("127.0.0.1", 9000, "hello", -1), "Send(string) should reject negative TTL values.");
            return Task.CompletedTask;
        }

        private static Task SendByteOverloadValidatesArgumentsAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());

            AssertEx.Throws<ArgumentNullException>(() => endpoint.Send(null!, 9000, new byte[] { 1 }), "Send(byte[]) should reject null IP values.");
            AssertEx.Throws<ArgumentException>(() => endpoint.Send("127.0.0.1", -1, new byte[] { 1 }), "Send(byte[]) should reject negative ports.");
            AssertEx.Throws<ArgumentException>(() => endpoint.Send("127.0.0.1", 65536, new byte[] { 1 }), "Send(byte[]) should reject ports above 65535.");
            AssertEx.Throws<ArgumentNullException>(() => endpoint.Send("127.0.0.1", 9000, (byte[])null!), "Send(byte[]) should reject null payloads.");
            AssertEx.Throws<ArgumentNullException>(() => endpoint.Send("127.0.0.1", 9000, Array.Empty<byte>()), "Send(byte[]) should reject empty payloads.");
            AssertEx.Throws<ArgumentOutOfRangeException>(() => endpoint.Send("127.0.0.1", 9000, new byte[] { 1 }, -1), "Send(byte[]) should reject negative TTL values.");
            return Task.CompletedTask;
        }

        private static async Task SendAsyncStringOverloadValidatesArgumentsAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());

            await AssertEx.ThrowsAsync<ArgumentNullException>(() => endpoint.SendAsync(null!, 9000, "hello"), "SendAsync(string) should reject null IP values.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentException>(() => endpoint.SendAsync("127.0.0.1", -1, "hello"), "SendAsync(string) should reject negative ports.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentException>(() => endpoint.SendAsync("127.0.0.1", 65536, "hello"), "SendAsync(string) should reject ports above 65535.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentNullException>(() => endpoint.SendAsync("127.0.0.1", 9000, (string)null!), "SendAsync(string) should reject null text.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentNullException>(() => endpoint.SendAsync("127.0.0.1", 9000, String.Empty), "SendAsync(string) should reject empty text.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentOutOfRangeException>(() => endpoint.SendAsync("127.0.0.1", 9000, "hello", -1), "SendAsync(string) should reject negative TTL values.").ConfigureAwait(false);
        }

        private static async Task SendAsyncByteOverloadValidatesArgumentsAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());

            await AssertEx.ThrowsAsync<ArgumentNullException>(() => endpoint.SendAsync(null!, 9000, new byte[] { 1 }), "SendAsync(byte[]) should reject null IP values.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentException>(() => endpoint.SendAsync("127.0.0.1", -1, new byte[] { 1 }), "SendAsync(byte[]) should reject negative ports.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentException>(() => endpoint.SendAsync("127.0.0.1", 65536, new byte[] { 1 }), "SendAsync(byte[]) should reject ports above 65535.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentNullException>(() => endpoint.SendAsync("127.0.0.1", 9000, (byte[])null!), "SendAsync(byte[]) should reject null payloads.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentNullException>(() => endpoint.SendAsync("127.0.0.1", 9000, Array.Empty<byte>()), "SendAsync(byte[]) should reject empty payloads.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentOutOfRangeException>(() => endpoint.SendAsync("127.0.0.1", 9000, new byte[] { 1 }, -1), "SendAsync(byte[]) should reject negative TTL values.").ConfigureAwait(false);
        }

        private static async Task SendStringReportsConfiguredSourcePortAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<EndpointMetadata> detected = UdpTestHelpers.CreateCompletionSource<EndpointMetadata>();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.EndpointDetected += (_, md) => detected.TrySetResult(md);
            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);

            sender.Send("127.0.0.1", receiverPort, "ping");

            EndpointMetadata metadata = await UdpTestHelpers.WithTimeout(detected.Task, "Endpoint detection after Send(string)").ConfigureAwait(false);
            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Datagram delivery after Send(string)").ConfigureAwait(false);

            AssertEx.Equal("127.0.0.1", metadata.Ip, "EndpointDetected should report the sender IP.");
            AssertEx.Equal(senderPort, metadata.Port, "EndpointDetected should report the sender's configured local port.");
            AssertEx.Equal(senderPort, datagram.Port, "DatagramReceived should report the sender's configured local port.");
            AssertEx.Equal("ping", Encoding.UTF8.GetString(datagram.Data), "DatagramReceived should preserve the text payload.");
            AssertEx.Contains("127.0.0.1:" + senderPort, receiver.Endpoints, "Endpoints should include the sender endpoint.");
        }

        private static async Task SendByteOverloadDeliversExactPayloadAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();
            byte[] payload = new byte[] { 0, 1, 2, 3, 254, 255 };

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);

            sender.Send("127.0.0.1", receiverPort, payload);

            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Datagram delivery after Send(byte[])").ConfigureAwait(false);

            AssertEx.Equal(senderPort, datagram.Port, "Byte sends should preserve the sender's configured local port.");
            AssertEx.SequenceEqual(payload, datagram.Data, "Byte sends should preserve the payload exactly.");
        }

        private static async Task SendAsyncDetectsTheSameEndpointOnlyOnceAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<bool> messagesReceived = UdpTestHelpers.CreateCompletionSource<bool>();
            List<string> payloads = new List<string>();
            int endpointDetectedCount = 0;

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.EndpointDetected += (_, _) => Interlocked.Increment(ref endpointDetectedCount);
            receiver.DatagramReceived += (_, dg) =>
            {
                lock (payloads)
                {
                    payloads.Add(Encoding.UTF8.GetString(dg.Data));
                    if (payloads.Count == 2) messagesReceived.TrySetResult(true);
                }
            };

            await sender.SendAsync("127.0.0.1", receiverPort, "first").ConfigureAwait(false);
            await sender.SendAsync("127.0.0.1", receiverPort, "second").ConfigureAwait(false);

            await UdpTestHelpers.WithTimeout(messagesReceived.Task, "Receiving repeated datagrams from the same endpoint").ConfigureAwait(false);

            AssertEx.Equal(1, endpointDetectedCount, "EndpointDetected should only fire once per remote endpoint.");
            AssertEx.Equal(2, payloads.Count, "Both async datagrams should be received.");
            AssertEx.Equal("first", payloads[0], "The first payload should be preserved.");
            AssertEx.Equal("second", payloads[1], "The second payload should be preserved.");
        }

        private static async Task SendAsyncSupportsReplyingToDetectedPortAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> receiverDatagram = UdpTestHelpers.CreateCompletionSource<Datagram>();
            TaskCompletionSource<Datagram> senderReply = UdpTestHelpers.CreateCompletionSource<Datagram>();
            byte[] requestPayload = Encoding.UTF8.GetBytes("request");

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.DatagramReceived += (_, dg) => receiverDatagram.TrySetResult(dg);
            sender.DatagramReceived += (_, dg) => senderReply.TrySetResult(dg);

            await sender.SendAsync("127.0.0.1", receiverPort, requestPayload).ConfigureAwait(false);

            Datagram inbound = await UdpTestHelpers.WithTimeout(receiverDatagram.Task, "Receiving async request datagram").ConfigureAwait(false);

            AssertEx.Equal(senderPort, inbound.Port, "The receiver should observe the sender's configured local port.");

            await receiver.SendAsync(inbound.Ip, inbound.Port, "reply").ConfigureAwait(false);

            Datagram reply = await UdpTestHelpers.WithTimeout(senderReply.Task, "Receiving reply on the sender's configured local port").ConfigureAwait(false);

            AssertEx.Equal(receiverPort, reply.Port, "The reply should originate from the receiver's configured local port.");
            AssertEx.Equal("reply", Encoding.UTF8.GetString(reply.Data), "The sender should receive the reply payload.");
        }

        private static async Task MaxDatagramSizeBlocksOversizeSendsAcrossAllOverloadsAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());

            endpoint.MaxDatagramSize = 4;

            AssertEx.Throws<ArgumentException>(() => endpoint.Send("127.0.0.1", 9000, "12345"), "Send(string) should reject payloads above MaxDatagramSize.");
            AssertEx.Throws<ArgumentException>(() => endpoint.Send("127.0.0.1", 9000, new byte[] { 1, 2, 3, 4, 5 }), "Send(byte[]) should reject payloads above MaxDatagramSize.");
            await AssertEx.ThrowsAsync<ArgumentException>(() => endpoint.SendAsync("127.0.0.1", 9000, "12345"), "SendAsync(string) should reject payloads above MaxDatagramSize.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ArgumentException>(() => endpoint.SendAsync("127.0.0.1", 9000, new byte[] { 1, 2, 3, 4, 5 }), "SendAsync(byte[]) should reject payloads above MaxDatagramSize.").ConfigureAwait(false);
        }

        private static Task DisposeIsIdempotentAsync()
        {
            UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());
            endpoint.Dispose();
            endpoint.Dispose();
            return Task.CompletedTask;
        }

        private static async Task DisposeRaisesServerStoppedForTheReceiveLoopAsync()
        {
            UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());
            TaskCompletionSource<bool> stopped = UdpTestHelpers.CreateCompletionSource<bool>();

            endpoint.ServerStopped += (_, _) => stopped.TrySetResult(true);

            endpoint.Dispose();

            try
            {
                await UdpTestHelpers.WithTimeout(stopped.Task, "ServerStopped after dispose", TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            finally
            {
                endpoint.Dispose();
            }
        }
    }
}
