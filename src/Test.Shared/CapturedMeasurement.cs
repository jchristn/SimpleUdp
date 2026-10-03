namespace Test.Shared
{
    using System.Collections.Generic;

    /// <summary>
    /// One measurement captured by <see cref="TelemetryCapture"/>.
    /// </summary>
    internal sealed class CapturedMeasurement
    {
        internal CapturedMeasurement(string name, double value, Dictionary<string, object?> tags)
        {
            Name = name;
            Value = value;
            Tags = tags;
        }

        internal string Name { get; }

        internal double Value { get; }

        internal Dictionary<string, object?> Tags { get; }
    }
}
