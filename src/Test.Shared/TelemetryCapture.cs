namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using SimpleUdp;

    /// <summary>
    /// In-memory subscriber for the SimpleUdp meter and activity source.
    /// </summary>
    internal sealed class TelemetryCapture : IDisposable
    {
        private readonly MeterListener _MeterListener;
        private readonly ActivityListener _ActivityListener;
        private readonly object _Lock = new object();
        private readonly List<CapturedMeasurement> _Measurements = new List<CapturedMeasurement>();
        private readonly List<Activity> _Activities = new List<Activity>();
        private readonly HashSet<string> _Instruments = new HashSet<string>();

        internal TelemetryCapture()
        {
            _MeterListener = new MeterListener();
            _MeterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name != SimpleUdpTelemetryNames.MeterName) return;
                lock (_Lock) _Instruments.Add(instrument.Name);
                listener.EnableMeasurementEvents(instrument);
            };
            _MeterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) => Add(instrument, value, tags));
            _MeterListener.SetMeasurementEventCallback<double>((instrument, value, tags, state) => Add(instrument, value, tags));
            _MeterListener.Start();

            _ActivityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SimpleUdpTelemetryNames.ActivitySourceName || source.Name == "Test.Shared",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    lock (_Lock) _Activities.Add(activity);
                }
            };
            ActivitySource.AddActivityListener(_ActivityListener);
        }

        internal IReadOnlyCollection<string> Instruments
        {
            get
            {
                lock (_Lock) return _Instruments.ToList();
            }
        }

        internal void RecordObservable()
        {
            _MeterListener.RecordObservableInstruments();
        }

        internal List<CapturedMeasurement> Measurements(string name, params (string Key, object Value)[] tags)
        {
            lock (_Lock)
            {
                return _Measurements
                    .Where(m => m.Name == name)
                    .Where(m => tags.All(t => m.Tags.TryGetValue(t.Key, out object? v) && Equals(v, t.Value)))
                    .ToList();
            }
        }

        internal double Sum(string name, params (string Key, object Value)[] tags)
        {
            return Measurements(name, tags).Sum(m => m.Value);
        }

        internal int Count(string name, params (string Key, object Value)[] tags)
        {
            return Measurements(name, tags).Count;
        }

        internal List<Activity> Activities(string name)
        {
            lock (_Lock) return _Activities.Where(a => a.OperationName == name).ToList();
        }

        internal List<Activity> Activities(string name, string tagKey, object tagValue)
        {
            return Activities(name).Where(a => Equals(a.GetTagItem(tagKey), tagValue)).ToList();
        }

        internal List<Activity> Children(Activity parent)
        {
            lock (_Lock) return _Activities.Where(a => a.ParentSpanId == parent.SpanId && a.TraceId == parent.TraceId).ToList();
        }

        public void Dispose()
        {
            _MeterListener.Dispose();
            _ActivityListener.Dispose();
        }

        private void Add(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            Dictionary<string, object?> copy = new Dictionary<string, object?>();
            foreach (KeyValuePair<string, object?> tag in tags) copy[tag.Key] = tag.Value;
            lock (_Lock) _Measurements.Add(new CapturedMeasurement(instrument.Name, value, copy));
        }
    }
}
