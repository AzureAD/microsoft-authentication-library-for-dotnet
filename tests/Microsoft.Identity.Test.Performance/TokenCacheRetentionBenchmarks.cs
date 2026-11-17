// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Performance
{
    [Config(typeof(RetentionConfig))]
    public class TokenCacheRetentionBenchmarks
    {
        private ITokenCacheSerializer _cache;
        private ITokenCacheSnapshotProvider _provider;
        private byte[] _canonical;
        private object[] _retained;

        public enum CacheRepresentation
        {
            Json,
            Snapshot
        }

        [Params(1, 10, 100)]
        public int TokensPerPartition { get; set; }

        [Params(200)]
        public int Partitions { get; set; }

        [ParamsAllValues]
        public CacheRepresentation Representation { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            if (Partitions <= 0 || TokensPerPartition <= 0)
            {
                throw new InvalidOperationException("Retention benchmarks require positive partition and token counts.");
            }

            var cache = ConfidentialClientApplicationBuilder.Create("snapshot-benchmark").Build().AppTokenCache;
            _cache = (ITokenCacheSerializer)cache;
            _provider = (ITokenCacheSnapshotProvider)cache;
            var seed = new TokenCacheSnapshotBenchmarks { TokensPerPartition = TokensPerPartition };
            _cache.DeserializeMsalV3(seed.CreatePartition("11111111-1111-1111-1111-111111111111"), true);
            _canonical = _cache.SerializeMsalV3();
            _provider.RestoreSnapshot(null);
            RetainPartitions();
        }

        [Benchmark]
        public void RetainPartitions()
        {
            _retained = new object[Partitions];
            for (int i = 0; i < _retained.Length; i++)
            {
                if (Representation == CacheRepresentation.Snapshot)
                {
                    // Parse independently so partitions do not share token strings.
                    _cache.DeserializeMsalV3(_canonical, true);
                    _retained[i] = _provider.CreateSnapshot();
                }
                else
                {
                    _retained[i] = _canonical.Clone();
                }
            }
            _provider.RestoreSnapshot(null);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            using var process = Process.GetCurrentProcess();
            string resultPath = RetainedMemoryDiagnoser.GetResultPath(process);
            try
            {
                // Only the extra diagnostic run requests this post-timing measurement.
                if (File.Exists(resultPath))
                {
                    var samples = new double[3];
                    for (int i = 0; i < samples.Length; i++)
                    {
                        _retained = null;
                        long before = GetLiveHeapBytes();
                        RetainPartitions();
                        long after = GetLiveHeapBytes();
                        GC.KeepAlive(_retained);
                        samples[i] = (after - before) / (double)Partitions;
                    }
                    Array.Sort(samples);
                    if (samples[1] <= 0)
                    {
                        throw new InvalidOperationException("Retained heap measurement must be positive.");
                    }
                    File.WriteAllText(resultPath, samples[1].ToString("R", CultureInfo.InvariantCulture));
                }
            }
            finally
            {
                _retained = null;
                _provider.RestoreSnapshot(null);
            }
        }

        private static long GetLiveHeapBytes()
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            var info = GC.GetGCMemoryInfo();
            return info.HeapSizeBytes - info.FragmentedBytes;
        }

        public sealed class RetentionConfig : ManualConfig
        {
            public RetentionConfig()
            {
                AddDiagnoser(new RetainedMemoryDiagnoser());
            }
        }
    }
}
