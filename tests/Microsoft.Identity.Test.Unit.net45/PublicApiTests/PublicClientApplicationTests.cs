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

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Mats.Internal.Constants;
using Microsoft.Identity.Client.Mats.Internal.Events;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class PublicClientApplicationTests
    {
        private TokenCacheHelper _tokenCacheHelper;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();

            _tokenCacheHelper = new TokenCacheHelper();
        }

#if !NET_CORE
        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [Description("Tests the public interfaces can be mocked")]
        [Ignore("Bug 1001, as we deprecate public API, new methods aren't mockable.  Working on prototype.")]
        public void MockPublicClientApplication()
        {
            //// Setup up a public client application that returns a dummy result
            //// The caller asks for two scopes, but only one is returned
            //var mockResult = new AuthenticationResult(
            //   accessToken: "",
            //   isExtendedLifeTimeToken: false,
            //   uniqueId: "",
            //   expiresOn: DateTimeOffset.Now,
            //   extendedExpiresOn: DateTimeOffset.Now,
            //   tenantId: "",
            //   account: null,
            //   idToken: "id token",
            //   scopes: new[] { "scope1" });

            //var mockApp = Substitute.For<IPublicClientApplication>();
            //mockApp.AcquireTokenInteractive(new string[] { "scope1", "scope2" }, null).ExecuteAsync(CancellationToken.None).ReturnsForAnyArgs(mockResult);

            //// Now call the substitute with the args to get the substitute result
            //AuthenticationResult actualResult = mockApp
            //    .AcquireTokenInteractive(new string[] { "scope1" }, null)
            //    .ExecuteAsync(CancellationToken.None)
            //    .Result;

            //Assert.IsNotNull(actualResult);
            //Assert.AreEqual("id token", actualResult.IdToken, "Mock result failed to return the expected id token");

            //// Check the users properties returns the dummy users
            //IEnumerable<string> scopes = actualResult.Scopes;
            //Assert.IsNotNull(scopes);
            //CollectionAssert.AreEqual(new string[] { "scope1" }, actualResult.Scopes.ToArray());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        [Description("Tests the public application interfaces can be mocked to throw MSAL exceptions")]
        [Ignore("Bug 1001, as we deprecate public API, new methods aren't mockable.  Working on prototype.")]
        public void MockPublicClientApplication_Exception()
        {
            //// Setup up a confidential client application that returns throws
            //var mockApp = Substitute.For<IPublicClientApplication>();
            //mockApp
            //    .WhenForAnyArgs(x => x.AcquireTokenAsync(Arg.Any<string[]>()))
            //    .Do(x => throw new MsalServiceException("my error code", "my message"));

            //// Now call the substitute and check the exception is thrown
            //MsalServiceException ex =
            //    AssertException.Throws<MsalServiceException>(() => mockApp.AcquireTokenAsync(new string[] { "scope1" }));
            //Assert.AreEqual("my error code", ex.ErrorCode);
            //Assert.AreEqual("my message", ex.Message);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void ConstructorsTest()
        {
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId)
                .BuildConcrete();

            Assert.IsNotNull(app);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(MsalTestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual(Constants.DefaultRedirectUri, app.AppConfig.RedirectUri);

            app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId)
                .WithAuthority(MsalTestConstants.AuthorityGuestTenant)
                .BuildConcrete();

            Assert.IsNotNull(app);
            Assert.AreEqual(MsalTestConstants.AuthorityGuestTenant, app.Authority);
            Assert.AreEqual(MsalTestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual(Constants.DefaultRedirectUri, app.AppConfig.RedirectUri);

            app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId)
                .WithB2CAuthority(MsalTestConstants.B2CAuthorityHost, MsalTestConstants.B2CTenantId)
                .BuildConcrete();

            Assert.IsNotNull(app);
            Assert.AreEqual(
                "https://login.microsoftonline.in/tfp/sometenantid/",
                app.ServiceBundle.Config.AuthorityInfo.CanonicalAuthority);
            Assert.AreEqual(MsalTestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual(Constants.DefaultRedirectUri, app.AppConfig.RedirectUri);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task NoStateReturnedTestAsync()
        {
            var receiver = new MyReceiver();

            using (var harness = new MockHttpAndServiceBundle(telemetryCallback: receiver.HandleTelemetryEvents))
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .WithTelemetry(receiver.HandleTelemetryEvents)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    AddStateInAuthorizationResult = false,
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);
                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(MsalTestConstants.Scope)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.Fail("API should have failed here");
                }
                catch (MsalClientException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.StateMismatchError, exc.ErrorCode);
                }

                Assert.IsNotNull(
                    receiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                            anEvent[MsalTelemetryBlobEventNames.ApiIdConstStrKey] == "170" && anEvent[ApiEvent.WasSuccessfulKey] == "false" &&
                            anEvent[ApiEvent.ApiErrorCodeKey] == "state_mismatch"));
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task DifferentStateReturnedTestAsync()
        {
            var receiver = new MyReceiver();

            using (var harness = new MockHttpAndServiceBundle(telemetryCallback: receiver.HandleTelemetryEvents))
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    AddStateInAuthorizationResult = false,
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code&state=mismatched")
                };

                MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);
                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(MsalTestConstants.Scope)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.Fail("API should have failed here");
                }
                catch (MsalClientException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.StateMismatchError, exc.ErrorCode);
                }
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenNoClientInfoReturnedTestAsync()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);

                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            "some-scope1 some-scope2",
                            MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                            string.Empty)
                    });

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(MsalTestConstants.Scope)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (MsalClientException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.JsonParseError, exc.ErrorCode);
                    Assert.AreEqual("client info is null", exc.Message);
                }
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSameUserTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityCommonTenant);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityCommonTenant);

                result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

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
            using (var harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityCommonTenant);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Utid, result.TenantId);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.Scope.ToString(),
                            MockHelpers.CreateIdToken(
                                MsalTestConstants.UniqueId + "more",
                                MsalTestConstants.DisplayableId + "more",
                                MsalTestConstants.Utid + "more"),
                            MockHelpers.CreateClientInfo(MsalTestConstants.Uid + "more", MsalTestConstants.Utid + "more"))
                    });

                result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

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
            var receiver = new MyReceiver();
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(receiver.HandleTelemetryEvents)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityCommonTenant);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

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
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"),
                    dict);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.Scope.AsSingleString(),
                            MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                            MockHelpers.CreateClientInfo(MsalTestConstants.Uid, MsalTestConstants.Utid + "more"))
                    });

                try
                {
                    result = app
                        .AcquireTokenInteractive(MsalTestConstants.Scope)
                        .WithAccount(result.Account)
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync(CancellationToken.None)
                        .Result;

                    Assert.Fail("API should have failed here");
                }
                catch (AggregateException ex)
                {
                    MsalClientException exc = (MsalClientException)ex.InnerException;
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.UserMismatch, exc.ErrorCode);
                }

                Assert.IsNotNull(
                    receiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                            anEvent[MsalTelemetryBlobEventNames.ApiIdConstStrKey] == "176" && anEvent[ApiEvent.WasSuccessfulKey] == "false" &&
                            anEvent[ApiEvent.ApiErrorCodeKey] == "user_mismatch"));

                var users = app.GetAccountsAsync().Result;
                Assert.AreEqual(1, users.Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenNullUserPassedInAndNewUserReturnedFromServiceTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityCommonTenant);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(MsalTestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                // TODO: Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.Scope.AsSingleString(),
                            MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                            MockHelpers.CreateClientInfo(MsalTestConstants.Uid, MsalTestConstants.Utid + "more"))
                    });

                result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(
                    MsalTestConstants.CreateUserIdentifier(MsalTestConstants.Uid, MsalTestConstants.Utid + "more"),
                    result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                var users = app.GetAccountsAsync().Result;
                Assert.AreEqual(2, users.Count());
                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                IEnumerable<IAccount> accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.IsFalse(accounts.Any());
                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);
                accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.AreEqual(1, accounts.Count());

                var atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefCacheEnvironment,
                    MsalTestConstants.ClientId,
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(7200)),
                    MockHelpers.CreateClientInfo());

                atItem.Secret = atItem.GetKey().ToString();
                app.UserTokenCacheInternal.Accessor.SaveAccessToken(atItem);

                // another cache entry for different uid. user count should be 2.

                MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem(
                    MsalTestConstants.ProductionPrefCacheEnvironment,
                    MsalTestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo("uId1", "uTId1"));

                app.UserTokenCacheInternal.Accessor.SaveRefreshToken(rtItem);

                MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                    MsalTestConstants.ProductionPrefCacheEnvironment,
                    MsalTestConstants.ClientId,
                    MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                    MockHelpers.CreateClientInfo("uId1", "uTId1"),
                    "uTId1");

                app.UserTokenCacheInternal.Accessor.SaveIdToken(idTokenCacheItem);

                MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem(
                    MsalTestConstants.ProductionPrefCacheEnvironment,
                    null,
                    MockHelpers.CreateClientInfo("uId1", "uTId1"),
                    null,
                    null,
                    "uTId1",
                    null,
                    null);

                app.UserTokenCacheInternal.Accessor.SaveAccount(accountCacheItem);

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.AreEqual(2, accounts.Count()); // scoped by env

                // another cache entry for different environment. user count should still be 2. Sovereign cloud user must not be returned
                rtItem = new MsalRefreshTokenCacheItem(
                    MsalTestConstants.SovereignNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    "someRT",
                    MockHelpers.CreateClientInfo(MsalTestConstants.Uid + "more1", MsalTestConstants.Utid));

                app.UserTokenCacheInternal.Accessor.SaveRefreshToken(rtItem);
                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                accounts = app.GetAccountsAsync().Result;
                Assert.IsNotNull(accounts);
                Assert.AreEqual(2, accounts.Count());
            }
        }

        [TestMethod]
        public async Task TestAccountAcrossMultipleClientIdsAsync()
        {
            // Arrange

            PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).BuildConcrete();

            // Populate with tokens tied to ClientId2
            _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, clientId: MsalTestConstants.ClientId2);

            app.UserTokenCacheInternal.Accessor.AssertItemCount(
                expectedAtCount: 2,
                expectedRtCount: 1,
                expectedAccountCount: 1,
                expectedIdtCount: 1,
                expectedAppMetadataCount: 1);

            // Act
            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);

            // Assert - an account is returned even if app is scoped to ClientId1
            Assert.AreEqual(1, accounts.Count());

            // Arrange

            // Populate for clientid2
            _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, clientId: MsalTestConstants.ClientId);

            app.UserTokenCacheInternal.Accessor.AssertItemCount(
                expectedAtCount: 4,
                expectedRtCount: 2,
                expectedAccountCount: 1, // still 1 account
                expectedIdtCount: 2,
                expectedAppMetadataCount: 2);

            // Act
            await app.RemoveAsync(accounts.Single()).ConfigureAwait(false);

            // Assert
            app.UserTokenCacheInternal.Accessor.AssertItemCount(
               expectedAtCount: 0,
               expectedRtCount: 0,
               expectedAccountCount: 0,
               expectedIdtCount: 0,
               expectedAppMetadataCount: 2); // app metadata is never deleted

        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersAndSignThemOutTest()
        {
            PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).BuildConcrete();
            _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

            foreach (var user in app.GetAccountsAsync().Result)
            {
                app.RemoveAsync(user).Wait();
            }

            Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
        }



        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void HttpRequestExceptionIsNotSuppressedAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // add mock response bigger than 1MB for Http Client
                httpManager.AddFailingRequest(new InvalidOperationException());

                AssertException.TaskThrows<InvalidOperationException>(
                    () => app
                        .AcquireTokenInteractive(MsalTestConstants.Scope.ToArray()).ExecuteAsync(CancellationToken.None));
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task AuthUiFailedExceptionTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new MockWebUI()
                    {
                        ExceptionToThrow = new MsalClientException(
                            MsalError.AuthenticationUiFailedError,
                            "Failed to invoke webview",
                            new InvalidOperationException("some-inner-Exception"))
                    });

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(MsalTestConstants.Scope)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.Fail("API should have failed here");
                }
                catch (MsalClientException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.AuthenticationUiFailedError, exc.ErrorCode);
                    Assert.AreEqual("some-inner-Exception", exc.InnerException.Message);
                }
            }
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUserTest()
        {
            var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).BuildConcrete();
            var users = app.GetAccountsAsync().Result;
            Assert.IsNotNull(users);
            // no users in the cache
            Assert.AreEqual(0, users.Count());

            var fetchedUser = app.GetAccountAsync(null).Result;
            Assert.IsNull(fetchedUser);

            fetchedUser = app.GetAccountAsync("").Result;
            Assert.IsNull(fetchedUser);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCacheInternal.Accessor, MsalTestConstants.Uid,
                MsalTestConstants.Utid, MsalTestConstants.Name);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCacheInternal.Accessor, MsalTestConstants.Uid,
                MsalTestConstants.Utid);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCacheInternal.Accessor, MsalTestConstants.Uid + "1",
                MsalTestConstants.Utid, MsalTestConstants.Name + "1");
            TokenCacheHelper.AddAccountToCache(app.UserTokenCacheInternal.Accessor, MsalTestConstants.Uid + "1",
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
            var receiver = new MyReceiver();
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(receiver.HandleTelemetryEvents)
                                                                            .WithDebugLoggingCallback(logLevel: LogLevel.Verbose)
                                                                            .BuildConcrete();

                // Interactive call and user cancels authentication
                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.UserCancel,
                        MsalTestConstants.AuthorityHomeTenant + "?error=user_canceled")
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(MsalTestConstants.Scope)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (MsalClientException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual("authentication_canceled", exc.ErrorCode);
                    Assert.IsNotNull(
                        receiver.EventsReceived.Find(
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
            var receiver = new MyReceiver();
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(httpManager)
                    .WithTelemetry(receiver.HandleTelemetryEvents)
                    .BuildConcrete();

                // Interactive call and authentication fails with access denied
                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.ProtocolError,
                        MsalTestConstants.AuthorityHomeTenant + "?error=access_denied")
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(MsalTestConstants.Scope)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (MsalServiceException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual("access_denied", exc.ErrorCode);
                    Assert.IsNotNull(
                        receiver.EventsReceived.Find(
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
            PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).BuildConcrete();

            var authoriy = ClientApplicationBase.GetAuthority(app.ServiceBundle, new Account(null, MsalTestConstants.Name, MsalTestConstants.ProductionPrefNetworkEnvironment));
            Assert.AreEqual(ClientApplicationBase.DefaultAuthority, authoriy.AuthorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        [Description("ClientApplicationBase.GetAuthoriy tests")]
        public void GetAuthority_AccountWithIdPassed_TenantedAuthorityUsed()
        {
            PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).BuildConcrete();

            var authority = ClientApplicationBase.GetAuthority(
                app.ServiceBundle,
                new Account(
                    "objectId." + MsalTestConstants.Utid,
                    MsalTestConstants.Name,
                    MsalTestConstants.ProductionPrefNetworkEnvironment));

            Assert.AreEqual(MsalTestConstants.AuthorityTestTenant, authority.AuthorityInfo.CanonicalAuthority);
        }

        [TestCategory("PublicClientApplicationTests")]
        public async Task AcquireTokenSilentNullAccountErrorTestAsync()
        {
            PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).BuildConcrete();

            try
            {
                AuthenticationResult result = await app
                    .AcquireTokenSilent(MsalTestConstants.Scope.ToArray(), string.Empty)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual("user_null", MsalError.UserNullError);
            }
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void NoB2CPolicyFailTest()
        {
            using (var httpManager = new MockHttpManager())
            {

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                             .WithB2CAuthority(
                                                                                MsalTestConstants.B2CAuthorityHost,
                                                                                MsalTestConstants.B2CTenantId)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                try
                {
                    AuthenticationResult result = app
                        .AcquireTokenInteractive(MsalTestConstants.Scope)
                        .ExecuteAsync(CancellationToken.None)
                        .Result;
                }
                catch(Exception ex)
                {
                    Assert.AreEqual(MsalErrorMessage.TrustFrameworkPolicyIsMissing, ex.InnerException.Message);
                }
            }
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithB2CAuthority(
                                                                                MsalTestConstants.B2CAuthorityHost,
                                                                                MsalTestConstants.B2CTenantId)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.B2CAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.B2CAuthority);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .WithTrustFameworkPolicy(MsalTestConstants.B2CPolicy)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CLoginAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithB2CAuthority(
                                                                                MsalTestConstants.B2CLoginHost,
                                                                                MsalTestConstants.B2CTenantId)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                var ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.B2CLoginAuthority + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.B2CLoginAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.B2CLoginAuthority);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .WithTrustFameworkPolicy(MsalTestConstants.B2CPolicy)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CAcquireTokenWithCustomDomainTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                             .WithB2CAuthority(
                                                                                MsalTestConstants.B2CCustomDomainHost,
                                                                                MsalTestConstants.B2CCustomDomainTenantId)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.B2CCustomDomain);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.B2CCustomDomain);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .WithTrustFameworkPolicy(MsalTestConstants.B2CPolicy)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CAcquireTokenWithAuthorityBuilderAndNoPolicyUsedErrorTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

                try
                {
                    AuthenticationResult result = app
                        .AcquireTokenInteractive(MsalTestConstants.Scope)
                        //.WithTrustFameworkPolicy(MsalTestConstants.B2CPolicy)
                        .ExecuteAsync(CancellationToken.None)
                        .Result;
                }
                catch (Exception exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalErrorMessage.TrustFrameworkPolicyIsMissing, exc.InnerException.Message);
                    return;
                }
            }

            Assert.Fail("Should not reach here. Exception was not thrown.");
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CAcquireTokenWithAuthorityBuilderAndPolicyTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.B2CLoginAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.B2CLoginAuthority);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .WithTrustFameworkPolicy(MsalTestConstants.B2CPolicy)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CAcquireTokenWithB2CLoginAuthorityTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                ValidateB2CLoginAuthority(
                    harness,
                    MsalTestConstants.B2CAuthorityHost,
                    MsalTestConstants.B2CTenantId,
                    MsalTestConstants.B2CAuthority);
                ValidateB2CLoginAuthority(
                    harness,
                    MsalTestConstants.B2CLoginHost,
                    MsalTestConstants.B2CTenantId,
                    MsalTestConstants.B2CLoginAuthority);
                ValidateB2CLoginAuthority(
                    harness,
                    MsalTestConstants.B2CBlackforestHost,
                    MsalTestConstants.B2CTenantId,
                    MsalTestConstants.B2CLoginAuthorityBlackforest);
                ValidateB2CLoginAuthority(
                    harness,
                    MsalTestConstants.B2CMoonCakeHost,
                    MsalTestConstants.B2CTenantId,
                    MsalTestConstants.B2CLoginAuthorityMoonCake);
                ValidateB2CLoginAuthority(
                    harness,
                    MsalTestConstants.B2CUSGovHost,
                    MsalTestConstants.B2CTenantId,
                    MsalTestConstants.B2CLoginAuthorityUsGov);
                ValidateB2CLoginAuthority(
                    harness,
                    MsalTestConstants.B2CCustomDomainHost,
                    MsalTestConstants.B2CCustomDomainTenantId,
                    MsalTestConstants.B2CCustomDomain);
            }
        }

        private static void ValidateB2CLoginAuthority(
            MockHttpAndServiceBundle harness,
            string authorityHost,
            string tenantId,
            string authority)
        {
            var app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId)
                .WithB2CAuthority(authorityHost, tenantId)
                .WithHttpManager(harness.HttpManager)
                .BuildConcrete();

            var ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(
                  AuthorizationStatus.Success,
                 authority + "?code=some-code")
            };

            MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);
            harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(authority);
            harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(authority);

            var result = app
                .AcquireTokenInteractive(MsalTestConstants.Scope)
                .WithTrustFameworkPolicy(MsalTestConstants.B2CPolicy)
                .ExecuteAsync(CancellationToken.None)
                .Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void EnsurePublicApiSurfaceExistsOnInterface()
        {
            IPublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).Build();

            // This test is to ensure that the methods we want/need on the IPublicClientApplication exist and compile.  This isn't testing functionality, that's done elsewhere.
            // It's solely to ensure we know that the methods we want/need are available where we expect them since we tend to do most testing on the concrete types.

            var interactiveBuilder = app.AcquireTokenInteractive(MsalTestConstants.Scope)
               .WithAccount(MsalTestConstants.User)
               .WithExtraScopesToConsent(MsalTestConstants.Scope)
               .WithLoginHint("loginhint")
               .WithPrompt(Prompt.ForceLogin)
               .WithUseEmbeddedWebView(true);
            CheckBuilderCommonMethods(interactiveBuilder);

            var iwaBuilder = app.AcquireTokenByIntegratedWindowsAuth(MsalTestConstants.Scope)
               .WithUsername("upn@live.com");
            CheckBuilderCommonMethods(iwaBuilder);

            var usernamePasswordBuilder = app.AcquireTokenByUsernamePassword(MsalTestConstants.Scope, "upn@live.com", new SecureString());
            CheckBuilderCommonMethods(usernamePasswordBuilder);

            var deviceCodeBuilder = app.AcquireTokenWithDeviceCode(MsalTestConstants.Scope, result => Task.FromResult(0))
               .WithDeviceCodeResultCallback(result => Task.FromResult(0));
            CheckBuilderCommonMethods(deviceCodeBuilder);

            var silentBuilder = app.AcquireTokenSilent(MsalTestConstants.Scope, MsalTestConstants.User)
               .WithForceRefresh(true);
            CheckBuilderCommonMethods(silentBuilder);

            silentBuilder = app.AcquireTokenSilent(MsalTestConstants.Scope, "upn@live.co.uk")
              .WithForceRefresh(true);
            CheckBuilderCommonMethods(silentBuilder);

            var byRefreshTokenBuilder = ((IByRefreshToken)app).AcquireTokenByRefreshToken(MsalTestConstants.Scope, "refreshtoken")
                                  .WithRefreshToken("refreshtoken");
            CheckBuilderCommonMethods(byRefreshTokenBuilder);
        }

#endif

#if NET_CORE

        [TestMethod]
        public void NetCore_AcquireToken_ThrowsPlatformNotSupported()
        {
            // Arrange
            PublicClientApplication pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).BuildConcrete();
            var account = new Account("a.b", null, null);

            // All interactive auth overloads
            IEnumerable<Func<Task<AuthenticationResult>>> acquireTokenInteractiveMethods = new List<Func<Task<AuthenticationResult>>>
            {
                // without UI Parent
                async () => await pca.AcquireTokenInteractive(MsalTestConstants.Scope).ExecuteAsync(CancellationToken.None).ConfigureAwait(false),
                async () => await pca.AcquireTokenInteractive(MsalTestConstants.Scope).WithLoginHint("login hint").ExecuteAsync(CancellationToken.None).ConfigureAwait(false),
                async () => await pca.AcquireTokenInteractive(MsalTestConstants.Scope).WithAccount(account).ExecuteAsync(CancellationToken.None).ConfigureAwait(false),
                async () => await pca.AcquireTokenInteractive(MsalTestConstants.Scope).WithLoginHint("login hint").WithPrompt(Prompt.Consent).WithExtraQueryParameters("extra_query_params").ExecuteAsync(CancellationToken.None).ConfigureAwait(false),
                async () => await pca.AcquireTokenInteractive(MsalTestConstants.Scope).WithAccount(account).WithPrompt(Prompt.Consent).WithExtraQueryParameters("extra_query_params").ExecuteAsync(CancellationToken.None).ConfigureAwait(false),
                async () => await pca.AcquireTokenInteractive(MsalTestConstants.Scope)
                    .WithLoginHint("login hint")
                    .WithPrompt(Prompt.Consent)
                    .WithExtraQueryParameters("extra_query_params")
                    .WithExtraScopesToConsent(new[] {"extra scopes" })
                    .WithAuthority(MsalTestConstants.AuthorityCommonTenant)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false),

                async () => await pca.AcquireTokenInteractive(MsalTestConstants.Scope)
                    .WithAccount(account)
                    .WithPrompt(Prompt.Consent)
                    .WithExtraQueryParameters("extra_query_params")
                    .WithExtraScopesToConsent(new[] {"extra scopes" })
                    .WithAuthority(MsalTestConstants.AuthorityCommonTenant)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false),

                async () => await pca.AcquireTokenByIntegratedWindowsAuth(MsalTestConstants.Scope).ExecuteAsync(CancellationToken.None).ConfigureAwait(false)

            };

            // Act and Assert
            foreach (var acquireTokenInteractiveMethod in acquireTokenInteractiveMethods)
            {
                AssertException.TaskThrows<PlatformNotSupportedException>(acquireTokenInteractiveMethod);
            }
        }

#endif
        public static void CheckBuilderCommonMethods<T>(AbstractAcquireTokenParameterBuilder<T> builder) where T : AbstractAcquireTokenParameterBuilder<T>
        {
            builder.WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, true)
                .WithAuthority(AzureCloudInstance.AzureChina, AadAuthorityAudience.AzureAdMultipleOrgs, true)
                .WithAuthority(AzureCloudInstance.AzurePublic, Guid.NewGuid(), true)
                .WithAuthority(AzureCloudInstance.AzureChina, Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture), true)
                .WithAuthority(MsalTestConstants.AuthorityCommonTenant, Guid.NewGuid(), true)
                .WithAuthority(MsalTestConstants.AuthorityCommonTenant, Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture), true)
                .WithAuthority(MsalTestConstants.AuthorityGuestTenant, true)
                .WithAdfsAuthority(MsalTestConstants.AuthorityGuestTenant, true)
                .WithExtraQueryParameters(
                    new Dictionary<string, string>
                    {
                        {"key1", "value1"}
                    });
        }
    }
}
