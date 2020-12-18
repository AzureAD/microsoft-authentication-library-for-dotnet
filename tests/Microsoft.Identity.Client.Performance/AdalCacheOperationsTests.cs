// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class AdalCacheOperationsTests
    {
        private ITokenCacheInternal _cache;
        private MsalTokenResponse _response;
        private AuthenticationRequestParameters _requestParams;

        [Params(1, 100, 1000)]
        public int TokenCacheSize { get; set; }

        [GlobalSetup(Targets = new[] { 
            nameof(SaveTokenResponse_EnabledAdalCache_TestAsync), 
            nameof(FindRefreshToken_EnabledAdalCache_TestAsync) })]
        public void GlobalSetup_EnabledAdalCache()
        {
            GlobalSetup(true);
        }

        [GlobalSetup(Targets = new[] { 
            nameof(SaveTokenResponse_DisabledAdalCache_TestAsync), 
            nameof(FindRefreshToken_DisabledAdalCache_TestAsync) })]
        public void GlobalSetup_DisabledAdalCache()
        {
            GlobalSetup(false);
        }

        private void GlobalSetup(bool enableAdalCache)
        {
            var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(null, isAdalCacheEnabled: enableAdalCache);

            _cache = new TokenCache(serviceBundle, false);
            _response = TestConstants.CreateMsalTokenResponse();

            _requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle);
            _requestParams.TenantUpdatedCanonicalAuthority = Authority.CreateAuthorityWithTenant(
                _requestParams.AuthorityInfo,
                TestConstants.Utid);

            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            TokenCacheHelper.AddRefreshTokensToCache(_cache.Accessor, TokenCacheSize);
        }

        #region SaveToken
        [BenchmarkCategory("SaveTokenResponse"), Benchmark(Baseline = true)]
        public async Task<string> SaveTokenResponse_EnabledAdalCache_TestAsync()
        {
            return await SaveTokenResponse_TestAsync().ConfigureAwait(true);
        }

        [BenchmarkCategory("SaveTokenResponse"), Benchmark]
        public async Task<string> SaveTokenResponse_DisabledAdalCache_TestAsync()
        {
            return await SaveTokenResponse_TestAsync().ConfigureAwait(true);
        }

        private async Task<string> SaveTokenResponse_TestAsync()
        {
            var result = await _cache.SaveTokenResponseAsync(_requestParams, _response).ConfigureAwait(true);
            return result.Item1.ClientId;
        }
        #endregion

        #region GetRefreshToken
        [BenchmarkCategory("GetRefreshToken"), Benchmark(Baseline = true)]
        public async Task<string> FindRefreshToken_EnabledAdalCache_TestAsync()
        {
            return await FindRefreshToken_TestAsync().ConfigureAwait(true);
        }

        [BenchmarkCategory("GetRefreshToken"), Benchmark]
        public async Task<string> FindRefreshToken_DisabledAdalCache_TestAsync()
        {
            return await FindRefreshToken_TestAsync().ConfigureAwait(true);
        }

        private async Task<string> FindRefreshToken_TestAsync()
        {
            var result = await _cache.FindRefreshTokenAsync(_requestParams).ConfigureAwait(true);
            return result.ClientId;
        }
        #endregion

        private void AddHostToInstanceCache(IServiceBundle serviceBundle, string host)
        {
            (serviceBundle.InstanceDiscoveryManager as InstanceDiscoveryManager)
                .AddTestValueToStaticProvider(
                    host,
                    new InstanceDiscoveryMetadataEntry
                    {
                        PreferredNetwork = host,
                        PreferredCache = host,
                        Aliases = new string[]
                        {
                            host
                        }
                    });
        }
    }
}
