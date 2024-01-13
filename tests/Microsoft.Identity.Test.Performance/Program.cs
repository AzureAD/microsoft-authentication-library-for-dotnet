// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Microsoft.Identity.Test.Performance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Logger.Log("Started running performance tests.");

            try
            {
                var results = BenchmarkSwitcher.FromTypes(new[] {
                    typeof(AcquireTokenForClientCacheTests),
                    typeof(AcquireTokenForOboCacheTests),
                    typeof(TokenCacheTests),
                    typeof(AcquireTokenNoCacheTests),
                }).RunAll(
#if DEBUG
                    new DebugInProcessConfig() // Allows debugging into benchmarks
                        .WithOptions(ConfigOptions.DontOverwriteResults) // When debugging locally
#else
                    DefaultConfig.Instance
                        .AddJob(Job.Default.WithId("Job-PerfTests"))
#endif
                        .WithOptions(ConfigOptions.DisableLogFile)
                        .WithOptions(ConfigOptions.StopOnFirstError)
                        //.WithOptions(ConfigOptions.JoinSummary) // Should be commented for Benchmark GitHub Action to work.
                        .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Method))
                        .HideColumns(Column.UnrollFactor, Column.Type, Column.InvocationCount, Column.Error, Column.StdDev, Column.Median, Column.Job)
                        .AddDiagnoser(MemoryDiagnoser.Default) // https://benchmarkdotnet.org/articles/configs/diagnosers.html
                        //.AddDiagnoser(new EtwProfiler()) // https://adamsitnik.com/ETW-Profiler/
                 , args);

                // If no tests ran for whatever reason, throw an exception to break the build.
                Summary summary = results?.FirstOrDefault();
                BenchmarkReport report = summary?.Reports.FirstOrDefault();
                if (summary == null || report == null || !report.Success)
                {
                    throw new InvalidOperationException("No performance tests ran.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error running performance tests. See logs for details.");
                Logger.LogError(ex.ToString());
                throw;
            }

            Logger.Log("Completed running performance tests.");
        }
    }

    public static class Logger
    {
        private const string LogPrefix = "[Test.Performance]";
        public static void Log(string message, ConsoleColor color = ConsoleColor.Blue)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"{LogPrefix} {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void LogError(string message) => Log(message, ConsoleColor.Red);
    }
}
