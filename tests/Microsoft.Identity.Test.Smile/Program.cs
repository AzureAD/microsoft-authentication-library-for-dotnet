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
                // For now we assume that if it ends with .json, it is a batch file
                if (testcaseUrl.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var executor = new SmileTestBatchExecutor(testcaseUrl);
                    var results = await executor.RunTestsAsync().ConfigureAwait(false);

                    Console.WriteLine("Summary of test results:");
                    // Loop through the results and print them
                    foreach (var result in results)
                    {
                        string status = result.Value ? "Passed" : "Failed";
                        Console.WriteLine($"Test case: {result.Key} - Status: {status}");
                    }
                    // Return 0 if all tests passed, otherwise return 1
                    return results.Values.All(r => r) ? 0 : 1;
                }
                else
                {
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
                    // Return 0 if all steps passed, otherwise return 1
                    return results.All(r => r) ? 0 : 1;
                }
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
