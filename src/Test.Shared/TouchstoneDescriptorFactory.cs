namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Touchstone.Core;

    internal static class TouchstoneDescriptorFactory
    {
        internal static TestCaseDescriptor Case(string suiteId, string caseId, string displayName, Func<Task> executeAsync)
        {
            if (String.IsNullOrWhiteSpace(suiteId)) throw new ArgumentNullException(nameof(suiteId));
            if (String.IsNullOrWhiteSpace(caseId)) throw new ArgumentNullException(nameof(caseId));
            if (String.IsNullOrWhiteSpace(displayName)) throw new ArgumentNullException(nameof(displayName));
            if (executeAsync == null) throw new ArgumentNullException(nameof(executeAsync));

            return new TestCaseDescriptor(
                suiteId: suiteId,
                caseId: caseId,
                displayName: displayName,
                executeAsync: _ => executeAsync(),
                tags: Array.Empty<string>(),
                skip: false,
                skipReason: null);
        }

        internal static TestSuiteDescriptor Suite(string suiteId, string displayName, IReadOnlyList<TestCaseDescriptor> cases)
        {
            if (String.IsNullOrWhiteSpace(suiteId)) throw new ArgumentNullException(nameof(suiteId));
            if (String.IsNullOrWhiteSpace(displayName)) throw new ArgumentNullException(nameof(displayName));
            if (cases == null) throw new ArgumentNullException(nameof(cases));

            return new TestSuiteDescriptor(
                suiteId: suiteId,
                displayName: displayName,
                cases: cases,
                beforeSuiteAsync: null,
                afterSuiteAsync: null);
        }
    }
}
