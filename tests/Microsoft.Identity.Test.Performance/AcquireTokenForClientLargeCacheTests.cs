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
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    /// <summary>
    /// Used to test the performance of token cache with large amount of items.
    /// </summary>
    public class AcquireTokenForClientLargeCacheTests
    {
        ConfidentialClientApplication _ccaTokensDifferByScope;
        ConfidentialClientApplication _ccaTokensDifferByTenant;

        [Params(10000)]
        public int TokenCacheSize { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _ccaTokensDifferByScope = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(new Uri(TestConstants.AuthorityTestTenant))
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();
          
            PopulateAppCache(_ccaTokensDifferByScope, TokenDifference.ByScope, TokenCacheSize);

            _ccaTokensDifferByTenant = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)                
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();
        }

        /// <summary>
        /// Scenario where app token cache has a large number of tokes, each token for a different tenant. This is a common
        /// multi-tenant scenario for which MSAL is optimized. In this case, the cache operations are O(1).
        /// </summary>
        /// <returns></returns>
        [Benchmark(Description = "Different tenants - O(1)")]
        public async Task AcquireTokenForClient_DifferentTenants_TestAsync()
        {
            Random random = new Random();
            string tenant = $"tid_{random.Next(0, TokenCacheSize)}";

            await _ccaTokensDifferByTenant.AcquireTokenForClient(new[] { "scope" })
              .WithForceRefresh(false)
              .WithAuthority($"https://login.microsoftonline.com/{tenant}")
              .ExecuteAsync()
              .ConfigureAwait(false);
        }

        /// <summary>
        /// This is an uncommon scenario for which MSAL is not optimized - the app token cache has a large number of
        /// tokens, each token for different scopes. In this case, the cache operations are slow, at least O(n).
        /// </summary>        
        [Benchmark(Description = "Different scopes - O(n)")]
        public async Task AcquireTokenForClient_DifferentScopes_TestAsync()
        {
            Random random = new Random();
            string scope = $"scope_{random.Next(0, TokenCacheSize)}";

            await _ccaTokensDifferByScope.AcquireTokenForClient(new[] { scope })
              .WithForceRefresh(false)
              .WithAuthority($"https://login.microsoftonline.com/tid")
              .ExecuteAsync()
              .ConfigureAwait(false);
        }

        private enum TokenDifference
        {
            ByScope,
            ByTenant
        }

        private void PopulateAppCache(ConfidentialClientApplication cca, TokenDifference tokenDifference, int size)
        {
            Dictionary<string, InMemoryTokenCacheAccessor> accessors = new Dictionary<string, InMemoryTokenCacheAccessor>();
            string key = "";
            for (int i = 0; i < size; i++)
            {
                string tenantId = "tid";
                string scope = "scope";

                switch (tokenDifference)
                {
                    case TokenDifference.ByScope:
                        scope = $"scope_{i}";
                        break;
                    case TokenDifference.ByTenant:
                        tenantId = $"tid_{i}";
                        break;
                    default:
                        throw new NotImplementedException();
                }

                MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                  TestConstants.ProductionPrefCacheEnvironment,
                  TestConstants.ClientId,
                  scope,
                  tenantId,
                  "",
                  new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                  new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                  null,
                  null);

                key = SuggestedWebCacheKeyFactory.GetClientCredentialKey(atItem.ClientId, atItem.TenantId);
                InMemoryTokenCacheAccessor accessor;
                if (!accessors.TryGetValue(key, out accessor))
                {
                    accessor = new InMemoryTokenCacheAccessor(new NullLogger());
                    accessors[key] = accessor;
                }

                accessor.SaveAccessToken(atItem);

                if (tokenDifference == TokenDifference.ByTenant || (tokenDifference == TokenDifference.ByScope && i == size - 1))
                {
                    byte[] bytes = new TokenCacheJsonSerializer(accessor).Serialize(null);
                    cca.InMemoryPartitionedCacheSerializer.CachePartition[key] = bytes;
                }
            }

            // force a cache read, otherwise MSAL won't have the tokens in memory
            // force a cache read
            var args = new TokenCacheNotificationArgs(
                                     cca.AppTokenCacheInternal,
                                     cca.AppConfig.ClientId,
                                     null,
                                     hasStateChanged: false,
                                     true,
                                     hasTokens: true,
                                     cancellationToken: CancellationToken.None,
                                     suggestedCacheKey: key);
            cca.AppTokenCacheInternal.OnBeforeAccessAsync(args).GetAwaiter().GetResult();
        }
    }
}
