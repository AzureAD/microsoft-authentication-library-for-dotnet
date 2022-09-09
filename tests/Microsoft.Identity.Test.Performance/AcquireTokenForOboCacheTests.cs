// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
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
    /// Used to test the performance of acquiring tokens using token cache with different amount of items.
    /// </summary>
    /// <remarks>
    /// For OBO user cache, the partition key is
    /// AT, RT: user assertion hash.
    /// IDT, Accounts: home account ID.
    /// </remarks>
    [MinColumn, MaxColumn]
    public class AcquireTokenForOboCacheTests
    {
        private readonly string _tenantPrefix = "l6a331n5-4fh7-7788-a78a-96f19f5d7a73";
        private readonly string _scopePrefix = "https://resource.com/.default";
        private ConfidentialClientApplication _cca;
        private InMemoryCache _serializationCache;
        private string _scope;
        private string _tenantId;
        private UserAssertion _userAssertion;

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

        [ParamsAllValues]
        public bool EnableCacheSerialization { get; set; }

        //[Params(false)]
        public bool UseMicrosoftIdentityWebCache { get; set; }

        // If the tokens are saved with different tenants.
        // This results in ID tokens and accounts having multiple tenant profiles.
        public bool IsMultiTenant { get; set; } = false;

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
                    _serializationCache = new InMemoryCache(_cca.UserTokenCache);
                }
            }

            await PopulateUserCacheAsync(CacheSize.TotalUsers, CacheSize.TokensPerUser, EnableCacheSerialization).ConfigureAwait(false);

            _userAssertion = new UserAssertion($"{TestConstants.DefaultAccessToken}0");
            _scope = $"{_scopePrefix}0";
            _tenantId = IsMultiTenant ? $"{_tenantPrefix}0" : _tenantPrefix;
        }

        [Benchmark(Description = PerfConstants.AcquireTokenForObo)]
        [BenchmarkCategory("With cache")]
        public async Task<AuthenticationResult> AcquireTokenOnBehalfOf_TestAsync()
        {
            return await _cca.AcquireTokenOnBehalfOf(new[] { _scope }, _userAssertion)
                .WithTenantId(_tenantId)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        private async Task PopulateUserCacheAsync(int totalUsers, int tokensPerUser, bool enableCacheSerialization)
        {
            for (int user = 0; user < totalUsers; user++)
            {
                string userAssertionHash = new UserAssertion($"{TestConstants.DefaultAccessToken}{user}").AssertionHash;
                string homeAccountId = $"{user}.{_tenantPrefix}";

                for (int token = 0; token < tokensPerUser; token++)
                {
                    string tenant = IsMultiTenant ? $"{_tenantPrefix}{token}" : _tenantPrefix;
                    string scope = $"{_scopePrefix}{token}";

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
                        uid: user.ToString());
                    _cca.UserTokenCacheInternal.Accessor.SaveIdToken(idtItem);

                    MsalAccountCacheItem accItem = TokenCacheHelper.CreateAccountItem(tenant, homeAccountId);
                    _cca.UserTokenCacheInternal.Accessor.SaveAccount(accItem);
                }

                if (enableCacheSerialization)
                {
                    var args = new TokenCacheNotificationArgs(
                        _cca.UserTokenCacheInternal,
                         _cca.AppConfig.ClientId,
                         account: null,
                         hasStateChanged: true,
                         isApplicationCache: false,
                         suggestedCacheKey: userAssertionHash,
                         hasTokens: true,
                         suggestedCacheExpiry: null,
                         cancellationToken: CancellationToken.None);
                    await _cca.UserTokenCacheInternal.OnAfterAccessAsync(args).ConfigureAwait(false);
                    _cca.UserTokenCacheInternal.Accessor.Clear();
                }
            }
        }
    }
}
