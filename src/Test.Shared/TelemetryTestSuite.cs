namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using SimpleUdp;
    using Touchstone.Core;
    using N = SimpleUdp.SimpleUdpTelemetryNames;

    public static class TelemetryTestSuite
    {
        public const string SuiteId = "telemetry";

        public const string SuiteName = "Telemetry";

        private static readonly ActivitySource _TestSource = new ActivitySource("Test.Shared");

        public static TestSuiteDescriptor Create()
        {
            return TouchstoneDescriptorFactory.Suite(
                SuiteId,
                SuiteName,
                new List<TestCaseDescriptor>
                {
                    TouchstoneDescriptorFactory.Case(SuiteId, "names-contract", "Meter, source, and every documented instrument are published under stable names", NamesContractAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "no-listener", "Send, receive, handler failure, and dispose do not throw with no listener attached", NoListenerPathDoesNotThrowAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-sync-success", "Synchronous send emits outcome, duration, bytes, stage metrics, and a client span with stage children", SendSyncSuccessAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-async-success", "Asynchronous send emits outcome, duration, stage metrics, and a client span with stage children", SendAsyncSuccessAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-validation-failure", "Send validation failures are counted by error.type and mark the span as error", SendValidationFailureAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-invalid-ip-failure", "Invalid destination IP is counted as a FormatException failure", SendInvalidIpFailureAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-after-dispose-failure", "Send after dispose records a failed transmit stage", SendAfterDisposeFailureAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "send-queued-concurrency", "Concurrent sends record queued and transmit stages and return slot gauges to zero", SendQueuedConcurrencyAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "receive-success", "Receive emits counters, size, duration, a root consumer span, and handler stage spans", ReceiveSuccessAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "receive-handler-context", "Event handlers run inside the receive trace so their spans nest under it", ReceiveHandlerContextAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "handler-exception", "Swallowed handler exceptions are counted by error.type and mark the stage span as error", HandlerExceptionAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "receive-truncated", "Datagrams truncated to MaxDatagramSize are counted", ReceiveTruncatedAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "remote-endpoint-cache", "Remote endpoint detection, cache size, and capacity are tracked and released on dispose", RemoteEndpointCacheAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "remote-endpoint-eviction", "Remote endpoint cache evictions are counted", RemoteEndpointEvictionAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "endpoint-lifecycle", "Endpoint start and dispose update starts, active endpoints, and the start span", EndpointLifecycleAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "endpoint-bind-failure", "Bind conflicts are counted as failed starts with a SocketException error.type", EndpointBindFailureAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "receive-loop-stop", "Disposing stops the receive loop and records a disposed stop", ReceiveLoopStopAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "build-info", "Build info gauge reports 1 with the library version", BuildInfoAsync)
                });
        }

        private static Task NamesContractAsync()
        {
            AssertEx.Equal("SimpleUdp", N.MeterName, "Meter name is a public contract.");
            AssertEx.Equal("SimpleUdp", N.ActivitySourceName, "Activity source name is a public contract.");

            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", 0);
            using TelemetryCapture capture = new TelemetryCapture();

            string[] expected = typeof(SimpleUdpTelemetryNames)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && ((string)f.GetValue(null)!).StartsWith("simpleudp.", StringComparison.Ordinal) && !f.Name.StartsWith("Attr", StringComparison.Ordinal))
                .Select(f => (string)f.GetValue(null)!)
                .ToArray();

            AssertEx.True(expected.Length >= 20, "Expected the instrument catalog to be discoverable from SimpleUdpTelemetryNames.");
            foreach (string name in expected)
            {
                AssertEx.Contains(name, capture.Instruments, "Instrument " + name + " should be published on the SimpleUdp meter.");
            }

            return Task.CompletedTask;
        }

        private static async Task NoListenerPathDoesNotThrowAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();

            UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);

            receiver.EndpointDetected += (_, _) => throw new InvalidOperationException("handler failure");
            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);

            sender.Send("127.0.0.1", receiverPort, "a");
            await sender.SendAsync("127.0.0.1", receiverPort, Encoding.UTF8.GetBytes("b")).ConfigureAwait(false);
            await UdpTestHelpers.WithTimeout(received.Task, "Receiving without a listener").ConfigureAwait(false);

            AssertEx.Throws<FormatException>(() => sender.Send("not-an-ip", receiverPort, "x"), "Failures still surface to the caller without a listener.");

            receiver.Dispose();
            receiver.Dispose();
        }

        private static async Task SendSyncSuccessAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using TelemetryCapture capture = new TelemetryCapture();

            sender.Send("127.0.0.1", receiverPort, "hello");

            AssertEx.Equal(1, capture.Count(N.SendDatagrams, (N.AttrSendMode, N.SendModeSync), (N.AttrOutcome, N.OutcomeSuccess)), "One successful sync send should be counted.");
            AssertEx.Equal(1, capture.Count(N.SendDuration, (N.AttrSendMode, N.SendModeSync), (N.AttrOutcome, N.OutcomeSuccess)), "Send duration should be recorded.");
            AssertEx.True(capture.Measurements(N.SendDuration).All(m => m.Value >= 0), "Send duration should be non-negative seconds.");
            AssertEx.Equal(5d, capture.Sum(N.SentBytes), "Sent bytes should equal the payload size.");
            AssertEx.Equal(1, capture.Count(N.DatagramSize, (N.AttrNetworkIoDirection, N.DirectionTransmit)), "Transmit datagram size should be recorded.");
            AssertEx.Equal(1, capture.Count(N.SendStageEvents, (N.AttrStage, N.StageQueued), (N.AttrOutcome, N.OutcomeSuccess)), "Queued stage should be counted.");
            AssertEx.Equal(1, capture.Count(N.SendStageEvents, (N.AttrStage, N.StageTransmit), (N.AttrOutcome, N.OutcomeSuccess)), "Transmit stage should be counted.");
            AssertEx.Equal(1, capture.Count(N.SendStageDuration, (N.AttrStage, N.StageTransmit)), "Transmit stage duration should be recorded.");
            AssertEx.Equal(0d, capture.Sum(N.SendQueued), "Queued gauge should net to zero.");
            AssertEx.Equal(0d, capture.Sum(N.SendActive), "Active gauge should net to zero.");

            Activity span = capture.Activities(N.SpanSend, N.AttrNetworkPeerPort, receiverPort).Single();
            AssertEx.Equal(ActivityKind.Client, span.Kind, "Send span should be a client span.");
            AssertEx.Equal(ActivityStatusCode.Ok, span.Status, "Send span status should be Ok.");
            AssertEx.Equal("127.0.0.1", span.GetTagItem(N.AttrNetworkPeerAddress), "Send span should carry the peer address.");
            AssertEx.Equal(senderPort, span.GetTagItem(N.AttrNetworkLocalPort), "Send span should carry the local port.");
            AssertEx.Equal(5, span.GetTagItem(N.AttrDatagramSize), "Send span should carry the datagram size.");
            AssertEx.Equal("udp", span.GetTagItem(N.AttrNetworkTransport), "Send span should carry the transport.");

            List<string> children = capture.Children(span).Select(a => a.OperationName).ToList();
            AssertEx.Contains("stage:queued", children, "Send span should have a queued stage child.");
            AssertEx.Contains("stage:transmit", children, "Send span should have a transmit stage child.");
        }

        private static async Task SendAsyncSuccessAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using TelemetryCapture capture = new TelemetryCapture();

            await sender.SendAsync("127.0.0.1", receiverPort, new byte[] { 1, 2, 3 }).ConfigureAwait(false);

            AssertEx.Equal(1, capture.Count(N.SendDatagrams, (N.AttrSendMode, N.SendModeAsync), (N.AttrOutcome, N.OutcomeSuccess)), "One successful async send should be counted.");
            AssertEx.Equal(3d, capture.Sum(N.SentBytes), "Sent bytes should equal the payload size.");
            AssertEx.Equal(1, capture.Count(N.SendStageEvents, (N.AttrStage, N.StageTransmit), (N.AttrOutcome, N.OutcomeSuccess)), "Async transmit stage should be counted.");

            Activity span = capture.Activities(N.SpanSend, N.AttrNetworkPeerPort, receiverPort).Single();
            AssertEx.Equal(N.SendModeAsync, span.GetTagItem(N.AttrSendMode), "Async send span should carry the mode.");
            AssertEx.Equal(ActivityStatusCode.Ok, span.Status, "Async send span status should be Ok.");
            AssertEx.Equal(2, capture.Children(span).Count, "Async send span should have queued and transmit children.");
        }

        private static async Task SendValidationFailureAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();
            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", 0);
            sender.MaxDatagramSize = 4;
            using TelemetryCapture capture = new TelemetryCapture();

            AssertEx.Throws<ArgumentException>(() => sender.Send("127.0.0.1", receiverPort, new byte[10]), "Oversize sync send should throw.");
            await AssertEx.ThrowsAsync<ArgumentNullException>(() => sender.SendAsync(null!, receiverPort, "x"), "Null IP async send should throw.").ConfigureAwait(false);

            AssertEx.Equal(1, capture.Count(N.SendDatagrams, (N.AttrOutcome, N.OutcomeFailure), (N.AttrErrorType, "System.ArgumentException"), (N.AttrSendMode, N.SendModeSync)), "Oversize failure should be counted by error.type.");
            AssertEx.Equal(1, capture.Count(N.SendDatagrams, (N.AttrOutcome, N.OutcomeFailure), (N.AttrErrorType, "System.ArgumentNullException"), (N.AttrSendMode, N.SendModeAsync)), "Null IP failure should be counted by error.type.");
            AssertEx.Equal(0d, capture.Sum(N.SentBytes), "Failed sends should not count sent bytes.");

            Activity span = capture.Activities(N.SpanSend, N.AttrNetworkPeerPort, receiverPort).First(a => Equals(a.GetTagItem(N.AttrSendMode), N.SendModeSync));
            AssertEx.Equal(ActivityStatusCode.Error, span.Status, "Failed send span should have Error status.");
            AssertEx.Equal("System.ArgumentException", span.GetTagItem(N.AttrErrorType), "Failed send span should carry error.type.");
            AssertEx.True(span.Events.Any(e => e.Name == "exception"), "Failed send span should record an exception event.");
        }

        private static Task SendInvalidIpFailureAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", 0);
            using TelemetryCapture capture = new TelemetryCapture();

            AssertEx.Throws<FormatException>(() => sender.Send("999.1.1.1", receiverPort, "x"), "Invalid IP should throw.");
            sender.Send("127.0.0.1", receiverPort, "ok");

            AssertEx.Equal(1, capture.Count(N.SendDatagrams, (N.AttrOutcome, N.OutcomeFailure), (N.AttrErrorType, "System.FormatException")), "Invalid IP should be counted as a FormatException failure.");
            AssertEx.Equal(1, capture.Count(N.SendDatagrams, (N.AttrOutcome, N.OutcomeSuccess)), "The next send should still succeed.");
            AssertEx.Equal(0d, capture.Sum(N.SendQueued), "Queued gauge should net to zero after a failure.");
            AssertEx.Equal(0d, capture.Sum(N.SendActive), "Active gauge should net to zero after a failure.");
            return Task.CompletedTask;
        }

        private static async Task SendAfterDisposeFailureAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();
            UdpEndpoint sender = new UdpEndpoint("127.0.0.1", 0);
            sender.Dispose();
            using TelemetryCapture capture = new TelemetryCapture();

            AssertEx.Throws<ObjectDisposedException>(() => sender.Send("127.0.0.1", receiverPort, "x"), "Send after dispose should throw.");
            await AssertEx.ThrowsAsync<ObjectDisposedException>(() => sender.SendAsync("127.0.0.1", receiverPort, "x"), "SendAsync after dispose should throw.").ConfigureAwait(false);

            AssertEx.Equal(2, capture.Count(N.SendDatagrams, (N.AttrOutcome, N.OutcomeFailure), (N.AttrErrorType, "System.ObjectDisposedException")), "Both disposed sends should be counted as failures.");
            AssertEx.Equal(2, capture.Count(N.SendStageEvents, (N.AttrStage, N.StageTransmit), (N.AttrOutcome, N.OutcomeFailure)), "Both transmit stages should be counted as failures.");

            Activity transmit = capture.Activities("stage:transmit").First();
            AssertEx.Equal(ActivityStatusCode.Error, transmit.Status, "Failed transmit stage span should have Error status.");
        }

        private static async Task SendQueuedConcurrencyAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
            using TelemetryCapture capture = new TelemetryCapture();

            const int count = 25;
            await Task.WhenAll(Enumerable.Range(0, count).Select(i => sender.SendAsync("127.0.0.1", receiverPort, "m" + i))).ConfigureAwait(false);

            AssertEx.Equal(count, capture.Count(N.SendStageEvents, (N.AttrStage, N.StageQueued), (N.AttrOutcome, N.OutcomeSuccess)), "Every send should pass through the queued stage.");
            AssertEx.Equal(count, capture.Count(N.SendStageEvents, (N.AttrStage, N.StageTransmit), (N.AttrOutcome, N.OutcomeSuccess)), "Every send should pass through the transmit stage.");
            AssertEx.Equal(0d, capture.Sum(N.SendQueued), "Queued gauge should net to zero.");
            AssertEx.Equal(0d, capture.Sum(N.SendActive), "Active gauge should net to zero.");
            AssertEx.True(capture.Measurements(N.SendActive).Where(m => m.Value > 0).Count() == count, "Each send should hold the slot exactly once.");
        }

        private static async Task ReceiveSuccessAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();

            using TelemetryCapture capture = new TelemetryCapture();
            UdpEndpoint receiver;
            using (Activity? ambient = _TestSource.StartActivity("ambient-construct"))
            {
                receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            }

            using (receiver)
            {
                using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);
                receiver.EndpointDetected += (_, _) => { };
                receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);

                sender.Send("127.0.0.1", receiverPort, "payload");
                await UdpTestHelpers.WithTimeout(received.Task, "Receiving datagram").ConfigureAwait(false);
                await UdpTestHelpers.WaitUntil(() => capture.Activities(N.SpanReceive, N.AttrNetworkPeerPort, senderPort).Count == 1, "Receive span").ConfigureAwait(false);

                Activity span = capture.Activities(N.SpanReceive, N.AttrNetworkPeerPort, senderPort).Single();
                AssertEx.Equal(ActivityKind.Consumer, span.Kind, "Receive span should be a consumer span.");
                AssertEx.Equal(default(ActivitySpanId), span.ParentSpanId, "Receive span should be a root span, not a child of the constructing context.");
                AssertEx.Equal(ActivityStatusCode.Ok, span.Status, "Receive span status should be Ok.");
                AssertEx.Equal(7, span.GetTagItem(N.AttrDatagramSize), "Receive span should carry the datagram size.");
                AssertEx.Equal(true, span.GetTagItem(N.AttrNewEndpoint), "First datagram should mark the remote endpoint as new.");

                List<string> children = capture.Children(span).Select(a => a.OperationName).ToList();
                AssertEx.Contains("stage:endpoint_detected", children, "Receive span should have an endpoint_detected handler stage.");
                AssertEx.Contains("stage:datagram_received", children, "Receive span should have a datagram_received handler stage.");

                AssertEx.True(capture.Count(N.ReceiveDatagrams) >= 1, "Received datagrams should be counted.");
                AssertEx.True(capture.Sum(N.ReceivedBytes) >= 7, "Received bytes should be counted.");
                AssertEx.True(capture.Count(N.DatagramSize, (N.AttrNetworkIoDirection, N.DirectionReceive)) >= 1, "Receive datagram size should be recorded.");
                AssertEx.True(capture.Count(N.ReceiveDuration, (N.AttrOutcome, N.OutcomeSuccess)) >= 1, "Receive duration should be recorded.");
                AssertEx.True(capture.Count(N.HandlerInvocations, (N.AttrEvent, N.EventDatagramReceived), (N.AttrOutcome, N.OutcomeSuccess)) >= 1, "DatagramReceived handler invocation should be counted.");
                AssertEx.True(capture.Count(N.HandlerInvocations, (N.AttrEvent, N.EventEndpointDetected), (N.AttrOutcome, N.OutcomeSuccess)) >= 1, "EndpointDetected handler invocation should be counted.");
                AssertEx.True(capture.Count(N.HandlerDuration, (N.AttrEvent, N.EventDatagramReceived)) >= 1, "Handler duration should be recorded.");
            }
        }

        private static async Task ReceiveHandlerContextAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<bool> handled = UdpTestHelpers.CreateCompletionSource<bool>();

            using TelemetryCapture capture = new TelemetryCapture();
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);

            receiver.DatagramReceived += (_, _) =>
            {
                using (Activity? child = _TestSource.StartActivity("app-handler-work"))
                {
                }

                handled.TrySetResult(true);
            };

            sender.Send("127.0.0.1", receiverPort, "ctx");
            await UdpTestHelpers.WithTimeout(handled.Task, "Handler invocation").ConfigureAwait(false);
            await UdpTestHelpers.WaitUntil(() => capture.Activities(N.SpanReceive, N.AttrNetworkPeerPort, senderPort).Count == 1, "Receive span").ConfigureAwait(false);

            Activity receive = capture.Activities(N.SpanReceive, N.AttrNetworkPeerPort, senderPort).Single();
            Activity appWork = capture.Activities("app-handler-work").Single();
            AssertEx.Equal(receive.TraceId, appWork.TraceId, "Handler spans should join the receive trace.");
        }

        private static async Task HandlerExceptionAsync()
        {
            (int senderPort, int receiverPort) = UdpTestHelpers.GetAvailableUdpPortPair();
            TaskCompletionSource<bool> second = UdpTestHelpers.CreateCompletionSource<bool>();
            int calls = 0;

            using TelemetryCapture capture = new TelemetryCapture();
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            using UdpEndpoint sender = new UdpEndpoint("127.0.0.1", senderPort);

            receiver.DatagramReceived += (_, _) =>
            {
                if (System.Threading.Interlocked.Increment(ref calls) == 2) second.TrySetResult(true);
                throw new InvalidOperationException("boom");
            };

            sender.Send("127.0.0.1", receiverPort, "1");
            sender.Send("127.0.0.1", receiverPort, "2");
            await UdpTestHelpers.WithTimeout(second.Task, "Receive loop continues after handler failure").ConfigureAwait(false);
            await UdpTestHelpers.WaitUntil(() => capture.Count(N.HandlerInvocations, (N.AttrOutcome, N.OutcomeFailure)) >= 2, "Handler failure metrics").ConfigureAwait(false);

            AssertEx.True(capture.Count(N.HandlerInvocations, (N.AttrEvent, N.EventDatagramReceived), (N.AttrOutcome, N.OutcomeFailure), (N.AttrErrorType, "System.InvalidOperationException")) >= 2, "Handler failures should be counted by error.type.");

            Activity stage = capture.Activities("stage:datagram_received").First(a => a.Status == ActivityStatusCode.Error);
            AssertEx.Equal("System.InvalidOperationException", stage.GetTagItem(N.AttrErrorType), "Failed handler stage should carry error.type.");
            AssertEx.True(stage.Events.Any(e => e.Name == "exception"), "Failed handler stage should record an exception event.");
            // Only error stops are attributable here; endpoints disposed by earlier cases may still report disposed stops asynchronously.
            AssertEx.Equal(0, capture.Count(N.ReceiveLoopStops, (N.AttrStopReason, N.StopReasonError)), "Handler failures must not stop the receive loop.");
        }

        private static async Task ReceiveTruncatedAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();
            TaskCompletionSource<Datagram> received = UdpTestHelpers.CreateCompletionSource<Datagram>();

            using TelemetryCapture capture = new TelemetryCapture();
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);
            receiver.MaxDatagramSize = 4;
            receiver.DatagramReceived += (_, dg) => received.TrySetResult(dg);

            int senderPort = UdpTestHelpers.SendRawLoopback(receiverPort, new byte[10]);
            await UdpTestHelpers.WithTimeout(received.Task, "Receiving oversize datagram").ConfigureAwait(false);
            await UdpTestHelpers.WaitUntil(() => capture.Activities(N.SpanReceive, N.AttrNetworkPeerPort, senderPort).Count == 1, "Receive span").ConfigureAwait(false);

            AssertEx.Equal(1, capture.Count(N.ReceiveTruncated), "Truncated datagram should be counted.");
            Activity span = capture.Activities(N.SpanReceive, N.AttrNetworkPeerPort, senderPort).Single();
            AssertEx.Equal(true, span.GetTagItem(N.AttrTruncated), "Receive span should mark truncation.");
        }

        private static async Task RemoteEndpointCacheAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();

            using TelemetryCapture capture = new TelemetryCapture();
            UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            UdpTestHelpers.SendRawLoopback(receiverPort, new byte[] { 1 });
            UdpTestHelpers.SendRawLoopback(receiverPort, new byte[] { 2 });
            await UdpTestHelpers.WaitUntil(() => capture.Sum(N.RemoteEndpointsCached) == 2, "Cached remote endpoints").ConfigureAwait(false);

            AssertEx.Equal(2d, capture.Sum(N.RemoteEndpointsDetected), "Two remote endpoints should be detected.");
            AssertEx.Equal(100d, capture.Sum(N.RemoteEndpointsCapacity), "Capacity should reflect one cache of 100.");

            receiver.Dispose();
            AssertEx.Equal(0d, capture.Sum(N.RemoteEndpointsCached), "Dispose should release cached endpoints.");
            AssertEx.Equal(0d, capture.Sum(N.RemoteEndpointsCapacity), "Dispose should release cache capacity.");
        }

        private static async Task RemoteEndpointEvictionAsync()
        {
            int receiverPort = UdpTestHelpers.GetAvailableUdpPort();

            using TelemetryCapture capture = new TelemetryCapture();
            using UdpEndpoint receiver = new UdpEndpoint("127.0.0.1", receiverPort);

            // Each raw send uses a fresh ephemeral port, so a dropped loopback datagram is simply retried from a new remote endpoint.
            DateTime deadline = DateTime.UtcNow.AddSeconds(30);
            while (capture.Sum(N.RemoteEndpointsDetected) < 103)
            {
                if (DateTime.UtcNow > deadline) throw new TimeoutException("Detecting 103 remote endpoints timed out.");
                double before = capture.Sum(N.RemoteEndpointsDetected);
                UdpTestHelpers.SendRawLoopback(receiverPort, new byte[] { 1 });
                DateTime wait = DateTime.UtcNow.AddMilliseconds(250);
                while (capture.Sum(N.RemoteEndpointsDetected) <= before && DateTime.UtcNow < wait) await Task.Delay(1).ConfigureAwait(false);
            }

            await UdpTestHelpers.WaitUntil(() => capture.Sum(N.RemoteEndpointsCached) == 100, "Cache size settles at capacity").ConfigureAwait(false);

            AssertEx.Equal(capture.Sum(N.RemoteEndpointsDetected) - 100d, capture.Sum(N.RemoteEndpointsEvicted), "Every endpoint detected beyond capacity 100 should be counted as evicted.");
            AssertEx.Equal(100d, capture.Sum(N.RemoteEndpointsCached), "Cache size should be capped at 100.");
        }

        private static Task EndpointLifecycleAsync()
        {
            using TelemetryCapture capture = new TelemetryCapture();
            UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", 0);

            AssertEx.Equal(1, capture.Count(N.EndpointStarts, (N.AttrOutcome, N.OutcomeSuccess)), "Successful start should be counted.");
            AssertEx.Equal(1d, capture.Sum(N.EndpointsActive), "Active endpoints should increase.");

            Activity start = capture.Activities(N.SpanStart).Single();
            AssertEx.Equal(ActivityStatusCode.Ok, start.Status, "Start span should be Ok.");
            AssertEx.True((int)start.GetTagItem(N.AttrNetworkLocalPort)! > 0, "Start span should carry the bound ephemeral port.");

            endpoint.Dispose();
            endpoint.Dispose();
            AssertEx.Equal(0d, capture.Sum(N.EndpointsActive), "Dispose should decrement active endpoints exactly once.");
            return Task.CompletedTask;
        }

        private static Task EndpointBindFailureAsync()
        {
            int port = UdpTestHelpers.GetAvailableUdpPort();
            using UdpEndpoint first = new UdpEndpoint("127.0.0.1", port);
            using TelemetryCapture capture = new TelemetryCapture();

            AssertEx.Throws<SocketException>(() => new UdpEndpoint("127.0.0.1", port), "Duplicate bind should throw.");

            AssertEx.Equal(1, capture.Count(N.EndpointStarts, (N.AttrOutcome, N.OutcomeFailure), (N.AttrErrorType, "System.Net.Sockets.SocketException")), "Bind failure should be counted by error.type.");
            AssertEx.Equal(0d, capture.Sum(N.EndpointsActive), "Failed starts must not change active endpoints.");

            Activity start = capture.Activities(N.SpanStart).Single();
            AssertEx.Equal(ActivityStatusCode.Error, start.Status, "Failed start span should have Error status.");
            AssertEx.True(start.Events.Any(e => e.Name == "exception"), "Failed start span should record an exception event.");
            return Task.CompletedTask;
        }

        private static async Task ReceiveLoopStopAsync()
        {
            using TelemetryCapture capture = new TelemetryCapture();
            UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", 0);
            TaskCompletionSource<bool> stopped = UdpTestHelpers.CreateCompletionSource<bool>();
            endpoint.ServerStopped += (_, _) => stopped.TrySetResult(true);

            endpoint.Dispose();
            await UdpTestHelpers.WithTimeout(stopped.Task, "ServerStopped after dispose").ConfigureAwait(false);

            // At least one, since endpoints disposed by earlier cases may still report disposed stops asynchronously.
            AssertEx.True(capture.Count(N.ReceiveLoopStops, (N.AttrStopReason, N.StopReasonDisposed)) >= 1, "Disposal should record a disposed receive loop stop.");
            AssertEx.Equal(0, capture.Count(N.ReceiveLoopStops, (N.AttrStopReason, N.StopReasonError)), "Disposal should not record an error stop.");
        }

        private static Task BuildInfoAsync()
        {
            using UdpEndpoint endpoint = new UdpEndpoint("127.0.0.1", 0);
            using TelemetryCapture capture = new TelemetryCapture();
            capture.RecordObservable();

            CapturedMeasurement info = capture.Measurements(N.BuildInfo).Single();
            AssertEx.Equal(1d, info.Value, "Build info should report 1.");
            string? version = info.Tags[N.AttrVersion] as string;
            AssertEx.True(!String.IsNullOrEmpty(version) && version != "0.0.0", "Build info should carry the library version.");
            return Task.CompletedTask;
        }
    }
}
