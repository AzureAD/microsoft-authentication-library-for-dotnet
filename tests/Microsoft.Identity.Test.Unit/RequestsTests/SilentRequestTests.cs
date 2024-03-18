// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Internal.Broker;
using NSubstitute;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests.Silent;
using System.Data;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class SilentRequestTests : TestBase
    {
        [TestMethod]
        public void ConstructorTests()
        {
            using (var harness = new MockHttpTestHarness(TestConstants.AuthorityHomeTenant))
            {
                var parameters = harness.CreateRequestParams(harness.Cache, null);
                var silentParameters = new AcquireTokenSilentParameters();
                var request = new SilentRequest(harness.ServiceBundle, parameters, silentParameters);
                Assert.IsNotNull(request);

                parameters.Account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null);
                request = new SilentRequest(harness.ServiceBundle, parameters, silentParameters);
                Assert.IsNotNull(request);

                request = new SilentRequest(harness.ServiceBundle, parameters, silentParameters);
                Assert.IsNotNull(request);
            }
        }

        [TestMethod]
        public void ExpiredTokenRefreshFlowTest()
        {
            using (var harness = new MockHttpTestHarness(TestConstants.AuthorityHomeTenant))
            {
                TokenCacheHelper.PopulateCache(harness.Cache.Accessor);
                var parameters = harness.CreateRequestParams(
                    harness.Cache,
                    null,
                    TestConstants.ExtraQueryParameters,
                    TestConstants.Claims,
                    authorityOverride: AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityHomeTenant, false));

                var silentParameters = new AcquireTokenSilentParameters()
                {
                    Account = new Account(TestConstants.HomeAccountId, TestConstants.DisplayableId, TestConstants.ProductionPrefCacheEnvironment),
                };

                TokenCacheHelper.ExpireAllAccessTokens(harness.Cache);

                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        ExpectedQueryParams = TestConstants.ExtraQueryParameters,
                        ExpectedPostData = new Dictionary<string, string>() { { OAuth2Parameter.Claims, TestConstants.Claims } }
                    });

                var request = new SilentRequest(harness.ServiceBundle, parameters, silentParameters);

                Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
                var result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
            }
        }

        [TestMethod]
        [DataRow(true, true)] //Broker is configured by user and is installed.
        [DataRow(false, true)] //Broker is not configured by user but is installed
        [DataRow(true, false)] //Broker is configured by user but is not installed
        [DataRow(false, false)] //Broker is not configured by user and is not installed
        public async Task BrokerSilentRequestLocalCacheTestAsync(bool brokerConfiguredByUser, bool brokerIsInstalledAndInvokable)
        {
            string brokerID = "Broker@broker.com";
            using (var harness = new MockHttpTestHarness(TestConstants.AuthorityHomeTenant))
            {
                // result will be from the local cache
                TokenCacheHelper.PopulateCache(harness.Cache.Accessor,
                    TestConstants.Uid,
                    TestConstants.Utid,
                    TestConstants.ClientId,
                    TestConstants.ProductionPrefCacheEnvironment,
                    brokerID);

                IBroker mockBroker = Substitute.For<IBroker>();
                mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).ReturnsForAnyArgs(brokerIsInstalledAndInvokable);

                harness.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

                var parameters = harness.CreateRequestParams(
                    harness.Cache,
                    null,
                    TestConstants.ExtraQueryParameters,
                    null,
                    authorityOverride: AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, false));
                parameters.AppConfig.IsBrokerEnabled = brokerConfiguredByUser;

                var silentParameters = new AcquireTokenSilentParameters()
                {
                    Account = new Account(TestConstants.HomeAccountId, TestConstants.DisplayableId, TestConstants.ProductionPrefCacheEnvironment),
                };

                var request = new SilentRequest(harness.ServiceBundle, parameters, silentParameters);

                var result = await request.RunAsync(default).ConfigureAwait(false);

                // even if broker is configured by user but not installed, the result will be from the local cache
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
                Assert.AreEqual(brokerID, result.Account.Username);
                if (!brokerConfiguredByUser)
                {
                    await mockBroker.DidNotReceiveWithAnyArgs().AcquireTokenSilentAsync(null, null).ConfigureAwait(false);
                }
            }
        }

      

        [TestMethod]
        public void RequestParamsNullArg()
        {
            using (var harness = new MockHttpTestHarness(TestConstants.AuthorityHomeTenant))
            {
                AssertException.Throws<ArgumentNullException>(() => harness.CreateRequestParams(
                   null,
                   TestConstants.s_scope,
                   authorityOverride: AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityHomeTenant, false)));
            }
        }

        [TestMethod]
        public void SilentRefreshFailedNoCacheItemFoundTest()
        {
            using (var harness = new MockHttpTestHarness(TestConstants.AuthorityHomeTenant))
            {
                var parameters = harness.CreateRequestParams(
                    harness.Cache,
                    ScopeHelper.CreateScopeSet(
                        new[]
                        {
                            "some-scope1",
                            "some-scope2"
                        }),
                    authorityOverride: AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityHomeTenant, false));

                var silentParameters = new AcquireTokenSilentParameters()
                {
                    Account = new Account(TestConstants.HomeAccountId, TestConstants.DisplayableId, TestConstants.ProductionPrefCacheEnvironment),
                };

                try
                {
                    var request = new SilentRequest(harness.ServiceBundle, parameters, silentParameters);
                    Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
                    var authenticationResult = task.Result;
                    Assert.Fail("MsalUiRequiredException should be thrown here");
                }
                catch (AggregateException ae)
                {
                    var exc = ae.InnerException as MsalUiRequiredException;
                    Assert.IsNotNull(exc, "Actual exception type is " + ae.InnerException.GetType());
                    Assert.AreEqual(MsalError.NoTokensFoundError, exc.ErrorCode);
                }
            }
        }

        [TestMethod]
        public async Task IosBrokerSilentRequestLocalCacheTestAsync()
        {
            string brokerID = "Broker@broker.com";
            IBroker mockBroker = Microsoft.Identity.Test.Unit.BrokerTests.BrokerTests.CreateBroker(typeof(IosBrokerMock));
            IPlatformProxy platformProxy = Substitute.For<IPlatformProxy>();
            platformProxy.CanBrokerSupportSilentAuth().Returns(false); //Ios sets this to false so local cache should be used
            platformProxy.CreateTokenCacheAccessor(Arg.Any<CacheOptions>()).Returns(PlatformProxyFactory.CreatePlatformProxy(null).CreateTokenCacheAccessor(null));
            platformProxy.CreateBroker(Arg.Any<ApplicationConfiguration>(), Arg.Any<CoreUIParent>()).ReturnsForAnyArgs(mockBroker);

            using (var harness = new MockHttpTestHarness(TestConstants.AuthorityHomeTenant, platformProxy))
            {
                // result will be from the local cache
                TokenCacheHelper.PopulateCache(harness.Cache.Accessor,
                    TestConstants.Uid,
                    TestConstants.Utid,
                    TestConstants.ClientId,
                    TestConstants.ProductionPrefCacheEnvironment,
                    brokerID);

                harness.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

                var parameters = harness.CreateRequestParams(
                    harness.Cache,
                    null,
                    TestConstants.ExtraQueryParameters,
                    null,
                    authorityOverride: AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, false));
                parameters.AppConfig.IsBrokerEnabled = true;

                var silentParameters = new AcquireTokenSilentParameters()
                {
                    Account = new Account(TestConstants.HomeAccountId, TestConstants.DisplayableId, TestConstants.ProductionPrefCacheEnvironment),
                };

                var request = new SilentRequest(harness.ServiceBundle, parameters, silentParameters);

                var result = await request.RunAsync(default).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
                Assert.AreEqual(brokerID, result.Account.Username);
            }
        }

        internal class MockHttpTestHarness : IDisposable
        {
            private readonly MockHttpAndServiceBundle _mockHttpAndServiceBundle;

            public MockHttpTestHarness(string authorityUri, IPlatformProxy platformProxy = null)
            {
                _mockHttpAndServiceBundle = new MockHttpAndServiceBundle(platformProxy: platformProxy);
                Authority = Authority.CreateAuthority(authorityUri);
                Cache = new TokenCache(ServiceBundle, false);
            }

            public IServiceBundle ServiceBundle => _mockHttpAndServiceBundle.ServiceBundle;
            public MockHttpManager HttpManager => _mockHttpAndServiceBundle.HttpManager;
            public Authority Authority { get; }
            public ITokenCacheInternal Cache { get; }

            /// <inheritdoc/>
            public void Dispose()
            {
                _mockHttpAndServiceBundle.Dispose();
            }

            public AuthenticationRequestParameters CreateRequestParams(
                ITokenCacheInternal cache,
                HashSet<string> scopes,
                IDictionary<string, string> extraQueryParams = null,
                string claims = null,
                AuthorityInfo authorityOverride = null)
            {
                var commonParameters = new AcquireTokenCommonParameters
                {
                    Scopes = scopes ?? TestConstants.s_scope,
                    ExtraQueryParameters = extraQueryParams,
                    Claims = claims,
                    AuthorityOverride = authorityOverride
                };

                var requestContext = new RequestContext(ServiceBundle, Guid.NewGuid());

                var authority = Microsoft.Identity.Client.Instance.Authority.CreateAuthorityForRequestAsync(
                    requestContext,
                    commonParameters.AuthorityOverride).Result;

                var parameters = new AuthenticationRequestParameters(
                    ServiceBundle,
                    cache,
                    commonParameters,
                    requestContext,
                    authority)
                {
                    Account = new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null),
                };
                return parameters;
            }
        }
    }
}
