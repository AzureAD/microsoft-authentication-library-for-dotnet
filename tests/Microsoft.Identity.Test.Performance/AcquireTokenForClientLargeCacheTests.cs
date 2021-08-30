// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
    /// Testing combinations
    /// Tenants - Tokens - Total tokens
    /// 1 - 10,000 - 10,000
    /// 1 - 100,000 - 100,000
    /// 100 - 10,000 - 1,000,000
    /// 1,000 - 1,000 - 1,000,000
    /// 10,000 - 100 - 1,000,000
    /// </remarks>
    [MeanColumn, StdDevColumn, MedianColumn, MinColumn, MaxColumn]
    public class AcquireTokenForClientLargeCacheTests
    {
        readonly string _tenantPrefix = "tid";
        readonly string _scopePrefix = "scope";
        ConfidentialClientApplication _cca;
        string _tenantId = "tid";
        string _scope = "scope";

        [Params(1000, Priority = 0)]
        public int Tenants { get; set; }

        [Params(1000, Priority = 1)]
        public int TokensPerTenant { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            PopulateAppCache(_cca, Tenants, TokensPerTenant);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            Random random = new Random();
            _tenantId = $"{_tenantPrefix}{random.Next(0, Tenants)}";
            _scope = $"{_scopePrefix}{random.Next(0, TokensPerTenant)}";
        }

        [Benchmark]
        public async Task AcquireTokenForClient_TestAsync()
        {
            var result = await _cca.AcquireTokenForClient(new[] { _scope })
              .WithForceRefresh(false)
              .WithAuthority($"https://login.microsoftonline.com/{_tenantId}")
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
