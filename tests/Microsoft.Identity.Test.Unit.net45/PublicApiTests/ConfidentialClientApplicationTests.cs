// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Microsoft.Identity.Test.Common;

#if !ANDROID && !iOS && !WINDOWS_APP // No Confidential Client
namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    [DeploymentItem(@"Resources\valid.crtfile")]
    [DeploymentItem("Resources\\OpenidConfiguration-QueryParams-B2C.json")]
    public class ConfidentialClientApplicationTests
    {
        private byte[] _serializedCache;
        private TokenCacheHelper _tokenCacheHelper;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
            _tokenCacheHelper = new TokenCacheHelper();
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        [Description("Tests the public interfaces can be mocked")]
        public void MockConfidentialClientApplication_AcquireToken()
        {
            // Setup up a confidential client application that returns a dummy result
            var mockResult = new AuthenticationResult(
                "",
                false,
                "",
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                "",
                null,
                "id token",
                new[]
                {
                    "scope1",
                    "scope2"
                });

            var mockApp = Substitute.For<IConfidentialClientApplication>();
            mockApp.AcquireTokenByAuthorizationCodeAsync("123", null).Returns(mockResult);

            // Now call the substitute with the args to get the substitute result
            var actualResult = mockApp.AcquireTokenByAuthorizationCodeAsync("123", null).Result;
            Assert.IsNotNull(actualResult);
            Assert.AreEqual("id token", mockResult.IdToken, "Mock result failed to return the expected id token");
            // Check the scope property
            IEnumerable<string> scopes = actualResult.Scopes;
            Assert.IsNotNull(scopes);
            Assert.AreEqual("scope1", scopes.First());
            Assert.AreEqual("scope2", scopes.Last());
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        [Description("Tests the public interfaces can be mocked")]
        public void MockConfidentialClientApplication_Users()
        {
            // Setup up a confidential client application with mocked users
            var mockApp = Substitute.For<IConfidentialClientApplication>();
            IList<IAccount> users = new List<IAccount>();

            var mockUser1 = Substitute.For<IAccount>();
            mockUser1.Username.Returns("DisplayableId_1");

            var mockUser2 = Substitute.For<IAccount>();
            mockUser2.Username.Returns("DisplayableId_2");

            users.Add(mockUser1);
            users.Add(mockUser2);
            mockApp.GetAccountsAsync().Returns(users);

            // Now call the substitute
            IEnumerable<IAccount> actualUsers = mockApp.GetAccountsAsync().Result;

            // Check the users property
            Assert.IsNotNull(actualUsers);
            Assert.AreEqual(2, actualUsers.Count());

            Assert.AreEqual("DisplayableId_1", users.First().Username);
            Assert.AreEqual("DisplayableId_2", users.Last().Username);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        [Description("Tests the public application interfaces can be mocked to throw MSAL exceptions")]
        public void MockConfidentialClientApplication_Exception()
        {
            // Setup up a confidential client application that returns throws
            var mockApp = Substitute.For<IConfidentialClientApplication>();
            mockApp.WhenForAnyArgs(x => x.AcquireTokenForClientAsync(Arg.Any<string[]>())).Do(
                x => throw new MsalServiceException("my error code", "my message", new HttpRequestException()));

            // Now call the substitute and check the exception is thrown
            var ex = AssertException.Throws<MsalServiceException>(
                () => mockApp.AcquireTokenForClientAsync(
                    new string[]
                    {
                        "scope1"
                    }));
            Assert.AreEqual("my error code", ex.ErrorCode);
            Assert.AreEqual("my message", ex.Message);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConstructorsTest()
        {
            var app = new ConfidentialClientApplication(
                MsalTestConstants.ClientId,
                MsalTestConstants.RedirectUri,
                new ClientCredential(MsalTestConstants.ClientSecret),
                new TokenCache(),
                new TokenCache());
            Assert.IsNotNull(app);
            Assert.IsNotNull(app.UserTokenCache);
            Assert.IsNotNull(app.AppTokenCache);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(MsalTestConstants.ClientId, app.ClientId);
            Assert.AreEqual(MsalTestConstants.RedirectUri, app.RedirectUri);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.IsNotNull(app.ClientCredential);
            Assert.IsNotNull(app.ClientCredential.Secret);
            Assert.AreEqual(MsalTestConstants.ClientSecret, app.ClientCredential.Secret);
            Assert.IsNull(app.ClientCredential.Certificate);
            Assert.IsNull(app.ClientCredential.Assertion);

            app = new ConfidentialClientApplication(
                MsalTestConstants.ClientId,
                MsalTestConstants.AuthorityGuestTenant,
                MsalTestConstants.RedirectUri,
                new ClientCredential("secret"),
                new TokenCache(),
                new TokenCache());
            Assert.AreEqual(MsalTestConstants.AuthorityGuestTenant, app.Authority);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task ConfidentialClientUsingSecretNoCacheProvidedTestAsync()
        {
            await RunWithMockHttpAsync(
                async (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var app = new ConfidentialClientApplication(
                        serviceBundle,
                        MsalTestConstants.ClientId,
                        ClientApplicationBase.DefaultAuthority,
                        MsalTestConstants.RedirectUri,
                        new ClientCredential(MsalTestConstants.ClientSecret),
                        null,
                        null)
                    {
                        ValidateAuthority = false
                    };

                    httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                    var result = await app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray()).ConfigureAwait(false);
                    Assert.IsNotNull(result);
                    Assert.IsNotNull("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

                    Assert.IsNull(app.UserTokenCache);
                    Assert.IsNull(app.AppTokenCache);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task ConfidentialClientUsingSecretTestAsync()
        {
            await RunWithMockHttpAsync(
                async (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var app = new ConfidentialClientApplication(
                        serviceBundle,
                        MsalTestConstants.ClientId,
                        ClientApplicationBase.DefaultAuthority,
                        MsalTestConstants.RedirectUri,
                        new ClientCredential(MsalTestConstants.ClientSecret),
                        new TokenCache(),
                        new TokenCache())
                    {
                        ValidateAuthority = false
                    };

                    httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);
                    httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                    var result = await app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray()).ConfigureAwait(false);
                    Assert.IsNotNull(result);
                    Assert.IsNotNull("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

                    // make sure user token cache is empty
                    Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.AccessTokenCount);
                    Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.RefreshTokenCount);

                    // check app token cache count to be 1
                    Assert.AreEqual(1, app.AppTokenCache.TokenCacheAccessor.AccessTokenCount);
                    Assert.AreEqual(0, app.AppTokenCache.TokenCacheAccessor.RefreshTokenCount); // no refresh tokens are returned

                    // call AcquireTokenForClientAsync again to get result back from the cache
                    result = await app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray()).ConfigureAwait(false);
                    Assert.IsNotNull(result);
                    Assert.IsNotNull("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

                    // make sure user token cache is empty
                    Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.AccessTokenCount);
                    Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.RefreshTokenCount);

                    // check app token cache count to be 1
                    Assert.AreEqual(1, app.AppTokenCache.TokenCacheAccessor.AccessTokenCount);
                    Assert.AreEqual(0, app.AppTokenCache.TokenCacheAccessor.RefreshTokenCount); // no refresh tokens are returned
                }).ConfigureAwait(false);
        }

        private ConfidentialClientApplication CreateConfidentialClient(
            IServiceBundle serviceBundle,
            MockHttpManager httpManager,
            ClientCredential cc,
            int tokenResponses)
        {
            var app = new ConfidentialClientApplication(
                serviceBundle,
                MsalTestConstants.ClientId,
                ClientApplicationBase.DefaultAuthority,
                MsalTestConstants.RedirectUri,
                cc,
                new TokenCache(),
                new TokenCache())
            {
                ValidateAuthority = false
            };

            httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);

            for (int i = 0; i < tokenResponses; i++)
            {
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
            }

            return app;
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task ConfidentialClientUsingCertificateTestAsync()
        {
            await RunWithMockHttpAsync(
                async (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var cc = new ClientCredential(
                        new ClientAssertionCertificate(
                            new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"))));
                    var app = CreateConfidentialClient(serviceBundle, httpManager, cc, 3);

                    var result = await app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray()).ConfigureAwait(false);
                    Assert.IsNotNull(result);
                    Assert.IsNotNull("header.payload.signature", result.AccessToken);
                    Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

                    // make sure user token cache is empty
                    Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.AccessTokenCount);
                    Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.RefreshTokenCount);

                    // check app token cache count to be 1
                    Assert.AreEqual(1, app.AppTokenCache.TokenCacheAccessor.AccessTokenCount);
                    Assert.AreEqual(0, app.AppTokenCache.TokenCacheAccessor.RefreshTokenCount); // no refresh tokens are returned

                    // assert client credential
                    Assert.IsNotNull(cc.Assertion);
                    Assert.AreNotEqual(0, cc.ValidTo);

                    // save client assertion.
                    string cachedAssertion = cc.Assertion;
                    long cacheValidTo = cc.ValidTo;

                    result = await app.AcquireTokenForClientAsync(MsalTestConstants.ScopeForAnotherResource.ToArray())
                                      .ConfigureAwait(false);
                    Assert.IsNotNull(result);
                    Assert.AreEqual(cacheValidTo, cc.ValidTo);
                    Assert.AreEqual(cachedAssertion, cc.Assertion);

                    // validate the send x5c forces a refresh of the cached client assertion
                    await ((IConfidentialClientApplicationWithCertificate)app)
                          .AcquireTokenForClientWithCertificateAsync(MsalTestConstants.Scope.ToArray(), true)
                          .ConfigureAwait(false);
                    Assert.AreNotEqual(cachedAssertion, cc.Assertion);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task ConfidentialClientUsingCertificateTelemetryTestAsync()
        {
            await RunWithMockHttpAsync(
                async (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var cc = new ClientCredential(
                        new ClientAssertionCertificate(
                            new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"))));

                    // TODO: previous test had the final parameter here as 2 instead of 1.
                    // However, this 2nd one is NOT consumed by this test and the previous
                    // test did not check for all mock requests to be flushed out...

                    var app = CreateConfidentialClient(serviceBundle, httpManager, cc, 1);
                    var result = await app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray()).ConfigureAwait(false);
                    Assert.IsNotNull(
                        receiver.EventsReceived.Find(
                            anEvent => // Expect finding such an event
                                anEvent[EventBase.EventNameKey].EndsWith("http_event") &&
                                anEvent[HttpEvent.ResponseCodeKey] == "200" && anEvent[HttpEvent.HttpPathKey]
                                    .Contains(
                                        EventBase
                                            .TenantPlaceHolder) // The tenant info is expected to be replaced by a holder
                        ));
                    Assert.IsNotNull(
                        receiver.EventsReceived.Find(
                            anEvent => // Expect finding such an event
                                anEvent[EventBase.EventNameKey].EndsWith("token_cache_lookup") &&
                                anEvent[CacheEvent.TokenTypeKey] == "at"));
                    Assert.IsNotNull(
                        receiver.EventsReceived.Find(
                            anEvent => // Expect finding such an event
                                anEvent[EventBase.EventNameKey].EndsWith("token_cache_write") &&
                                anEvent[CacheEvent.TokenTypeKey] == "at"));
                    Assert.IsNotNull(
                        receiver.EventsReceived.Find(
                            anEvent => // Expect finding such an event
                                anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                                anEvent[ApiEvent.WasSuccessfulKey] == "true" && anEvent[ApiEvent.ApiIdKey] == "726"));
                }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task GetAuthorizationRequestUrlNoRedirectUriTestAsync()
        {
            await RunWithMockHttpAsync(
                async (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var app = new ConfidentialClientApplication(
                        serviceBundle,
                        MsalTestConstants.ClientId,
                        ClientApplicationBase.DefaultAuthority,
                        MsalTestConstants.RedirectUri,
                        new ClientCredential(MsalTestConstants.ClientSecret),
                        new TokenCache(),
                        new TokenCache())
                    {
                        ValidateAuthority = false
                    };

                    httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);

                    var uri = await app.GetAuthorizationRequestUrlAsync(
                                  MsalTestConstants.Scope,
                                  MsalTestConstants.DisplayableId,
                                  null).ConfigureAwait(false);
                    Assert.IsNotNull(uri);
                    Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                    ValidateCommonQueryParams(qp);
                    Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2", qp["scope"]);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task GetAuthorizationRequestUrlB2CTestAsync()
        {
            await RunWithMockHttpAsync(
                async (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var app = new ConfidentialClientApplication(
                        serviceBundle,
                        MsalTestConstants.ClientId,
                        ClientApplicationBase.DefaultAuthority,
                        MsalTestConstants.RedirectUri,
                        new ClientCredential(MsalTestConstants.ClientSecret),
                        new TokenCache(),
                        new TokenCache())
                    {
                        ValidateAuthority = false
                    };

                    // add mock response for tenant endpoint discovery
                    httpManager.AddMockHandler(
                        new MockHttpMessageHandler
                        {
                            Method = HttpMethod.Get,
                            ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                                File.ReadAllText(
                                    ResourceHelper.GetTestResourceRelativePath(@"OpenidConfiguration-QueryParams-B2C.json")))
                        });

                    var uri = await app.GetAuthorizationRequestUrlAsync(
                                  MsalTestConstants.Scope,
                                  MsalTestConstants.DisplayableId,
                                  null).ConfigureAwait(false);
                    Assert.IsNotNull(uri);
                    Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                    Assert.IsNotNull(qp);

                    Assert.AreEqual("my-policy", qp["p"]);
                    ValidateCommonQueryParams(qp);
                    Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2", qp["scope"]);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task GetAuthorizationRequestUrlDuplicateParamsTestAsync()
        {
            await RunWithMockHttpAsync(
                async (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var app = new ConfidentialClientApplication(
                        serviceBundle,
                        MsalTestConstants.ClientId,
                        ClientApplicationBase.DefaultAuthority,
                        MsalTestConstants.RedirectUri,
                        new ClientCredential(MsalTestConstants.ClientSecret),
                        new TokenCache(),
                        new TokenCache())
                    {
                        ValidateAuthority = false
                    };

                    httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);

                    try
                    {
                        var uri = await app.GetAuthorizationRequestUrlAsync(
                                      MsalTestConstants.Scope,
                                      MsalTestConstants.DisplayableId,
                                      "login_hint=some@value.com").ConfigureAwait(false);
                        Assert.Fail("MSALException should be thrown here");
                    }
                    catch (MsalException exc)
                    {
                        Assert.AreEqual("duplicate_query_parameter", exc.ErrorCode);
                        Assert.AreEqual("Duplicate query parameter 'login_hint' in extraQueryParameters", exc.Message);
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail("Wrong type of exception thrown: " + ex);
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlCustomRedirectUriTest()
        {
            RunWithMockHttp(
                (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var app = new ConfidentialClientApplication(
                        serviceBundle,
                        MsalTestConstants.ClientId,
                        MsalTestConstants.AuthorityGuestTenant,
                        MsalTestConstants.RedirectUri,
                        new ClientCredential(MsalTestConstants.ClientSecret),
                        new TokenCache(),
                        new TokenCache())
                    {
                        ValidateAuthority = false
                    };

                    httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);

                    const string CustomRedirectUri = "custom://redirect-uri";
                    Task<Uri> task = app.GetAuthorizationRequestUrlAsync(
                        MsalTestConstants.Scope,
                        CustomRedirectUri,
                        MsalTestConstants.DisplayableId,
                        "extra=qp",
                        MsalTestConstants.ScopeForAnotherResource,
                        MsalTestConstants.AuthorityGuestTenant);
                    var uri = task.Result;
                    Assert.IsNotNull(uri);
                    Assert.IsTrue(
                        uri.AbsoluteUri.StartsWith(MsalTestConstants.AuthorityGuestTenant, StringComparison.CurrentCulture));
                    Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                    ValidateCommonQueryParams(qp, CustomRedirectUri);
                    Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2 r2/scope1 r2/scope2", qp["scope"]);
                    Assert.IsFalse(qp.ContainsKey("client_secret"));
                    Assert.AreEqual("qp", qp["extra"]);
                });
        }

        private static void ValidateCommonQueryParams(
            Dictionary<string, string> qp,
            string redirectUri = MsalTestConstants.RedirectUri)
        {
            Assert.IsNotNull(qp);

            Assert.IsTrue(qp.ContainsKey("client-request-id"));
            Assert.AreEqual(MsalTestConstants.ClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual(redirectUri, qp["redirect_uri"]);
            Assert.AreEqual(MsalTestConstants.DisplayableId, qp["login_hint"]);
            Assert.AreEqual(UIBehavior.SelectAccount.PromptValue, qp["prompt"]);
            Assert.AreEqual(PlatformProxyFactory.GetPlatformProxy().GetProductName(), qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));

#if !NET_CORE
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
#endif
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void HttpRequestExceptionIsNotSuppressed()
        {
            RunWithMockHttp(
                (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var app = new ConfidentialClientApplication(
                        serviceBundle,
                        MsalTestConstants.ClientId,
                        ClientApplicationBase.DefaultAuthority,
                        MsalTestConstants.RedirectUri,
                        new ClientCredential(MsalTestConstants.ClientSecret),
                        new TokenCache(),
                        new TokenCache())
                    {
                        ValidateAuthority = false
                    };

                    // add mock response bigger than 1MB for Http Client
                    httpManager.AddFailingRequest(new InvalidOperationException());

                    AssertException.TaskThrows<InvalidOperationException>(
                        () => app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray()));
                });
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task ForceRefreshParameterFalseTestAsync()
        {
            await RunWithMockHttpAsync(
                async (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();
                    var cache = new TokenCache();
                    _tokenCacheHelper.PopulateCacheForClientCredential(cache.TokenCacheAccessor);

                    string authority = Authority.CreateAuthority(serviceBundle, MsalTestConstants.AuthorityTestTenant, false)
                                                .CanonicalAuthority;
                    var app = new ConfidentialClientApplication(
                        serviceBundle,
                        MsalTestConstants.ClientId,
                        authority,
                        MsalTestConstants.RedirectUri,
                        new ClientCredential(MsalTestConstants.ClientSecret),
                        null,
                        cache)
                    {
                        ValidateAuthority = false
                    };

                    ICollection<MsalAccessTokenCacheItem> accessTokens =
                        cache.GetAllAccessTokensForClient(new RequestContext(null, new MsalLogger(Guid.NewGuid(), null)));
                    var accessTokenInCache = accessTokens
                                             .Where(item => ScopeHelper.ScopeContains(item.ScopeSet, MsalTestConstants.Scope))
                                             .ToList().FirstOrDefault();

                    // Don't add mock to fail in case of network call
                    // If there's a network call by mistake, then there won't be a proper number
                    // of mock web request/response objects in the queue and we'll fail.

                    var result = await app.AcquireTokenForClientAsync(MsalTestConstants.Scope, false).ConfigureAwait(false);

                    Assert.AreEqual(accessTokenInCache.Secret, result.AccessToken);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task ForceRefreshParameterTrueTestAsync()
        {
            await RunWithMockHttpAsync(
                async (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var cache = new TokenCache();
                    _tokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);

                    string authority = Authority.CreateAuthority(serviceBundle, MsalTestConstants.AuthorityTestTenant, false)
                                                .CanonicalAuthority;
                    var app = new ConfidentialClientApplication(
                        serviceBundle,
                        MsalTestConstants.ClientId,
                        authority,
                        MsalTestConstants.RedirectUri,
                        new ClientCredential(MsalTestConstants.ClientSecret),
                        null,
                        cache)
                    {
                        ValidateAuthority = false
                    };

                    httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);

                    // add mock response for successful token retrival
                    const string TokenRetrievedFromNetCall = "token retrieved from network call";
                    httpManager.AddMockHandler(
                        new MockHttpMessageHandler
                        {
                            Method = HttpMethod.Post,
                            ResponseMessage =
                                MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(TokenRetrievedFromNetCall)
                        });

                    var result = await app.AcquireTokenForClientAsync(MsalTestConstants.Scope, true).ConfigureAwait(false);
                    Assert.AreEqual(TokenRetrievedFromNetCall, result.AccessToken);

                    // make sure token in Cache was updated
                    ICollection<MsalAccessTokenCacheItem> accessTokens =
                        cache.GetAllAccessTokensForClient(new RequestContext(null, new MsalLogger(Guid.NewGuid(), null)));
                    var accessTokenInCache = accessTokens
                                             .Where(item => ScopeHelper.ScopeContains(item.ScopeSet, MsalTestConstants.Scope))
                                             .ToList().FirstOrDefault();

                    Assert.AreEqual(TokenRetrievedFromNetCall, accessTokenInCache.Secret);
                    Assert.IsNotNull(
                        receiver.EventsReceived.Find(
                            anEvent => // Expect finding such an event
                                anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                                anEvent[ApiEvent.WasSuccessfulKey] == "true" && anEvent[ApiEvent.ApiIdKey] == "727"));
                }).ConfigureAwait(false);
        }

        private static async Task RunWithMockHttpAsync(Func<MockHttpManager, IServiceBundle, MyReceiver, Task> action)
        {
            using (var httpManager = new MockHttpManager())
            {
                var receiver = new MyReceiver();
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager, receiver);
                await action(httpManager, serviceBundle, receiver).ConfigureAwait(false);
            }
        }

        private static void RunWithMockHttp(Action<MockHttpManager, IServiceBundle, MyReceiver> action)
        {
            using (var httpManager = new MockHttpManager())
            {
                var receiver = new MyReceiver();
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager, receiver);
                action(httpManager, serviceBundle, receiver);
            }
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task AuthorizationCodeRequestTestAsync()
        {
            await RunWithMockHttpAsync(
                async (httpManager, serviceBundle, receiver) =>
                {
                    httpManager.AddInstanceDiscoveryMockHandler();

                    var cache = new TokenCache()
                    {
                        BeforeAccess = BeforeCacheAccess,
                        AfterAccess = AfterCacheAccess
                    };

                    var cc = new ClientCredential("secret");
                    var app = new ConfidentialClientApplication(
                        serviceBundle,
                        MsalTestConstants.ClientId,
                        "https://" + MsalTestConstants.ProductionPrefNetworkEnvironment + "/tfp/home/policy",
                        MsalTestConstants.RedirectUri,
                        cc,
                        cache,
                        null)
                    {
                        ValidateAuthority = false
                    };

                    httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant, "p=policy");
                    httpManager.AddSuccessTokenResponseMockHandlerForPost();

                    var result = await app.AcquireTokenByAuthorizationCodeAsync("some-code", MsalTestConstants.Scope)
                                          .ConfigureAwait(false);
                    Assert.IsNotNull(result);
                    Assert.AreEqual(1, app.UserTokenCache.TokenCacheAccessor.AccessTokenCount);
                    Assert.AreEqual(1, app.UserTokenCache.TokenCacheAccessor.RefreshTokenCount);

                    cache = new TokenCache()
                    {
                        BeforeAccess = BeforeCacheAccess,
                        AfterAccess = AfterCacheAccess
                    };

                    app = new ConfidentialClientApplication(
                        MsalTestConstants.ClientId,
                        "https://" + MsalTestConstants.ProductionPrefNetworkEnvironment + "/tfp/home/policy",
                        MsalTestConstants.RedirectUri,
                        cc,
                        cache,
                        null)
                    {
                        ValidateAuthority = false
                    };

                    IEnumerable<IAccount> users = await app.GetAccountsAsync().ConfigureAwait(false);
                    Assert.AreEqual(1, users.Count());
                }).ConfigureAwait(false);
        }

        private void BeforeCacheAccess(TokenCacheNotificationArgs args)
        {
            args.TokenCache.Deserialize(_serializedCache);
        }

        private void AfterCacheAccess(TokenCacheNotificationArgs args)
        {
            _serializedCache = args.TokenCache.Serialize();
        }
    }
}

#endif