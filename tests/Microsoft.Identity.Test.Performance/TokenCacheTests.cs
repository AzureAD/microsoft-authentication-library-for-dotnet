// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Performance.Helpers;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    /// <summary>
    /// Used to test the performance of acquiring tokens using token cache with different amount of items.
    /// </summary>
    /// <remarks>
    /// For non-OBO user cache, the partition key is home account ID.
    /// </remarks>
    [MinColumn, MaxColumn]
    public class TokenCacheTests
    {
        private readonly string _tenantPrefix = "l6a331n5-4fh7-7788-a78a-96f19f5d7a73";
        private readonly string _scopePrefix = "https://resource.com/.default";
        private ConfidentialClientApplication _cca;
        private string _scope;
        private string _tenantId;
        private IAccount _account;

        // i.e. (partitions, tokens per partition)
        [ParamsSource(nameof(CacheSizeSource), Priority = 0)]
        public (int TotalUsers, int TokensPerUser) CacheSize { get; set; }

        // By default, benchmarks are run for all combinations of params.
        // This is a workaround to specify the exact param combinations to be used.
        public IEnumerable<(int, int)> CacheSizeSource => new[] {
            (1, 10),
            (1, 1000),
            (10000, 10),
        };

        // If the tokens are saved with different tenants.
        // This results in ID tokens and accounts having multiple tenant profiles.
        public bool IsMultiTenant { get; set; } = false;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithLegacyCacheCompatibility(false)
                .BuildConcrete();

            PopulateUserCache(CacheSize.TotalUsers, CacheSize.TokensPerUser);

            _account = new Account($"0.{_tenantPrefix}", TestConstants.DisplayableId, TestConstants.ProductionPrefCacheEnvironment);
            _scope = $"{_scopePrefix}{0}";
            _tenantId = IsMultiTenant ? $"{_tenantPrefix}0" : _tenantPrefix;
        }

        [Benchmark(Description = PerfConstants.AcquireTokenSilent)]
        [BenchmarkCategory("With cache")]
        public async Task<AuthenticationResult> AcquireTokenSilent_TestAsync()
        {
            return await _cca.AcquireTokenSilent(new string[] { _scope }, _account)
                .WithTenantId(_tenantId)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        [Benchmark(Description = PerfConstants.GetAccount)]
        [BenchmarkCategory("With cache")]
        public async Task<IAccount> GetAccountAsync_TestAsync()
        {
            return await _cca.GetAccountAsync(_account.HomeAccountId.Identifier)
                .ConfigureAwait(false);
        }

        //GetAccounts is only available in PCA and is not high-perf scenario.
        //[Benchmark(Description = "GetAccounts")]
        //[BenchmarkCategory("With cache")]
        public async Task<IAccount> GetAccountsAsync_TestAsync()
        {
            return (await _cca.GetAccountsAsync()
                .ConfigureAwait(false)).FirstOrDefault();
        }

        [Benchmark(Description = PerfConstants.RemoveAccount)]
        [BenchmarkCategory("With cache")]
        public async Task RemoveAccountAsync_TestAsync()
        {
            await _cca.RemoveAsync(_account)
                .ConfigureAwait(false);
        }

        [IterationCleanup(Target = nameof(RemoveAccountAsync_TestAsync))]
        public void IterationCleanup_RemoveAccount()
        {
            PopulationPartition("0", CacheSize.TokensPerUser.ToString());
        }

        private void PopulateUserCache(int totalUsers, int tokensPerUser)
        {
            for (int userId = 0; userId < totalUsers; userId++)
            {
                for (int tokenId = 0; tokenId < tokensPerUser; tokenId++)
                {
                    InsertCacheItem(userId.ToString(), tokenId.ToString());
                }
            }
        }

        // Inserts cache items into a partition
        private void PopulationPartition(string userId, string tokensPerUser)
        {
            for (int tokenId = 0; tokenId < int.Parse(tokensPerUser); tokenId++)
            {
                InsertCacheItem(userId, tokenId.ToString());
            }
        }

        // Inserts a single AT, RT, IDT, Acc cache item
        private void InsertCacheItem(string userId, string tokenId)
        {
            string userAssertionHash = null;
            string tenant = IsMultiTenant ? $"{_tenantPrefix}{tokenId}" : _tenantPrefix;
            string homeAccountId = $"{userId}.{tenant}";
            string scope = $"{_scopePrefix}{tokenId}";

            MsalAccessTokenCacheItem atItem = TokenCacheHelper.CreateAccessTokenItem(
                scope,
                tenant,
                homeAccountId,
                oboCacheKey: userAssertionHash,
                accessToken: TestConstants.UserAccessToken);
            _cca.UserTokenCacheInternal.Accessor.SaveAccessToken(atItem);

            MsalRefreshTokenCacheItem rtItem = TokenCacheHelper.CreateRefreshTokenItem(
                userAssertionHash,
                homeAccountId,
                refreshToken: TestConstants.RefreshToken);
            _cca.UserTokenCacheInternal.Accessor.SaveRefreshToken(rtItem);

            MsalIdTokenCacheItem idtItem = TokenCacheHelper.CreateIdTokenCacheItem(
                tenant,
                homeAccountId,
                uid: userId);
            _cca.UserTokenCacheInternal.Accessor.SaveIdToken(idtItem);

            MsalAccountCacheItem accItem = TokenCacheHelper.CreateAccountItem(tenant, homeAccountId);
            _cca.UserTokenCacheInternal.Accessor.SaveAccount(accItem);
        }
    }
}
