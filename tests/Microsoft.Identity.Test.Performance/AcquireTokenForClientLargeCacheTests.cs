// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    /// <summary>
    /// Used to test the performance of token cache with large amount of items.
    /// </summary>
    public class AcquireTokenForClientLargeCacheTests
    {
        private AcquireTokenForClientParameterBuilder _acquireTokenForClientBuilder;

        [Params(1000)]
        public int TokenCacheSize { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(new Uri(TestConstants.AuthorityTestTenant))
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            var inMemoryTokenCache = new InMemoryTokenCache();
            inMemoryTokenCache.Bind(cca.AppTokenCache);

            PopulateAppCache(cca, TokenCacheSize);

            _acquireTokenForClientBuilder = cca
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithForceRefresh(false);
        }

        private void PopulateAppCache(ConfidentialClientApplication cca, int size)
        {
            for (int i = 0; i < size; i++)
            {
                InMemoryTokenCacheAccessor accessor = new InMemoryTokenCacheAccessor(new NullLogger());
                string tenantId = $"tid{i}";

                MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                  TestConstants.ProductionPrefCacheEnvironment,
                  TestConstants.ClientId,
                  "scope",
                  tenantId,
                  "",
                  new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                  new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                  null,
                  null);

                accessor.SaveAccessToken(atItem);

                string key = SuggestedWebCacheKeyFactory.GetClientCredentialKey(atItem.ClientId, atItem.TenantId);
                byte[] bytes = new TokenCacheJsonSerializer(accessor).Serialize(null);
                cca.InMemoryPartitionedCacheSerializer.CachePartition[key] = bytes;
            }

            // force a cache read, otherwise MSAL won't have the tokens in memory
        }

        [Benchmark]
        public async Task<AuthenticationResult> AcquireTokenForClientTestAsync()
        {
            return await _acquireTokenForClientBuilder
                .ExecuteAsync(System.Threading.CancellationToken.None).ConfigureAwait(true);
        }
    }
}
