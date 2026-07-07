namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
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
                    TouchstoneDescriptorFactory.Case(SuiteId, "constructor-empty-ip-loopback", "Constructor accepts an empty IP and still receives loopback traffic", ConstructorAcceptsEmptyIpAndReceivesLoopbackTrafficAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "constructor-port-zero", "Constructor accepts port zero and can send from the assigned ephemeral port", ConstructorAcceptsPortZeroAndCanSendFromEphemeralPortAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "constructor-binding-conflict", "Constructor rejects a port already bound by another endpoint", ConstructorRejectsPortAlreadyBoundByAnotherEndpointAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "endpoints-start-empty", "Endpoints starts empty", EndpointsStartsEmptyAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "endpoints-snapshot-copy", "Endpoints returns a snapshot copy", EndpointsReturnsSnapshotCopyAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "endpoints-multiple-remotes", "Endpoints tracks multiple remote endpoints distinctly", EndpointsTracksMultipleRemoteEndpointsDistinctlyAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "endpoints-cache-limit", "Endpoints cache is bounded to 100 remote endpoints", EndpointsCacheIsBoundedToOneHundredRemoteEndpointsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "endpoints-cache-evicts-oldest", "Endpoints cache evicts the oldest endpoint when capacity is exceeded", EndpointsCacheEvictsTheOldestEndpointWhenCapacityIsExceededAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "endpoints-cache-refreshes-recency", "Endpoints cache refreshes recency when an endpoint sends again", EndpointsCacheRefreshesRecencyWhenAnEndpointSendsAgainAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "max-datagram-size-validation", "MaxDatagramSize validates range and stores a valid value", MaxDatagramSizeValidatesRangeAndStoresValueAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "max-datagram-size-boundaries", "MaxDatagramSize accepts documented boundary values", MaxDatagramSizeAcceptsDocumentedBoundaryValuesAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "receive-max-datagram-size-caps-data", "MaxDatagramSize caps delivered receive data", MaxDatagramSizeCapsDeliveredReceiveDataAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "enable-broadcast-toggle", "EnableBroadcast defaults to disabled and can be toggled", EnableBroadcastDefaultsToDisabledAndCanBeToggledAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "enable-broadcast-send", "EnableBroadcast allows end-to-end broadcast delivery", EnableBroadcastAllowsBroadcastSendsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "enable-broadcast-sendasync", "EnableBroadcast allows end-to-end async broadcast delivery", EnableBroadcastAllowsAsyncBroadcastSendsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-string-validation", "Send string overload validates arguments", SendStringOverloadValidatesArgumentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-bytes-validation", "Send byte overload validates arguments", SendByteOverloadValidatesArgumentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-string-validation", "SendAsync string overload validates arguments", SendAsyncStringOverloadValidatesArgumentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-bytes-validation", "SendAsync byte overload validates arguments", SendAsyncByteOverloadValidatesArgumentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-invalid-ip-format", "Send overloads reject invalid destination IP formats", SendOverloadsRejectInvalidDestinationIpFormatsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-ttl-zero", "Send overloads allow TTL zero", SendOverloadsAllowTtlZeroAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-string-source-port", "Send string reports the configured source port", SendStringReportsConfiguredSourcePortAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-string-utf8-payload", "Send string delivers UTF-8 payloads exactly", SendStringDeliversUtf8PayloadsExactlyAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-bytes-exact-payload", "Send byte overload delivers the exact payload", SendByteOverloadDeliversExactPayloadAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-bytes-boundary-payload", "Send byte overload delivers payloads exactly at MaxDatagramSize", SendByteOverloadDeliversPayloadsAtMaxDatagramSizeAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-bytes-large-boundary-payloads", "Send byte overload delivers large boundary payloads", SendByteOverloadDeliversLargeBoundaryPayloadsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-string-utf8-payload", "SendAsync string delivers UTF-8 payloads exactly", SendAsyncStringDeliversUtf8PayloadsExactlyAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-bytes-exact-payload", "SendAsync byte overload delivers the exact payload", SendAsyncByteOverloadDeliversExactPayloadAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-single-endpoint-detection", "SendAsync detects the same endpoint only once", SendAsyncDetectsTheSameEndpointOnlyOnceAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-reply-to-detected-port", "SendAsync supports replying to the detected port", SendAsyncSupportsReplyingToDetectedPortAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-before-peer-starts", "Sending before a peer starts does not stop the receive loop", SendingBeforePeerStartsDoesNotStopTheReceiveLoopAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "multi-process-send-before-peer-starts", "Multi-process startup order does not stop the receive loop", MultiProcessStartupOrderDoesNotStopTheReceiveLoopAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "event-handler-exceptions-isolated", "Event handler exceptions do not stop the receive loop", EventHandlerExceptionsDoNotStopTheReceiveLoopAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "event-order-first-datagram", "EndpointDetected is raised before DatagramReceived for a new endpoint", EndpointDetectedIsRaisedBeforeDatagramReceivedForNewEndpointAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "sendasync-concurrent", "SendAsync serializes concurrent sends without data loss", SendAsyncSerializesConcurrentSendsWithoutDataLossAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "max-datagram-size-oversize-guards", "MaxDatagramSize blocks oversize sends across all overloads", MaxDatagramSizeBlocksOversizeSendsAcrossAllOverloadsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "dispose-idempotent", "Dispose is idempotent", DisposeIsIdempotentAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "dispose-blocks-future-sends", "Dispose blocks future sends", DisposeBlocksFutureSendsAsync),
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

        private static async Task ConstructorAcceptsEmptyIpAndReceivesLoopbackTrafficAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();

            using UdpEndpoint receiver = new UdpEndpoint(String.Empty, receiverPort);
            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);

            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);

            sender.Send("127.0.0.1", receiverPort, "empty-ip");

            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Receiving loopback datagram on an empty-IP endpoint").ConfigureAwait(false);

            AssertEx.Equal("127.0.0.1", datagram.Ip, "Receiver should report the sender IP.");
            AssertEx.Equal(senderPort, datagram.Port, "Receiver should report the sender's configured local port.");
            AssertEx.Equal("empty-ip", Encoding.UTF8.GetString(datagram.Data), "Receiver should get the original payload.");
        }

        private static async Task ConstructorAcceptsPortZeroAndCanSendFromEphemeralPortAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", 0);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);

            sender.Send("127.0.0.1", receiverPort, "ephemeral");

            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Receiving datagram from ephemeral sender").ConfigureAwait(false);

            AssertEx.True(datagram.Port > 0, "A port-zero sender should be assigned an ephemeral source port.");
            AssertEx.Equal("ephemeral", Encoding.UTF8.GetString(datagram.Data), "Receiver should get the original payload.");
        }

        private static Task ConstructorRejectsPortAlreadyBoundByAnotherEndpointAsync()
        {
            int port = UdpTestHelpers.GetAvailableUdpPort();

            using UdpEndpoint first = new UdpEndpoint("127.0.0.1", port);

            AssertEx.Throws<SocketException>(() => new UdpEndpoint("127.0.0.1", port), "UdpEndpoint should reject a duplicate bind on the same IP and port.");
            return Task.CompletedTask;
        }

        private static Task EndpointsStartsEmptyAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());
            AssertEx.NotNull(endpoint.Endpoints, "Endpoints should never be null.");
            AssertEx.Equal(0, endpoint.Endpoints.Count, "Endpoints should start empty.");
            return Task.CompletedTask;
        }

        private static async Task EndpointsReturnsSnapshotCopyAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);
            sender.Send("127.0.0.1", receiverPort, "snapshot");

            await UdpTestHelpers.WithTimeout(received.Task, "Receiving datagram before endpoint snapshot check").ConfigureAwait(false);

            List<string> firstSnapshot = receiver.Endpoints;
            firstSnapshot.Clear();

            AssertEx.Equal(1, receiver.Endpoints.Count, "Mutating the returned endpoint list should not mutate the internal cache.");
            AssertEx.Contains("127.0.0.1:" + senderPort, receiver.Endpoints, "The internal endpoint cache should retain the sender endpoint.");
        }

        private static async Task EndpointsTracksMultipleRemoteEndpointsDistinctlyAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();
            int datagramCount = 0;

            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            receiver.DatagramReceived += (_, _) => Interlocked.Increment(ref datagramCount);

            int firstPort = UdpTestHelpers.SendRawLoopback(receiverPort, new byte[] { 1 });
            int secondPort = UdpTestHelpers.SendRawLoopback(receiverPort, new byte[] { 2 });
            int thirdPort = UdpTestHelpers.SendRawLoopback(receiverPort, new byte[] { 3 });

            await UdpTestHelpers.WaitUntil(() => Volatile.Read(ref datagramCount) == 3, "Receiving datagrams from three raw endpoints").ConfigureAwait(false);

            List<string> endpoints = receiver.Endpoints;
            AssertEx.Contains("127.0.0.1:" + firstPort, endpoints, "Endpoints should include the first raw sender.");
            AssertEx.Contains("127.0.0.1:" + secondPort, endpoints, "Endpoints should include the second raw sender.");
            AssertEx.Contains("127.0.0.1:" + thirdPort, endpoints, "Endpoints should include the third raw sender.");
        }

        private static async Task EndpointsCacheIsBoundedToOneHundredRemoteEndpointsAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();
            int datagramCount = 0;

            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            receiver.DatagramReceived += (_, _) => Interlocked.Increment(ref datagramCount);

            for (int i = 0; i < 105; i++)
            {
                UdpTestHelpers.SendRawLoopback(receiverPort, new byte[] { (byte)i });
            }

            await UdpTestHelpers.WaitUntil(() => Volatile.Read(ref datagramCount) == 105, "Receiving datagrams from more than 100 remote endpoints", TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            AssertEx.Equal(100, receiver.Endpoints.Count, "Endpoints should retain only the 100 most recent remote endpoints.");
        }

        private static async Task EndpointsCacheEvictsTheOldestEndpointWhenCapacityIsExceededAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();
            int datagramCount = 0;
            List<Socket> senders = new List<Socket>();
            List<int> senderPorts = new List<int>();

            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            receiver.DatagramReceived += (_, _) => Interlocked.Increment(ref datagramCount);

            try
            {
                for (int i = 0; i < 101; i++)
                {
                    Socket sender = UdpTestHelpers.CreateBoundLoopbackDatagramSocket(out int senderPort);
                    senders.Add(sender);
                    senderPorts.Add(senderPort);
                    sender.SendTo(new byte[] { (byte)i }, new IPEndPoint(IPAddress.Loopback, receiverPort));
                }

                await UdpTestHelpers.WaitUntil(() => Volatile.Read(ref datagramCount) == 101, "Receiving datagrams from 101 unique endpoints", TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                List<string> endpoints = receiver.Endpoints;
                AssertEx.Equal(100, endpoints.Count, "The endpoint cache should remain capped at 100 entries.");
                AssertEx.DoesNotContain("127.0.0.1:" + senderPorts[0], endpoints, "The oldest endpoint should be evicted after capacity is exceeded.");
                AssertEx.Contains("127.0.0.1:" + senderPorts[100], endpoints, "The newest endpoint should remain in the cache.");
            }
            finally
            {
                foreach (Socket sender in senders)
                {
                    sender.Dispose();
                }
            }
        }

        private static async Task EndpointsCacheRefreshesRecencyWhenAnEndpointSendsAgainAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();
            int datagramCount = 0;
            List<Socket> senders = new List<Socket>();
            List<int> senderPorts = new List<int>();

            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            receiver.DatagramReceived += (_, _) => Interlocked.Increment(ref datagramCount);

            try
            {
                for (int i = 0; i < 100; i++)
                {
                    Socket sender = UdpTestHelpers.CreateBoundLoopbackDatagramSocket(out int senderPort);
                    senders.Add(sender);
                    senderPorts.Add(senderPort);
                    sender.SendTo(new byte[] { (byte)i }, new IPEndPoint(IPAddress.Loopback, receiverPort));
                }

                await UdpTestHelpers.WaitUntil(() => Volatile.Read(ref datagramCount) == 100, "Receiving datagrams from 100 unique endpoints", TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                senders[0].SendTo(new byte[] { 200 }, new IPEndPoint(IPAddress.Loopback, receiverPort));

                Socket newestSender = UdpTestHelpers.CreateBoundLoopbackDatagramSocket(out int newestSenderPort);
                senders.Add(newestSender);
                newestSender.SendTo(new byte[] { 201 }, new IPEndPoint(IPAddress.Loopback, receiverPort));

                await UdpTestHelpers.WaitUntil(() => Volatile.Read(ref datagramCount) == 102, "Receiving recency refresh and eviction datagrams", TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                List<string> endpoints = receiver.Endpoints;
                AssertEx.Equal(100, endpoints.Count, "The endpoint cache should remain capped at 100 entries.");
                AssertEx.Contains("127.0.0.1:" + senderPorts[0], endpoints, "A repeat datagram should refresh endpoint recency.");
                AssertEx.DoesNotContain("127.0.0.1:" + senderPorts[1], endpoints, "The least-recent endpoint should be evicted.");
                AssertEx.Contains("127.0.0.1:" + newestSenderPort, endpoints, "The newest endpoint should be cached.");
            }
            finally
            {
                foreach (Socket sender in senders)
                {
                    sender.Dispose();
                }
            }
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

        private static Task MaxDatagramSizeAcceptsDocumentedBoundaryValuesAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());

            endpoint.MaxDatagramSize = 1;
            AssertEx.Equal(1, endpoint.MaxDatagramSize, "MaxDatagramSize should accept one byte.");

            endpoint.MaxDatagramSize = 65507;
            AssertEx.Equal(65507, endpoint.MaxDatagramSize, "MaxDatagramSize should accept the documented UDP payload maximum.");
            return Task.CompletedTask;
        }

        private static async Task MaxDatagramSizeCapsDeliveredReceiveDataAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();
            byte[] payload = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort) { MaxDatagramSize = 4 };

            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);
            sender.Send("127.0.0.1", receiverPort, payload);

            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Receiving payload capped by MaxDatagramSize").ConfigureAwait(false);

            AssertEx.SequenceEqual(new byte[] { 1, 2, 3, 4 }, datagram.Data, "Receive delivery should be capped to the configured MaxDatagramSize.");
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

        private static async Task EnableBroadcastAllowsBroadcastSendsAsync()
        {
            byte[] payload = new byte[] { 1, 2, 3 };
            (Datagram datagram, EndpointMetadata metadata, int senderPort) = await SendAndReceiveBroadcastAsync(
                (sender, receiverPort) =>
                {
                    sender.Send("255.255.255.255", receiverPort, payload);
                    return Task.CompletedTask;
                },
                payload).ConfigureAwait(false);

            AssertEx.Equal(senderPort, datagram.Port, "Broadcast receive should preserve the sender's configured local port.");
            AssertEx.Equal(senderPort, metadata.Port, "Broadcast endpoint detection should preserve the sender's configured local port.");
            AssertEx.NotNull(datagram.Ip, "Broadcast receive should report the sender IP.");
            AssertEx.NotNull(metadata.Ip, "Broadcast endpoint detection should report the sender IP.");
            AssertEx.Equal(datagram.Ip, metadata.Ip, "Broadcast endpoint detection should match the received sender IP.");
            AssertEx.SequenceEqual(payload, datagram.Data, "Broadcast receive should preserve the exact payload.");
        }

        private static async Task EnableBroadcastAllowsAsyncBroadcastSendsAsync()
        {
            byte[] payload = new byte[] { 4, 5, 6, 7 };
            (Datagram datagram, EndpointMetadata metadata, int senderPort) = await SendAndReceiveBroadcastAsync(
                (sender, receiverPort) => sender.SendAsync("255.255.255.255", receiverPort, payload),
                payload).ConfigureAwait(false);

            AssertEx.Equal(senderPort, datagram.Port, "Async broadcast receive should preserve the sender's configured local port.");
            AssertEx.Equal(senderPort, metadata.Port, "Async broadcast endpoint detection should preserve the sender's configured local port.");
            AssertEx.NotNull(datagram.Ip, "Async broadcast receive should report the sender IP.");
            AssertEx.NotNull(metadata.Ip, "Async broadcast endpoint detection should report the sender IP.");
            AssertEx.Equal(datagram.Ip, metadata.Ip, "Async broadcast endpoint detection should match the received sender IP.");
            AssertEx.SequenceEqual(payload, datagram.Data, "Async broadcast receive should preserve the exact payload.");
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

        private static async Task SendOverloadsRejectInvalidDestinationIpFormatsAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());

            AssertEx.Throws<FormatException>(() => endpoint.Send("not-an-ip-address", 9000, "hello"), "Send(string) should reject invalid destination IP values.");
            AssertEx.Throws<FormatException>(() => endpoint.Send("not-an-ip-address", 9000, new byte[] { 1 }), "Send(byte[]) should reject invalid destination IP values.");
            await AssertEx.ThrowsAsync<FormatException>(() => endpoint.SendAsync("not-an-ip-address", 9000, "hello"), "SendAsync(string) should reject invalid destination IP values.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<FormatException>(() => endpoint.SendAsync("not-an-ip-address", 9000, new byte[] { 1 }), "SendAsync(byte[]) should reject invalid destination IP values.").ConfigureAwait(false);
        }

        private static async Task SendOverloadsAllowTtlZeroAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<bool> receivedBoth = UdpTestHelpers.CreateCompletionSource<bool>();
            List<string> payloads = new List<string>();

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.DatagramReceived += (_, dg) =>
            {
                lock (payloads)
                {
                    payloads.Add(Encoding.UTF8.GetString(dg.Data));
                    if (payloads.Count == 2) receivedBoth.TrySetResult(true);
                }
            };

            sender.Send("127.0.0.1", receiverPort, "ttl-zero-text", 0);
            await sender.SendAsync("127.0.0.1", receiverPort, Encoding.UTF8.GetBytes("ttl-zero-bytes"), 0).ConfigureAwait(false);

            await UdpTestHelpers.WithTimeout(receivedBoth.Task, "Receiving TTL-zero datagrams").ConfigureAwait(false);
            AssertEx.SetEqual(new[] { "ttl-zero-text", "ttl-zero-bytes" }, payloads, "TTL-zero sends should deliver on loopback.");
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

        private static async Task SendStringDeliversUtf8PayloadsExactlyAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();
            string payload = "plain ascii, accents cafe, symbols !@#$%^&*(), and emoji \uD83D\uDE80";

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);
            sender.Send("127.0.0.1", receiverPort, payload);

            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Datagram delivery after Send(string) UTF-8 payload").ConfigureAwait(false);
            AssertEx.Equal(payload, Encoding.UTF8.GetString(datagram.Data), "Send(string) should UTF-8 encode and deliver the original text.");
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

        private static async Task SendByteOverloadDeliversPayloadsAtMaxDatagramSizeAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();
            byte[] payload = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort) { MaxDatagramSize = payload.Length };
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);
            sender.Send("127.0.0.1", receiverPort, payload);

            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Datagram delivery at MaxDatagramSize").ConfigureAwait(false);
            AssertEx.SequenceEqual(payload, datagram.Data, "Sends exactly at MaxDatagramSize should be allowed and preserve the payload.");
        }

        private static async Task SendByteOverloadDeliversLargeBoundaryPayloadsAsync()
        {
            int[] sizes = new[] { 1, 512, 8192, 32768, 65507 };

            foreach (int size in sizes)
            {
                (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
                TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();
                byte[] payload = CreatePayload(size);

                using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
                using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

                receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);
                sender.Send("127.0.0.1", receiverPort, payload);

                Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Datagram delivery for " + size + " byte payload", TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                AssertEx.Equal(senderPort, datagram.Port, "Large payload sends should preserve the sender port for size " + size + ".");
                AssertEx.SequenceEqual(payload, datagram.Data, "Large payload sends should preserve exactly " + size + " bytes.");
            }
        }

        private static async Task SendAsyncStringDeliversUtf8PayloadsExactlyAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();
            string payload = "async text with unicode \u2603 and multi-byte \uD83D\uDE80";

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);
            await sender.SendAsync("127.0.0.1", receiverPort, payload).ConfigureAwait(false);

            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Datagram delivery after SendAsync(string) UTF-8 payload").ConfigureAwait(false);
            AssertEx.Equal(payload, Encoding.UTF8.GetString(datagram.Data), "SendAsync(string) should UTF-8 encode and deliver the original text.");
        }

        private static async Task SendAsyncByteOverloadDeliversExactPayloadAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();
            byte[] payload = new byte[] { 9, 8, 7, 0, 6, 5, 4 };

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);
            await sender.SendAsync("127.0.0.1", receiverPort, payload).ConfigureAwait(false);

            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Datagram delivery after SendAsync(byte[])").ConfigureAwait(false);
            AssertEx.Equal(senderPort, datagram.Port, "Async byte sends should preserve the sender's configured local port.");
            AssertEx.SequenceEqual(payload, datagram.Data, "Async byte sends should preserve the payload exactly.");
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

        private static async Task SendingBeforePeerStartsDoesNotStopTheReceiveLoopAsync()
        {
            (int peerPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();
            int serverStoppedCount = 0;

            using UdpEndpoint receiverStartedFirst = new UdpEndpoint("127.0.0.1", receiverPort);

            receiverStartedFirst.DatagramReceived += (_, dg) => received.TrySetResult(dg);
            receiverStartedFirst.ServerStopped += (_, _) => Interlocked.Increment(ref serverStoppedCount);

            await receiverStartedFirst.SendAsync("127.0.0.1", peerPort, "sent-before-peer-started").ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

            using UdpEndpoint peerStartedSecond = new UdpEndpoint("127.0.0.1", peerPort);

            await peerStartedSecond.SendAsync("127.0.0.1", receiverPort, "peer-started-later").ConfigureAwait(false);

            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Receiving datagram after sending to a peer that was not started yet").ConfigureAwait(false);

            AssertEx.Equal(peerPort, datagram.Port, "The later-started peer should use its configured source port.");
            AssertEx.Equal("peer-started-later", Encoding.UTF8.GetString(datagram.Data), "The started-first receiver should keep receiving after an earlier send to a closed UDP port.");
            AssertEx.Equal(0, Volatile.Read(ref serverStoppedCount), "The receive loop should not stop after an ICMP UDP reset from a closed peer port.");
        }

        private static async Task MultiProcessStartupOrderDoesNotStopTheReceiveLoopAsync()
        {
            string helperPath = UdpTestHelpers.GetProcessHelperPath();
            AssertEx.True(File.Exists(helperPath), "The multi-process helper should be built at " + helperPath + ".");

            (int peerPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            string expectedPayload = "multi-process-startup-order-" + Guid.NewGuid().ToString("N");
            List<string> receiverOutput = new List<string>();
            List<string> receiverError = new List<string>();
            TaskCompletionSource<bool> receiverReady = UdpTestHelpers.CreateCompletionSource<bool>();

            using Process receiverProcess = CreateProcess(
                "dotnet",
                "\"" + helperPath + "\" send-before-peer-and-receive " + receiverPort + " " + peerPort + " " + expectedPayload + " 10000",
                UdpTestHelpers.FindRepositoryRoot());

            receiverProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                lock (receiverOutput) receiverOutput.Add(e.Data);
                if (e.Data.Contains("READY", StringComparison.OrdinalIgnoreCase)) receiverReady.TrySetResult(true);
            };
            receiverProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                lock (receiverError) receiverError.Add(e.Data);
            };

            AssertEx.True(receiverProcess.Start(), "The receiver helper process should start.");
            receiverProcess.BeginOutputReadLine();
            receiverProcess.BeginErrorReadLine();

            try
            {
                await UdpTestHelpers.WithTimeout(receiverReady.Task, "Waiting for receiver helper readiness").ConfigureAwait(false);

                await UdpTestHelpers.RunProcessAsync(
                    "dotnet",
                    "\"" + helperPath + "\" send " + peerPort + " " + receiverPort + " " + expectedPayload,
                    UdpTestHelpers.FindRepositoryRoot(),
                    TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                Task waitForReceiverExit = receiverProcess.WaitForExitAsync();
                await UdpTestHelpers.WithTimeout(waitForReceiverExit, "Waiting for receiver helper completion", TimeSpan.FromSeconds(12)).ConfigureAwait(false);

                AssertEx.Equal(0, receiverProcess.ExitCode, "The receiver helper should exit successfully. Stdout: " + String.Join(" | ", receiverOutput) + ". Stderr: " + String.Join(" | ", receiverError));
                AssertEx.True(receiverOutput.Any(line => line.StartsWith("RECEIVED ", StringComparison.OrdinalIgnoreCase)), "The receiver helper should report the received datagram.");
            }
            finally
            {
                if (!receiverProcess.HasExited)
                {
                    receiverProcess.Kill(entireProcessTree: true);
                }
            }
        }

        private static async Task EventHandlerExceptionsDoNotStopTheReceiveLoopAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<EndpointMetadata> endpointDetected = UdpTestHelpers.CreateCompletionSource<EndpointMetadata>();
            TaskCompletionSource<bool> receivedBoth = UdpTestHelpers.CreateCompletionSource<bool>();
            List<string> payloads = new List<string>();
            int serverStoppedCount = 0;

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.ServerStopped += (_, _) => Interlocked.Increment(ref serverStoppedCount);
            receiver.EndpointDetected += (_, _) => throw new InvalidOperationException("Intentional endpoint handler failure.");
            receiver.EndpointDetected += (_, metadata) => endpointDetected.TrySetResult(metadata);
            receiver.DatagramReceived += (_, _) => throw new InvalidOperationException("Intentional datagram handler failure.");
            receiver.DatagramReceived += (_, datagram) =>
            {
                lock (payloads)
                {
                    payloads.Add(Encoding.UTF8.GetString(datagram.Data));
                    if (payloads.Count == 2) receivedBoth.TrySetResult(true);
                }
            };

            sender.Send("127.0.0.1", receiverPort, "first-after-handler-failure");
            sender.Send("127.0.0.1", receiverPort, "second-after-handler-failure");

            EndpointMetadata metadata = await UdpTestHelpers.WithTimeout(endpointDetected.Task, "EndpointDetected after a failing event subscriber").ConfigureAwait(false);
            await UdpTestHelpers.WithTimeout(receivedBoth.Task, "DatagramReceived after failing event subscribers").ConfigureAwait(false);

            AssertEx.Equal(senderPort, metadata.Port, "A later EndpointDetected subscriber should still receive metadata after an earlier subscriber throws.");
            AssertEx.SetEqual(new[] { "first-after-handler-failure", "second-after-handler-failure" }, payloads, "Later DatagramReceived subscribers should still receive payloads after an earlier subscriber throws.");
            AssertEx.Equal(0, Volatile.Read(ref serverStoppedCount), "Subscriber exceptions should not stop the receive loop.");
        }

        private static async Task EndpointDetectedIsRaisedBeforeDatagramReceivedForNewEndpointAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<bool> received = UdpTestHelpers.CreateCompletionSource<bool>();
            List<string> events = new List<string>();

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.EndpointDetected += (_, _) =>
            {
                lock (events) events.Add("detected");
            };
            receiver.DatagramReceived += (_, _) =>
            {
                lock (events) events.Add("datagram");
                received.TrySetResult(true);
            };

            sender.Send("127.0.0.1", receiverPort, "order");
            await UdpTestHelpers.WithTimeout(received.Task, "Receiving first datagram for event-order check").ConfigureAwait(false);

            AssertEx.Equal("detected", events[0], "EndpointDetected should be raised before DatagramReceived for a new endpoint.");
            AssertEx.Equal("datagram", events[1], "DatagramReceived should follow endpoint detection.");
        }

        private static async Task SendAsyncSerializesConcurrentSendsWithoutDataLossAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            const int MessageCount = 20;
            TaskCompletionSource<bool> receivedAll = UdpTestHelpers.CreateCompletionSource<bool>();
            List<string> payloads = new List<string>();

            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            receiver.DatagramReceived += (_, dg) =>
            {
                lock (payloads)
                {
                    payloads.Add(Encoding.UTF8.GetString(dg.Data));
                    if (payloads.Count == MessageCount) receivedAll.TrySetResult(true);
                }
            };

            List<Task> sends = new List<Task>();
            List<string> expected = new List<string>();
            for (int i = 0; i < MessageCount; i++)
            {
                string payload = "message-" + i;
                expected.Add(payload);
                sends.Add(sender.SendAsync("127.0.0.1", receiverPort, payload));
            }

            await Task.WhenAll(sends).ConfigureAwait(false);
            await UdpTestHelpers.WithTimeout(receivedAll.Task, "Receiving all concurrent async sends", TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            AssertEx.SetEqual(expected, payloads, "Concurrent async sends should deliver each payload exactly once.");
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

        private static async Task DisposeBlocksFutureSendsAsync()
        {
            UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", UdpTestHelpers.GetAvailableUdpPort());
            endpoint.Dispose();

            AssertEx.Throws<ObjectDisposedException>(() => endpoint.Send("127.0.0.1", 9000, "after-dispose"), "Send(string) should fail after disposal.");
            AssertEx.Throws<ObjectDisposedException>(() => endpoint.Send("127.0.0.1", 9000, new byte[] { 1 }), "Send(byte[]) should fail after disposal.");
            await AssertEx.ThrowsAsync<ObjectDisposedException>(() => endpoint.SendAsync("127.0.0.1", 9000, "after-dispose"), "SendAsync(string) should fail after disposal.").ConfigureAwait(false);
            await AssertEx.ThrowsAsync<ObjectDisposedException>(() => endpoint.SendAsync("127.0.0.1", 9000, new byte[] { 1 }), "SendAsync(byte[]) should fail after disposal.").ConfigureAwait(false);
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

        private static byte[] CreatePayload(int length)
        {
            byte[] payload = new byte[length];

            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte)(i % 251);
            }

            return payload;
        }

        private static Process CreateProcess(string fileName, string arguments, string workingDirectory)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
        }

        private static async Task<(Datagram Datagram, EndpointMetadata Metadata, int SenderPort)> SendAndReceiveBroadcastAsync(Func<UdpEndpoint, int, Task> send, byte[] expectedPayload)
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<EndpointMetadata> detected = UdpTestHelpers.CreateCompletionSource<EndpointMetadata>();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();

            using UdpEndpoint sender = new UdpEndpoint(null!, senderPort);
            using UdpEndpoint receiver = new UdpEndpoint(null!, receiverPort);

            receiver.EndpointDetected += (_, md) => detected.TrySetResult(md);
            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);

            AssertEx.Throws<SocketException>(() => sender.Send("255.255.255.255", receiverPort, expectedPayload), "Broadcast sends should fail while EnableBroadcast is disabled.");

            sender.EnableBroadcast = true;
            await send(sender, receiverPort).ConfigureAwait(false);

            EndpointMetadata metadata = await UdpTestHelpers.WithTimeout(detected.Task, "Broadcast endpoint detection").ConfigureAwait(false);
            Datagram datagram = await UdpTestHelpers.WithTimeout(received.Task, "Broadcast datagram delivery").ConfigureAwait(false);
            AssertEx.Contains(metadata.Ip + ":" + senderPort, receiver.Endpoints, "Broadcast receive should add the sender endpoint to the endpoint cache.");
            return (datagram, metadata, senderPort);
        }
    }
}
