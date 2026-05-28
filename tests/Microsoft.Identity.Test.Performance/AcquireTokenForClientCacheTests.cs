// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Performance.Helpers;
using Microsoft.Identity.Test.Unit;
#if USE_IDENTITY_WEB
using Microsoft.Identity.Web;
#endif

namespace Microsoft.Identity.Test.Performance
{
    /// <summary>
    /// Used to test the performance of acquiring tokens using token cache with different number of items.
    /// </summary>
    /// <remarks>
    /// For app cache, the number of partitions is the number of tenants.
    /// </remarks>
    [MinColumn, MaxColumn]
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
        public (int TotalTenants, int TokensPerTenant) CacheSize { get; set; }

        // By default, benchmarks are run for all combinations of params.
        // This is a workaround to specify the exact param combinations to be used.
        public IEnumerable<(int, int)> CacheSizeSource => new[] {
            (1, 10),
            (10000, 10),
        };

        [ParamsAllValues]
        public bool EnableCacheSerialization { get; set; }
        //[Params(false)]
        public bool UseMicrosoftIdentityWebCache { get; set; }

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
                if (UseMicrosoftIdentityWebCache)
                {
#if USE_IDENTITY_WEB
                    (_cca as IConfidentialClientApplication).AddInMemoryTokenCache();
#endif
                }
                else
                {
                    _serializationCache = new InMemoryCache(_cca.AppTokenCache);
                }
            }

            await PopulateAppCacheAsync(_cca, CacheSize.TotalTenants, CacheSize.TokensPerTenant, EnableCacheSerialization).ConfigureAwait(false);

            _tenantId = $"{_tenantPrefix}0";
            _scope = $"{_scopePrefix}0";
        }

        [Benchmark(Description = PerfConstants.AcquireTokenForClient)]
        [BenchmarkCategory("With cache")]
        public async Task<AuthenticationResult> AcquireTokenForClient_TestAsync()
        {
            return await _cca.AcquireTokenForClient(new[] { _scope })
              .WithTenantId(_tenantId)
              .ExecuteAsync()
              .ConfigureAwait(false);
        }

        [Benchmark(Description = "AcquireTokenForClient + Write (Below Threshold)")]
        public async Task AcquireTokenForClient_WriteBelowThreshold_TestAsync()
        {
            // Acquire a brand new token using a unique scope to trigger a cache write.
            // Under CacheOptions, EnableAppCacheBounding is true with default MaxEntries.
            // Since we use a unique scope/tenant configuration, this simulates the overhead of constant writes
            // when the cache limit is not reached.
            string uniqueTenant = $"{_tenantPrefix}_new_" + System.Guid.NewGuid().ToString("N");
            string uniqueScope = $"{_scopePrefix}_new";

            await _cca.AcquireTokenForClient(new[] { uniqueScope })
              .WithTenantId(uniqueTenant)
              .ExecuteAsync()
              .ConfigureAwait(false);
        }

        [Benchmark(Description = "AcquireTokenForClient + Write (Over Threshold)")]
        public async Task AcquireTokenForClient_WriteOverThreshold_TestAsync()
        {
            // Set up a custom CCA with a very low bounds limit to ensure every single concurrent write pushes
            // the count over the limit and triggers eviction. Measuring un-amortized eviction writes.
            string uniqueTenant = $"{_tenantPrefix}_new_" + System.Guid.NewGuid().ToString("N");
            string uniqueScope = $"{_scopePrefix}_new";

            // Dispatches to a small bounded client to force eviction to run on every loop iteration
            var boundedCca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithLegacyCacheCompatibility(false)
                .WithCacheOptions(new CacheOptions
                {
                    AppCacheMaxEntries = 5 // tiny cap
                })
                .BuildConcrete();

            await boundedCca.AcquireTokenForClient(new[] { uniqueScope })
              .WithTenantId(uniqueTenant)
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
                string key = CacheKeyFactory.GetAppTokenCacheItemKey(_cca.AppConfig.ClientId, $"{_tenantPrefix}{tenant}", "");

                for (int token = 0; token < tokensPerTenant; token++)
                {
                    MsalAccessTokenCacheItem atItem = TokenCacheHelper.CreateAccessTokenItem(
                        scopes: $"{_scopePrefix}{token}",
                        tenant: $"{_tenantPrefix}{tenant}",
                        accessToken: TestConstants.AppAccessToken);

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
