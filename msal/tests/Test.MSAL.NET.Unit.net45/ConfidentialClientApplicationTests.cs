//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Core.Telemetry;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using NSubstitute;
using Test.Microsoft.Identity.Core.Unit;
using Test.Microsoft.Identity.Core.Unit.Mocks;

#if !ANDROID && !iOS && !WINDOWS_APP // No Confidential Client
namespace Test.MSAL.NET.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\valid.crtfile")]
    [DeploymentItem("Resources\\OpenidConfiguration-B2C.json")]
    public class ConfidentialClientApplicationTests
    {
        private readonly MyReceiver _myReceiver = new MyReceiver();
        private byte[] _serializedCache = null;

        [TestInitialize]
        public void TestInitialize()
        {
            Authority.ValidatedAuthorities.Clear();
            ModuleInitializer.ForceModuleInitializationTestOnly();
            Telemetry.GetInstance().RegisterReceiver(_myReceiver.OnEvents);
            AadInstanceDiscovery.Instance.Cache.Clear();
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        [Description("Tests the public interfaces can be mocked")]
        public void MockConfidentialClientApplication_AcquireToken()
        {
            // Setup up a confidential client application that returns a dummy result
            var mockResult = Substitute.For<AuthenticationResult>();
            mockResult.IdToken.Returns("id token");
            mockResult.Scopes.Returns(new string[] { "scope1", "scope2" });

            var mockApp = Substitute.For<IConfidentialClientApplication>();
            mockApp.AcquireTokenByAuthorizationCodeAsync("123", null).Returns(mockResult);

            // Now call the substitute with the args to get the substitute result
            AuthenticationResult actualResult = mockApp.AcquireTokenByAuthorizationCodeAsync("123", null).Result;
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

            IAccount mockUser1 = Substitute.For<IAccount>();
            mockUser1.Username.Returns("DisplayableId_1");

            IAccount mockUser2 = Substitute.For<IAccount>();
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
            mockApp
                .WhenForAnyArgs(x => x.AcquireTokenForClientAsync(Arg.Any<string[]>()))
                .Do(x => { throw new MsalServiceException("my error code", "my message", new HttpRequestException()); });


            // Now call the substitute and check the exception is thrown
            MsalServiceException ex = AssertException.Throws<MsalServiceException>(() => mockApp.AcquireTokenForClientAsync(new string[] { "scope1" }));
            Assert.AreEqual("my error code", ex.ErrorCode);
            Assert.AreEqual("my message", ex.Message);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConstructorsTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(MsalTestConstants.ClientId,
                MsalTestConstants.RedirectUri, new ClientCredential(MsalTestConstants.ClientSecret),
                new TokenCache(), new TokenCache());
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

            app = new ConfidentialClientApplication(MsalTestConstants.ClientId,
                MsalTestConstants.AuthorityGuestTenant,
                MsalTestConstants.RedirectUri, new ClientCredential("secret"), new TokenCache(),
                new TokenCache());
            Assert.AreEqual(MsalTestConstants.AuthorityGuestTenant, app.Authority);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConfidentialClientUsingSecretNoCacheProvidedTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplication app = new ConfidentialClientApplication(
                    httpManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority,
                    MsalTestConstants.RedirectUri,
                    new ClientCredential(MsalTestConstants.ClientSecret),
                    userTokenCache: null,
                    appTokenCache: null)
                {
                    ValidateAuthority = false
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                Task<AuthenticationResult> task = app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray());
                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

                Assert.IsNull(app.UserTokenCache);
                Assert.IsNull(app.AppTokenCache);
            }
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConfidentialClientUsingSecretTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplication app = new ConfidentialClientApplication(
                    httpManager,
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

                Task<AuthenticationResult> task = app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray());
                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

                //make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCache.tokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(0, app.UserTokenCache.tokenCacheAccessor.RefreshTokenCount);

                //check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCache.tokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(
                    0,
                    app.AppTokenCache.tokenCacheAccessor.RefreshTokenCount); //no refresh tokens are returned

                //call AcquireTokenForClientAsync again to get result back from the cache
                task = app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray());
                result = task.Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

                //make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCache.tokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(0, app.UserTokenCache.tokenCacheAccessor.RefreshTokenCount);

                //check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCache.tokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(
                    0,
                    app.AppTokenCache.tokenCacheAccessor.RefreshTokenCount); //no refresh tokens are returned
            }
        }

        private ConfidentialClientApplication CreateConfidentialClient(MockHttpManager httpManager, ClientCredential cc, int tokenResponses)
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(
                httpManager,
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
        public void ConfidentialClientUsingCertificateTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ClientCredential cc = new ClientCredential(
                    new ClientAssertionCertificate(
                        new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"))));
                var app = CreateConfidentialClient(httpManager, cc, 3);

                Task<AuthenticationResult> task = app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray());
                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

                //make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCache.tokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(0, app.UserTokenCache.tokenCacheAccessor.RefreshTokenCount);

                //check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCache.tokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(
                    0,
                    app.AppTokenCache.tokenCacheAccessor.RefreshTokenCount); //no refresh tokens are returned

                //assert client credential
                Assert.IsNotNull(cc.Assertion);
                Assert.AreNotEqual(0, cc.ValidTo);

                //save client assertion.
                string cachedAssertion = cc.Assertion;
                long cacheValidTo = cc.ValidTo;

                task = app.AcquireTokenForClientAsync(MsalTestConstants.ScopeForAnotherResource.ToArray());
                result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(cacheValidTo, cc.ValidTo);
                Assert.AreEqual(cachedAssertion, cc.Assertion);

                //validate the send x5c forces a refresh of the cached client assertion
                (app as IConfidentialClientApplicationWithCertificate).AcquireTokenForClientWithCertificateAsync(
                    MsalTestConstants.Scope.ToArray(),
                    true);
                Assert.AreNotEqual(cachedAssertion, cc.Assertion);
            }
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConfidentialClientUsingCertificateTelemetryTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ClientCredential cc = new ClientCredential(new ClientAssertionCertificate(
                    new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"))));

                // TODO: previous test had the final parameter here as 2 instead of 1.
                // However, this 2nd one is NOT consumed by this test and the previous
                // test did not check for all mock requests to be flushed out...

                var app = CreateConfidentialClient(httpManager, cc, 1);
                Task<AuthenticationResult> task = app.AcquireTokenForClientAsync(MsalTestConstants.Scope.ToArray());
                AuthenticationResult result = task.Result;
                Assert.IsNotNull(
                    _myReceiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("http_event") &&
                            anEvent[HttpEvent.ResponseCodeKey] == "200" &&
                            anEvent[HttpEvent.HttpPathKey]
                                .Contains(
                                    EventBase
                                        .TenantPlaceHolder) // The tenant info is expected to be replaced by a holder
                    ));
                Assert.IsNotNull(
                    _myReceiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("token_cache_lookup") &&
                            anEvent[CacheEvent.TokenTypeKey] == "at"));
                Assert.IsNotNull(
                    _myReceiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("token_cache_write") &&
                            anEvent[CacheEvent.TokenTypeKey] == "at"));
                Assert.IsNotNull(
                    _myReceiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                            anEvent[ApiEvent.WasSuccessfulKey] == "true" && anEvent[ApiEvent.ApiIdKey] == "726"));
            }
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlNoRedirectUriTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplication app = new ConfidentialClientApplication(
                    httpManager,
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

                Task<Uri> task = app.GetAuthorizationRequestUrlAsync(MsalTestConstants.Scope, MsalTestConstants.DisplayableId, null);
                Uri uri = task.Result;
                Assert.IsNotNull(uri);
                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                ValidateCommonQueryParams(qp);
                Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2", qp["scope"]);

            }
        }

  

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlB2CTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplication app = new ConfidentialClientApplication(
                    httpManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority,
                    MsalTestConstants.RedirectUri,
                    new ClientCredential(MsalTestConstants.ClientSecret),
                    new TokenCache(),
                    new TokenCache())
                {
                    ValidateAuthority = false
                };

                //add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                File.ReadAllText(
                                    ResourceHelper.GetTestResourceRelativePath(
                                        @"OpenidConfiguration-B2C.json")))
                    });

                Task<Uri> task = app.GetAuthorizationRequestUrlAsync(MsalTestConstants.Scope, MsalTestConstants.DisplayableId, null);
                Uri uri = task.Result;
                Assert.IsNotNull(uri);
                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                Assert.IsNotNull(qp);

                Assert.AreEqual("my-policy", qp["p"]);
                ValidateCommonQueryParams(qp);
                Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2", qp["scope"]);

            }
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlDuplicateParamsTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplication app = new ConfidentialClientApplication(
                    httpManager,
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
                    Task<Uri> task = app.GetAuthorizationRequestUrlAsync(
                        MsalTestConstants.Scope,
                        MsalTestConstants.DisplayableId,
                        "login_hint=some@value.com");
                    Uri uri = task.Result;
                    Assert.Fail("MSALException should be thrown here");
                }
                catch (Exception exc)
                {
                    Assert.IsTrue(exc.InnerException is MsalException);
                    Assert.AreEqual("duplicate_query_parameter", ((MsalException)exc.InnerException).ErrorCode);
                    Assert.AreEqual(
                        "Duplicate query parameter 'login_hint' in extraQueryParameters",
                        ((MsalException)exc.InnerException).Message);
                }
            }
        }


        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlCustomRedirectUriTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplication app = new ConfidentialClientApplication(
                    httpManager,
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
                Uri uri = task.Result;
                Assert.IsNotNull(uri);
                Assert.IsTrue(uri.AbsoluteUri.StartsWith(MsalTestConstants.AuthorityGuestTenant, StringComparison.CurrentCulture));
                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                ValidateCommonQueryParams(qp, CustomRedirectUri);
                Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2 r2/scope1 r2/scope2", qp["scope"]);
                Assert.IsFalse(qp.ContainsKey("client_secret"));
                Assert.AreEqual("qp", qp["extra"]);
            }
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
            Assert.AreEqual(PlatformProxyFactory.GetPlatformProxy().GetProductName(),
                qp["x-client-sku"]);
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
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = new ConfidentialClientApplication(
                    httpManager,
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
            }
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ForceRefreshParameterFalseTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                var cache = new TokenCache();
                TokenCacheHelper.PopulateCacheForClientCredential(cache.tokenCacheAccessor);

                var authority = Authority.CreateAuthority(MsalTestConstants.AuthorityTestTenant, false).CanonicalAuthority;
                var app = new ConfidentialClientApplication(
                    httpManager,
                    MsalTestConstants.ClientId,
                    authority,
                    MsalTestConstants.RedirectUri,
                    new ClientCredential(MsalTestConstants.ClientSecret),
                    null,
                    cache)
                {
                    ValidateAuthority = false
                };

                var accessTokens = cache.GetAllAccessTokensForClient(new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
                var accessTokenInCache = accessTokens.Where(item => ScopeHelper.ScopeContains(item.ScopeSet, MsalTestConstants.Scope))
                                                     .ToList().FirstOrDefault();

                // Don't add mock to fail in case of network call
                // If there's a network call by mistake, then there won't be a proper number
                // of mock web request/response objects in the queue and we'll fail.

                var task = app.AcquireTokenForClientAsync(MsalTestConstants.Scope, false);
                var result = task.Result;

                Assert.AreEqual(accessTokenInCache.Secret, result.AccessToken);
            }
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task ForceRefreshParameterTrueTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cache = new TokenCache();
                TokenCacheHelper.PopulateCache(cache.tokenCacheAccessor);

                var authority = Authority.CreateAuthority(MsalTestConstants.AuthorityTestTenant, false).CanonicalAuthority;
                var app = new ConfidentialClientApplication(
                    httpManager,
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

                //add mock response for successful token retrival
                const string tokenRetrievedFromNetCall = "token retrieved from network call";
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage =
                            MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(tokenRetrievedFromNetCall)
                    });

                var result = await app.AcquireTokenForClientAsync(MsalTestConstants.Scope, true).ConfigureAwait(false);
                Assert.AreEqual(tokenRetrievedFromNetCall, result.AccessToken);

                // make sure token in Cache was updated
                var accessTokens = cache.GetAllAccessTokensForClient(new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
                var accessTokenInCache = accessTokens.Where(item => ScopeHelper.ScopeContains(item.ScopeSet, MsalTestConstants.Scope))
                                                     .ToList().FirstOrDefault();

                Assert.AreEqual(tokenRetrievedFromNetCall, accessTokenInCache.Secret);
                Assert.IsNotNull(
                    _myReceiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                            anEvent[ApiEvent.WasSuccessfulKey] == "true" && anEvent[ApiEvent.ApiIdKey] == "727"));
            }
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task AuthorizationCodeRequestTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                TokenCache cache = new TokenCache()
                {
                    BeforeAccess = BeforeCacheAccess,
                    AfterAccess = AfterCacheAccess
                };

                ClientCredential cc = new ClientCredential("secret");
                var app = new ConfidentialClientApplication(
                    httpManager,
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

                AuthenticationResult result = await app.AcquireTokenByAuthorizationCodeAsync("some-code", MsalTestConstants.Scope)
                                                       .ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.AreEqual(1, app.UserTokenCache.tokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(1, app.UserTokenCache.tokenCacheAccessor.RefreshTokenCount);

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

                var users = app.GetAccountsAsync().Result;
                Assert.AreEqual(1, users.Count());
            }
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