// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
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
                BenchmarkSwitcher.FromTypes(new[] {
                    typeof(AcquireTokenForClientCacheTests),
                    typeof(AcquireTokenForOboCacheTests),
                    typeof(TokenCacheTests),
                    typeof(AcquireTokenNoCacheTests),
            }).RunAll(
#if DEBUG
                    new DebugInProcessConfig()
                        .WithOptions(ConfigOptions.DontOverwriteResults) // When running manually locally
#else
                    DefaultConfig.Instance
#endif
                        .WithOptions(ConfigOptions.DisableLogFile)
                        .WithOptions(ConfigOptions.StopOnFirstError)
                        .WithOptions(ConfigOptions.JoinSummary)
                        .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Method))
                        .HideColumns(Column.UnrollFactor, Column.Type, Column.InvocationCount, Column.Error, Column.StdDev, Column.Median, Column.Job)
                        .AddDiagnoser(MemoryDiagnoser.Default) // https://benchmarkdotnet.org/articles/configs/diagnosers.html
                        //.AddDiagnoser(new EtwProfiler()) // https://adamsitnik.com/ETW-Profiler/
                .AddJob(
                    Job.Default
                        .WithId("Job-PerfTests")));
            }
            catch (Exception ex)
            {
                Logger.Log("Error running performance tests.");
                Logger.Log(ex.ToString());
                throw;
            }

            Logger.Log("Completed running performance tests.");
        }
    }

    public static class Logger
    {
        private const string LogPrefix = "[Test.Performance]";
        public static void Log(string message) => Console.WriteLine($"{LogPrefix} {message}");
    }
}
