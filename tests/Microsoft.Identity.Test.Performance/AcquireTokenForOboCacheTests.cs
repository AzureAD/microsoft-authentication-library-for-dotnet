// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
    /// For OBO user cache, the partition key is
    /// AT, RT: user assertion hash.
    /// IDT, Accounts: home account ID.
    /// 
    /// Testing combinations
    /// Users - Tokens - Total tokens
    /// 1 - 10,000 - 10,000
    /// 100 - 10,000 - 1,000,000
    /// 1,000 - 1,000 - 1,000,000
    /// </remarks>
    [MeanColumn, StdDevColumn, MedianColumn, MinColumn, MaxColumn]
    public class AcquireTokenForOboCacheTests
    {
        readonly string _tenantPrefix = "l6a331n5-4fh7-7788-a78a-";
        readonly string _scopePrefix = "https://resource.com/.default";
        ConfidentialClientApplication _cca;
        string _scope;
        string _authority;
        UserAssertion _userAssertion;

        [ParamsSource(nameof(CacheSizeSource), Priority = 0)]
        public (int TotalUsers, int TokensPerUser) CacheSize { get; set; }

        // By default, benchmarks are run for all combinations of params.
        // This is a workaround to specify the exact param combinations to be used.
        public IEnumerable<(int, int)> CacheSizeSource => new[] {
            (1, 10000),
            (100, 10000),
            (1000, 1000),
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
        public void IterationSetup_AcquireTokenOnBehalfOf()
        {
            Random random = new Random();
            _userAssertion = new UserAssertion($"{TestConstants.DefaultAccessToken}{random.Next(0, CacheSize.TotalUsers)}");
            string id = random.Next(0, CacheSize.TokensPerUser).ToString();
            _scope = $"{_scopePrefix}{id}";
            _authority = IsMultiTenant ?
                $"https://{TestConstants.ProductionPrefNetworkEnvironment}/{_tenantPrefix}{id}" :
                $"https://{TestConstants.ProductionPrefNetworkEnvironment}/{_tenantPrefix}";
        }

        [Benchmark(Description = "AcquireTokenForOBO")]
        [BenchmarkCategory("With cache")]
        public async Task<AuthenticationResult> AcquireTokenOnBehalfOf_TestAsync()
        {
            return await _cca.AcquireTokenOnBehalfOf(new[] { _scope }, _userAssertion)
                .WithAuthority(_authority)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        private void PopulateUserCache(int totalUsers, int tokensPerUser)
        {
            for (int user = 0; user < totalUsers; user++)
            {
                for (int token = 0; token < tokensPerUser; token++)
                {
                    string userAssertionHash = new UserAssertion($"{TestConstants.DefaultAccessToken}{user}").AssertionHash;
                    string homeAccountId = $"{user}.{_tenantPrefix}";
                    string tenant = IsMultiTenant ? $"{_tenantPrefix}{token}" : _tenantPrefix;
                    string scope = $"{_scopePrefix}{token}";

                    MsalAccessTokenCacheItem atItem = TokenCacheHelper.CreateAccessTokenItem(scope, tenant, homeAccountId, oboCacheKey: userAssertionHash);
                    _cca.UserTokenCacheInternal.Accessor.SaveAccessToken(atItem);

                    MsalRefreshTokenCacheItem rtItem = TokenCacheHelper.CreateRefreshTokenItem(userAssertionHash, homeAccountId);
                    _cca.UserTokenCacheInternal.Accessor.SaveRefreshToken(rtItem);

                    MsalIdTokenCacheItem idtItem = TokenCacheHelper.CreateIdTokenCacheItem(tenant, homeAccountId, user.ToString());
                    _cca.UserTokenCacheInternal.Accessor.SaveIdToken(idtItem);

                    MsalAccountCacheItem accItem = TokenCacheHelper.CreateAccountItem(tenant, homeAccountId);
                    _cca.UserTokenCacheInternal.Accessor.SaveAccount(accItem);
                }
            }
        }
    }
}
