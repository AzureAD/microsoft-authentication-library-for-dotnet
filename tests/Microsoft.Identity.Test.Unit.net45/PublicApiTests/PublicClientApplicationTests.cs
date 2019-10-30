// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
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
        public void ConstructorsTest()
        {
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .BuildConcrete();

            Assert.IsNotNull(app);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual(Constants.DefaultRedirectUri, app.AppConfig.RedirectUri);

            app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityGuestTenant)
                .BuildConcrete();

            Assert.IsNotNull(app);
            Assert.AreEqual(TestConstants.AuthorityGuestTenant, app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual(Constants.DefaultRedirectUri, app.AppConfig.RedirectUri);

            app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(new Uri("https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/B2C_1_B2C_Signup_Signin_Policy/oauth2/v2.0"))
                .BuildConcrete();

            Assert.IsNotNull(app);
            Assert.AreEqual(
                "https://login.microsoftonline.com/tfp/vibrob2c.onmicrosoft.com/b2c_1_b2c_signup_signin_policy/",
                app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual(Constants.DefaultRedirectUri, app.AppConfig.RedirectUri);

            //app = new PublicClientApplication(TestConstants.ClientId, TestConstants.OnPremiseAuthority);
            app = PublicClientApplicationBuilder.Create(TestConstants.ClientId).WithAuthority(TestConstants.OnPremiseAuthority).BuildConcrete();
            Assert.IsNotNull(app);
            Assert.AreEqual("https://fs.contoso.com/adfs/", app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.AppConfig.RedirectUri);
        }

        [TestMethod]
        public async Task NoStateReturnedTestAsync()
        {
            var receiver = new MyReceiver();

            using (var harness = CreateTestHarness(telemetryCallback: receiver.HandleTelemetryEvents))
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
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
                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(TestConstants.s_scope)
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
                            anEvent[MsalTelemetryBlobEventNames.ApiIdConstStrKey] == "1005" && anEvent[ApiEvent.WasSuccessfulKey] == "false" &&
                            anEvent[ApiEvent.ApiErrorCodeKey] == "state_mismatch"));
            }
        }

        [TestMethod]
        public async Task ClaimsAreSentTo_AuthorizationEndpoint_And_TokenEndpoint_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                var mockUi = MsalMockHelpers.ConfigureMockWebUI(
                     app.ServiceBundle.PlatformProxy,
                     AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                mockUi.QueryParamsToValidate = new Dictionary<string, string> { { OAuth2Parameter.Claims, TestConstants.Claims } };

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityCommonTenant,
                    queryParameters: mockUi.QueryParamsToValidate);

                AuthenticationResult result = await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithClaims(TestConstants.Claims)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.Account);
            }
        }

        [TestMethod]
        public async Task DifferentStateReturnedTestAsync()
        {
            var receiver = new MyReceiver();

            using (var harness = CreateTestHarness(telemetryCallback: receiver.HandleTelemetryEvents))
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .WithTelemetry(new TraceTelemetryConfig())
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    AddStateInAuthorizationResult = false,
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?code=some-code&state=mismatched")
                };

                MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);
                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(TestConstants.s_scope)
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
        public async Task AcquireTokenNoClientInfoReturnedTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);

                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            "some-scope1 some-scope2",
                            MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                            string.Empty)
                    });

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(TestConstants.s_scope)
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
        public void AcquireTokenSameUserTest()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                Guid correlationId = Guid.NewGuid();

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithCorrelationId(correlationId)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                userCacheAccess.AssertAccessCounts(0, 1);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddSuccessfulTokenResponseWithHttpTelemetryMockHandlerForPost(
                    TestConstants.AuthorityCommonTenant,
                    null,
                    null,
                    HttpTelemetryTests.CreateHttpTelemetryHeaders(
                        correlationId,
                        TestConstants.InteractiveRequestApiId,
                        null,
                        TelemetryConstants.Zero));

                result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                userCacheAccess.AssertAccessCounts(0, 2);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public async Task AcquireTokenInterative_WithValidCustomInstanceMetadata_Async()
        {
            string instanceMetadataJson = File.ReadAllText(
                ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));

            using (var harness = CreateTestHarness())
            {
                // No instance discovery is made - it is important to not have this mock handler added
                // harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri("https://login.windows.net/common/"), false)
                    .WithInstanceDicoveryMetadata(instanceMetadataJson)
                    .WithHttpManager(harness.HttpManager)
                    .WithTelemetry(new TraceTelemetryConfig())
                    .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                // the rest of the communcation with AAD happens on the preferred_network alias, not on login.windows.net
                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                AuthenticationResult result = await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public async Task AcquireTokenInterative_WithBadCustomInstanceMetadata_Async()
        {
            string instanceMetadataJson = File.ReadAllText(
                ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));

            using (var harness = CreateTestHarness())
            {
                // No instance discovery is made - it is important to not have this mock handler added
                // harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(@"https://sts.windows.net/common/"), false)
                    .WithInstanceDicoveryMetadata(instanceMetadataJson)
                    .WithHttpManager(harness.HttpManager)
                    .WithTelemetry(new TraceTelemetryConfig())
                    .BuildConcrete();

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(() => app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None))
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.InvalidUserInstanceMetadata, ex.ErrorCode);
            }
        }

        [TestMethod]
        public void AcquireTokenWithDefaultRedirectURITest()
        {
            using (var harness = CreateTestHarness())
            {
                //harness.HttpManager.AddInstanceDiscoveryMockHandler();
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .BuildConcrete();
                //Validate legacy default uri
                Assert.AreEqual(app.AppConfig.RedirectUri, "urn:ietf:wg:oauth:2.0:oob");

                app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .WithDefaultRedirectUri()
                                                                            .BuildConcrete();

                //Validate new default redirect uri
#if DESKTOP
                Assert.AreEqual(app.AppConfig.RedirectUri, "https://login.microsoftonline.com/common/oauth2/nativeclient");
#elif NET_CORE
                Assert.AreEqual(app.AppConfig.RedirectUri, "http://localhost");
#endif
            }
        }

        [TestMethod]
        public void AcquireTokenAddTwoUsersTest()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.Utid, result.TenantId);

                // repeat interactive call and pass in the same user
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.s_scope.ToString(),
                            MockHelpers.CreateIdToken(
                                TestConstants.UniqueId + "more",
                                TestConstants.DisplayableId + "more",
                                TestConstants.Utid + "more"),
                            MockHelpers.CreateClientInfo(TestConstants.Uid + "more", TestConstants.Utid + "more"))
                    });

                result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId + "more", result.UniqueId);
                Assert.AreEqual(
                    TestConstants.CreateUserIdentifier(TestConstants.Uid + "more", TestConstants.Utid + "more"),
                    result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId + "more", result.Account.Username);
                Assert.AreEqual(TestConstants.Utid + "more", result.TenantId);
            }
        }

        [TestMethod]
        public void AcquireTokenDifferentUserReturnedFromServiceTest()
        {
            var receiver = new MyReceiver();
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(receiver.HandleTelemetryEvents)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);

                // TODO: allow checking in the middle of a using block --> Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

                var dict = new Dictionary<string, string>
                {
                    [OAuth2Parameter.DomainReq] = TestConstants.Utid,
                    [OAuth2Parameter.LoginReq] = TestConstants.Uid
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
                            TestConstants.s_scope.AsSingleString(),
                            MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                            MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid + "more"))
                    });

                try
                {
                    result = app
                        .AcquireTokenInteractive(TestConstants.s_scope)
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
                            anEvent[MsalTelemetryBlobEventNames.ApiIdConstStrKey] == "1005" && anEvent[ApiEvent.WasSuccessfulKey] == "false" &&
                            anEvent[ApiEvent.ApiErrorCodeKey] == "user_mismatch"));

                var users = app.GetAccountsAsync().Result;
                Assert.AreEqual(1, users.Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        public void AcquireTokenNullUserPassedInAndNewUserReturnedFromServiceTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
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
                            TestConstants.s_scope.AsSingleString(),
                            MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                            MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid + "more"))
                    });

                result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(
                    TestConstants.CreateUserIdentifier(TestConstants.Uid, TestConstants.Utid + "more"),
                    result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                var users = app.GetAccountsAsync().Result;
                Assert.AreEqual(2, users.Count());
                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        public async Task HttpRequestExceptionIsNotSuppressedAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(httpManager)
                    .WithTelemetry(new TraceTelemetryConfig())
                    .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                // add mock response bigger than 1MB for Http Client
                httpManager.AddFailingRequest(new InvalidOperationException());

                await AssertException.TaskThrowsAsync<InvalidOperationException>(
                    () => app
                        .AcquireTokenInteractive(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task AuthUiFailedExceptionTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);

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
                        .AcquireTokenInteractive(TestConstants.s_scope)
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
        public void GetAccountTests()
        {
            var app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithTelemetry(new TraceTelemetryConfig())
                .BuildConcrete();

            var accounts = app.GetAccountsAsync().Result;
            Assert.IsTrue(!accounts.Any());

            var acc = app.GetAccountAsync(null).Result;
            Assert.IsNull(acc);

            acc = app.GetAccountAsync("").Result;
            Assert.IsNull(acc);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCacheInternal.Accessor, TestConstants.Uid,
                TestConstants.Utid, TestConstants.ClientId);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCacheInternal.Accessor, TestConstants.Uid,
                TestConstants.Utid);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCacheInternal.Accessor, TestConstants.Uid + "1",
                TestConstants.Utid, TestConstants.ClientId);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCacheInternal.Accessor, TestConstants.Uid + "1",
                TestConstants.Utid);

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

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
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

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(TestConstants.s_scope)
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
        [Description("Test for AcquireToken with user resetting password")]
        public async Task B2CAcquireTokenWithResetPasswordTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithB2CAuthority(TestConstants.B2CLoginAuthority)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithDebugLoggingCallback(logLevel: LogLevel.Verbose)
                                                                            .BuildConcrete();

                // Interactive call and user wants to reset password
                var ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(TestConstants.B2CLoginAuthority +
                    "?error=access_denied&error_description=AADB2C90091%3a+The+user+has+cancelled+entering+self-asserted+information.")
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.B2CLoginAuthority);
                MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(TestConstants.s_scope)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (MsalServiceException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual("access_denied", exc.ErrorCode);
                    Assert.AreEqual("AADB2C90091: The user has cancelled entering self-asserted information.", exc.Message);
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
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(httpManager)
                    .WithTelemetry(receiver.HandleTelemetryEvents)
                    .BuildConcrete();

                // Interactive call and authentication fails with access denied
                MockWebUI ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?error=access_denied")
                };

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);

                try
                {
                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(TestConstants.s_scope)
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
        [Description("ClientApplicationBase.GetAuthority tests")]
        public void GetAuthority_AccountWithNullIdPassed_CommonAuthorityReturned()
        {
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithTelemetry(new TraceTelemetryConfig())
                .BuildConcrete();

            var authoriy = Authority.CreateAuthorityWithTenant(app.ServiceBundle.Config.AuthorityInfo, null);
            Assert.AreEqual(ClientApplicationBase.DefaultAuthority, authoriy.AuthorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        [Description("ClientApplicationBase.GetAuthority tests")]
        public void GetAuthority_AccountWithIdPassed_TenantedAuthorityUsed()
        {
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithTelemetry(new TraceTelemetryConfig())
                .BuildConcrete();

            var authority = Authority.CreateAuthorityWithTenant(
                app.ServiceBundle.Config.AuthorityInfo,
                TestConstants.Utid);

            Assert.AreEqual(TestConstants.AuthorityTestTenant, authority.AuthorityInfo.CanonicalAuthority);
        }

        public async Task AcquireTokenSilentNullAccountErrorTestAsync()
        {
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithTelemetry(new TraceTelemetryConfig())
                .BuildConcrete();

            try
            {
                AuthenticationResult result = await app
                    .AcquireTokenSilent(TestConstants.s_scope.ToArray(), string.Empty)
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
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.B2CLoginAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.B2CLoginAuthority);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
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
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.B2CAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.B2CAuthority);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
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
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.B2CLoginAuthority);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.B2CLoginAuthority);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
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
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CCustomDomain), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.B2CCustomDomain);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.B2CCustomDomain);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
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
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                try
                {
                    AuthenticationResult result = app
                        .AcquireTokenInteractive(TestConstants.s_scope)
                        .WithB2CAuthority(TestConstants.B2CLoginAuthorityWrongHost)
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
                ValidateB2CLoginAuthority(harness, TestConstants.B2CAuthority);
                ValidateB2CLoginAuthority(harness, TestConstants.B2CLoginAuthority);
                ValidateB2CLoginAuthority(harness, TestConstants.B2CLoginAuthorityBlackforest);
                ValidateB2CLoginAuthority(harness, TestConstants.B2CLoginAuthorityMoonCake);
                ValidateB2CLoginAuthority(harness, TestConstants.B2CLoginAuthorityUsGov);
                ValidateB2CLoginAuthority(harness, TestConstants.B2CCustomDomain);
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
                PublicClientApplication pca = CreatePcaFromFileWithAuthority(httpManager);

                // Act
                var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                AuthenticationResult response = await
                    pca.AcquireTokenSilent(new[] { "User.Read" }, accounts.First())
                    .WithAuthority(tenantedAuthority1)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant1, response.TenantId);

                // Act
                accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                response = await
                    pca.AcquireTokenSilent(new[] { "User.Read" }, accounts.First())
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
                PublicClientApplication pca = CreatePcaFromFileWithAuthority(httpManager, tenantedAuthority1);

                // Act
                var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                AuthenticationResult response = await
                    pca.AcquireTokenSilent(new[] { "User.Read" }, accounts.First())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant1, response.TenantId);

                // Arrange
                PublicClientApplication pca2 = CreatePcaFromFileWithAuthority(httpManager, tenantedAuthority2);

                // Act
                accounts = await pca2.GetAccountsAsync().ConfigureAwait(false);
                response = await
                    pca2.AcquireTokenSilent(new[] { "User.Read" }, accounts.First())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant2, response.TenantId);
            }
        }

        [TestMethod]
        [TestCategory("Regression")]
        [WorkItem(1365)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1365
        public async Task PCAAuthority_DirtiedByATS_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(ClientApplicationBase.DefaultAuthority, app.ServiceBundle.Config.AuthorityInfo.CanonicalAuthority);

                // ATS must not update the PCA authority
                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                await app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(ClientApplicationBase.DefaultAuthority, app.ServiceBundle.Config.AuthorityInfo.CanonicalAuthority);

                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                // this would fail because the request should go to /common but instead it goes to tenanted authority
                await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(ClientApplicationBase.DefaultAuthority, app.ServiceBundle.Config.AuthorityInfo.CanonicalAuthority);
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
                .WithTelemetry(new TraceTelemetryConfig())
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
                .Create(TestConstants.ClientId)
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
                .AcquireTokenInteractive(TestConstants.s_scope)
                .ExecuteAsync(CancellationToken.None)
                .Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
        }

        [TestMethod]
        public void AcquireTokenFromAdfs()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAdfsAuthority(TestConstants.OnPremiseAuthority, true)
                    .WithHttpManager(httpManager)
                    .WithTelemetry(new TraceTelemetryConfig())
                    .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                                app.ServiceBundle.PlatformProxy,
                                AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                MockHttpManagerExtensions.AddAdfs2019MockHandler(httpManager);

                AuthenticationResult result = app.AcquireTokenInteractive(TestConstants.s_scope).ExecuteAsync().Result;
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.OnPremiseUniqueId, result.UniqueId);
                Assert.AreEqual(new AccountId(TestConstants.OnPremiseUniqueId), result.Account.HomeAccountId);
                Assert.AreEqual(TestConstants.OnPremiseDisplayableId, result.Account.Username);

                //Find token in cache now
                AuthenticationResult cachedAuth = null;
                try
                {
                    cachedAuth = app.AcquireTokenSilent(TestConstants.s_scope, result.Account).ExecuteAsync().Result;
                }
                catch
                {
                    Assert.Fail("Did not find access token");
                }
                Assert.IsNotNull(cachedAuth);
                Assert.IsNotNull(cachedAuth.Account);
                Assert.AreEqual(TestConstants.OnPremiseUniqueId, cachedAuth.UniqueId);
                Assert.AreEqual(new AccountId(TestConstants.OnPremiseUniqueId), cachedAuth.Account.HomeAccountId);
                Assert.AreEqual(TestConstants.OnPremiseDisplayableId, cachedAuth.Account.Username);
            }
        }

        [TestMethod]
        public void AcquireTokenFromAdfsWithNoLoginHintWithAccountInCacheTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAdfsAuthority(TestConstants.OnPremiseAuthority, true)
                    .WithHttpManager(httpManager)
                    .WithTelemetry(new TraceTelemetryConfig())
                    .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                                app.ServiceBundle.PlatformProxy,
                                AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                MockHttpManagerExtensions.AddAdfs2019MockHandler(httpManager);

                AuthenticationResult result = app.AcquireTokenInteractive(TestConstants.s_scope).ExecuteAsync().Result;
                Assert.IsNotNull(result);

                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateAdfsSuccessTokenResponseMessage()
                });

                // Complete AT call again w/no login hint w/account already in cache
                AuthenticationResult result2 = app.AcquireTokenInteractive(TestConstants.s_scope).ExecuteAsync().Result;
                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.Account);
                Assert.AreEqual(TestConstants.OnPremiseUniqueId, result2.UniqueId);
                Assert.AreEqual(new AccountId(TestConstants.OnPremiseUniqueId), result2.Account.HomeAccountId);
                Assert.AreEqual(TestConstants.OnPremiseDisplayableId, result2.Account.Username);
                Assert.AreEqual(app.UserTokenCacheInternal.Semaphore.CurrentCount, 1);
            }
        }

        [TestMethod]
        public void EnsurePublicApiSurfaceExistsOnInterface()
        {
            IPublicClientApplication app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithTelemetry(new TraceTelemetryConfig())
                .Build();

            // This test is to ensure that the methods we want/need on the IPublicClientApplication exist and compile.  This isn't testing functionality, that's done elsewhere.
            // It's solely to ensure we know that the methods we want/need are available where we expect them since we tend to do most testing on the concrete types.

            var interactiveBuilder = app.AcquireTokenInteractive(TestConstants.s_scope)
               .WithAccount(TestConstants.s_user)
               .WithExtraScopesToConsent(TestConstants.s_scope)
               .WithLoginHint("loginhint")
               .WithPrompt(Prompt.ForceLogin);

#if DESKTOP
            interactiveBuilder = interactiveBuilder.WithUseEmbeddedWebView(true);
#endif
            CheckBuilderCommonMethods(interactiveBuilder);

            var iwaBuilder = app.AcquireTokenByIntegratedWindowsAuth(TestConstants.s_scope)
               .WithUsername("upn@live.com");
            CheckBuilderCommonMethods(iwaBuilder);

            var usernamePasswordBuilder = app.AcquireTokenByUsernamePassword(TestConstants.s_scope, "upn@live.com", new SecureString());
            CheckBuilderCommonMethods(usernamePasswordBuilder);

            var deviceCodeBuilder = app.AcquireTokenWithDeviceCode(TestConstants.s_scope, result => Task.FromResult(0))
               .WithDeviceCodeResultCallback(result => Task.FromResult(0));
            CheckBuilderCommonMethods(deviceCodeBuilder);

            var silentBuilder = app.AcquireTokenSilent(TestConstants.s_scope, TestConstants.s_user)
               .WithForceRefresh(true);
            CheckBuilderCommonMethods(silentBuilder);

            silentBuilder = app.AcquireTokenSilent(TestConstants.s_scope, "upn@live.co.uk")
              .WithForceRefresh(true);
            CheckBuilderCommonMethods(silentBuilder);

            var byRefreshTokenBuilder = ((IByRefreshToken)app).AcquireTokenByRefreshToken(TestConstants.s_scope, "refreshtoken")
                                  .WithRefreshToken("refreshtoken");
            CheckBuilderCommonMethods(byRefreshTokenBuilder);
        }

        [TestMethod]
        public void CheckUserProvidedCorrelationIDTest()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var correlationId = Guid.NewGuid();
                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .WithTelemetry(new TraceTelemetryConfig())
                    .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithCorrelationId(correlationId)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull((result.CorrelationId));
                Assert.AreEqual(correlationId.AsMatsCorrelationId(), result.CorrelationId.AsMatsCorrelationId());
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
            }
        }

        public static void CheckBuilderCommonMethods<T>(AbstractAcquireTokenParameterBuilder<T> builder) where T : AbstractAcquireTokenParameterBuilder<T>
        {
            builder.WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, true)
                .WithAuthority(AzureCloudInstance.AzureChina, AadAuthorityAudience.AzureAdMultipleOrgs, true)
                .WithAuthority(AzureCloudInstance.AzurePublic, Guid.NewGuid(), true)
                .WithAuthority(AzureCloudInstance.AzureChina, Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture), true)
                .WithAuthority(TestConstants.AuthorityCommonTenant, Guid.NewGuid(), true)
                .WithAuthority(TestConstants.AuthorityCommonTenant, Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture), true)
                .WithAuthority(TestConstants.AuthorityGuestTenant, true)
                .WithAdfsAuthority(TestConstants.AuthorityGuestTenant, true)
                .WithB2CAuthority(TestConstants.B2CAuthority)
                .WithExtraQueryParameters(
                    new Dictionary<string, string>
                    {
                        {"key1", "value1"}
                    });
        }
    }
}
