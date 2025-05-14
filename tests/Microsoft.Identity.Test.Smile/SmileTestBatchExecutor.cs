// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmileTestRunner
{
    /// <summary>
    /// Executes a batch of Smile tests from a JSON file containing a list of test URLs
    /// </summary>
    public class SmileTestBatchExecutor
    {
        private readonly string _batchUrl;
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Creates a new instance of SmileTestBatchExecutor
        /// </summary>
        /// <param name="batchUrl">URL pointing to a JSON file containing test URLs</param>
        public SmileTestBatchExecutor(string batchUrl)
        {
            _batchUrl = batchUrl ?? throw new ArgumentNullException(nameof(batchUrl));
        }

        /// <summary>
        /// Runs all tests specified in the batch file
        /// </summary>
        /// <returns>A dictionary mapping each test URL to its result</returns>
        public async Task<Dictionary<string, bool>> RunTestsAsync()
        {
            var testUrls = await GetTestUrlsAsync().ConfigureAwait(false);
            var results = new Dictionary<string, bool>();

            foreach (string testUrl in testUrls)
            {
                try
                {
                    Console.WriteLine($"Test {testUrl}: Spawning separate process...");
                    var processResult = await RunTestInSeparateProcessAsync(testUrl).ConfigureAwait(false);
                    results[testUrl] = processResult.Success;

                    Console.WriteLine($"Test {testUrl}: {(processResult.Success ? "PASSED" : "FAILED")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing test {testUrl}: {ex.Message}");
                    results[testUrl] = false;
                }
            }

            return results;
        }

        /// <summary>
        /// Retrieves the list of test URLs from the batch file
        /// </summary>
        /// <returns>A list of test URLs</returns>
        private async Task<List<string>> GetTestUrlsAsync()
        {
            if (!Uri.IsWellFormedUriString(_batchUrl, UriKind.Absolute))
            {
                throw new ArgumentException($"Invalid batch URL: {_batchUrl}");
            }

            HttpResponseMessage response = await _httpClient.GetAsync(_batchUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            string jsonContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            
            if (!doc.RootElement.TryGetProperty("testcases", out JsonElement testcasesElement) || 
                testcasesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Batch file does not contain a valid 'testcases' array");
            }

            var testUrls = new List<string>();
            foreach (JsonElement element in testcasesElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    string? url = element.GetString();
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        testUrls.Add(url);
                    }
                }
            }

            return testUrls;
        }

        /// <summary>
        /// Runs a single test in a separate process
        /// </summary>
        /// <param name="testUrl">URL of the test to run</param>
        /// <returns>The result of the test execution</returns>
        private async Task<(bool Success, string Output)> RunTestInSeparateProcessAsync(string testUrl)
        {
            // Create a temporary file to store the test result
            string resultFile = Path.GetTempFileName();
            
            try
            {
                // Create process info
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    // Run the test using the existing Microsoft.Identity.Test.Smile.csproj
                    Arguments = $"run --project \"{Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Microsoft.Identity.Test.Smile.csproj")}\" -- \"{testUrl}\" --output-file \"{resultFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Start the process
                using var process = new Process { StartInfo = startInfo };
                var outputBuilder = new StringBuilder();
                
                process.OutputDataReceived += (sender, args) => 
                {
                    if (args.Data != null)
                    {
                        outputBuilder.AppendLine(args.Data);
                        Console.WriteLine($"[Test Process] {args.Data}");
                    }
                };
                
                process.ErrorDataReceived += (sender, args) => 
                {
                    if (args.Data != null)
                    {
                        outputBuilder.AppendLine($"[ERROR] {args.Data}");
                        Console.WriteLine($"[Test Process Error] {args.Data}");
                    }
                };
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                await process.WaitForExitAsync().ConfigureAwait(false);

                /*
                // Read result from file
                if (File.Exists(resultFile))
                {
                    string resultJson = await File.ReadAllTextAsync(resultFile).ConfigureAwait(false);
                    var result = JsonSerializer.Deserialize<TestResult>(resultJson);
                    return (result?.Success ?? false, outputBuilder.ToString());
                }
                */
                
                return (process.ExitCode == 0, outputBuilder.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running test in separate process: {ex.Message}");
                return (false, ex.ToString());
            }
            finally
            {
                // Clean up temp file
                try { if (File.Exists(resultFile)) File.Delete(resultFile); } catch { }
            }
        }

        private class TestResult
        {
            public bool Success { get; set; }
            public string[] StepResults { get; set; } = Array.Empty<string>();
        }
    }
}
