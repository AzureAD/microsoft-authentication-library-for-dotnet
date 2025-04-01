// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SmileTestRunner
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.Error.WriteLine("Error: Please provide a test case url");
                    return 1;
                }

                string testcaseUrl = args[0];
                var executor = new SmileTestExecutor(testcaseUrl);
                var results = await executor.RunTestAsync().ConfigureAwait(false);

                // Output results in a more structured format
                Console.WriteLine($"Test results for: {testcaseUrl}");
                Console.WriteLine($"Total steps executed: {results.Count}");

                for (int i = 0; i < results.Count; i++)
                {
                    string resultStatus = results[i] ? "Passed" : "Failed";
                    Console.WriteLine($"\nStep {i + 1} result: {resultStatus}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error executing test: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }
    }
}
