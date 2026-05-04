namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SimpleUdp;
    using Touchstone.Core;

    public static class CommonTestSuite
    {
        public const string SuiteId = "common-api";

        public const string SuiteName = "Common API";

        public static TestSuiteDescriptor Create()
        {
            return TouchstoneDescriptorFactory.Suite(
                SuiteId,
                SuiteName,
                new List<TestCaseDescriptor>
                {
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-valid-endpoint", "ParseIpPort splits a valid endpoint", ParseIpPortSplitsValidEndpointAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-missing-port", "ParseIpPort leaves missing port unresolved", ParseIpPortLeavesMissingPortUnresolvedAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-null-input", "ParseIpPort rejects null input", ParseIpPortRejectsNullInputAsync)
                });
        }

        private static Task ParseIpPortSplitsValidEndpointAsync()
        {
            Common.ParseIpPort("127.0.0.1:9000", out string ip, out int port);
            AssertEx.Equal("127.0.0.1", ip, "ParseIpPort should preserve the IP.");
            AssertEx.Equal(9000, port, "ParseIpPort should parse the port.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortLeavesMissingPortUnresolvedAsync()
        {
            Common.ParseIpPort("127.0.0.1", out string ip, out int port);
            AssertEx.Null(ip, "ParseIpPort should leave the IP null when no port separator exists.");
            AssertEx.Equal(-1, port, "ParseIpPort should leave the port at -1 when parsing fails.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortRejectsNullInputAsync()
        {
            AssertEx.Throws<ArgumentNullException>(() => Common.ParseIpPort(null!, out _, out _), "ParseIpPort should reject null input.");
            return Task.CompletedTask;
        }
    }
}
