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

#if !NET_CORE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.Telemetry;
using Microsoft.Identity.Core.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Test.Microsoft.Identity.Core.Unit;
using Test.Microsoft.Identity.Core.Unit.Mocks;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class PublicClientApplicationTests
    {
        private TokenCache _cache;
        private MyReceiver _myReceiver;
        private ITelemetryManager _telemetryManager;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();

            _cache = new TokenCache();
            _myReceiver = new MyReceiver();
            _telemetryManager = new TelemetryManager(_myReceiver);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _cache.TokenCacheAccessor.ClearAccessTokens();
            _cache.TokenCacheAccessor.ClearRefreshTokens();
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [Description("Tests the public interfaces can be mocked")]
        public void MockPublicClientApplication()
        {
            // Setup up a public client application that returns a dummy result
            // The caller asks for two scopes, but only one is returned
            var mockResult = new AuthenticationResult(
               accessToken: "",
               isExtendedLifeTimeToken: false,
               uniqueId: "",
               expiresOn: DateTimeOffset.Now,
               extendedExpiresOn: DateTimeOffset.Now,
               tenantId: "",
               account: null,
               idToken: "id token",
               scopes: new[] { "scope1"});

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
            PublicClientApplication app = new PublicClientApplication(MsalTestConstants.ClientId);
            Assert.IsNotNull(app);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(MsalTestConstants.ClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);

            app = new PublicClientApplication(MsalTestConstants.ClientId, MsalTestConstants.AuthorityGuestTenant);
            Assert.IsNotNull(app);
            Assert.AreEqual(MsalTestConstants.AuthorityGuestTenant, app.Authority);
            Assert.AreEqual(MsalTestConstants.ClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);

            app = new PublicClientApplication(MsalTestConstants.ClientId,
                "https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/B2C_1_B2C_Signup_Signin_Policy/oauth2/v2.0");
            Assert.IsNotNull(app);
            Assert.AreEqual(
                "https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/b2c_1_b2c_signup_signin_policy/",
                app.Authority);
            Assert.AreEqual(MsalTestConstants.ClientId, app.ClientId);
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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    AddStateInAuthorizationResult = false,
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(ui);
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(MsalTestConstants.Scope).ConfigureAwait(false);
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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    AddStateInAuthorizationResult = false,
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code&state=mistmatched")
                };

                MsalMockHelpers.ConfigureMockWebUI(ui);
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(MsalTestConstants.Scope).ConfigureAwait(false);
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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            "some-scope1 some-scope2",
                            MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                            string.Empty)
                    });

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(MsalTestConstants.Scope).ConfigureAwait(false);
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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifer(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifer(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifer(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Utid, result.TenantId);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.Scope.ToString(),
                            MockHelpers.CreateIdToken(
                                MsalTestConstants.UniqueId + "more",
                                MsalTestConstants.DisplayableId + "more",
                                MsalTestConstants.Utid + "more"),
                            MockHelpers.CreateClientInfo(MsalTestConstants.Uid + "more", MsalTestConstants.Utid + "more"))
                    });

                result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId + "more", result.UniqueId);
                Assert.AreEqual(
                    MsalTestConstants.CreateUserIdentifier(MsalTestConstants.Uid + "more", MsalTestConstants.Utid + "more"),
                    result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId + "more", result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Utid + "more", result.TenantId);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenDifferentUserReturnedFromServiceTest()
        {
            _cache.ClientId = MsalTestConstants.ClientId;

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifer(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);

                // TODO: allow checking in the middle of a using block --> Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

                var dict = new Dictionary<string, string>
                {
                    [OAuth2Parameter.DomainReq] = MsalTestConstants.Utid,
                    [OAuth2Parameter.LoginReq] = MsalTestConstants.Uid
                };

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"),
                    dict);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.Scope.AsSingleString(),
                            MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                            MockHelpers.CreateClientInfo(MsalTestConstants.Uid, MsalTestConstants.Utid + "more"))
                    });

                try
                {
                    result = app.AcquireTokenAsync(MsalTestConstants.Scope, result.Account, UIBehavior.SelectAccount, null).Result;
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
                Assert.AreEqual(1, _cache.TokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenNullUserPassedInAndNewUserReturnedFromServiceTest()
        {
            _cache.ClientId = MsalTestConstants.ClientId;

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache
                };

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifer(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                // TODO: Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.Scope.AsSingleString(),
                            MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                            MockHelpers.CreateClientInfo(MsalTestConstants.Uid, MsalTestConstants.Utid + "more"))
                    });

                result = app.AcquireTokenAsync(MsalTestConstants.Scope, (IAccount)null, UIBehavior.SelectAccount, null).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(
                    MsalTestConstants.CreateUserIdentifier(MsalTestConstants.Uid, MsalTestConstants.Utid + "more"),
                    result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                var users = app.GetAccountsAsync().Result;
                Assert.AreEqual(2, users.Count());
                Assert.AreEqual(2, _cache.TokenCacheAccessor.AccessTokenCount);
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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                IEnumerable<IAccount> users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.IsFalse(users.Any());
                _cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId
                };

                app.UserTokenCache = _cache;
                TokenCacheHelper.PopulateCache(_cache.TokenCacheAccessor);
                users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.AreEqual(1, users.Count());

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "Bearer",
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(7200)),
                    MockHelpers.CreateClientInfo());

                atItem.Secret = atItem.GetKey().ToString();
                _cache.TokenCacheAccessor.SaveAccessToken(atItem);


                // another cache entry for different uid. user count should be 2.

                MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo("uId1", "uTId1"));

                _cache.TokenCacheAccessor.SaveRefreshToken(rtItem);

                MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                    MockHelpers.CreateClientInfo("uId1", "uTId1"),
                    "uTId1");

                _cache.TokenCacheAccessor.SaveIdToken(idTokenCacheItem);


                MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    null,
                    MockHelpers.CreateClientInfo("uId1", "uTId1"),
                    null,
                    null,
                    "uTId1",
                    null,
                    null);

                _cache.TokenCacheAccessor.SaveAccount(accountCacheItem);


                Assert.AreEqual(2, _cache.TokenCacheAccessor.RefreshTokenCount);
                users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.AreEqual(2, users.Count());

                // another cache entry for different environment. user count should still be 2. Sovereign cloud user must not be returned
                rtItem = new MsalRefreshTokenCacheItem(
                    MsalTestConstants.SovereignEnvironment,
                    MsalTestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo(MsalTestConstants.Uid + "more1", MsalTestConstants.Utid));

                _cache.TokenCacheAccessor.SaveRefreshToken(rtItem);
                Assert.AreEqual(3, _cache.TokenCacheAccessor.RefreshTokenCount);
                users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.AreEqual(2, users.Count());
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersAndSignThemOutTest()
        {
            PublicClientApplication app = new PublicClientApplication(MsalTestConstants.ClientId)
            {
                UserTokenCache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId
                }
            };
            TokenCacheHelper.PopulateCache(_cache.TokenCacheAccessor);

            foreach (var user in app.GetAccountsAsync().Result)
            {
                app.RemoveAsync(user).Wait();
            }

            Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.AccessTokenCount);
            Assert.AreEqual(0, app.UserTokenCache.TokenCacheAccessor.RefreshTokenCount);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenSilentScopeAndEmptyCacheTestAsync()
        {
            PublicClientApplication app =
                new PublicClientApplication(null, _telemetryManager, MsalTestConstants.ClientId, ClientApplicationBase.DefaultAuthority)
                {
                    ValidateAuthority = false
                };

            _cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            try
            {
                AuthenticationResult result = await app.AcquireTokenSilentAsync(MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null)).ConfigureAwait(false);
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
                new PublicClientApplication(MsalTestConstants.ClientId)
                {
                    ValidateAuthority = false
                };

            _cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            TokenCacheHelper.PopulateCache(_cache.TokenCacheAccessor);
            try
            {
                AuthenticationResult result = await app.AcquireTokenSilentAsync(MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null)).ConfigureAwait(false);
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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    ValidateAuthority = false
                };

                var aadInstanceDiscovery = new AadInstanceDiscovery(httpManager, new TelemetryManager());

                _cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    AadInstanceDiscovery = aadInstanceDiscovery
                };

                app.UserTokenCache = _cache;
                TokenCacheHelper.PopulateCache(_cache.TokenCacheAccessor);
                _cache.TokenCacheAccessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        MsalTestConstants.ProductionPrefNetworkEnvironment,
                        MsalTestConstants.Utid,
                        MsalTestConstants.UserIdentifier,
                        MsalTestConstants.ClientId,
                        MsalTestConstants.ScopeForAnotherResourceStr));

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    MsalTestConstants.ScopeForAnotherResource.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null));

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.ScopeForAnotherResource.AsSingleString(), result.Scopes.AsSingleString());
                Assert.AreEqual(2, _cache.TokenCacheAccessor.GetAllAccessTokensAsString().Count());
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentScopeAndUserOverloadDefaultAuthorityTest()
        {
            PublicClientApplication app =
                new PublicClientApplication(MsalTestConstants.ClientId)
                {
                    ValidateAuthority = false
                };

            _cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            TokenCacheHelper.PopulateCache(_cache.TokenCacheAccessor);
            _cache.TokenCacheAccessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                MsalTestConstants.Utid,
                MsalTestConstants.UserIdentifier,
                MsalTestConstants.ClientId,
                MsalTestConstants.ScopeForAnotherResourceStr));

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(MsalTestConstants.Scope.ToArray(),
                new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null));

            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentScopeAndUserOverloadTenantSpecificAuthorityTest()
        {
            PublicClientApplication app =
                new PublicClientApplication(MsalTestConstants.ClientId, MsalTestConstants.AuthorityGuestTenant)
                {
                    ValidateAuthority = false
                };

            _cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            TokenCacheHelper.PopulateCache(_cache.TokenCacheAccessor);
            _cache.TokenCacheAccessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                MsalTestConstants.Utid,
                MsalTestConstants.UserIdentifier,
                MsalTestConstants.ClientId,
                MsalTestConstants.ScopeForAnotherResourceStr));

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(MsalTestConstants.Scope.ToArray(),
                new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null));
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentCacheOnlyLookupTest()
        {
            PublicClientApplication app =
                new PublicClientApplication(null, _telemetryManager, MsalTestConstants.ClientId, MsalTestConstants.AuthorityTestTenant)
                {
                    ValidateAuthority = false
                };

            _cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            TokenCacheHelper.PopulateCache(_cache.TokenCacheAccessor);
            _cache.TokenCacheAccessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
                MsalTestConstants.ProductionPrefNetworkEnvironment,
                MsalTestConstants.Utid,
                MsalTestConstants.UserIdentifier,
                MsalTestConstants.ClientId,
                MsalTestConstants.ScopeForAnotherResourceStr));

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(MsalTestConstants.Scope.ToArray(),
                new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null), app.Authority, false);

            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
            Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    ValidateAuthority = false
                };

                _cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId
                };

                app.UserTokenCache = _cache;
                TokenCacheHelper.PopulateCache(_cache.TokenCacheAccessor);

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    null,
                    true);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(2, _cache.TokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(1, _cache.TokenCacheAccessor.RefreshTokenCount);
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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    ValidateAuthority = false
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);

                //populate cache
                _cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId
                };

                app.UserTokenCache = _cache;
                TokenCacheHelper.PopulateCache(_cache.TokenCacheAccessor);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage()
                    });
                try
                {
                    Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                        MsalTestConstants.CacheMissScope,
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
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
        public void HttpRequestExceptionIsNotSuppressedAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                // add mock response bigger than 1MB for Http Client
                httpManager.AddFailingRequest(new InvalidOperationException());

                AssertException.TaskThrows<InvalidOperationException>(
                    () => app.AcquireTokenAsync(MsalTestConstants.Scope.ToArray()));
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AuthUiFailedExceptionTestAsync()
        {
            _cache.ClientId = MsalTestConstants.ClientId;
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache = _cache

                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);

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
                    AuthenticationResult result = await app.AcquireTokenAsync(MsalTestConstants.Scope).ConfigureAwait(false);
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
            var app = new PublicClientApplication(MsalTestConstants.ClientId);
            var users = app.GetAccountsAsync().Result;
            Assert.IsNotNull(users);
            // no users in the cache
            Assert.AreEqual(0, users.Count());

            var fetchedUser = app.GetAccountAsync(null).Result;
            Assert.IsNull(fetchedUser);

            fetchedUser = app.GetAccountAsync("").Result;
            Assert.IsNull(fetchedUser);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCache.TokenCacheAccessor, MsalTestConstants.Uid,
                MsalTestConstants.Utid, MsalTestConstants.Name);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCache.TokenCacheAccessor, MsalTestConstants.Uid,
                MsalTestConstants.Utid);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCache.TokenCacheAccessor, MsalTestConstants.Uid + "1",
                MsalTestConstants.Utid, MsalTestConstants.Name + "1");
            TokenCacheHelper.AddAccountToCache(app.UserTokenCache.TokenCacheAccessor, MsalTestConstants.Uid + "1",
                MsalTestConstants.Utid);

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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                // Interactive call and user cancels authentication
                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.UserCancel,
                        MsalTestConstants.AuthorityHomeTenant + "?error=user_canceled")
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                MsalMockHelpers.ConfigureMockWebUI(ui);

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(MsalTestConstants.Scope).ConfigureAwait(false);
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
                    _telemetryManager,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority);

                // Interactive call and authentication fails with access denied
                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.ProtocolError,
                        MsalTestConstants.AuthorityHomeTenant + "?error=access_denied")
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                MsalMockHelpers.ConfigureMockWebUI(ui);

                try
                {
                    AuthenticationResult result = await app.AcquireTokenAsync(MsalTestConstants.Scope).ConfigureAwait(false);
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
        public void GetAuthority_AccountWithNullIdPassed_CommonAuthorityReturned()
        {
            PublicClientApplication app = new PublicClientApplication(MsalTestConstants.ClientId);

            var authoriy = app.GetAuthority(new Account(null, MsalTestConstants.Name, MsalTestConstants.ProductionPrefNetworkEnvironment));
            Assert.AreEqual(ClientApplicationBase.DefaultAuthority, authoriy.CanonicalAuthority);
        }

        [TestMethod]
        [Description("ClientApplicationBase.GetAuthoriy tests")]
        public void GetAuthority_AccountWithIdPassed_TenantedAuthorityUsed()
        {
            PublicClientApplication app = new PublicClientApplication(MsalTestConstants.ClientId);

            var authority = app.GetAuthority(
                new Account(
                    "objectId." + MsalTestConstants.Utid,
                    MsalTestConstants.Name,
                    MsalTestConstants.ProductionPrefNetworkEnvironment));

            Assert.AreEqual(MsalTestConstants.AuthorityTestTenant, authority.CanonicalAuthority);
        }

        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenSilentNullAccountErrorTestAsync()
        {
            PublicClientApplication app = new PublicClientApplication(MsalTestConstants.ClientId)
            {
                ValidateAuthority = false
            };

            _cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId
            };

            app.UserTokenCache = _cache;
            try
            {
                AuthenticationResult result = await app.AcquireTokenSilentAsync(MsalTestConstants.Scope.ToArray(), null).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual("user_null", MsalUiRequiredException.UserNullError);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void B2CLoginAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    new TelemetryManager(),
                    CoreTestConstants.ClientId,
                     CoreTestConstants.B2CLoginAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        CoreTestConstants.B2CLoginAuthority + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(CoreTestConstants.B2CLoginAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(CoreTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void B2CAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    new TelemetryManager(),
                    CoreTestConstants.ClientId,
                    CoreTestConstants.B2CAuthority);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        CoreTestConstants.B2CAuthority + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(CoreTestConstants.B2CAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(CoreTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void B2CAcquireTokenWithValidateAuthorityTrueTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = new PublicClientApplication(
                   httpManager,
                   new TelemetryManager(),
                   CoreTestConstants.ClientId,
                   CoreTestConstants.B2CLoginAuthority);
                app.ValidateAuthority = true;

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        CoreTestConstants.B2CLoginAuthority + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(CoreTestConstants.B2CLoginAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(CoreTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void B2CAcquireTokenWithValidateAuthorityTrueAndRandomAuthorityTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = new PublicClientApplication(
                  httpManager,
                  new TelemetryManager(),
                  CoreTestConstants.ClientId,
                  CoreTestConstants.B2CRandomHost);
                app.ValidateAuthority = true;

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        CoreTestConstants.B2CRandomHost + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(CoreTestConstants.B2CRandomHost);
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                AuthenticationResult result = app.AcquireTokenAsync(CoreTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }
    }
}
#endif