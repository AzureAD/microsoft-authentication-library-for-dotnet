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
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
    public class AdalCacheOperationsTests
    {
        private ITokenCacheInternal _cache;
        private MsalTokenResponse _response;
        private AuthenticationRequestParameters _requestParams;

        [Params(1, 100, 1000)]
        public int TokenCacheSize { get; set; }

        [Params(true, false)]
        public bool EnableAdalCache { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(null, isAdalCacheEnabled: EnableAdalCache);

            _cache = new TokenCache(serviceBundle, false);
            _response = TestConstants.CreateMsalTokenResponse();

            _requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle);
            _requestParams.TenantUpdatedCanonicalAuthority = Authority.CreateAuthorityWithTenant(
                _requestParams.AuthorityInfo,
                TestConstants.Utid);
            _requestParams.Account = new Account(TestConstants.s_userIdentifier, $"1{TestConstants.DisplayableId}", TestConstants.ProductionPrefNetworkEnvironment);

            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefNetworkEnvironment);

            LegacyTokenCacheHelper.PopulateLegacyCache(serviceBundle.DefaultLogger, _cache.LegacyPersistence, TokenCacheSize);
            TokenCacheHelper.AddRefreshTokensToCache(_cache.Accessor, TokenCacheSize);
        }

        [Benchmark(Description = "SaveToken")]
        public async Task<string> SaveTokenResponseTestAsync()
        {
            var result = await _cache.SaveTokenResponseAsync(_requestParams, _response).ConfigureAwait(true);
            return result.Item1.ClientId;
        }

        [Benchmark(Description = "FindToken")]
        public async Task<string> FindRefreshTokenTestAsync()
        {
            var result = await _cache.FindRefreshTokenAsync(_requestParams).ConfigureAwait(true);
            return result?.ClientId;
        }

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
