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
                    TouchstoneDescriptorFactory.Case(SuiteId, "port-setter-validation", "Port setter enforces valid range", PortSetterEnforcesValidRangeAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "data-null-to-empty", "Data setter converts null to empty array", DataSetterConvertsNullToEmptyArrayAsync)
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

        private static Task PortSetterEnforcesValidRangeAsync()
        {
            Datagram datagram = new Datagram();
            AssertEx.Throws<ArgumentOutOfRangeException>(() => datagram.Port = -1, "Datagram.Port should reject negative values.");
            AssertEx.Throws<ArgumentOutOfRangeException>(() => datagram.Port = 65536, "Datagram.Port should reject values above 65535.");
            datagram.Port = 4321;
            AssertEx.Equal(4321, datagram.Port, "Datagram.Port should accept valid values.");
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
    }
}
