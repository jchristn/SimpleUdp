namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SimpleUdp;
    using Touchstone.Core;

    public static class EndpointMetadataTestSuite
    {
        public const string SuiteId = "endpoint-metadata-model";

        public const string SuiteName = "Endpoint Metadata Model";

        public static TestSuiteDescriptor Create()
        {
            return TouchstoneDescriptorFactory.Suite(
                SuiteId,
                SuiteName,
                new List<TestCaseDescriptor>
                {
                    TouchstoneDescriptorFactory.Case(SuiteId, "default-constructor", "Default constructor initializes safe defaults", DefaultConstructorInitializesSafeDefaultsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "ip-setter-validation", "Ip setter rejects null or empty values", IpSetterRejectsNullOrEmptyValuesAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "port-setter-validation", "Port setter enforces valid range", PortSetterEnforcesValidRangeAsync)
                });
        }

        private static Task DefaultConstructorInitializesSafeDefaultsAsync()
        {
            EndpointMetadata metadata = new EndpointMetadata();
            AssertEx.Equal("127.0.0.1", metadata.Ip, "EndpointMetadata default IP should be loopback.");
            AssertEx.Equal(0, metadata.Port, "EndpointMetadata default port should be zero.");
            return Task.CompletedTask;
        }

        private static Task IpSetterRejectsNullOrEmptyValuesAsync()
        {
            EndpointMetadata metadata = new EndpointMetadata();
            AssertEx.Throws<ArgumentNullException>(() => metadata.Ip = null!, "EndpointMetadata.Ip should reject null.");
            AssertEx.Throws<ArgumentNullException>(() => metadata.Ip = String.Empty, "EndpointMetadata.Ip should reject empty input.");
            metadata.Ip = "10.0.0.5";
            AssertEx.Equal("10.0.0.5", metadata.Ip, "EndpointMetadata.Ip should accept a valid address string.");
            return Task.CompletedTask;
        }

        private static Task PortSetterEnforcesValidRangeAsync()
        {
            EndpointMetadata metadata = new EndpointMetadata();
            AssertEx.Throws<ArgumentOutOfRangeException>(() => metadata.Port = -1, "EndpointMetadata.Port should reject negative values.");
            AssertEx.Throws<ArgumentOutOfRangeException>(() => metadata.Port = 65536, "EndpointMetadata.Port should reject values above 65535.");
            metadata.Port = 12345;
            AssertEx.Equal(12345, metadata.Port, "EndpointMetadata.Port should accept valid values.");
            return Task.CompletedTask;
        }
    }
}
