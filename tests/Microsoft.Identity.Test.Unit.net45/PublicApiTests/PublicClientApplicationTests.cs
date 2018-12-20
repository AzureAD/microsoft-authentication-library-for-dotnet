//----------------------------------------------------------------------
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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Config;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class PublicClientApplicationTests
    {
        private MyReceiver _myReceiver;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();

            _myReceiver = new MyReceiver();
        }

#if !NET_CORE
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
               scopes: new[] { "scope1" });

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
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId)
                .WithAuthority("https://login.microsoftonline.com/common/", true, true)
                .BuildConcrete();

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
            Assert.IsFalse(app.ValidateAuthority);   // TODO: need to validate this with others.  This SHOULD be false (as i've changed it to) since this is a B2C authority being used here.
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task NoStateReturnedTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant)
                    .WithHttpManager(httpManager)
                    .WithTelemetryCallback(_myReceiver)
                    .BuildConcrete();

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

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant)
                    .WithHttpManager(httpManager)
                    .WithTelemetryCallback(_myReceiver)
                    .BuildConcrete();

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

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant)
                    .WithHttpManager(httpManager)
                    .WithTelemetryCallback(_myReceiver)
                    .BuildConcrete();

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
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager, _myReceiver);
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant)
                    .WithHttpManager(httpManager)
                    .WithTelemetryCallback(_myReceiver)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityHomeTenant);

                AuthenticationResult result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityHomeTenant);

                result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenAddTwoUsersTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager, _myReceiver);
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant)
                    .WithHttpManager(httpManager)
                    .WithTelemetryCallback(_myReceiver)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityHomeTenant);

                AuthenticationResult result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
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
            var cache = new TokenCache
            {
                ClientId = MsalTestConstants.ClientId
            };

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant)
                    .WithHttpManager(httpManager)
                    .WithTelemetryCallback(_myReceiver)
                    .WithUserTokenCache(cache)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityHomeTenant);

                AuthenticationResult result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
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
                Assert.AreEqual(1, cache.TokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenNullUserPassedInAndNewUserReturnedFromServiceTest()
        {
            var cache = new TokenCache
            {
                ClientId = MsalTestConstants.ClientId
            };

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant)
                    .WithHttpManager(httpManager)
                    .WithTelemetryCallback(_myReceiver)
                    .WithUserTokenCache(cache)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityHomeTenant);

                AuthenticationResult result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
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
                Assert.AreEqual(2, cache.TokenCacheAccessor.AccessTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, ClientApplicationBase.DefaultAuthority)
                    .WithHttpManager(httpManager)
                    .WithTelemetryCallback(_myReceiver)
                    .BuildConcrete();

                IEnumerable<IAccount> users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.IsFalse(users.Any());
                var cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId
                };

                app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, ClientApplicationBase.DefaultAuthority)
                    .WithHttpManager(httpManager)
                    .WithTelemetryCallback(_myReceiver)
                    .WithUserTokenCache(cache)
                    .BuildConcrete();

                TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
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
                cache.TokenCacheAccessor.SaveAccessToken(atItem);

                // another cache entry for different uid. user count should be 2.

                MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo("uId1", "uTId1"));

                cache.TokenCacheAccessor.SaveRefreshToken(rtItem);

                MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                    MockHelpers.CreateClientInfo("uId1", "uTId1"),
                    "uTId1");

                cache.TokenCacheAccessor.SaveIdToken(idTokenCacheItem);

                MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    null,
                    MockHelpers.CreateClientInfo("uId1", "uTId1"),
                    null,
                    null,
                    "uTId1",
                    null,
                    null);

                cache.TokenCacheAccessor.SaveAccount(accountCacheItem);

                Assert.AreEqual(2, cache.TokenCacheAccessor.RefreshTokenCount);
                users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.AreEqual(2, users.Count());

                // another cache entry for different environment. user count should still be 2. Sovereign cloud user must not be returned
                rtItem = new MsalRefreshTokenCacheItem(
                    MsalTestConstants.SovereignEnvironment,
                    MsalTestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo(MsalTestConstants.Uid + "more1", MsalTestConstants.Utid));

                cache.TokenCacheAccessor.SaveRefreshToken(rtItem);
                Assert.AreEqual(3, cache.TokenCacheAccessor.RefreshTokenCount);
                users = app.GetAccountsAsync().Result;
                Assert.IsNotNull(users);
                Assert.AreEqual(2, users.Count());
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersAndSignThemOutTest()
        {
            var cache = new TokenCache();
            var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId, ClientApplicationBase.DefaultAuthority).WithUserTokenCache(cache).BuildConcrete();
            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);

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
            var cache = new TokenCache();

            var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, ClientApplicationBase.DefaultAuthority, false)
                    .WithTelemetryCallback(_myReceiver)
                    .WithUserTokenCache(cache)
                    .BuildConcrete();

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
            var cache = new TokenCache();

            var app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId, ClientApplicationBase.DefaultAuthority, false)
                .WithTelemetryCallback(_myReceiver)
                .WithUserTokenCache(cache)
                .BuildConcrete();

            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
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

                var cache = new TokenCache();

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, ClientApplicationBase.DefaultAuthority, false)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .WithUserTokenCache(cache)
                    .BuildConcrete();

                var aadInstanceDiscovery = new AadInstanceDiscovery(httpManager, new TelemetryManager());

                TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
                cache.TokenCacheAccessor.DeleteAccessToken(
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
                Assert.AreEqual(2, cache.TokenCacheAccessor.GetAllAccessTokensAsString().Count());
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentScopeAndUserOverloadDefaultAuthorityTest()
        {
            var cache = new TokenCache();
            var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, ClientApplicationBase.DefaultAuthority, false)
                    .WithTelemetryCallback(_myReceiver)
                    .WithUserTokenCache(cache)
                    .BuildConcrete();

            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
            cache.TokenCacheAccessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
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
            using (var httpManager = new MockHttpManager())
            {
                var cache = new TokenCache();
                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityGuestTenant, false)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .WithUserTokenCache(cache)
                    .BuildConcrete();

                TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
                cache.TokenCacheAccessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.Utid,
                    MsalTestConstants.UserIdentifier,
                    MsalTestConstants.ClientId,
                    MsalTestConstants.ScopeForAnotherResourceStr));

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityGuestTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null));
                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentCacheOnlyLookupTest()
        {
            var cache = new TokenCache();
            var app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityTestTenant, false)
                .WithTelemetryCallback(_myReceiver)
                .WithUserTokenCache(cache)
                .BuildConcrete();

            TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);
            cache.TokenCacheAccessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
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
        [TestCategory("Regression")]
        [WorkItem(695)] // Fix for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/695
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentForceRefreshTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var cache = new TokenCache();
                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                    .WithAadAuthority(ClientApplicationBase.DefaultAuthority, false, true)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .WithUserTokenCache(cache)
                    .WithDebugLoggingCallback()
                    .WithLoggingLevel(LogLevel.Verbose)
                    .BuildConcrete();

                TokenCacheHelper.PopulateCacheWithOneAccessToken(cache.TokenCacheAccessor);

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery("https://login.microsoftonline.com/my-utid/");

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

                Assert.AreEqual(1, cache.TokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(1, cache.TokenCacheAccessor.RefreshTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentServiceErrorTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var cache = new TokenCache();
                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant, false)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .WithUserTokenCache(cache)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);

                // populate cache
                TokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);

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
                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, ClientApplicationBase.DefaultAuthority, false)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

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
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cache = new TokenCache();

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant, false)
                    .WithTelemetryCallback(_myReceiver)
                    .WithUserTokenCache(cache)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

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
            var app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId)
                .WithAadAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, true, true)
                .WithUserTokenCache(new TokenCache())
                .BuildConcrete();

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

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant, false)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

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

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, MsalTestConstants.AuthorityHomeTenant, false)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

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
        [Description("ClientApplicationBase.GetAuthority tests")]
        public void GetAuthority_AccountWithNullIdPassed_CommonAuthorityReturned()
        {
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId)
                .WithAadAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, true, true)
                .BuildConcrete();

            var authority = app.GetAuthority(new Account(null, MsalTestConstants.Name, MsalTestConstants.ProductionPrefNetworkEnvironment));
            Assert.AreEqual(ClientApplicationBase.DefaultAuthority, authority.AuthorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        [Description("ClientApplicationBase.GetAuthority tests")]
        public void GetAuthority_AccountWithIdPassed_TenantedAuthorityUsed()
        {
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId)
                .WithAadAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, true, true)
                .BuildConcrete();

            var authority = app.GetAuthority(
                new Account(
                    "objectId." + MsalTestConstants.Utid,
                    MsalTestConstants.Name,
                    MsalTestConstants.ProductionPrefNetworkEnvironment));

            Assert.AreEqual(MsalTestConstants.AuthorityTestTenant, authority.AuthorityInfo.CanonicalAuthority);
        }

        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenSilentNullAccountErrorTestAsync()
        {
            var cache = new TokenCache();
            var app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId, ClientApplicationBase.DefaultAuthority, false)
                .WithTelemetryCallback(_myReceiver)
                .WithUserTokenCache(cache)
                .BuildConcrete();

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
                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, CoreTestConstants.B2CLoginAuthority)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        CoreTestConstants.B2CLoginAuthority + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(CoreTestConstants.B2CLoginAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(CoreTestConstants.B2CLoginAuthority);

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

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, CoreTestConstants.B2CAuthority)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        CoreTestConstants.B2CAuthority + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(CoreTestConstants.B2CAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(CoreTestConstants.B2CAuthority);

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
                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, CoreTestConstants.B2CLoginAuthority, true)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        CoreTestConstants.B2CLoginAuthority + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(CoreTestConstants.B2CLoginAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(CoreTestConstants.B2CLoginAuthority);

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

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId, CoreTestConstants.B2CRandomHost, true)
                    .WithTelemetryCallback(_myReceiver)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        CoreTestConstants.B2CRandomHost + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    new AuthorizationResult(AuthorizationStatus.Success, app.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(CoreTestConstants.B2CRandomHost);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(CoreTestConstants.B2CRandomHost);

                AuthenticationResult result = app.AcquireTokenAsync(CoreTestConstants.Scope).Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }
#endif

#if NET_CORE

        [TestMethod]
        public void NetCore_AcquireToken_ThrowsPlatformNotSupported()
        {
            // Arrange
            PublicClientApplication pca = PublicClientApplicationBuilder.Create("cid").BuildConcrete();
            var account = new Account("a.b", null, null);

            // All interactive auth overloads
            IEnumerable<Func<Task<AuthenticationResult>>> acquireTokenInteractiveMethods = new List<Func<Task<AuthenticationResult>>>
            {
                // without UI Parent
                async () => await pca.AcquireTokenAsync(CoreTestConstants.Scope).ConfigureAwait(false),
                async () => await pca.AcquireTokenAsync(CoreTestConstants.Scope, "login hint").ConfigureAwait(false),
                async () => await pca.AcquireTokenAsync(CoreTestConstants.Scope, account).ConfigureAwait(false),
                async () => await pca.AcquireTokenAsync(CoreTestConstants.Scope, "login hint", UIBehavior.Consent, "extra_query_params").ConfigureAwait(false),
                async () => await pca.AcquireTokenAsync(CoreTestConstants.Scope, account, UIBehavior.Consent, "extra_query_params").ConfigureAwait(false),
                async () => await pca.AcquireTokenAsync(
                    CoreTestConstants.Scope,
                    "login hint",
                    UIBehavior.Consent,
                    "extra_query_params",
                    new[] {"extra scopes" },
                    CoreTestConstants.AuthorityCommonTenant).ConfigureAwait(false),

                async () => await pca.AcquireTokenAsync(
                    CoreTestConstants.Scope,
                    account,
                    UIBehavior.Consent,
                    "extra_query_params",
                    new[] {"extra scopes" },
                    CoreTestConstants.AuthorityCommonTenant).ConfigureAwait(false),

                // with UIParent
                async () => await pca.AcquireTokenAsync(CoreTestConstants.Scope, (UIParent)null).ConfigureAwait(false),
                async () => await pca.AcquireTokenAsync(CoreTestConstants.Scope, "login hint", (UIParent)null).ConfigureAwait(false),
                async () => await pca.AcquireTokenAsync(CoreTestConstants.Scope, account, (UIParent)null).ConfigureAwait(false),
                async () => await pca.AcquireTokenAsync(CoreTestConstants.Scope, "login hint", UIBehavior.Consent, "extra_query_params", (UIParent)null).ConfigureAwait(false),
                async () => await pca.AcquireTokenAsync(CoreTestConstants.Scope, account, UIBehavior.Consent, "extra_query_params", (UIParent)null).ConfigureAwait(false),
                async () => await pca.AcquireTokenAsync(
                    CoreTestConstants.Scope,
                    "login hint",
                    UIBehavior.Consent,
                    "extra_query_params",
                    new[] {"extra scopes" },
                    CoreTestConstants.AuthorityCommonTenant,
                    (UIParent)null).ConfigureAwait(false),

                async () => await pca.AcquireTokenAsync(
                    CoreTestConstants.Scope,
                    account,
                    UIBehavior.Consent,
                    "extra_query_params",
                    new[] {"extra scopes" },
                    CoreTestConstants.AuthorityCommonTenant,
                    (UIParent)null).ConfigureAwait(false),

                async () => await pca.AcquireTokenByIntegratedWindowsAuthAsync(CoreTestConstants.Scope).ConfigureAwait(false)

            };

            // Act and Assert
            foreach (var acquireTokenInteractiveMethod in acquireTokenInteractiveMethods)
            {
                AssertException.TaskThrows<PlatformNotSupportedException>(acquireTokenInteractiveMethod);
            }
        }

#endif
    }
}
