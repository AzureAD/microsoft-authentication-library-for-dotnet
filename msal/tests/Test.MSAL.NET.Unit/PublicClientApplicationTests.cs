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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.Telemetry;
using Microsoft.Identity.Core.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Test.Microsoft.Identity.Core.Unit.Mocks;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class PublicClientApplicationTests
    {
        private TokenCache _cache;
        private readonly MyReceiver _myReceiver = new MyReceiver();

        [TestInitialize]
        public void TestInitialize()
        {
            ModuleInitializer.ForceModuleInitializationTestOnly();

            _cache = new TokenCache();
            Authority.ValidatedAuthorities.Clear();
            Telemetry.GetInstance().RegisterReceiver(_myReceiver.OnEvents);

            AadInstanceDiscovery.Instance.Cache.Clear();
            // AddMockResponseForInstanceDisovery();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _cache.tokenCacheAccessor.ClearAccessTokens();
            _cache.tokenCacheAccessor.ClearRefreshTokens();
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [Description("Tests the public interfaces can be mocked")]
        public void MockPublicClientApplication()
        {
            // Setup up a public client application that returns a dummy result
            // The caller asks for two scopes, but only one is returned
            var mockResult = Substitute.For<AuthenticationResult>();
            mockResult.IdToken.Returns("id token");
            mockResult.Scopes.Returns(new string[] { "scope1" });

            var mockApp = Substitute.For<IPublicClientApplication>();
            mockApp.AcquireTokenAsync(new string[] { "scope1", "scope2" }).ReturnsForAnyArgs(mockResult);

            // Now call the substitute with the args to get the substitute result
            AuthenticationResult actualResult = mockApp.AcquireTokenAsync(new string[] { "scope1" }).Result;
            Assert.IsNotNull(actualResult);
            Assert.AreEqual("id token", actualResult.IdToken, "Mock result failed to return the expected id token");

            // Check the users properties returns the dummy users
            IEnumerable<string> scopes = actualResult.Scopes;
            Assert.IsNotNull(scopes);
            CollectionAssert.AreEqual(new string[] { "scope1" }, actualResult.Scopes.ToArray());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [Description("Tests the public application interfaces can be mocked to throw MSAL exceptions")]
        public void MockPublicClientApplication_Exception()
        {
            // Setup up a confidential client application that returns throws
            var mockApp = Substitute.For<IPublicClientApplication>();
            mockApp
                .WhenForAnyArgs(x => x.AcquireTokenAsync(Arg.Any<string[]>()))
                .Do(x => throw new MsalServiceException("my error code", "my message"));


            // Now call the substitute and check the exception is thrown
            MsalServiceException ex =
                AssertException.Throws<MsalServiceException>(() => mockApp.AcquireTokenAsync(new string[] { "scope1" }));
            Assert.AreEqual("my error code", ex.ErrorCode);
            Assert.AreEqual("my message", ex.Message);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void ConstructorsTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);
            Assert.IsNotNull(app);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);

            app = new PublicClientApplication(TestConstants.ClientId, TestConstants.AuthorityGuestTenant);
            Assert.IsNotNull(app);
            Assert.AreEqual(TestConstants.AuthorityGuestTenant, app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);

            app = new PublicClientApplication(TestConstants.ClientId,
                "https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/B2C_1_B2C_Signup_Signin_Policy/oauth2/v2.0");
            Assert.IsNotNull(app);
            Assert.AreEqual(
                "https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/b2c_1_b2c_signup_signin_policy/",
                app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task NoStateReturnedTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    AddStateInAuthorizationResult = false,
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        TestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(ui);
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
                    Assert.Fail("API should have failed here");
                }
                catch (MsalClientException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalClientException.StateMismatchError, exc.ErrorCode);
                }

                Assert.IsNotNull(
                    _myReceiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                            anEvent[ApiEvent.ApiIdKey] == "170" && anEvent[ApiEvent.WasSuccessfulKey] == "false" &&
                            anEvent[ApiEvent.ApiErrorCodeKey] == "state_mismatch"));
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task DifferentStateReturnedTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    AddStateInAuthorizationResult = false,
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        TestConstants.AuthorityHomeTenant + "?code=some-code&state=mistmatched")
                };

                MsalMockHelpers.ConfigureMockWebUI(ui);
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
                    Assert.Fail("API should have failed here");
                }
                catch (MsalClientException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalClientException.StateMismatchError, exc.ErrorCode);
                }
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenNoClientInfoReturnedTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        TestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            "some-scope1 some-scope2",
                            MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                            string.Empty)
                    });

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
                }
                catch (MsalClientException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalClientException.JsonParseError, exc.ErrorCode);
                    Assert.AreEqual("client info is null", exc.Message);
                }
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSameUserTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        TestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(TestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.Account.HomeAccountId);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                result = app.AcquireTokenAsync(TestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.Account.HomeAccountId);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenAddTwoUsersTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        TestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(TestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.Account.HomeAccountId);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.Utid, result.TenantId);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.Scope.ToString(),
                            MockHelpers.CreateIdToken(
                                TestConstants.UniqueId + "more",
                                TestConstants.DisplayableId + "more",
                                TestConstants.Utid + "more"),
                            MockHelpers.CreateClientInfo(TestConstants.Uid + "more", TestConstants.Utid + "more"))
                    });

                result = app.AcquireTokenAsync(TestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId + "more", result.UniqueId);
                Assert.AreEqual(
                    TestConstants.CreateUserIdentifier(TestConstants.Uid + "more", TestConstants.Utid + "more"),
                    result.Account.HomeAccountId);
                Assert.AreEqual(TestConstants.DisplayableId + "more", result.Account.Username);
                Assert.AreEqual(TestConstants.Utid + "more", result.TenantId);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenDifferentUserReturnedFromServiceTest()
        {
            _cache.ClientId = TestConstants.ClientId;

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        TestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(TestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.Account.HomeAccountId);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);

                // TODO: allow checking in the middle of a using block --> Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

                var dict = new Dictionary<string, string>();
                dict[OAuth2Parameter.DomainReq] = TestConstants.Utid;
                dict[OAuth2Parameter.LoginReq] = TestConstants.Uid;

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"),
                    dict);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.Scope.AsSingleString(),
                            MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                            MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid + "more"))
                    });

                try
                {
                    result = app.AcquireTokenAsync(TestConstants.Scope, result.Account, UIBehavior.SelectAccount, null).Result;
                    Assert.Fail("API should have failed here");
                }
                catch (AggregateException ex)
                {
                    MsalClientException exc = (MsalClientException)ex.InnerException;
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.UserMismatch, exc.ErrorCode);
                }

                Assert.IsNotNull(
                    _myReceiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                            anEvent[ApiEvent.ApiIdKey] == "174" && anEvent[ApiEvent.WasSuccessfulKey] == "false" &&
                            anEvent[ApiEvent.ApiErrorCodeKey] == "user_mismatch"));

                var users = app.GetAccountsAsync().Result;
                Assert.AreEqual(1, users.Count());
                Assert.AreEqual(1, _cache.tokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenNullUserPassedInAndNewUserReturnedFromServiceTest()
        {
            _cache.ClientId = TestConstants.ClientId;

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        TestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(TestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifer(), result.Account.HomeAccountId);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                // TODO: Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.Scope.AsSingleString(),
                            MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                            MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid + "more"))
                    });

                result = app.AcquireTokenAsync(TestConstants.Scope, (IAccount)null, UIBehavior.SelectAccount, null).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(
                    TestConstants.CreateUserIdentifier(TestConstants.Uid, TestConstants.Utid + "more"),
                    result.Account.HomeAccountId);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                var users = app.GetAccountsAsync().Result;
                Assert.AreEqual(2, users.Count());
                Assert.AreEqual(2, _cache.tokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                IEnumerable<IAccount> users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.IsFalse(users.Any());
                _cache = new TokenCache()
                {
                    ClientId = TestConstants.ClientId
                };

                app.UserTokenCache = _cache;
                TokenCacheHelper.PopulateCache(_cache.tokenCacheAccessor);
                users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.AreEqual(1, users.Count());

                var atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    "Bearer",
                    TestConstants.Scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                    MockHelpers.CreateClientInfo());

                atItem.Secret = atItem.GetKey().ToString();
                _cache.tokenCacheAccessor.SaveAccessToken(atItem);


                // another cache entry for different uid. user count should be 2.

                MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo("uId1", "uTId1"));

                _cache.tokenCacheAccessor.SaveRefreshToken(rtItem);

                MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                    MockHelpers.CreateClientInfo("uId1", "uTId1"),
                    "uTId1");

                _cache.tokenCacheAccessor.SaveIdToken(idTokenCacheItem);


                MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    null,
                    MockHelpers.CreateClientInfo("uId1", "uTId1"),
                    null,
                    null,
                    "uTId1");

                _cache.tokenCacheAccessor.SaveAccount(accountCacheItem);


                Assert.AreEqual(2, _cache.tokenCacheAccessor.RefreshTokenCount);
                users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.AreEqual(2, users.Count());

                // another cache entry for different environment. user count should still be 2. Sovereign cloud user must not be returned
                rtItem = new MsalRefreshTokenCacheItem(
                    TestConstants.SovereignEnvironment,
                    TestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo(TestConstants.Uid + "more1", TestConstants.Utid));

                _cache.tokenCacheAccessor.SaveRefreshToken(rtItem);
                Assert.AreEqual(3, _cache.tokenCacheAccessor.RefreshTokenCount);
                users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.AreEqual(2, users.Count());
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersAndSignThemOutTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                UserTokenCache = new TokenCache()
                {
                    ClientId = TestConstants.ClientId
                }
            };
            TokenCacheHelper.PopulateCache(_cache.tokenCacheAccessor);

            foreach (var user in app.GetAccountsAsync().Result)
            {
                app.RemoveAsync(user).Wait();
            }

            Assert.AreEqual(0, app.UserTokenCache.tokenCacheAccessor.AccessTokenCount);
            Assert.AreEqual(0, app.UserTokenCache.tokenCacheAccessor.RefreshTokenCount);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenSilentScopeAndEmptyCacheTestAsync()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId)
                {
                    ValidateAuthority = false
                };

            _cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            try
            {
                AuthenticationResult result = await app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                        new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null)).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException exc)
            {
                Assert.AreEqual(MsalUiRequiredException.NoTokensFoundError, exc.ErrorCode);
            }
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.ApiIdKey] == "30"
                && anEvent[ApiEvent.WasSuccessfulKey] == "false" && anEvent[ApiEvent.ApiErrorCodeKey] == "no_tokens_found"
                ));
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenSilentScopeAndUserMultipleTokensFoundTestAsync()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId)
                {
                    ValidateAuthority = false
                };

            _cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            TokenCacheHelper.PopulateCache(_cache.tokenCacheAccessor);
            try
            {
                AuthenticationResult result = await app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                        new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null)).ConfigureAwait(false);
            }
            catch (MsalClientException exc)
            {
                Assert.AreEqual(MsalClientException.MultipleTokensMatchedError, exc.ErrorCode);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentScopeAndUserOverloadWithNoMatchingScopesInCacheTest()
        {
            // this test ensures that the API can
            // get authority (if unique) from the cache entries where scope does not match.
            // it should only happen for case where no authority is passed.

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    ValidateAuthority = false
                };

                _cache = new TokenCache()
                {
                    ClientId = TestConstants.ClientId,
                    HttpManager = httpManager
                };

                // add mock response for tenant endpoint discovery

                // TODO: validate with others that this test is still valid.
                // Previous test was NOT validating that we flushed the mock input queue
                // and these are NOT being consumed in this case.  ensure that's correct.

                //httpManager.AddMockHandler(
                //    new MockHttpMessageHandler
                //    {
                //        Method = HttpMethod.Get,
                //        ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
                //    });
                //httpManager.AddMockHandler(
                //    new MockHttpMessageHandler()
                //    {
                //        Method = HttpMethod.Post,
                //        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                //            TestConstants.UniqueId,
                //            TestConstants.DisplayableId,
                //            TestConstants.ScopeForAnotherResource.ToArray())
                //    });

                app.UserTokenCache = _cache;
                TokenCacheHelper.PopulateCache(_cache.tokenCacheAccessor);
                _cache.tokenCacheAccessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.Utid,
                        TestConstants.UserIdentifier.Identifier,
                        TestConstants.ClientId,
                        TestConstants.ScopeForAnotherResourceStr));

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    TestConstants.ScopeForAnotherResource.ToArray(),
                    new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null));

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.ScopeForAnotherResource.AsSingleString(), result.Scopes.AsSingleString());
                Assert.AreEqual(2, _cache.tokenCacheAccessor.GetAllAccessTokensAsString().Count());
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentScopeAndUserOverloadDefaultAuthorityTest()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId)
                {
                    ValidateAuthority = false
                };

            _cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            TokenCacheHelper.PopulateCache(_cache.tokenCacheAccessor);
            _cache.tokenCacheAccessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.Utid,
                TestConstants.UserIdentifier.Identifier,
                TestConstants.ClientId,
                TestConstants.ScopeForAnotherResourceStr));

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null));

            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentScopeAndUserOverloadTenantSpecificAuthorityTest()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId, TestConstants.AuthorityGuestTenant)
                {
                    ValidateAuthority = false
                };

            _cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            TokenCacheHelper.PopulateCache(_cache.tokenCacheAccessor);
            _cache.tokenCacheAccessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.Utid,
                TestConstants.UserIdentifier.Identifier,
                TestConstants.ClientId,
                TestConstants.ScopeForAnotherResourceStr));

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null));
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentCacheOnlyLookupTest()
        {
            PublicClientApplication app =
                new PublicClientApplication(TestConstants.ClientId, TestConstants.AuthorityTestTenant)
                {
                    ValidateAuthority = false
                };

            _cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            TokenCacheHelper.PopulateCache(_cache.tokenCacheAccessor);
            _cache.tokenCacheAccessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.Utid,
                TestConstants.UserIdentifier.Identifier,
                TestConstants.ClientId,
                TestConstants.ScopeForAnotherResourceStr));

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(),
                new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null), app.Authority, false);

            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.WasSuccessfulKey] == "true"
                && anEvent[ApiEvent.ApiIdKey] == "31"));
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentForceRefreshTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    ValidateAuthority = false
                };

                _cache = new TokenCache()
                {
                    ClientId = TestConstants.ClientId
                };

                app.UserTokenCache = _cache;
                TokenCacheHelper.PopulateCache(_cache.tokenCacheAccessor);

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.UniqueId,
                            TestConstants.DisplayableId,
                            TestConstants.Scope.ToArray())
                    });

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    TestConstants.Scope.ToArray(),
                    new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null),
                    null,
                    true);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.Scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(2, _cache.tokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(1, _cache.tokenCacheAccessor.RefreshTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentServiceErrorTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    ValidateAuthority = false
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);

                //populate cache
                _cache = new TokenCache()
                {
                    ClientId = TestConstants.ClientId
                };

                app.UserTokenCache = _cache;
                TokenCacheHelper.PopulateCache(_cache.tokenCacheAccessor);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage()
                    });
                try
                {
                    Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                        TestConstants.CacheMissScope,
                        new Account(TestConstants.UserIdentifier, TestConstants.DisplayableId, null),
                        app.Authority,
                        false);
                    AuthenticationResult result = task.Result;
                    Assert.Fail("MsalUiRequiredException was expected");
                }
                catch (AggregateException ex)
                {
                    Assert.IsNotNull(ex.InnerException);
                    Assert.IsTrue(ex.InnerException is MsalUiRequiredException);
                    var msalExc = (MsalUiRequiredException)ex.InnerException;
                    Assert.AreEqual(msalExc.ErrorCode, MsalUiRequiredException.InvalidGrantError);
                }
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [ExpectedException(typeof(HttpRequestException),
            "Cannot write more bytes to the buffer than the configured maximum buffer size: 1048576.")]
        public async Task HttpRequestExceptionIsNotSuppressedAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                // add mock response bigger than 1MB for Http Client
                httpManager.AddMockHandlerTooLargeGetResponse();
                await app.AcquireTokenAsync(TestConstants.Scope.ToArray()).ConfigureAwait(false);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AuthUiFailedExceptionTestAsync()
        {
            _cache.ClientId = TestConstants.ClientId;
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache

                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    new MockWebUI()
                    {
                        ExceptionToThrow = new MsalClientException(
                            MsalClientException.AuthenticationUiFailedError,
                            "Failed to invoke webview",
                            new InvalidOperationException("some-inner-Exception"))
                    });

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
                    Assert.Fail("API should have failed here");
                }
                catch (MsalClientException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalClientException.AuthenticationUiFailedError, exc.ErrorCode);
                    Assert.AreEqual("some-inner-Exception", exc.InnerException.Message);
                }
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUserTest()
        {
            var app = new PublicClientApplication(TestConstants.ClientId);
            var users = app.GetAccountsAsync().Result;
            Assert.IsNotNull(users);
            // no users in the cache
            Assert.AreEqual(0, users.Count());

            var fetchedUser = app.GetAccountAsync(null).Result;
            Assert.IsNull(fetchedUser);

            fetchedUser = app.GetAccountAsync("").Result;
            Assert.IsNull(fetchedUser);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCache.tokenCacheAccessor, TestConstants.Uid,
                TestConstants.Utid, TestConstants.Name);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCache.tokenCacheAccessor, TestConstants.Uid,
                TestConstants.Utid);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCache.tokenCacheAccessor, TestConstants.Uid + "1",
                TestConstants.Utid, TestConstants.Name + "1");
            TokenCacheHelper.AddAccountToCache(app.UserTokenCache.tokenCacheAccessor, TestConstants.Uid + "1",
                TestConstants.Utid);

            users = app.GetAccountsAsync().Result;
            Assert.IsNotNull(users);
            // two users in the cache
            Assert.AreEqual(2, users.Count());

            var userToFind = users.First();

            fetchedUser = app.GetAccountAsync(userToFind.HomeAccountId.Identifier).Result;

            Assert.AreEqual(userToFind.Username, fetchedUser.Username);
            Assert.AreEqual(userToFind.HomeAccountId, fetchedUser.HomeAccountId);
            Assert.AreEqual(userToFind.Environment, fetchedUser.Environment);
        }

        [TestMethod]
        [Description("Test for AcquireToken with user canceling authentication")]
        public async Task AcquireTokenWithAuthenticationCanceledTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                // Interactive call and user cancels authentication
                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.UserCancel,
                        TestConstants.AuthorityHomeTenant + "?error=user_canceled")
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);
                MsalMockHelpers.ConfigureMockWebUI(ui);

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
                }
                catch (MsalClientException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual("authentication_canceled", exc.ErrorCode);
                    Assert.IsNotNull(
                        _myReceiver.EventsReceived.Find(
                            anEvent => // Expect finding such an event
                                anEvent[EventBase.EventNameKey].EndsWith("ui_event") &&
                                anEvent[UiEvent.UserCancelledKey] == "true"));
                    return;
                }
            }

            Assert.Fail("Should not reach here. Exception was not thrown.");
        }

        [TestMethod]
        [Description("Test for AcquireToken with access denied error. This error will occur if" +
            "user cancels authentication with embedded webview")]
        public async Task AcquireTokenWithAccessDeniedErrorTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                // Interactive call and authentication fails with access denied
                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.ProtocolError,
                        TestConstants.AuthorityHomeTenant + "?error=access_denied")
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);
                MsalMockHelpers.ConfigureMockWebUI(ui);

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(TestConstants.Scope).ConfigureAwait(false);
                }
                catch (MsalServiceException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual("access_denied", exc.ErrorCode);
                    Assert.IsNotNull(
                        _myReceiver.EventsReceived.Find(
                            anEvent => // Expect finding such an event
                                anEvent[EventBase.EventNameKey].EndsWith("ui_event") &&
                                anEvent[UiEvent.AccessDeniedKey] == "true"));
                    return;
                }
            }

            Assert.Fail("Should not reach here. Exception was not thrown.");
        }

        [TestMethod]
        [Description("ClientApplicationBase.GetAuthoriy tests")]
        public void GetAuthority_AccountWithNullIdPassed_CommonAuthorityUsed()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            var authoriy = app.GetAuthority(new Account(null, TestConstants.Name, TestConstants.ProductionPrefNetworkEnvironment));
            Assert.AreEqual(ClientApplicationBase.DefaultAuthority, authoriy.CanonicalAuthority);
        }

        [TestMethod]
        [Description("ClientApplicationBase.GetAuthoriy tests")]
        public void GetAuthority_AccountWithIdPassed_TenantedAuthorityUsed()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId);

            var authority = app.GetAuthority(
                new Account(
                    new AccountId("objectId." + TestConstants.Utid, "objectId", TestConstants.Utid),
                    TestConstants.Name,
                    TestConstants.ProductionPrefNetworkEnvironment));

            Assert.AreEqual(TestConstants.AuthorityTestTenant, authority.CanonicalAuthority);
        }

        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenSilentNullAccountErrorTestAsync()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.ClientId)
            {
                ValidateAuthority = false
            };

            _cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            try
            {
                AuthenticationResult result = await app.AcquireTokenSilentAsync(TestConstants.Scope.ToArray(), null).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual("user_null", MsalUiRequiredException.UserNullError);
            }
        }
    }
}