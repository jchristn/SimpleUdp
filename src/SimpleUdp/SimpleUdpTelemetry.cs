namespace SimpleUdp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Reflection;

    /// <summary>
    /// Owns the process-wide SimpleUdp meter and activity source. Every recording method is best-effort:
    /// it is a near-zero-cost no-op when nothing is subscribed and never throws into the caller.
    /// </summary>
    internal static class SimpleUdpTelemetry
    {
        #region Internal-Members

        internal static readonly string Version = GetVersion();

        internal static readonly Meter Meter = new Meter(SimpleUdpTelemetryNames.MeterName, Version);

        internal static readonly ActivitySource Source = new ActivitySource(SimpleUdpTelemetryNames.ActivitySourceName, Version);

        #endregion

        #region Private-Members

        private static readonly Histogram<double> _SendDuration = Meter.CreateHistogram<double>(SimpleUdpTelemetryNames.SendDuration, "s", "End-to-end duration of a send call.");
        private static readonly Counter<long> _SendDatagrams = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.SendDatagrams, "{datagram}", "Send calls by outcome.");
        private static readonly Histogram<double> _SendStageDuration = Meter.CreateHistogram<double>(SimpleUdpTelemetryNames.SendStageDuration, "s", "Duration of each send stage.");
        private static readonly Counter<long> _SendStageEvents = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.SendStageEvents, "{event}", "Send stage executions by outcome.");
        private static readonly UpDownCounter<long> _SendQueued = Meter.CreateUpDownCounter<long>(SimpleUdpTelemetryNames.SendQueued, "{send}", "Sends waiting for the per-endpoint send slot.");
        private static readonly UpDownCounter<long> _SendActive = Meter.CreateUpDownCounter<long>(SimpleUdpTelemetryNames.SendActive, "{send}", "Sends holding the per-endpoint send slot.");
        private static readonly Counter<long> _SentBytes = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.SentBytes, "By", "Payload bytes sent successfully.");
        private static readonly Histogram<long> _DatagramSize = Meter.CreateHistogram<long>(SimpleUdpTelemetryNames.DatagramSize, "By", "Datagram payload size.");
        private static readonly Counter<long> _ReceiveDatagrams = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.ReceiveDatagrams, "{datagram}", "Datagrams received.");
        private static readonly Counter<long> _ReceivedBytes = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.ReceivedBytes, "By", "Payload bytes received and delivered.");
        private static readonly Counter<long> _ReceiveTruncated = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.ReceiveTruncated, "{datagram}", "Received datagrams truncated to MaxDatagramSize.");
        private static readonly Histogram<double> _ReceiveDuration = Meter.CreateHistogram<double>(SimpleUdpTelemetryNames.ReceiveDuration, "s", "Duration of processing one received datagram.");
        private static readonly Counter<long> _ReceiveLoopStops = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.ReceiveLoopStops, "{stop}", "Receive loop terminations.");
        private static readonly Histogram<double> _HandlerDuration = Meter.CreateHistogram<double>(SimpleUdpTelemetryNames.HandlerDuration, "s", "Duration of each application event handler invocation.");
        private static readonly Counter<long> _HandlerInvocations = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.HandlerInvocations, "{invocation}", "Application event handler invocations by outcome.");
        private static readonly Counter<long> _RemoteEndpointsDetected = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.RemoteEndpointsDetected, "{endpoint}", "New remote endpoints detected.");
        private static readonly UpDownCounter<long> _RemoteEndpointsCached = Meter.CreateUpDownCounter<long>(SimpleUdpTelemetryNames.RemoteEndpointsCached, "{endpoint}", "Remote endpoints held in the recent-endpoints cache.");
        private static readonly UpDownCounter<long> _RemoteEndpointsCapacity = Meter.CreateUpDownCounter<long>(SimpleUdpTelemetryNames.RemoteEndpointsCapacity, "{endpoint}", "Capacity of the recent-endpoints caches.");
        private static readonly Counter<long> _RemoteEndpointsEvicted = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.RemoteEndpointsEvicted, "{endpoint}", "Remote endpoints evicted from the recent-endpoints cache.");
        private static readonly UpDownCounter<long> _EndpointsActive = Meter.CreateUpDownCounter<long>(SimpleUdpTelemetryNames.EndpointsActive, "{endpoint}", "Local UdpEndpoint instances bound and not disposed.");
        private static readonly Counter<long> _EndpointStarts = Meter.CreateCounter<long>(SimpleUdpTelemetryNames.EndpointStarts, "{endpoint}", "UdpEndpoint construction attempts by outcome.");
        private static readonly ObservableGauge<long> _BuildInfo = Meter.CreateObservableGauge<long>(SimpleUdpTelemetryNames.BuildInfo, ObserveBuildInfo, "{info}", "Always 1; carries the library version.");

        #endregion

        #region Internal-Methods

        internal static Activity StartActivity(string name, ActivityKind kind)
        {
            try
            {
                if (!Source.HasListeners()) return null;
                Activity activity = Source.StartActivity(name, kind);
                activity?.SetTag(SimpleUdpTelemetryNames.AttrNetworkTransport, "udp");
                return activity;
            }
            catch
            {
                return null;
            }
        }

        internal static Activity StartRootActivity(string name, ActivityKind kind)
        {
            if (!Source.HasListeners()) return null;

            Activity previous = Activity.Current;

            try
            {
                Activity.Current = null;
                return StartActivity(name, kind);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (Activity.Current == null) Activity.Current = previous;
            }
        }

        internal static Activity StartStageActivity(string stage)
        {
            return StartActivity(SimpleUdpTelemetryNames.SpanStagePrefix + stage, ActivityKind.Internal);
        }

        internal static void SetSuccess(Activity activity)
        {
            try
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch
            {
            }
        }

        internal static void SetFailure(Activity activity, Exception e)
        {
            if (activity == null || e == null) return;

            try
            {
                activity.SetStatus(ActivityStatusCode.Error, e.Message);
                activity.SetTag(SimpleUdpTelemetryNames.AttrErrorType, ErrorType(e));
                activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
                {
                    { "exception.type", ErrorType(e) },
                    { "exception.message", e.Message },
                    { "exception.stacktrace", e.ToString() }
                }));
            }
            catch
            {
            }
        }

        internal static void StopActivity(Activity activity)
        {
            try
            {
                activity?.Dispose();
            }
            catch
            {
            }
        }

        internal static string ErrorType(Exception e)
        {
            if (e == null) return null;
            return e.GetType().FullName;
        }

        internal static double ElapsedSeconds(long startTimestamp)
        {
            return (double)(Stopwatch.GetTimestamp() - startTimestamp) / Stopwatch.Frequency;
        }

        internal static void RecordSend(string mode, string outcome, Exception e, double seconds)
        {
            try
            {
                if (!_SendDuration.Enabled && !_SendDatagrams.Enabled) return;
                TagList tags = new TagList
                {
                    { SimpleUdpTelemetryNames.AttrSendMode, mode },
                    { SimpleUdpTelemetryNames.AttrOutcome, outcome }
                };
                if (e != null) tags.Add(SimpleUdpTelemetryNames.AttrErrorType, ErrorType(e));
                _SendDuration.Record(seconds, tags);
                _SendDatagrams.Add(1, tags);
            }
            catch
            {
            }
        }

        internal static void RecordSendStage(string stage, string outcome, double seconds)
        {
            try
            {
                if (!_SendStageDuration.Enabled && !_SendStageEvents.Enabled) return;
                TagList tags = new TagList
                {
                    { SimpleUdpTelemetryNames.AttrStage, stage },
                    { SimpleUdpTelemetryNames.AttrOutcome, outcome }
                };
                _SendStageDuration.Record(seconds, tags);
                _SendStageEvents.Add(1, tags);
            }
            catch
            {
            }
        }

        internal static void AddSendQueued(long delta)
        {
            try
            {
                _SendQueued.Add(delta);
            }
            catch
            {
            }
        }

        internal static void AddSendActive(long delta)
        {
            try
            {
                _SendActive.Add(delta);
            }
            catch
            {
            }
        }

        internal static void RecordSentBytes(int bytes)
        {
            try
            {
                _SentBytes.Add(bytes);
                _DatagramSize.Record(bytes, new KeyValuePair<string, object>(SimpleUdpTelemetryNames.AttrNetworkIoDirection, SimpleUdpTelemetryNames.DirectionTransmit));
            }
            catch
            {
            }
        }

        internal static void RecordReceived(int bytes, bool truncated)
        {
            try
            {
                _ReceiveDatagrams.Add(1);
                _ReceivedBytes.Add(bytes);
                _DatagramSize.Record(bytes, new KeyValuePair<string, object>(SimpleUdpTelemetryNames.AttrNetworkIoDirection, SimpleUdpTelemetryNames.DirectionReceive));
                if (truncated) _ReceiveTruncated.Add(1);
            }
            catch
            {
            }
        }

        internal static void RecordReceiveDuration(string outcome, double seconds)
        {
            try
            {
                _ReceiveDuration.Record(seconds, new KeyValuePair<string, object>(SimpleUdpTelemetryNames.AttrOutcome, outcome));
            }
            catch
            {
            }
        }

        internal static void RecordReceiveLoopStop(string reason, Exception e)
        {
            try
            {
                TagList tags = new TagList
                {
                    { SimpleUdpTelemetryNames.AttrStopReason, reason }
                };
                if (e != null) tags.Add(SimpleUdpTelemetryNames.AttrErrorType, ErrorType(e));
                _ReceiveLoopStops.Add(1, tags);
            }
            catch
            {
            }
        }

        internal static void RecordHandler(string eventName, string outcome, Exception e, double seconds)
        {
            try
            {
                if (!_HandlerDuration.Enabled && !_HandlerInvocations.Enabled) return;
                TagList tags = new TagList
                {
                    { SimpleUdpTelemetryNames.AttrEvent, eventName },
                    { SimpleUdpTelemetryNames.AttrOutcome, outcome }
                };
                if (e != null) tags.Add(SimpleUdpTelemetryNames.AttrErrorType, ErrorType(e));
                _HandlerDuration.Record(seconds, tags);
                _HandlerInvocations.Add(1, tags);
            }
            catch
            {
            }
        }

        internal static void RecordRemoteEndpointDetected()
        {
            try
            {
                _RemoteEndpointsDetected.Add(1);
            }
            catch
            {
            }
        }

        internal static void AddRemoteEndpointsCached(long delta)
        {
            if (delta == 0) return;

            try
            {
                _RemoteEndpointsCached.Add(delta);
            }
            catch
            {
            }
        }

        internal static void AddRemoteEndpointsCapacity(long delta)
        {
            try
            {
                _RemoteEndpointsCapacity.Add(delta);
            }
            catch
            {
            }
        }

        internal static void RecordRemoteEndpointsEvicted(long count)
        {
            if (count < 1) return;

            try
            {
                _RemoteEndpointsEvicted.Add(count);
            }
            catch
            {
            }
        }

        internal static void AddEndpointsActive(long delta)
        {
            try
            {
                _EndpointsActive.Add(delta);
            }
            catch
            {
            }
        }

        internal static void RecordEndpointStart(string outcome, Exception e)
        {
            try
            {
                TagList tags = new TagList
                {
                    { SimpleUdpTelemetryNames.AttrOutcome, outcome }
                };
                if (e != null) tags.Add(SimpleUdpTelemetryNames.AttrErrorType, ErrorType(e));
                _EndpointStarts.Add(1, tags);
            }
            catch
            {
            }
        }

        #endregion

        #region Private-Methods

        private static string GetVersion()
        {
            try
            {
                Assembly assembly = typeof(SimpleUdpTelemetry).Assembly;
                AssemblyInformationalVersionAttribute info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                string version = info?.InformationalVersion;
                if (!String.IsNullOrEmpty(version))
                {
                    int plus = version.IndexOf('+');
                    if (plus > 0) version = version.Substring(0, plus);
                    return version;
                }

                return assembly.GetName().Version?.ToString() ?? "0.0.0";
            }
            catch
            {
                return "0.0.0";
            }
        }

        private static Measurement<long> ObserveBuildInfo()
        {
            return new Measurement<long>(1, new KeyValuePair<string, object>(SimpleUdpTelemetryNames.AttrVersion, Version));
        }

        #endregion
    }
}
