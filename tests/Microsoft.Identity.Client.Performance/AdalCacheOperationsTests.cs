using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    public class AdalCacheOperationsTests
    {
        private ITokenCacheInternal _cache;
        private MsalTokenResponse _response;
        private AuthenticationRequestParameters _requestParams;

        [Params(1, 100, 1000)]
        public int TokenCacheSize { get; set; }

        [GlobalSetup(Target = nameof(SaveTokenResponse_EnabledAdalCache_TestAsync))]
        public void GlobalSetup_EnabledAdalCache()
        {
            GlobalSetup(true);
        }

        [GlobalSetup(Target = nameof(SaveTokenResponse_DisabledAdalCache_TestAsync))]
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

        [Benchmark(Baseline = true)]
        public async Task<string> SaveTokenResponse_EnabledAdalCache_TestAsync()
        {
            return await SaveTokenResponse_TestAsync().ConfigureAwait(true);
        }

        [Benchmark]
        public async Task<string> SaveTokenResponse_DisabledAdalCache_TestAsync()
        {
            return await SaveTokenResponse_TestAsync().ConfigureAwait(true);
        }

        private async Task<string> SaveTokenResponse_TestAsync()
        {
            var result = await _cache.SaveTokenResponseAsync(_requestParams, _response).ConfigureAwait(true);
            return result.Item1.ClientId;
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
