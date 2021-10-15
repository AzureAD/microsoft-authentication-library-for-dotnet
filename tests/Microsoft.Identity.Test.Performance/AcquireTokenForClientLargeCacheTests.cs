// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    /// <summary>
    /// Used to test the performance of token cache with large amount of items.
    /// </summary>
    /// <remarks>
    /// For app cache, the number of partitions is the number of tenants
    /// 
    /// Testing combinations
    /// Tenants (partitions) - Tokens per partition - Total tokens
    /// 1 - 10,000 - 10,000
    /// 1 - 100,000 - 100,000
    /// 100 - 10,000 - 1,000,000
    /// 1,000 - 1,000 - 1,000,000
    /// 10,000 - 100 - 1,000,000
    /// </remarks>
    [MeanColumn, StdDevColumn, MedianColumn, MinColumn, MaxColumn]
    public class AcquireTokenForClientLargeCacheTests
    {
        readonly string _tenantPrefix = TestConstants.Utid;
        readonly string _scopePrefix = "scope";
        ConfidentialClientApplication _cca;
        string _tenantId;
        string _scope;

        [ParamsSource(nameof(CacheSizeSource))]
        public (int Tenants, int TokensPerTenant) CacheSize { get; set; }

        // By default, benchmarks are run for all combinations of params.
        // This is a workaround to specify the exact param combinations to be used.
        public IEnumerable<(int, int)> CacheSizeSource => new[] {
            (1, 10000),
            (1, 100000),
            (100, 10000),
            (1000, 1000),
            (10000, 100) };

        [GlobalSetup]
        public void GlobalSetup()
        {
            _cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            PopulateAppCache(_cca, CacheSize.Tenants, CacheSize.TokensPerTenant);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            Random random = new Random();
            _tenantId = $"{_tenantPrefix}{random.Next(0, CacheSize.Tenants)}";
            _scope = $"{_scopePrefix}{random.Next(0, CacheSize.TokensPerTenant)}";
        }

        [Benchmark]
        public async Task<AuthenticationResult> AcquireTokenForClient_TestAsync()
        {
            return await _cca.AcquireTokenForClient(new[] { _scope })
              .WithTenantId(_tenantId)
              .ExecuteAsync()
              .ConfigureAwait(false);
        }

        private void PopulateAppCache(ConfidentialClientApplication cca, int tenantsNumber, int tokensNumber)
        {
            for (int tenant = 0; tenant < tenantsNumber; tenant++)
            {
                for (int token = 0; token < tokensNumber; token++)
                {
                    MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                          TestConstants.ProductionPrefCacheEnvironment,
                          TestConstants.ClientId,
                          $"{_scopePrefix}{token}",
                          $"{_tenantPrefix}{tenant}",
                          "",
                          new DateTimeOffset(DateTime.UtcNow),
                          new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                          new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                          null,
                          null);
                    cca.AppTokenCacheInternal.Accessor.SaveAccessToken(atItem);
                }
            }
        }
    }
}
