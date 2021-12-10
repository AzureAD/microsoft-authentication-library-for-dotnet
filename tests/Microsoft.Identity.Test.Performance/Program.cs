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

            BenchmarkSwitcher.FromTypes(new[] {
                typeof(TokenCacheTests),
                typeof(AcquireTokenForClientCacheTests),
                typeof(AcquireTokenForOboCacheTests),
                }).RunAll(DefaultConfig.Instance
                        .WithOptions(ConfigOptions.DisableLogFile)
                        // .WithOptions(ConfigOptions.DontOverwriteResults) // Helpful when running manually
                        .AddDiagnoser(MemoryDiagnoser.Default)
                        .AddJob(
                            Job.Default
                                .WithId("Job-PerfTests")));

            Logger.Log("Completed running performance tests.");
        }
    }

    public static class Logger
    {
        private const string LogPrefix = "[Microsoft.Identity.Test.Performance]";
        public static void Log(string message) => Console.WriteLine($"{LogPrefix} {message}");
    }
}
