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
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-last-colon", "ParseIpPort splits on the final colon", ParseIpPortSplitsOnTheFinalColonAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-ipv6-shaped", "ParseIpPort preserves IPv6-shaped hosts before the final colon", ParseIpPortPreservesIpv6ShapedHostsAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-empty-host", "ParseIpPort allows an empty host component", ParseIpPortAllowsEmptyHostComponentAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-whitespace-preserved", "ParseIpPort preserves host whitespace and parses port whitespace", ParseIpPortPreservesHostWhitespaceAndParsesPortWhitespaceAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-missing-port", "ParseIpPort leaves missing port unresolved", ParseIpPortLeavesMissingPortUnresolvedAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-empty-port", "ParseIpPort rejects an empty port component", ParseIpPortRejectsEmptyPortComponentAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-nonnumeric-port", "ParseIpPort rejects a nonnumeric port component", ParseIpPortRejectsNonnumericPortComponentAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-overflow-port", "ParseIpPort rejects ports outside Int32 range", ParseIpPortRejectsPortsOutsideInt32RangeAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-negative-port", "ParseIpPort parses negative ports without range validation", ParseIpPortParsesNegativePortsWithoutRangeValidationAsync),
                    TouchstoneDescriptorFactory.Case(SuiteId, "parse-above-udp-range", "ParseIpPort parses ports above UDP range without range validation", ParseIpPortParsesPortsAboveUdpRangeWithoutRangeValidationAsync),
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

        private static Task ParseIpPortSplitsOnTheFinalColonAsync()
        {
            Common.ParseIpPort("host:with:colons:1234", out string ip, out int port);
            AssertEx.Equal("host:with:colons", ip, "ParseIpPort should use the final colon as the separator.");
            AssertEx.Equal(1234, port, "ParseIpPort should parse the last token as the port.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortPreservesIpv6ShapedHostsAsync()
        {
            Common.ParseIpPort("::1:5000", out string ip, out int port);
            AssertEx.Equal("::1", ip, "ParseIpPort should preserve the prefix before the final colon.");
            AssertEx.Equal(5000, port, "ParseIpPort should parse the trailing IPv6-shaped port.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortAllowsEmptyHostComponentAsync()
        {
            Common.ParseIpPort(":6000", out string ip, out int port);
            AssertEx.Equal(String.Empty, ip, "ParseIpPort should return an empty host when the separator is first.");
            AssertEx.Equal(6000, port, "ParseIpPort should still parse the port.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortPreservesHostWhitespaceAndParsesPortWhitespaceAsync()
        {
            Common.ParseIpPort(" 127.0.0.1 : 42 ", out string ip, out int port);
            AssertEx.Equal(" 127.0.0.1 ", ip, "ParseIpPort should not trim the host component.");
            AssertEx.Equal(42, port, "Convert.ToInt32 should parse a port with surrounding whitespace.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortLeavesMissingPortUnresolvedAsync()
        {
            Common.ParseIpPort("127.0.0.1", out string ip, out int port);
            AssertEx.Null(ip, "ParseIpPort should leave the IP null when no port separator exists.");
            AssertEx.Equal(-1, port, "ParseIpPort should leave the port at -1 when parsing fails.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortRejectsEmptyPortComponentAsync()
        {
            AssertEx.Throws<FormatException>(() => Common.ParseIpPort("127.0.0.1:", out _, out _), "ParseIpPort should reject an empty port token.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortRejectsNonnumericPortComponentAsync()
        {
            AssertEx.Throws<FormatException>(() => Common.ParseIpPort("127.0.0.1:not-a-port", out _, out _), "ParseIpPort should reject nonnumeric ports.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortRejectsPortsOutsideInt32RangeAsync()
        {
            AssertEx.Throws<OverflowException>(() => Common.ParseIpPort("127.0.0.1:2147483648", out _, out _), "ParseIpPort should reject Int32 overflow.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortParsesNegativePortsWithoutRangeValidationAsync()
        {
            Common.ParseIpPort("127.0.0.1:-2", out string ip, out int port);
            AssertEx.Equal("127.0.0.1", ip, "ParseIpPort should still split a negative-port endpoint.");
            AssertEx.Equal(-2, port, "ParseIpPort should not apply UDP port range validation.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortParsesPortsAboveUdpRangeWithoutRangeValidationAsync()
        {
            Common.ParseIpPort("127.0.0.1:65536", out string ip, out int port);
            AssertEx.Equal("127.0.0.1", ip, "ParseIpPort should still split an above-range endpoint.");
            AssertEx.Equal(65536, port, "ParseIpPort should not apply UDP port range validation.");
            return Task.CompletedTask;
        }

        private static Task ParseIpPortRejectsNullInputAsync()
        {
            AssertEx.Throws<ArgumentNullException>(() => Common.ParseIpPort(null!, out _, out _), "ParseIpPort should reject null input.");
            return Task.CompletedTask;
        }
    }
}
