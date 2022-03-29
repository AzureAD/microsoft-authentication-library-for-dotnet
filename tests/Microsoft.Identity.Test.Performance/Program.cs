// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
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
            }).RunAll(DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableLogFile)
                //.WithOptions(ConfigOptions.DontOverwriteResults) // Uncomment when running manually
                .AddDiagnoser(MemoryDiagnoser.Default) // https://benchmarkdotnet.org/articles/configs/diagnosers.html
                                                       //.AddDiagnoser(new EtwProfiler()) // https://adamsitnik.com/ETW-Profiler/
                .AddJob(
                    Job.Default
                        .WithId("Job-PerfTests")));
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }

            Logger.Log("Completed running performance tests.");
        }
    }

    public static class Logger
    {
        private const string LogPrefix = "[Microsoft.Identity.Test.Performance]";
        public static void Log(string message) => Console.WriteLine($"{LogPrefix} {message}");
    }
}
