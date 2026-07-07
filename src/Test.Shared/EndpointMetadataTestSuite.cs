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
                    TouchstoneDescriptorFactory.Case(SuiteId, "ip-setter-preserves-previous-value", "Ip setter preserves the previous value after invalid assignments", IpSetterPreservesPreviousValueAfterInvalidAssignmentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "ip-setter-allows-whitespace", "Ip setter allows whitespace-only values", IpSetterAllowsWhitespaceOnlyValuesAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "port-setter-validation", "Port setter enforces valid range", PortSetterEnforcesValidRangeAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "port-setter-boundaries", "Port setter accepts UDP boundary values", PortSetterAcceptsUdpBoundaryValuesAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "port-setter-preserves-previous-value", "Port setter preserves the previous value after invalid assignments", PortSetterPreservesPreviousValueAfterInvalidAssignmentsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "instances-independent", "EndpointMetadata instances keep independent state", EndpointMetadataInstancesKeepIndependentStateAsync)
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

        private static Task IpSetterPreservesPreviousValueAfterInvalidAssignmentsAsync()
        {
            EndpointMetadata metadata = new EndpointMetadata();
            metadata.Ip = "10.1.2.3";

            AssertEx.Throws<ArgumentNullException>(() => metadata.Ip = null!, "EndpointMetadata.Ip should reject null.");
            AssertEx.Throws<ArgumentNullException>(() => metadata.Ip = String.Empty, "EndpointMetadata.Ip should reject empty input.");
            AssertEx.Equal("10.1.2.3", metadata.Ip, "Failed IP assignments should not mutate the current value.");
            return Task.CompletedTask;
        }

        private static Task IpSetterAllowsWhitespaceOnlyValuesAsync()
        {
            EndpointMetadata metadata = new EndpointMetadata();
            metadata.Ip = "   ";
            AssertEx.Equal("   ", metadata.Ip, "EndpointMetadata.Ip currently validates empty strings but not whitespace-only strings.");
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

        private static Task PortSetterAcceptsUdpBoundaryValuesAsync()
        {
            EndpointMetadata metadata = new EndpointMetadata();
            metadata.Port = 0;
            AssertEx.Equal(0, metadata.Port, "EndpointMetadata.Port should accept zero.");

            metadata.Port = 65535;
            AssertEx.Equal(65535, metadata.Port, "EndpointMetadata.Port should accept the maximum UDP port.");
            return Task.CompletedTask;
        }

        private static Task PortSetterPreservesPreviousValueAfterInvalidAssignmentsAsync()
        {
            EndpointMetadata metadata = new EndpointMetadata();
            metadata.Port = 9999;

            AssertEx.Throws<ArgumentOutOfRangeException>(() => metadata.Port = -1, "EndpointMetadata.Port should reject negative values.");
            AssertEx.Throws<ArgumentOutOfRangeException>(() => metadata.Port = 65536, "EndpointMetadata.Port should reject above-range values.");
            AssertEx.Equal(9999, metadata.Port, "Failed port assignments should not mutate the current value.");
            return Task.CompletedTask;
        }

        private static Task EndpointMetadataInstancesKeepIndependentStateAsync()
        {
            EndpointMetadata first = new EndpointMetadata { Ip = "127.0.0.2", Port = 1000 };
            EndpointMetadata second = new EndpointMetadata { Ip = "127.0.0.3", Port = 2000 };

            AssertEx.Equal("127.0.0.2", first.Ip, "First metadata instance should retain its IP.");
            AssertEx.Equal(1000, first.Port, "First metadata instance should retain its port.");
            AssertEx.Equal("127.0.0.3", second.Ip, "Second metadata instance should retain its IP.");
            AssertEx.Equal(2000, second.Port, "Second metadata instance should retain its port.");
            return Task.CompletedTask;
        }
    }
}
