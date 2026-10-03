namespace SimpleUdp
{
    /// <summary>
    /// Stable names for every meter, activity source, instrument, span, and attribute emitted by SimpleUdp.
    /// These strings are a public contract consumed by collectors and dashboards; treat them as API.
    /// <para>Subscribe a collector to <see cref="MeterName"/> and <see cref="ActivitySourceName"/> to receive the data.</para>
    /// </summary>
    public static class SimpleUdpTelemetryNames
    {
        #region Sources

        /// <summary>
        /// Name of the <see cref="System.Diagnostics.Metrics.Meter"/> used by SimpleUdp.
        /// </summary>
        public const string MeterName = "SimpleUdp";

        /// <summary>
        /// Name of the <see cref="System.Diagnostics.ActivitySource"/> used by SimpleUdp.
        /// </summary>
        public const string ActivitySourceName = "SimpleUdp";

        #endregion

        #region Instruments

        /// <summary>
        /// Histogram (s): end-to-end duration of a send call, including validation, lock wait, and transmit.
        /// Labels: simpleudp.send.mode, outcome, error.type.
        /// </summary>
        public const string SendDuration = "simpleudp.send.duration";

        /// <summary>
        /// Counter ({datagram}): send calls by outcome.
        /// Labels: simpleudp.send.mode, outcome, error.type.
        /// </summary>
        public const string SendDatagrams = "simpleudp.send.datagrams";

        /// <summary>
        /// Histogram (s): duration of each send stage (queued, transmit).
        /// Labels: stage, outcome.
        /// </summary>
        public const string SendStageDuration = "simpleudp.send.stage.duration";

        /// <summary>
        /// Counter ({event}): send stage executions by outcome.
        /// Labels: stage, outcome.
        /// </summary>
        public const string SendStageEvents = "simpleudp.send.stage.events";

        /// <summary>
        /// UpDownCounter ({send}): sends currently waiting for the per-endpoint send slot.
        /// </summary>
        public const string SendQueued = "simpleudp.send.queued";

        /// <summary>
        /// UpDownCounter ({send}): sends currently holding the per-endpoint send slot.
        /// </summary>
        public const string SendActive = "simpleudp.send.active";

        /// <summary>
        /// Counter (By): payload bytes sent successfully.
        /// </summary>
        public const string SentBytes = "simpleudp.sent.bytes";

        /// <summary>
        /// Histogram (By): datagram payload size.
        /// Labels: network.io.direction (transmit, receive).
        /// </summary>
        public const string DatagramSize = "simpleudp.datagram.size";

        /// <summary>
        /// Counter ({datagram}): datagrams received.
        /// </summary>
        public const string ReceiveDatagrams = "simpleudp.receive.datagrams";

        /// <summary>
        /// Counter (By): payload bytes received and delivered.
        /// </summary>
        public const string ReceivedBytes = "simpleudp.received.bytes";

        /// <summary>
        /// Counter ({datagram}): received datagrams truncated to MaxDatagramSize.
        /// </summary>
        public const string ReceiveTruncated = "simpleudp.receive.truncated";

        /// <summary>
        /// Histogram (s): duration of processing one received datagram, including all event handlers.
        /// Labels: outcome.
        /// </summary>
        public const string ReceiveDuration = "simpleudp.receive.duration";

        /// <summary>
        /// Counter ({stop}): receive loop terminations.
        /// Labels: simpleudp.stop.reason (disposed, error), error.type.
        /// </summary>
        public const string ReceiveLoopStops = "simpleudp.receive.loop.stops";

        /// <summary>
        /// Histogram (s): duration of each application event handler invocation.
        /// Labels: simpleudp.event, outcome, error.type.
        /// </summary>
        public const string HandlerDuration = "simpleudp.handler.duration";

        /// <summary>
        /// Counter ({invocation}): application event handler invocations by outcome.
        /// Exceptions thrown by handlers are swallowed by SimpleUdp, so this is the only place they are visible.
        /// Labels: simpleudp.event, outcome, error.type.
        /// </summary>
        public const string HandlerInvocations = "simpleudp.handler.invocations";

        /// <summary>
        /// Counter ({endpoint}): new remote endpoints detected.
        /// </summary>
        public const string RemoteEndpointsDetected = "simpleudp.remote_endpoints.detected";

        /// <summary>
        /// UpDownCounter ({endpoint}): remote endpoints currently held in the recent-endpoints cache, across all local endpoints.
        /// </summary>
        public const string RemoteEndpointsCached = "simpleudp.remote_endpoints.cached";

        /// <summary>
        /// UpDownCounter ({endpoint}): total capacity of the recent-endpoints caches, across all local endpoints.
        /// </summary>
        public const string RemoteEndpointsCapacity = "simpleudp.remote_endpoints.capacity";

        /// <summary>
        /// Counter ({endpoint}): remote endpoints evicted from the recent-endpoints cache.
        /// </summary>
        public const string RemoteEndpointsEvicted = "simpleudp.remote_endpoints.evicted";

        /// <summary>
        /// UpDownCounter ({endpoint}): local UdpEndpoint instances currently bound and not disposed.
        /// </summary>
        public const string EndpointsActive = "simpleudp.endpoints.active";

        /// <summary>
        /// Counter ({endpoint}): local UdpEndpoint construction attempts (socket bind) by outcome.
        /// Labels: outcome, error.type.
        /// </summary>
        public const string EndpointStarts = "simpleudp.endpoint.starts";

        /// <summary>
        /// Gauge ({info}): always 1; carries the library version.
        /// Labels: simpleudp.version.
        /// </summary>
        public const string BuildInfo = "simpleudp.build.info";

        #endregion

        #region Spans

        /// <summary>
        /// Client span for one send call.
        /// </summary>
        public const string SpanSend = "simpleudp send";

        /// <summary>
        /// Root consumer span for one received datagram.
        /// </summary>
        public const string SpanReceive = "simpleudp receive";

        /// <summary>
        /// Internal span for endpoint construction (socket bind).
        /// </summary>
        public const string SpanStart = "simpleudp start";

        /// <summary>
        /// Prefix for stage spans, followed by the stage name, for example "stage:queued".
        /// </summary>
        public const string SpanStagePrefix = "stage:";

        #endregion

        #region Attributes

        /// <summary>
        /// Outcome label: success, failure, or canceled.
        /// </summary>
        public const string AttrOutcome = "outcome";

        /// <summary>
        /// Stage label.
        /// </summary>
        public const string AttrStage = "stage";

        /// <summary>
        /// Error type label: the fully qualified exception type name.
        /// </summary>
        public const string AttrErrorType = "error.type";

        /// <summary>
        /// Send mode label: sync or async.
        /// </summary>
        public const string AttrSendMode = "simpleudp.send.mode";

        /// <summary>
        /// Event label: endpoint_detected or datagram_received.
        /// </summary>
        public const string AttrEvent = "simpleudp.event";

        /// <summary>
        /// Receive loop stop reason label: disposed or error.
        /// </summary>
        public const string AttrStopReason = "simpleudp.stop.reason";

        /// <summary>
        /// Library version label.
        /// </summary>
        public const string AttrVersion = "simpleudp.version";

        /// <summary>
        /// Network I/O direction label: transmit or receive.
        /// </summary>
        public const string AttrNetworkIoDirection = "network.io.direction";

        /// <summary>
        /// Span attribute: transport, always "udp".
        /// </summary>
        public const string AttrNetworkTransport = "network.transport";

        /// <summary>
        /// Span attribute: remote peer address.
        /// </summary>
        public const string AttrNetworkPeerAddress = "network.peer.address";

        /// <summary>
        /// Span attribute: remote peer port.
        /// </summary>
        public const string AttrNetworkPeerPort = "network.peer.port";

        /// <summary>
        /// Span attribute: local address.
        /// </summary>
        public const string AttrNetworkLocalAddress = "network.local.address";

        /// <summary>
        /// Span attribute: local port.
        /// </summary>
        public const string AttrNetworkLocalPort = "network.local.port";

        /// <summary>
        /// Span attribute: datagram payload size in bytes.
        /// </summary>
        public const string AttrDatagramSize = "simpleudp.datagram.size";

        /// <summary>
        /// Span attribute: IP time-to-live used for a send.
        /// </summary>
        public const string AttrTtl = "simpleudp.ttl";

        /// <summary>
        /// Span attribute: true when the remote endpoint was seen for the first time.
        /// </summary>
        public const string AttrNewEndpoint = "simpleudp.remote_endpoint.new";

        /// <summary>
        /// Span attribute: true when a received datagram was truncated.
        /// </summary>
        public const string AttrTruncated = "simpleudp.truncated";

        /// <summary>
        /// Span attribute: configured maximum datagram size.
        /// </summary>
        public const string AttrMaxDatagramSize = "simpleudp.max_datagram_size";

        /// <summary>
        /// Span attribute: whether broadcast is enabled on the socket.
        /// </summary>
        public const string AttrBroadcast = "simpleudp.broadcast";

        #endregion

        #region Values

        /// <summary>
        /// Outcome value for successful operations.
        /// </summary>
        public const string OutcomeSuccess = "success";

        /// <summary>
        /// Outcome value for failed operations.
        /// </summary>
        public const string OutcomeFailure = "failure";

        /// <summary>
        /// Outcome value for canceled operations.
        /// </summary>
        public const string OutcomeCanceled = "canceled";

        /// <summary>
        /// Send stage: waiting for the per-endpoint send slot.
        /// </summary>
        public const string StageQueued = "queued";

        /// <summary>
        /// Send stage: writing the datagram to the socket.
        /// </summary>
        public const string StageTransmit = "transmit";

        /// <summary>
        /// Send mode value for synchronous sends.
        /// </summary>
        public const string SendModeSync = "sync";

        /// <summary>
        /// Send mode value for asynchronous sends.
        /// </summary>
        public const string SendModeAsync = "async";

        /// <summary>
        /// Event value for the EndpointDetected event.
        /// </summary>
        public const string EventEndpointDetected = "endpoint_detected";

        /// <summary>
        /// Event value for the DatagramReceived event.
        /// </summary>
        public const string EventDatagramReceived = "datagram_received";

        /// <summary>
        /// Stop reason value when the endpoint was disposed.
        /// </summary>
        public const string StopReasonDisposed = "disposed";

        /// <summary>
        /// Stop reason value when the socket faulted.
        /// </summary>
        public const string StopReasonError = "error";

        /// <summary>
        /// Direction value for outbound datagrams.
        /// </summary>
        public const string DirectionTransmit = "transmit";

        /// <summary>
        /// Direction value for inbound datagrams.
        /// </summary>
        public const string DirectionReceive = "receive";

        #endregion
    }
}
