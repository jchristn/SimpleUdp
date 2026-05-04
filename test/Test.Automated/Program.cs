namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Test.Shared;
    using Touchstone.Cli;
    using Touchstone.Core;

    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
            {
                PrintUsage();
                return 0;
            }

            (List<string> suiteFilters, string? resultsPath) options;

            try
            {
                options = ParseArgs(args);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Error: " + e.Message);
                Console.WriteLine();
                PrintUsage();
                return 1;
            }

            IReadOnlyList<TestSuiteDescriptor> suites = SimpleUdpTestCatalog.GetSuites();

            if (options.suiteFilters.Count > 0)
            {
                suites = suites
                    .Where(suite => options.suiteFilters.Any(filter =>
                        suite.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                        || suite.SuiteId.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (suites.Count == 0)
                {
                    Console.WriteLine("No suites matched: " + string.Join(", ", options.suiteFilters));
                    return 1;
                }
            }

            return await ConsoleRunner.RunAsync(suites, sink: null, resultsPath: options.resultsPath).ConfigureAwait(false);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project test/Test.Automated --framework net10.0");
            Console.WriteLine("  dotnet run --project test/Test.Automated --framework net10.0 -- --suite UdpEndpoint");
            Console.WriteLine("  dotnet run --project test/Test.Automated --framework net10.0 -- --results results.json");
        }

        private static (List<string> suiteFilters, string? resultsPath) ParseArgs(string[] args)
        {
            List<string> suites = new List<string>();
            string? resultsPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--suite", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length) throw new ArgumentException("--suite requires a value.");
                    suites.Add(args[++i]);
                    continue;
                }

                if (args[i].Equals("--results", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length) throw new ArgumentException("--results requires a value.");
                    resultsPath = args[++i];
                }
            }

            return (suites, resultsPath);
        }
    }
}
