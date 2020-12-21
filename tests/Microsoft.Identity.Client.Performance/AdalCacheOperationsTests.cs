// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
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
        private RequestContext _requestContext;
        private readonly Consumer _consumer = new Consumer();

        [Params(1, 100, 1000)]
        public int TokenCacheSize { get; set; }

        [ParamsAllValues]
        public bool EnableAdalCache { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(null, isAdalCacheEnabled: EnableAdalCache);

            _requestContext = new RequestContext(serviceBundle, Guid.NewGuid());
            _cache = new TokenCache(serviceBundle, false);
            _response = TestConstants.CreateMsalTokenResponse();

            _requestParams = TestCommon.CreateAuthenticationRequestParameters(serviceBundle);
            _requestParams.TenantUpdatedCanonicalAuthority = Authority.CreateAuthorityWithTenant(
                _requestParams.AuthorityInfo,
                TestConstants.Utid);
            _requestParams.Account = new Account(TestConstants.s_userIdentifier, $"1{TestConstants.DisplayableId}", TestConstants.ProductionPrefNetworkEnvironment);

            AddHostToInstanceCache(serviceBundle, TestConstants.ProductionPrefCacheEnvironment);

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

        [Benchmark(Description = "GetAllUsers")]
        public async Task GetAllAdalUsersTestAsync()
        {
            var result = await _cache.GetAccountsAsync(_requestParams).ConfigureAwait(true);
            result.Consume(_consumer);
        }

        [Benchmark(Description = "RemoveUser")]
        public async Task RemoveAdalUserTestAsync()
        {
            await _cache.RemoveAccountAsync(_requestParams.Account, _requestContext).ConfigureAwait(true);
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
