namespace Test.Xunit
{
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;

    public class CommonApiTests
    {
        public static TouchstoneTheoryData Cases => new TouchstoneTheoryData(CommonTestSuite.Create());

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }

    public class DatagramModelTests
    {
        public static TouchstoneTheoryData Cases => new TouchstoneTheoryData(DatagramTestSuite.Create());

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }

    public class EndpointMetadataModelTests
    {
        public static TouchstoneTheoryData Cases => new TouchstoneTheoryData(EndpointMetadataTestSuite.Create());

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }

    public class UdpEndpointApiTests
    {
        public static TouchstoneTheoryData Cases => new TouchstoneTheoryData(UdpEndpointTestSuite.Create());

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
