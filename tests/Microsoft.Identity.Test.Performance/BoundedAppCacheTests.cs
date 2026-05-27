// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn]
    public class BoundedAppCacheTests
    {
        private InMemoryPartitionedAppTokenCacheAccessor _unboundedAccessor;
        private InMemoryPartitionedAppTokenCacheAccessor _boundedAccessor;
        private InMemoryPartitionedAppTokenCacheAccessor _boundedFullAccessor;
        private InMemoryPartitionedAppTokenCacheAccessor _stressAccessor;

        private MsalAccessTokenCacheItem _insertItem;

        // Simple fixed-length token string used instead of a real JWT to keep the benchmark
        // focused on cache mechanics (insert, evict, count) rather than string allocation.
        // Length matches the ~2 KB average real-world access token size.
        private string _syntheticToken;

        [GlobalSetup]
        public void Setup()
        {
            var logger = new NullLogger();

            // Fixed 2 KB string — same byte footprint as a real access token but zero
            // allocation overhead from JWT parsing or base64 encoding.
            _syntheticToken = new string('a', 2048);

            // 1. Unbounded (baseline) — 500k entries, no eviction policy.
            var unboundedOpts = new CacheOptions { AppCacheMaxEntries = 0 };
            _unboundedAccessor = new InMemoryPartitionedAppTokenCacheAccessor(logger, unboundedOpts);
            PopulateAccessor(_unboundedAccessor, 500000);

            // 2. Bounded, not full — 500k entries with a 1M ceiling so eviction never fires.
            var boundedOpts = new CacheOptions { AppCacheMaxEntries = 1000000 };
            _boundedAccessor = new InMemoryPartitionedAppTokenCacheAccessor(logger, boundedOpts);
            PopulateAccessor(_boundedAccessor, 500000);

            // 3. Bounded, full — 500k entries at exactly the 500k ceiling.
            //    Every save past this point will trigger EvictDown() to trim back to 475k (95%).
            var boundedFullOpts = new CacheOptions { AppCacheMaxEntries = 500000 };
            _boundedFullAccessor = new InMemoryPartitionedAppTokenCacheAccessor(logger, boundedFullOpts);
            PopulateAccessor(_boundedFullAccessor, 500000);

            // 4. Stress accessor — tiny max (100) pre-filled to capacity so every single save
            //    triggers eviction. Measures the worst-case per-save cost of the eviction path.
            var stressOpts = new CacheOptions { AppCacheMaxEntries = 100 };
            _stressAccessor = new InMemoryPartitionedAppTokenCacheAccessor(logger, stressOpts);
            PopulateAccessor(_stressAccessor, 100);

            _insertItem = TokenCacheHelper.CreateAccessTokenItem(
                scopes: "scope_new",
                tenant: "tenant_new",
                accessToken: _syntheticToken);
        }

        private void PopulateAccessor(InMemoryPartitionedAppTokenCacheAccessor accessor, int count)
        {
            for (int i = 0; i < count; i++)
            {
                accessor.SaveAccessToken(TokenCacheHelper.CreateAccessTokenItem(
                    scopes: "scope" + i,
                    tenant: "tenant" + i,
                    accessToken: _syntheticToken));
            }
        }

        [Benchmark(Baseline = true)]
        public void SaveToken_Unbounded()
        {
            // We use a unique key each time to force an Add, not an Update
            _insertItem.ClientId = Guid.NewGuid().ToString(); 
            _unboundedAccessor.SaveAccessToken(_insertItem);
        }

        [Benchmark]
        public void SaveToken_Bounded_BelowThreshold()
        {
            _insertItem.ClientId = Guid.NewGuid().ToString(); 
            _boundedAccessor.SaveAccessToken(_insertItem);
        }

        [Benchmark]
        public void SaveToken_Bounded_OverThreshold_TriggersEviction()
        {
            // The cache is at capacity (500k) and bounded at 500k.
            // Each save that pushes past the limit triggers EvictDown() which trims to 475k (95%).
            // Subsequent 25k saves won't trigger eviction; then it fires again.
            // This gives the amortized real-world cost of the eviction path.
            _insertItem.ClientId = Guid.NewGuid().ToString();
            _boundedFullAccessor.SaveAccessToken(_insertItem);
        }

        [Benchmark]
        public void SaveToken_Bounded_StressEviction()
        {
            // The stress accessor is permanently at its 100-entry limit.
            // Every single save here crosses the threshold and triggers EvictDown(),
            // measuring the worst-case per-call overhead of eviction with no amortisation.
            _insertItem.ClientId = Guid.NewGuid().ToString();
            _stressAccessor.SaveAccessToken(_insertItem);
        }
    }
}
