namespace Test.Shared
{
    using System.Collections.Generic;
    using System.Linq;
    using Touchstone.Core;

    public static class SimpleUdpTestCatalog
    {
        public static IReadOnlyList<TestSuiteDescriptor> GetSuites()
        {
            return new[]
            {
                CommonTestSuite.Create(),
                DatagramTestSuite.Create(),
                EndpointMetadataTestSuite.Create(),
                UdpEndpointTestSuite.Create()
            };
        }

        public static IEnumerable<TestCaseDescriptor> GetAllCases()
        {
            return GetSuites().SelectMany(suite => suite.Cases);
        }
    }
}
