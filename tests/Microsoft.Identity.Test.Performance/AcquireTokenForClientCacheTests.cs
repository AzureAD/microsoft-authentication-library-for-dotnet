// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Test.Performance.Helpers;
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
    public class AcquireTokenForClientCacheTests
    {
        private readonly string _tenantPrefix = "l6a331n5-4fh7-7788-a78a-96f19f5d7a73";
        private readonly string _scopePrefix = "https://resource.com/.default";
        private ConfidentialClientApplication _cca;
        private InMemoryCache _serializationCache;
        private string _tenantId;
        private string _scope;

        // i.e. (partitions, tokens per partition)
        [ParamsSource(nameof(CacheSizeSource))]
        public (int TotalTenants, int TokensPerTenant) CacheSize { get; set; } = (3, 2);

        // By default, benchmarks are run for all combinations of params.
        // This is a workaround to specify the exact param combinations to be used.
        public IEnumerable<(int, int)> CacheSizeSource => new[] {
            //(1, 10000),
            //(100, 10000),
            //(1000, 1000),
            (1, 1),
            (1, 10),
            (1, 1000),
        };

        [ParamsAllValues]
        public bool EnableCacheSerialization { get; set; } = true;

        [GlobalSetup]
        public async Task GlobalSetupAsync()
        {
            _cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithLegacyCacheCompatibility(false)
                .BuildConcrete();

            if (EnableCacheSerialization)
            {
                _serializationCache = new InMemoryCache(_cca.AppTokenCache);
            }

            await PopulateAppCacheAsync(_cca, CacheSize.TotalTenants, CacheSize.TokensPerTenant, EnableCacheSerialization).ConfigureAwait(false);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            Random random = new Random();
            _tenantId = $"{_tenantPrefix}{random.Next(0, CacheSize.TotalTenants)}";
            _scope = $"{_scopePrefix}{random.Next(0, CacheSize.TokensPerTenant)}";
        }

        [Benchmark(Description = "AcquireTokenForClient")]
        [BenchmarkCategory("With cache")]
        public async Task<AuthenticationResult> AcquireTokenForClient_TestAsync()
        {
            return await _cca.AcquireTokenForClient(new[] { _scope })
              .WithAuthority($"https://login.microsoftonline.com/{_tenantId}")
              .ExecuteAsync()
              .ConfigureAwait(false);
        }

        /// <summary>
        /// Create a fake token and save into the internal cache.
        /// If cache serialization is enabled, call an event handler to serialize current cache state into external cache,
        /// then clear the internal cache before new token is inserted.
        /// </summary>
        private async Task PopulateAppCacheAsync(ConfidentialClientApplication cca, int totalTenants, int tokensPerTenant, bool enableCacheSerialization)
        {
            for (int tenant = 0; tenant < totalTenants; tenant++)
            {
                string key = CacheKeyFactory.GetClientCredentialKey(_cca.AppConfig.ClientId, $"{_tenantPrefix}{tenant}", "");

                for (int token = 0; token < tokensPerTenant; token++)
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

                if (enableCacheSerialization)
                {
                    var args = new TokenCacheNotificationArgs(
                         cca.AppTokenCacheInternal,
                         cca.AppConfig.ClientId,
                         account: null,
                         hasStateChanged: true,
                         isApplicationCache: true,
                         suggestedCacheKey: key,
                         hasTokens: true,
                         suggestedCacheExpiry: null,
                         cancellationToken: CancellationToken.None);
                    await cca.AppTokenCacheInternal.OnAfterAccessAsync(args).ConfigureAwait(false);
                    cca.AppTokenCacheInternal.Accessor.Clear();
                }
            }
        }
    }
}
