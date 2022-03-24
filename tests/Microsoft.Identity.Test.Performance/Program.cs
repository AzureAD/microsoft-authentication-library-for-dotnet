// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Microsoft.Identity.Test.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromTypes(new[] {
                typeof(AcquireTokenForClientCacheTests),
                typeof(AcquireTokenForOboCacheTests),
                typeof(TokenCacheTests),
            }).RunAll(DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableLogFile)
                .WithOptions(ConfigOptions.JoinSummary)
                .WithOptions(ConfigOptions.DontOverwriteResults) // Uncomment when running manually
                .AddDiagnoser(MemoryDiagnoser.Default) // https://benchmarkdotnet.org/articles/configs/diagnosers.html
                                                       //.AddDiagnoser(new EtwProfiler()) // https://adamsitnik.com/ETW-Profiler/
                .AddJob(
                    Job.Default
                        .WithId("Job-PerfTests")));

            Console.ReadKey();
        }
    }
}
