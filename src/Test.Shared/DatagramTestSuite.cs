namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SimpleUdp;
    using Touchstone.Core;

    public static class DatagramTestSuite
    {
        public const string SuiteId = "datagram-model";

        public const string SuiteName = "Datagram Model";

        public static TestSuiteDescriptor Create()
        {
            return TouchstoneDescriptorFactory.Suite(
                SuiteId,
                SuiteName,
                new List<TestCaseDescriptor>
                {
                    TouchstoneDescriptorFactory.Case(SuiteId, "default-constructor", "Default constructor initializes safe defaults", DefaultConstructorInitializesSafeDefaultsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "ip-setter-validation", "Ip setter rejects null or empty values", IpSetterRejectsNullOrEmptyValuesAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "ip-setter-preserves-previous-value", "Ip setter preserves the previous value after invalid assignments", IpSetterPreservesPreviousValueAfterInvalidAssignmentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "ip-setter-allows-whitespace", "Ip setter allows whitespace-only values", IpSetterAllowsWhitespaceOnlyValuesAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "port-setter-validation", "Port setter enforces valid range", PortSetterEnforcesValidRangeAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "port-setter-boundaries", "Port setter accepts UDP boundary values", PortSetterAcceptsUdpBoundaryValuesAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "port-setter-preserves-previous-value", "Port setter preserves the previous value after invalid assignments", PortSetterPreservesPreviousValueAfterInvalidAssignmentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "data-null-to-empty", "Data setter converts null to empty array", DataSetterConvertsNullToEmptyArrayAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "data-empty-array", "Data setter accepts an explicit empty array", DataSetterAcceptsExplicitEmptyArrayAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "data-reference-semantics", "Data setter stores the provided array reference", DataSetterStoresProvidedArrayReferenceAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "instances-independent", "Datagram instances keep independent state", DatagramInstancesKeepIndependentStateAsync)
                });
        }

        private static Task DefaultConstructorInitializesSafeDefaultsAsync()
        {
            Datagram datagram = new Datagram();
            AssertEx.Equal("127.0.0.1", datagram.Ip, "Datagram default IP should be loopback.");
            AssertEx.Equal(0, datagram.Port, "Datagram default port should be zero.");
            AssertEx.NotNull(datagram.Data, "Datagram default data should not be null.");
            AssertEx.Equal(0, datagram.Data.Length, "Datagram default data should be empty.");
            return Task.CompletedTask;
        }

        private static Task IpSetterRejectsNullOrEmptyValuesAsync()
        {
            Datagram datagram = new Datagram();
            AssertEx.Throws<ArgumentNullException>(() => datagram.Ip = null!, "Datagram.Ip should reject null.");
            AssertEx.Throws<ArgumentNullException>(() => datagram.Ip = String.Empty, "Datagram.Ip should reject empty input.");
            datagram.Ip = "192.168.1.10";
            AssertEx.Equal("192.168.1.10", datagram.Ip, "Datagram.Ip should accept a valid address string.");
            return Task.CompletedTask;
        }

        private static Task IpSetterPreservesPreviousValueAfterInvalidAssignmentsAsync()
        {
            Datagram datagram = new Datagram();
            datagram.Ip = "10.1.2.3";

            AssertEx.Throws<ArgumentNullException>(() => datagram.Ip = null!, "Datagram.Ip should reject null.");
            AssertEx.Throws<ArgumentNullException>(() => datagram.Ip = String.Empty, "Datagram.Ip should reject empty input.");
            AssertEx.Equal("10.1.2.3", datagram.Ip, "Failed IP assignments should not mutate the current value.");
            return Task.CompletedTask;
        }

        private static Task IpSetterAllowsWhitespaceOnlyValuesAsync()
        {
            Datagram datagram = new Datagram();
            datagram.Ip = "   ";
            AssertEx.Equal("   ", datagram.Ip, "Datagram.Ip currently validates empty strings but not whitespace-only strings.");
            return Task.CompletedTask;
        }

        private static Task PortSetterEnforcesValidRangeAsync()
        {
            Datagram datagram = new Datagram();
            AssertEx.Throws<ArgumentOutOfRangeException>(() => datagram.Port = -1, "Datagram.Port should reject negative values.");
            AssertEx.Throws<ArgumentOutOfRangeException>(() => datagram.Port = 65536, "Datagram.Port should reject values above 65535.");
            datagram.Port = 4321;
            AssertEx.Equal(4321, datagram.Port, "Datagram.Port should accept valid values.");
            return Task.CompletedTask;
        }

        private static Task PortSetterAcceptsUdpBoundaryValuesAsync()
        {
            Datagram datagram = new Datagram();
            datagram.Port = 0;
            AssertEx.Equal(0, datagram.Port, "Datagram.Port should accept zero.");

            datagram.Port = 65535;
            AssertEx.Equal(65535, datagram.Port, "Datagram.Port should accept the maximum UDP port.");
            return Task.CompletedTask;
        }

        private static Task PortSetterPreservesPreviousValueAfterInvalidAssignmentsAsync()
        {
            Datagram datagram = new Datagram();
            datagram.Port = 9999;

            AssertEx.Throws<ArgumentOutOfRangeException>(() => datagram.Port = -1, "Datagram.Port should reject negative values.");
            AssertEx.Throws<ArgumentOutOfRangeException>(() => datagram.Port = 65536, "Datagram.Port should reject above-range values.");
            AssertEx.Equal(9999, datagram.Port, "Failed port assignments should not mutate the current value.");
            return Task.CompletedTask;
        }

        private static Task DataSetterConvertsNullToEmptyArrayAsync()
        {
            Datagram datagram = new Datagram();
            datagram.Data = new byte[] { 1, 2, 3 };
            AssertEx.Equal(3, datagram.Data.Length, "Datagram.Data should store assigned values.");

            datagram.Data = null!;
            AssertEx.NotNull(datagram.Data, "Datagram.Data should convert null to an empty array.");
            AssertEx.Equal(0, datagram.Data.Length, "Datagram.Data should become empty after a null assignment.");
            return Task.CompletedTask;
        }

        private static Task DataSetterAcceptsExplicitEmptyArrayAsync()
        {
            Datagram datagram = new Datagram();
            datagram.Data = Array.Empty<byte>();
            AssertEx.NotNull(datagram.Data, "Datagram.Data should accept an explicit empty array.");
            AssertEx.Equal(0, datagram.Data.Length, "Datagram.Data should preserve explicit empty arrays.");
            return Task.CompletedTask;
        }

        private static Task DataSetterStoresProvidedArrayReferenceAsync()
        {
            Datagram datagram = new Datagram();
            byte[] payload = new byte[] { 1, 2, 3 };

            datagram.Data = payload;
            payload[1] = 99;

            AssertEx.Equal(99, datagram.Data[1], "Datagram.Data should expose the same array reference currently assigned.");
            return Task.CompletedTask;
        }

        private static Task DatagramInstancesKeepIndependentStateAsync()
        {
            Datagram first = new Datagram { Ip = "127.0.0.2", Port = 1000, Data = new byte[] { 1 } };
            Datagram second = new Datagram { Ip = "127.0.0.3", Port = 2000, Data = new byte[] { 2, 3 } };

            AssertEx.Equal("127.0.0.2", first.Ip, "First datagram should retain its IP.");
            AssertEx.Equal(1000, first.Port, "First datagram should retain its port.");
            AssertEx.SequenceEqual(new byte[] { 1 }, first.Data, "First datagram should retain its payload.");
            AssertEx.Equal("127.0.0.3", second.Ip, "Second datagram should retain its IP.");
            AssertEx.Equal(2000, second.Port, "Second datagram should retain its port.");
            AssertEx.SequenceEqual(new byte[] { 2, 3 }, second.Data, "Second datagram should retain its payload.");
            return Task.CompletedTask;
        }
    }
}
