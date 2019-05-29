// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Mats.Internal.Constants;
using Microsoft.Identity.Client.Mats.Internal.Events;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class PublicClientApplicationTests : TestBase
    {
        private TokenCacheHelper _tokenCacheHelper;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _tokenCacheHelper = new TokenCacheHelper();
        }

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
                .WithAuthority(new Uri("https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/B2C_1_B2C_Signup_Signin_Policy/oauth2/v2.0"))
                .BuildConcrete();

            Assert.IsNotNull(app);
            Assert.AreEqual(
                "https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/b2c_1_b2c_signup_signin_policy/",
                app.Authority);
            Assert.AreEqual(MsalTestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual(Constants.DefaultRedirectUri, app.AppConfig.RedirectUri);

            //app = new PublicClientApplication(MsalTestConstants.ClientId, MsalTestConstants.OnPremiseAuthority);
            app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).WithAuthority(MsalTestConstants.OnPremiseAuthority).BuildConcrete();
            Assert.IsNotNull(app);
            Assert.AreEqual("https://fs.contoso.com/adfs/", app.Authority);
            Assert.AreEqual(MsalTestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.AppConfig.RedirectUri);
            
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public async Task NoStateReturnedTestAsync()
        {
            var receiver = new MyReceiver();

            using (var harness = CreateTestHarness(telemetryCallback: receiver.HandleTelemetryEvents))
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
                    MockResult = AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code")
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

            using (var harness = CreateTestHarness(telemetryCallback: receiver.HandleTelemetryEvents))
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
                    MockResult = AuthorizationResult.FromUri(MsalTestConstants.AuthorityHomeTenant + "?code=some-code&state=mismatched")
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
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

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
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

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
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

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
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

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
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

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
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

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
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"),
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
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

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
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

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
        public async Task HttpRequestExceptionIsNotSuppressedAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();


                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                // add mock response bigger than 1MB for Http Client
                httpManager.AddFailingRequest(new InvalidOperationException());

                await AssertException.TaskThrowsAsync<InvalidOperationException>(
                    () => app
                        .AcquireTokenInteractive(MsalTestConstants.Scope.ToArray()).ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);
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
        public void GetAccountTests()
        {
            var app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).BuildConcrete();
            var accounts = app.GetAccountsAsync().Result;
            Assert.IsTrue(!accounts.Any());

            var acc = app.GetAccountAsync(null).Result;
            Assert.IsNull(acc);

            acc = app.GetAccountAsync("").Result;
            Assert.IsNull(acc);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCacheInternal.Accessor, MsalTestConstants.Uid,
                MsalTestConstants.Utid, MsalTestConstants.ClientId);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCacheInternal.Accessor, MsalTestConstants.Uid,
                MsalTestConstants.Utid);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCacheInternal.Accessor, MsalTestConstants.Uid + "1",
                MsalTestConstants.Utid, MsalTestConstants.ClientId);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCacheInternal.Accessor, MsalTestConstants.Uid + "1",
                MsalTestConstants.Utid);

            accounts = app.GetAccountsAsync().Result;
            Assert.IsNotNull(accounts);
            // two users in the cache
            Assert.AreEqual(2, accounts.Count());

            var userToFind = accounts.First();

            acc = app.GetAccountAsync(userToFind.HomeAccountId.Identifier).Result;

            Assert.AreEqual(userToFind.Username, acc.Username);
            Assert.AreEqual(userToFind.HomeAccountId, acc.HomeAccountId);
            Assert.AreEqual(userToFind.Environment, acc.Environment);
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
                var ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel)
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
                    MockResult = AuthorizationResult.FromUri(MsalTestConstants.AuthorityHomeTenant + "?error=access_denied")
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
                    .AcquireTokenSilentWithLoginHint(MsalTestConstants.Scope.ToArray(), string.Empty)
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
        public void B2CLoginAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.B2CLoginAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.B2CLoginAuthority);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.B2CAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.B2CAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.B2CAuthority);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CAcquireTokenWithValidateAuthorityTrueTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                var ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(MsalTestConstants.B2CLoginAuthority + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.B2CLoginAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.B2CLoginAuthority);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CAcquireTokenWithValidateAuthorityTrueAndRandomAuthorityTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.B2CCustomDomain), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.B2CCustomDomain);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.B2CCustomDomain);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CAcquireTokenAuthorityHostMisMatchErrorTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();
                try
                {
                    AuthenticationResult result = app
                        .AcquireTokenInteractive(MsalTestConstants.Scope)
                        .WithB2CAuthority(MsalTestConstants.B2CLoginAuthorityWrongHost)
                        .ExecuteAsync(CancellationToken.None)
                        .Result;
                }
                catch (Exception exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalErrorMessage.B2CAuthorityHostMisMatch, exc.InnerException.Message);
                    return;
                }
            }

            Assert.Fail("Should not reach here. Exception was not thrown.");
        }

        [TestMethod]
        [TestCategory("B2C")]
        public void B2CAcquireTokenWithB2CLoginAuthorityTest()
        {
            using (var harness = CreateTestHarness())
            {
                ValidateB2CLoginAuthority(harness, MsalTestConstants.B2CAuthority);
                ValidateB2CLoginAuthority(harness, MsalTestConstants.B2CLoginAuthority);
                ValidateB2CLoginAuthority(harness, MsalTestConstants.B2CLoginAuthorityBlackforest);
                ValidateB2CLoginAuthority(harness, MsalTestConstants.B2CLoginAuthorityMoonCake);
                ValidateB2CLoginAuthority(harness, MsalTestConstants.B2CLoginAuthorityUsGov);
                ValidateB2CLoginAuthority(harness, MsalTestConstants.B2CCustomDomain);
            }
        }

        /// <summary>
        /// Cache state:
        ///
        /// 2 users have acquired tokens
        /// 1 of them is a guest in another tenant => 1 request for each tenant
        ///
        /// There are 3 access tokens, 3 ATs, 3 Accounts but only 2 RT
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [DeploymentItem(@"Resources\MultiTenantTokenCache.json")]
        public async Task MultiTenantWithAuthorityOverrideAsync()
        {
            const string tenant1 = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            const string tenant2 = "49f548d0-12b7-4169-a390-bb5304d24462";
            string tenantedAuthority1 = $"https://login.microsoftonline.com/{tenant1}/";
            string tenantedAuthority2 = $"https://login.microsoftonline.com/{tenant2}/";

            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication pca = CreatePcaFromFileWithAuthority(httpManager);

                // Act
                var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                AuthenticationResult response = await
                    pca.AcquireTokenSilentWithAccount(new[] { "User.Read" }, accounts.First())
                    .WithAuthority(tenantedAuthority1)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant1, response.TenantId);

                // Act
                accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                response = await
                    pca.AcquireTokenSilentWithAccount(new[] { "User.Read" }, accounts.First())
                    .WithAuthority(tenantedAuthority2)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant2, response.TenantId);
            }
        }

        /// <summary>
        /// Cache state:
        ///
        /// 2 users have acquired tokens
        /// 1 of them is a guest in another tenant => 1 request for each tenant
        ///
        /// There are 3 access tokens, 3 ATs, 3 Accounts but only 2 RT
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [DeploymentItem(@"Resources\MultiTenantTokenCache.json")]
        public async Task MultiTenantViaPcaAsync()
        {
            const string tenant1 = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            const string tenant2 = "49f548d0-12b7-4169-a390-bb5304d24462";
            string tenantedAuthority1 = $"https://login.microsoftonline.com/{tenant1}/";
            string tenantedAuthority2 = $"https://login.microsoftonline.com/{tenant2}/";

            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication pca = CreatePcaFromFileWithAuthority(httpManager, tenantedAuthority1);

                // Act
                var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                AuthenticationResult response = await
                    pca.AcquireTokenSilentWithAccount(new[] { "User.Read" }, accounts.First())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant1, response.TenantId);

                // Arrange
                PublicClientApplication pca2 = CreatePcaFromFileWithAuthority(httpManager, tenantedAuthority2);

                // Act
                accounts = await pca2.GetAccountsAsync().ConfigureAwait(false);
                response = await
                    pca2.AcquireTokenSilentWithAccount(new[] { "User.Read" }, accounts.First())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant2, response.TenantId);
            }
        }

        private static PublicClientApplication CreatePcaFromFileWithAuthority(
            MockHttpManager httpManager,
            string authority = null)
        {
            const string clientIdInFile = "1d18b3b0-251b-4714-a02a-9956cec86c2d";
            const string tokenCacheFile = "MultiTenantTokenCache.json";

            var pcaBuilder = PublicClientApplicationBuilder
             .Create(clientIdInFile)
             .WithHttpManager(httpManager);

            if (authority != null)
            {
                pcaBuilder = pcaBuilder.WithAuthority(authority);
            }

            var pca = pcaBuilder.BuildConcrete();
            pca.InitializeTokenCacheFromFile(ResourceHelper.GetTestResourceRelativePath(tokenCacheFile), true);
            pca.UserTokenCacheInternal.Accessor.AssertItemCount(3, 2, 3, 3, 1);
            return pca;
        }

        private static void ValidateB2CLoginAuthority(MockHttpAndServiceBundle harness, string authority)
        {
            var app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId)
                .WithB2CAuthority(authority)
                .WithHttpManager(harness.HttpManager)
                .BuildConcrete();

            var ui = new MockWebUI()
            {
                MockResult = AuthorizationResult.FromUri(authority + "?code=some-code")
            };

            MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);
            harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(authority);
            harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(authority);

            var result = app
                .AcquireTokenInteractive(MsalTestConstants.Scope)
                .ExecuteAsync(CancellationToken.None)
                .Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenFromAdfs()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAdfsAuthority(MsalTestConstants.OnPremiseAuthority, true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                                app.ServiceBundle.PlatformProxy,
                                AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ExpectedUrl = "https://fs.contoso.com/.well-known/webfinger",
                    ExpectedQueryParams = new Dictionary<string, string>
                    {
                            {"resource", "https://fs.contoso.com"},
                            {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                    },
                    ResponseMessage = MockHelpers.CreateSuccessWebFingerResponseMessage("https://fs.contoso.com")
                });

                //add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(MsalTestConstants.OnPremiseAuthority)
                });

                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateAdfsSuccessTokenResponseMessage()
                });

                AuthenticationResult result = app.AcquireTokenInteractive(MsalTestConstants.Scope).ExecuteAsync().Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(MsalTestConstants.OnPremiseUniqueId, result.UniqueId);
                Assert.AreEqual(new AccountId(MsalTestConstants.OnPremiseUniqueId), result.Account.HomeAccountId);
                Assert.AreEqual(MsalTestConstants.OnPremiseDisplayableId, result.Account.Username);

                //Find token in cache now

                AuthenticationResult cachedAuth = null;
                try
                {
                    cachedAuth = app.AcquireTokenSilentWithAccount(MsalTestConstants.Scope, result.Account).ExecuteAsync().Result;
                }
                catch
                {
                    Assert.Fail("Did not find access token");
                }
                Assert.IsNotNull(cachedAuth);
                Assert.IsNotNull(cachedAuth.Account);
                Assert.AreEqual(MsalTestConstants.OnPremiseUniqueId, cachedAuth.UniqueId);
                Assert.AreEqual(new AccountId(MsalTestConstants.OnPremiseUniqueId), cachedAuth.Account.HomeAccountId);
                Assert.AreEqual(MsalTestConstants.OnPremiseDisplayableId, cachedAuth.Account.Username);
            }
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
               .WithPrompt(Prompt.ForceLogin);

#if DESKTOP
            interactiveBuilder = interactiveBuilder.WithUseEmbeddedWebView(true);
#endif
            CheckBuilderCommonMethods(interactiveBuilder);

            var iwaBuilder = app.AcquireTokenByIntegratedWindowsAuth(MsalTestConstants.Scope)
               .WithUsername("upn@live.com");
            CheckBuilderCommonMethods(iwaBuilder);

            var usernamePasswordBuilder = app.AcquireTokenByUsernamePassword(MsalTestConstants.Scope, "upn@live.com", new SecureString());
            CheckBuilderCommonMethods(usernamePasswordBuilder);

            var deviceCodeBuilder = app.AcquireTokenWithDeviceCode(MsalTestConstants.Scope, result => Task.FromResult(0))
               .WithDeviceCodeResultCallback(result => Task.FromResult(0));
            CheckBuilderCommonMethods(deviceCodeBuilder);

            var silentBuilder = app.AcquireTokenSilentWithAccount(MsalTestConstants.Scope, MsalTestConstants.User)
               .WithForceRefresh(true);
            CheckBuilderCommonMethods(silentBuilder);

            silentBuilder = app.AcquireTokenSilentWithLoginHint(MsalTestConstants.Scope, "upn@live.co.uk")
              .WithForceRefresh(true);
            CheckBuilderCommonMethods(silentBuilder);

            var byRefreshTokenBuilder = ((IByRefreshToken)app).AcquireTokenByRefreshToken(MsalTestConstants.Scope, "refreshtoken")
                                  .WithRefreshToken("refreshtoken");
            CheckBuilderCommonMethods(byRefreshTokenBuilder);
        }

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
                .WithB2CAuthority(MsalTestConstants.B2CAuthority)
                .WithExtraQueryParameters(
                    new Dictionary<string, string>
                    {
                        {"key1", "value1"}
                    });
        }
    }
}
