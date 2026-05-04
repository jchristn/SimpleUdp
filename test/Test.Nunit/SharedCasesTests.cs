namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    [TestFixture]
    [NonParallelizable]
    public class CommonApiTests
    {
        public static IEnumerable Cases => new TouchstoneTestCaseSource(new[] { CommonTestSuite.Create() });

        [TestCaseSource(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class DatagramModelTests
    {
        public static IEnumerable Cases => new TouchstoneTestCaseSource(new[] { DatagramTestSuite.Create() });

        [TestCaseSource(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class EndpointMetadataModelTests
    {
        public static IEnumerable Cases => new TouchstoneTestCaseSource(new[] { EndpointMetadataTestSuite.Create() });

        [TestCaseSource(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class UdpEndpointApiTests
    {
        public static IEnumerable Cases => new TouchstoneTestCaseSource(new[] { UdpEndpointTestSuite.Create() });

        [TestCaseSource(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
