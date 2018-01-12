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
using Test.MSAL.NET.Unit.Mocks;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.Telemetry;
using NSubstitute;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\valid.crtfile")]
    [DeploymentItem("Resources\\OpenidConfiguration-B2C.json")]
    public class ConfidentialClientApplicationTests
    {
        private readonly MyReceiver _myReceiver = new MyReceiver();
        private byte[] serializedCache = null;

        [TestInitialize]
        public void TestInitialize()
        {
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
            Telemetry.GetInstance().RegisterReceiver(_myReceiver.OnEvents);
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
            IList<IUser> users = new List<IUser>();

            IUser mockUser1 = Substitute.For<IUser>();
            mockUser1.Name.Returns("Name1");

            IUser mockUser2 = Substitute.For<IUser>();
            mockUser2.Name.Returns("Name2");

            users.Add(mockUser1);
            users.Add(mockUser2);
            mockApp.Users.Returns(users);

            // Now call the substitute
            IEnumerable<IUser> actualUsers = mockApp.Users;

            // Check the users property
            Assert.IsNotNull(actualUsers);
            Assert.AreEqual(2, actualUsers.Count());

            Assert.AreEqual("Name1", users.First().Name);
            Assert.AreEqual("Name2", users.Last().Name);
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
                .Do(x => { throw new MsalServiceException("my error code", "my message"); });


            // Now call the substitute and check the exception is thrown
            MsalServiceException ex = AssertException.Throws<MsalServiceException>(() => mockApp.AcquireTokenForClientAsync(new string[] { "scope1" }));
            Assert.AreEqual("my error code", ex.ErrorCode);
            Assert.AreEqual("my message", ex.Message);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConstructorsTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache());
            Assert.IsNotNull(app);
            Assert.IsNotNull(app.UserTokenCache);
            Assert.IsNotNull(app.AppTokenCache);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.ClientId);
            Assert.AreEqual(TestConstants.RedirectUri, app.RedirectUri);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.IsNotNull(app.ClientCredential);
            Assert.IsNotNull(app.ClientCredential.Secret);
            Assert.AreEqual(TestConstants.ClientSecret, app.ClientCredential.Secret);
            Assert.IsNull(app.ClientCredential.Certificate);
            Assert.IsNull(app.ClientCredential.Assertion);
            Assert.AreEqual(0, app.ClientCredential.ValidTo);

            app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.AuthorityGuestTenant,
                TestConstants.RedirectUri, new ClientCredential("secret"), new TokenCache(),
                new TokenCache());
            Assert.AreEqual(TestConstants.AuthorityGuestTenant, app.Authority);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConfidentialClientUsingSecretNoCacheProvidedTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                null, null)
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            });

            Task<AuthenticationResult> task = app.AcquireTokenForClientAsync(TestConstants.Scope.ToArray());
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull("header.payload.signature", result.AccessToken);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

            Assert.IsNull(app.UserTokenCache);
            Assert.IsNull(app.AppTokenCache);
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConfidentialClientUsingSecretTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache())
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            });

            Task<AuthenticationResult> task = app.AcquireTokenForClientAsync(TestConstants.Scope.ToArray());
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull("header.payload.signature", result.AccessToken);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

            //make sure user token cache is empty
            Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);

            //check app token cache count to be 1
            Assert.AreEqual(1, app.AppTokenCache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(0, app.AppTokenCache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count); //no refresh tokens are returned

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

            //call AcquireTokenForClientAsync again to get result back from the cache
            task = app.AcquireTokenForClientAsync(TestConstants.Scope.ToArray());
            result = task.Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull("header.payload.signature", result.AccessToken);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

            //make sure user token cache is empty
            Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);

            //check app token cache count to be 1
            Assert.AreEqual(1, app.AppTokenCache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(0, app.AppTokenCache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count); //no refresh tokens are returned
        }

        private ConfidentialClientApplication CreateConfidentialClient(ClientCredential cc)
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, cc, new TokenCache(),
                new TokenCache())
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            });
            return app;
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConfidentialClientUsingCertificateTest()
        {
            ClientCredential cc =
                new ClientCredential(new ClientAssertionCertificate(new X509Certificate2("valid.crtfile")));
            var app = CreateConfidentialClient(cc);

            Task<AuthenticationResult> task = app.AcquireTokenForClientAsync(TestConstants.Scope.ToArray());
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull("header.payload.signature", result.AccessToken);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

            //make sure user token cache is empty
            Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);

            //check app token cache count to be 1
            Assert.AreEqual(1, app.AppTokenCache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(0, app.AppTokenCache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count); //no refresh tokens are returned

            //assert client credential
            Assert.IsNotNull(cc.Assertion);
            Assert.AreNotEqual(0, cc.ValidTo);

            //save client assertion.
            string cachedAssertion = cc.Assertion;
            long cacheValidTo = cc.ValidTo;

            task = app.AcquireTokenForClientAsync(TestConstants.ScopeForAnotherResource.ToArray());
            result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(cacheValidTo, cc.ValidTo);
            Assert.AreEqual(cachedAssertion, cc.Assertion);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConfidentialClientUsingCertificateTelemetryTest()
        {
            ClientCredential cc =
                new ClientCredential(new ClientAssertionCertificate(new X509Certificate2("valid.crtfile")));
            var app = CreateConfidentialClient(cc);
            Task<AuthenticationResult> task = app.AcquireTokenForClientAsync(TestConstants.Scope.ToArray());
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent => // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("http_event") && anEvent[HttpEvent.ResponseCodeKey] == "200"
                && anEvent[HttpEvent.HttpPathKey].Contains(EventBase.TenantPlaceHolder) // The tenant info is expected to be replaced by a holder
                ));
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent => // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("token_cache_lookup") && anEvent[CacheEvent.TokenTypeKey] == "at"));
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent => // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("token_cache_write") && anEvent[CacheEvent.TokenTypeKey] == "at"));
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.WasSuccessfulKey] == "true"
                && anEvent[ApiEvent.ApiIdKey] == "726"));
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlNoRedirectUriTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache())
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            Task<Uri> task = app.GetAuthorizationRequestUrlAsync(TestConstants.Scope.AsArray(),
                TestConstants.DisplayableId, null);
            Uri uri = task.Result;
            Assert.IsNotNull(uri);
            Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
            Assert.IsNotNull(qp);
            Assert.AreEqual(11, qp.Count);
            Assert.IsTrue(qp.ContainsKey("client-request-id"));
            Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2", qp["scope"]);
            Assert.AreEqual(TestConstants.ClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual(TestConstants.RedirectUri, qp["redirect_uri"]);
            Assert.AreEqual(TestConstants.DisplayableId, qp["login_hint"]);
            Assert.AreEqual(UIBehavior.SelectAccount.PromptValue, qp["prompt"]);
            Assert.AreEqual("MSAL.Desktop", qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlB2CTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache())
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText(@"OpenidConfiguration-B2C.json"))
            });

            Task<Uri> task = app.GetAuthorizationRequestUrlAsync(TestConstants.Scope.AsArray(),
                TestConstants.DisplayableId, null);
            Uri uri = task.Result;
            Assert.IsNotNull(uri);
            Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
            Assert.IsNotNull(qp);
            Assert.AreEqual(12, qp.Count);
            Assert.AreEqual("my-policy", qp["p"]);
            Assert.IsTrue(qp.ContainsKey("client-request-id"));
            Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2", qp["scope"]);
            Assert.AreEqual(TestConstants.ClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual(TestConstants.RedirectUri, qp["redirect_uri"]);
            Assert.AreEqual(TestConstants.DisplayableId, qp["login_hint"]);
            Assert.AreEqual(UIBehavior.SelectAccount.PromptValue, qp["prompt"]);
            Assert.AreEqual("MSAL.Desktop", qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlDuplicateParamsTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache())
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            try
            {
                Task<Uri> task = app.GetAuthorizationRequestUrlAsync(TestConstants.Scope.AsArray(),
                    TestConstants.DisplayableId, "login_hint=some@value.com");
                Uri uri = task.Result;
                Assert.Fail("MSALException should be thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual("duplicate_query_parameter", ((MsalException) exc.InnerException).ErrorCode);
                Assert.AreEqual("Duplicate query parameter 'login_hint' in extraQueryParameters",
                    ((MsalException) exc.InnerException).Message);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }


        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlCustomRedirectUriTest()
        {
            ConfidentialClientApplication app =
                new ConfidentialClientApplication(TestConstants.ClientId, TestConstants.AuthorityGuestTenant,
                    TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                    new TokenCache(), new TokenCache())
                {ValidateAuthority = false};

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            const string CustomRedirectUri = "custom://redirect-uri";
            Task<Uri> task = app.GetAuthorizationRequestUrlAsync(TestConstants.Scope.AsArray(),
                CustomRedirectUri, TestConstants.DisplayableId, "extra=qp",
                TestConstants.ScopeForAnotherResource.AsArray(), TestConstants.AuthorityGuestTenant);
            Uri uri = task.Result;
            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.AbsoluteUri.StartsWith(TestConstants.AuthorityGuestTenant, StringComparison.CurrentCulture));
            Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
            Assert.IsNotNull(qp);
            Assert.AreEqual(12, qp.Count);
            Assert.IsTrue(qp.ContainsKey("client-request-id"));
            Assert.IsFalse(qp.ContainsKey("client_secret"));
            Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2 r2/scope1 r2/scope2", qp["scope"]);
            Assert.AreEqual(TestConstants.ClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual(CustomRedirectUri, qp["redirect_uri"]);
            Assert.AreEqual(TestConstants.DisplayableId, qp["login_hint"]);
            Assert.AreEqual("MSAL.Desktop", qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));
            Assert.AreEqual("qp", qp["extra"]);
            Assert.AreEqual("select_account", qp["prompt"]);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        [ExpectedException(typeof(HttpRequestException), "Cannot write more bytes to the buffer than the configured maximum buffer size: 1048576.")]
        public async Task HttpRequestExceptionIsNotSuppressed()
        {
            var app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache())
            {
                ValidateAuthority = false
            };

            // add mock response bigger than 1MB for Http Client
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(new string(new char[1048577]))
                }
            });

            await app.AcquireTokenForClientAsync(TestConstants.Scope.ToArray());
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ForceRefreshParameterFalseTestAsync()
        {
            var cache = new TokenCache();
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);

            var authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false).CanonicalAuthority;
            var app = new ConfidentialClientApplication(TestConstants.ClientId, authority,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                null, cache)
            {
                ValidateAuthority = false
            };

            var accessTokens = cache.GetAllAccessTokensForClient(new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
            var accessTokenInCache = accessTokens.Where(
                    item =>
                        item.ScopeSet.ScopeContains(TestConstants.Scope))
                .ToList()
                .FirstOrDefault();

            //add mock to fail in case of network call
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                ExceptionToThrow = new AssertFailedException("Unexpected Network Call")
            });

            var task = app.AcquireTokenForClientAsync(TestConstants.Scope, false);
            var result = task.Result;

            Assert.AreEqual(accessTokenInCache?.AccessToken, result.AccessToken);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task ForceRefreshParameterTrueTestAsync()
        {
            var cache = new TokenCache();
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);

            var authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false).CanonicalAuthority;
            var app = new ConfidentialClientApplication(TestConstants.ClientId, authority,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                null, cache)
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            //add mock response for successful token retrival
            const string tokenRetrievedFromNetCall = "token retrieved from network call";
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Post,
                ResponseMessage =
                    MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(tokenRetrievedFromNetCall)
            });

            var result = await app.AcquireTokenForClientAsync(TestConstants.Scope, true);
            Assert.AreEqual(tokenRetrievedFromNetCall, result.AccessToken);

            // make sure token in Cache was updated
            var accessTokens = cache.GetAllAccessTokensForClient(new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
            var accessTokenInCache = accessTokens.Where(
                    item =>
                        item.ScopeSet.ScopeContains(TestConstants.Scope))
                .ToList()
                .FirstOrDefault();

            Assert.AreEqual(tokenRetrievedFromNetCall, accessTokenInCache?.AccessToken);
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.WasSuccessfulKey] == "true"
                && anEvent[ApiEvent.ApiIdKey] == "727"));
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public async Task AuthorizationCodeRequestTest()
        {
            TokenCache cache = new TokenCache()
            {
                BeforeAccess = BeforeCacheAccess,
                AfterAccess = AfterCacheAccess
            };

            ClientCredential cc =
                new ClientCredential("secret");
            var app = new ConfidentialClientApplication(TestConstants.ClientId, "https://" + TestConstants.ProductionEnvironment + "/tfp/home/policy",
                TestConstants.RedirectUri, cc, cache, null)
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant, "p=policy")
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            AuthenticationResult result = await app.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.Scope).ConfigureAwait(false);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, app.UserTokenCache.TokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(1, app.UserTokenCache.TokenCacheAccessor.RefreshTokenCacheDictionary.Count);

            cache = new TokenCache()
            {
                BeforeAccess = BeforeCacheAccess,
                AfterAccess = AfterCacheAccess
            };

            app = new ConfidentialClientApplication(TestConstants.ClientId, "https://" + TestConstants.ProductionEnvironment + "/tfp/home/policy",
                TestConstants.RedirectUri, cc, cache, null)
            {
                ValidateAuthority = false
            };

            Assert.AreEqual(1, app.Users.Count());
        }

        private void BeforeCacheAccess(TokenCacheNotificationArgs args)
        {
            args.TokenCache.Deserialize(serializedCache);
        }

        private void AfterCacheAccess(TokenCacheNotificationArgs args)
        {
            serializedCache = args.TokenCache.Serialize();
        }
    }
}
