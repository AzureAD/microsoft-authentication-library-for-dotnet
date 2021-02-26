// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Microsoft.Identity.Test.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<JsonTests>(
                DefaultConfig.Instance
                    .WithOptions(ConfigOptions.DontOverwriteResults)
                    .AddJob(
                        Job.Default
                            .WithId("Job-PerfTests")
                            ));

            Console.ReadKey();
        }
    }
}
