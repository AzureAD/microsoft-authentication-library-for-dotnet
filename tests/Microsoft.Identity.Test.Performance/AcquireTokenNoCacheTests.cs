// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Performance.Helpers;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    /// <summary>
    /// Used to test the performance of acquiring tokens without cache and mocked network calls.
    /// </summary>
    [MinColumn, MaxColumn]
    public class AcquireTokenNoCacheTests
    {
        private ConfidentialClientApplication _cca;
        private MockHttpManager _httpManager;
        private readonly string[] _scope = TestConstants.s_scope.ToArray();
        private readonly UserAssertion _userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

        [GlobalSetup(Target = nameof(AcquireTokenForClient_TestAsync))]
        public void GlobalSetup_ForClient()
        {
            SetupApp(isAppFlow: true);
        }

        [GlobalSetup(Target = nameof(AcquireTokenOnBehalfOf_TestAsync))]
        public void GlobalSetup_ForObo()
        {
            SetupApp(isAppFlow: false);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _httpManager.Dispose();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _cca.AppTokenCacheInternal.Accessor.Clear();
            _cca.UserTokenCacheInternal.Accessor.Clear();
        }

        [Benchmark(Description = PerfConstants.AcquireTokenForClient)]
        [BenchmarkCategory("No cache")]
        public async Task<AuthenticationResult> AcquireTokenForClient_TestAsync()
        {
            return await _cca.AcquireTokenForClient(_scope)
              .ExecuteAsync()
              .ConfigureAwait(false);
        }

        [Benchmark(Description = PerfConstants.AcquireTokenForObo)]
        [BenchmarkCategory("No cache")]
        public async Task<AuthenticationResult> AcquireTokenOnBehalfOf_TestAsync()
        {
            return await _cca.AcquireTokenOnBehalfOf(_scope, _userAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        private void SetupApp(bool isAppFlow)
        {
            Func<MockHttpMessageHandler> messageHandlerFunc;
            if (isAppFlow)
            {
                messageHandlerFunc = () => new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
                };
            }
            else
            {
                messageHandlerFunc = () => new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.Uid, TestConstants.DisplayableId, TestConstants.s_scope.ToArray())
                };
            }

            _httpManager = new MockHttpManager(messageHandlerFunc: messageHandlerFunc);

            _cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityUtidTenant)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithLegacyCacheCompatibility(false)
                .WithHttpManager(_httpManager)
                .BuildConcrete();
            AddHostToInstanceCache(_cca.ServiceBundle, TestConstants.ProductionPrefNetworkEnvironment);

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
