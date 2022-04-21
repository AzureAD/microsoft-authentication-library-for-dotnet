// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    /// <summary>
    /// Used to test the performance of token cache with large amount of items.
    /// </summary>
    /// <remarks>
    /// For non-OBO user cache, the partition key is home account ID.
    /// 
    /// Testing combinations
    /// Partitions - Tokens per partition - Total tokens
    /// 1 - 10,000 - 10,000
    /// 100 - 10,000 - 1,000,000
    /// 1,000 - 1,000 - 1,000,000
    /// </remarks>
    [MeanColumn, StdDevColumn, MedianColumn, MinColumn, MaxColumn]
    public class TokenCacheTests
    {
        private readonly string _tenantPrefix = "l6a331n5-4fh7-7788-a78a-96f19f5d7a73";
        private readonly string _scopePrefix = "https://resource.com/.default";
        private ConfidentialClientApplication _cca;
        private string _scope;
        private string _authority;
        private IAccount _account;
        private string _tokenId;
        private string _userId;

        // i.e. (partitions, tokens per partition)
        [ParamsSource(nameof(CacheSizeSource), Priority = 0)]
        public (int TotalUsers, int TokensPerUser) CacheSize { get; set; }

        // By default, benchmarks are run for all combinations of params.
        // This is a workaround to specify the exact param combinations to be used.
        public IEnumerable<(int, int)> CacheSizeSource => new[] {
            (1, 10),
            (1, 10000),
            (1000, 10),
            (10000, 10),
            (100000, 10),
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
        }

        [IterationSetup]
        public void IterationSetup_AcquireTokenSilent()
        {
            Random random = new Random();
            _userId = random.Next(0, CacheSize.TotalUsers).ToString();
            _account = new Account($"{_userId}.{_tenantPrefix}", TestConstants.DisplayableId, TestConstants.ProductionPrefCacheEnvironment);
            _tokenId = random.Next(0, CacheSize.TokensPerUser).ToString();
            _scope = $"{_scopePrefix}{_tokenId}";
            _authority = IsMultiTenant ?
                $"https://{TestConstants.ProductionPrefNetworkEnvironment}/{_tenantPrefix}{_tokenId}" :
                $"https://{TestConstants.ProductionPrefNetworkEnvironment}/{_tenantPrefix}";
        }

        [Benchmark(Description = "AcquireTokenSilent")]
        [BenchmarkCategory("With cache")]
        public async Task<AuthenticationResult> AcquireTokenSilent_TestAsync()
        {
            return await _cca.AcquireTokenSilent(new string[] { _scope }, _account)
                .WithAuthority(_authority)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        [Benchmark(Description = "GetAccount")]
        [BenchmarkCategory("With cache")]
        public async Task<IAccount> GetAccountAsync_TestAsync()
        {
            return await _cca.GetAccountAsync(_account.HomeAccountId.Identifier)
                .ConfigureAwait(false);
        }

        [Benchmark(Description = "GetAccounts")]
        [BenchmarkCategory("With cache")]
        public async Task<IAccount> GetAccountsAsync_TestAsync()
        {
            var result = await _cca.GetAccountsAsync()
                .ConfigureAwait(false);
            return result.FirstOrDefault();
        }

        [Benchmark(Description = "RemoveAccount")]
        [BenchmarkCategory("With cache")]
        public async Task RemoveAccountAsync_TestAsync()
        {
            await _cca.RemoveAsync(_account)
                .ConfigureAwait(false);
        }

        [IterationCleanup(Target = nameof(RemoveAccountAsync_TestAsync))]
        public void IterationCleanup_RemoveAccount()
        {
            PopulationPartition(_userId, CacheSize.TokensPerUser.ToString());
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
                InsertCacheItem(userId.ToString(), tokenId.ToString());
            }
        }

        // Inserts a single AT, RT, IDT, Acc cache item
        private void InsertCacheItem(string userId, string tokenId)
        {
            string userAssertionHash = null;
            string homeAccountId = $"{userId}.{TestConstants.Utid}";
            string tenant = IsMultiTenant ? $"{_tenantPrefix}{tokenId}" : _tenantPrefix;
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
                uid: userId,
                idToken: TestConstants.IdToken);
            _cca.UserTokenCacheInternal.Accessor.SaveIdToken(idtItem);

            MsalAccountCacheItem accItem = TokenCacheHelper.CreateAccountItem(tenant, homeAccountId);
            _cca.UserTokenCacheInternal.Accessor.SaveAccount(accItem);
        }
    }
}
