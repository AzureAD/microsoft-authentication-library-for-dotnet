// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Advanced;
#if !NET5_0_OR_GREATER
using Microsoft.Identity.Client.Desktop;
#endif
#if !NET6_0
using Microsoft.Identity.Client.Broker;
#endif
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class PublicClientApplicationTests : TestBase
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
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
            Assert.AreEqual(TestConstants.RedirectUri, app.AppConfig.RedirectUri);
        }

        [TestMethod]
        public async Task NoStateReturnedTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    AddStateInAuthorizationResult = false,
                    MockResult = AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code")
                };

                app.ServiceBundle.ConfigureMockWebUI(ui);

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
        public async Task DifferentStateReturnedTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .BuildConcrete();

                MockWebUI ui = new MockWebUI()
                {
                    AddStateInAuthorizationResult = false,
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?code=some-code&state=mismatched")
                };

                app.ServiceBundle.ConfigureMockWebUI(ui);

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
                                                                            .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

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
                                                                            .BuildConcrete();
                app.ServiceBundle.ConfigureMockWebUI();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

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
                Assert.IsNull(userCacheAccess.LastBeforeAccessNotificationArgs.SuggestedCacheKey, "Don't suggest keys for public client");
                Assert.IsNull(userCacheAccess.LastAfterAccessNotificationArgs.SuggestedCacheKey, "Don't suggest keys for public client");
                userCacheAccess.AssertAccessCounts(0, 1);

                // repeat interactive call and pass in the same user
                app.ServiceBundle.ConfigureMockWebUI();

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityCommonTenant,
                    null,
                    null);

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
        public void AcquireTokenExtraHeadersTest()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();
                app.ServiceBundle.ConfigureMockWebUI();
                var userCacheAccess = app.UserTokenCache.RecordAccess();
                var extraExpectedHeaders = TestConstants.ExtraHttpHeader;
                extraExpectedHeaders.Add(Constants.CcsRoutingHintHeader, CoreHelpers.GetCcsUpnHint(TestConstants.s_user.Username));
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant, null, null, false, null, extraExpectedHeaders);

                Guid correlationId = Guid.NewGuid();

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithCorrelationId(correlationId)
                    .WithExtraHttpHeaders(TestConstants.ExtraHttpHeader)
                    .WithLoginHint(TestConstants.s_user.Username)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Account);
                Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.IsNull(userCacheAccess.LastBeforeAccessNotificationArgs.SuggestedCacheKey, "Don't suggest keys for public client");
                Assert.IsNull(userCacheAccess.LastAfterAccessNotificationArgs.SuggestedCacheKey, "Don't suggest keys for public client");
                userCacheAccess.AssertAccessCounts(0, 1);
            }
        }

        [TestMethod]
        public async Task AcquireTokenDifferentResourcesAsync()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .BuildConcrete();
                app.ServiceBundle.ConfigureMockWebUI();
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            "resource/scope1",
                            MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                            MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid))
                    });

                AuthenticationResult result;
                result = await app
                    .AcquireTokenInteractive(new[] { "resource/scope1" })
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                harness.HttpManager.AddMockHandler(
                   new MockHttpMessageHandler
                   {
                       ExpectedMethod = HttpMethod.Post,
                       ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                           "resource/scope1 resource/scope2",
                           MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                           MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid))
                   });

                var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
                result = await app
                    .AcquireTokenSilent(new[] { "resource/scope2" }, accounts.Single())
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource, "Second token can be obtained silently via refresh_token flow");

                result = await app
                    .AcquireTokenSilent(new[] { "resource/scope1" }, accounts.Single())
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource, "First token should still be in the cache");

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
                Assert.AreEqual(TestConstants.RedirectUri, app.AppConfig.RedirectUri);

                app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithDefaultRedirectUri()
                                                                            .BuildConcrete();

                //Validate new default redirect uri
#if NETFRAMEWORK || NET6_WIN
                Assert.AreEqual(Constants.NativeClientRedirectUri, app.AppConfig.RedirectUri);
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
                                                                            .WithCachePartitioningAsserts(harness.ServiceBundle.PlatformProxy)
                                                                            .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

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
                app.ServiceBundle.ConfigureMockWebUI();

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
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

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

                app.ServiceBundle.ConfigureMockWebUI(
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

                //Ensure Interactive flow does not fail when different home account id is returned
                AuthenticationResult result2 = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithAccount(result.Account)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.s_scope.AsSingleString(),
                            MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                            MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid + "more"))
                    });

                //Silent flow should fail when different account is returned.
                var exception = Assert.ThrowsException<AggregateException>( () =>
                    result = app
                    .AcquireTokenSilent(TestConstants.s_scope, result.Account)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None)
                    .Result
                    , "Silent API should have failed here");

                MsalClientException exc = (MsalClientException)exception.InnerException;
                Assert.IsNotNull(exc);
                Assert.AreEqual(MsalError.UserMismatch, exc.ErrorCode);

                var users = app.GetAccountsAsync().Result;
                Assert.AreEqual(2, users.Count());
                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
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
                                                                            .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

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
                app.ServiceBundle.ConfigureMockWebUI();

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
                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
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
                    .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

                // add mock response bigger than 1MB for Http Client
                httpManager.AddFailingRequest(new InvalidOperationException());

                await AssertException.TaskThrowsAsync<InvalidOperationException>(
                    () => app
                        .AcquireTokenInteractive(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None))
                    .ConfigureAwait(false);
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
                                                                            .BuildConcrete();

                // repeat interactive call and pass in the same user

                app.ServiceBundle.ConfigureMockWebUI(
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
        public async Task GetAccountByUserFlowTestsAsync()
        {
            var app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithB2CAuthority(TestConstants.B2CLoginAuthority)
                .BuildConcrete();

            var accounts = app.GetAccountsAsync(TestConstants.B2CSignUpSignIn).Result;
            Assert.AreEqual(0, accounts.Count());

            await AssertException.TaskThrowsAsync<ArgumentException>(() =>
              app.GetAccountsAsync(string.Empty)).ConfigureAwait(false);

            accounts = PopulateB2CTokenCacheAsync(TestConstants.B2CSignUpSignIn, app).Result;

            var userToFind = accounts.First();

            Assert.IsNotNull(accounts);
            // one account in the cache for susi user flow

            Assert.IsNull(userToFind.Username);
            Assert.AreEqual(TestConstants.B2CSuSiHomeAccountIdentifer, userToFind.HomeAccountId.Identifier);
            Assert.AreEqual(TestConstants.B2CEnvironment, userToFind.Environment);
            Assert.AreEqual(TestConstants.Utid, userToFind.HomeAccountId.TenantId);
            Assert.AreEqual(TestConstants.B2CSuSiHomeAccountObjectId, userToFind.HomeAccountId.ObjectId);

            accounts = PopulateB2CTokenCacheAsync(TestConstants.B2CEditProfile, app).Result;

            Assert.IsNotNull(accounts);
            // one account in the cache for edit profile user flow

            userToFind = accounts.First();
            Assert.IsNull(userToFind.Username);
            Assert.AreEqual(TestConstants.B2CEditProfileHomeAccountIdentifer, userToFind.HomeAccountId.Identifier);
            Assert.AreEqual(TestConstants.B2CEnvironment, userToFind.Environment);
            Assert.AreEqual(TestConstants.Utid, userToFind.HomeAccountId.TenantId);
            Assert.AreEqual(TestConstants.B2CEditProfileHomeAccountObjectId, userToFind.HomeAccountId.ObjectId);

            accounts = PopulateB2CTokenCacheAsync(TestConstants.B2CProfileWithDot, app).Result;

            Assert.IsNotNull(accounts);
            // one account in the cache for edit profile user flow

            userToFind = accounts.First();
            Assert.IsNull(userToFind.Username);
            Assert.AreEqual(TestConstants.B2CProfileWithDotHomeAccountIdentifer, userToFind.HomeAccountId.Identifier);
            Assert.AreEqual(TestConstants.B2CEnvironment, userToFind.Environment);
            Assert.AreEqual(TestConstants.Utid, userToFind.HomeAccountId.TenantId);
            Assert.AreEqual(TestConstants.B2CProfileWithDotHomeAccountObjectId, userToFind.HomeAccountId.ObjectId);
        }

        [TestMethod]
        [Description("Test for AcquireToken with user canceling authentication")]
        public async Task AcquireTokenWithAuthenticationCanceledTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithDebugLoggingCallback(logLevel: LogLevel.Verbose)
                                                                            .BuildConcrete();

                // Interactive call and user cancels authentication
                var ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel)
                };

                app.ServiceBundle.ConfigureMockWebUI(ui);
                ;

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

                PublicClientApplication app = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // Interactive call and authentication fails with access denied
                MockWebUI ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?error=access_denied")
                };

                app.ServiceBundle.ConfigureMockWebUI(ui);
                ;

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
                .BuildConcrete();

            var authority = Authority.CreateAuthorityWithTenant(app.ServiceBundle.Config.Authority.AuthorityInfo, null);
            Assert.AreEqual(new Uri(ClientApplicationBase.DefaultAuthority), authority.AuthorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        [Description("ClientApplicationBase.GetAuthority tests")]
        public void GetAuthority_AccountWithIdPassed_TenantedAuthorityUsed()
        {
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .BuildConcrete();

            var authority = Authority.CreateAuthorityWithTenant(
                app.ServiceBundle.Config.Authority.AuthorityInfo,
                TestConstants.Utid);

            Assert.AreEqual(new Uri(TestConstants.AuthorityTestTenant), authority.AuthorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        public async Task AcquireTokenSilent_EmptyLoginHint_TestAsync()
        {
            var app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .Build();

            await AssertException.TaskThrowsAsync<ArgumentNullException>(() =>
               app.AcquireTokenSilent(TestConstants.s_scope.ToArray(), string.Empty).ExecuteAsync())
                .ConfigureAwait(false);
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

            using (var httpManager = new MockHttpManager())
            {
                // Arrange
                PublicClientApplication pca = CreatePcaFromFileWithAuthority(httpManager);

                // Act
                var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                var account = accounts.Single(a => a.HomeAccountId.TenantId == tenant1);
                var tenantProfiles = account.GetTenantProfiles();

                AuthenticationResult response = await
                    pca.AcquireTokenSilent(new[] { "User.Read" }, account)
                    .WithTenantIdFromAuthority(new Uri(tenantedAuthority1))
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant1, response.TenantId);
                AssertTenantProfiles(tenantProfiles, tenant1, tenant2);
                AssertTenantProfiles(response.Account.GetTenantProfiles(), tenant1, tenant2);
                AssertWamIds(response.Account as Account, 0);
                Assert.AreEqual(tenant1, response.ClaimsPrincipal.FindFirst("tid").Value);

                // Act
                accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                account = accounts.Single(a => a.HomeAccountId.TenantId == tenant2);
                response = await
                    pca.AcquireTokenSilent(new[] { "User.Read" }, account)
                    .WithTenantId(tenant2)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant2, response.TenantId);
                Assert.AreEqual(tenant2, response.ClaimsPrincipal.FindFirst("tid").Value);
            }
        }

        private void AssertWamIds(Account account, int count, string wamId = null)
        {
            Assert.IsNotNull(account);
            if (count == 0)
            {
                Assert.IsNull(account.WamAccountIds);
            }
            else
            {
                Assert.IsNotNull(account.WamAccountIds);
                Assert.AreEqual(1, account.WamAccountIds.Count);
                Assert.AreEqual(wamId, account.WamAccountIds["1d18b3b0-251b-4714-a02a-9956cec86c2d"]);
            }
        }

        private void AssertTenantProfiles(IEnumerable<TenantProfile> tenantProfiles, string tenant1, string tenant2)
        {
            var tenantProfile1 = tenantProfiles.Single(tp => tp.TenantId == tenant1);
            var tenantProfile2 = tenantProfiles.Single(tp => tp.TenantId == tenant2);

            Assert.AreEqual(2, tenantProfiles.Count());

            Assert.AreEqual(tenant1, tenantProfile1.TenantId);
            Assert.AreEqual(tenant2, tenantProfile2.TenantId);

            Assert.IsTrue(tenantProfile1.IsHomeTenant);
            Assert.IsFalse(tenantProfile2.IsHomeTenant);

            Assert.IsNotNull(tenantProfile1.ClaimsPrincipal);
            Assert.IsTrue(tenantProfile1.ClaimsPrincipal.Claims.Count() > 0);

            Assert.IsNotNull(tenantProfile2.ClaimsPrincipal);
            Assert.IsTrue(tenantProfile2.ClaimsPrincipal.Claims.Count() > 0);
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
                PublicClientApplication pca = CreatePcaFromFileWithAuthority(httpManager, authority: tenantedAuthority1);

                // Act
                var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                var account = accounts.Single(a => a.HomeAccountId.TenantId == tenant1);
                AuthenticationResult response = await
                    pca.AcquireTokenSilent(new[] { "User.Read" }, account)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant1, response.TenantId);
                AssertTenantProfiles(account.GetTenantProfiles(), tenant1, tenant2);

                // Arrange
                PublicClientApplication pca2 = CreatePcaFromFileWithAuthority(httpManager, authority: tenantedAuthority2);

                // Act
                accounts = await pca2.GetAccountsAsync().ConfigureAwait(false);
                account = accounts.Single(a => a.HomeAccountId.TenantId == tenant2);
                response = await
                    pca2.AcquireTokenSilent(new[] { "User.Read" }, account)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(tenant2, response.TenantId);
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.Regression)]
        [WorkItem(1365)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1365
        public async Task PCAAuthority_DirtiedByATS_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(new Uri(ClientApplicationBase.DefaultAuthority), app.ServiceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority);

                // ATS must not update the PCA authority
                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                await app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(new Uri(ClientApplicationBase.DefaultAuthority), app.ServiceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority);

                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                // this would fail because the request should go to /common but instead it goes to tenanted authority
                await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(new Uri(ClientApplicationBase.DefaultAuthority), app.ServiceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority);
            }
        }

        private static PublicClientApplication CreatePcaFromFileWithAuthority(
            MockHttpManager httpManager,
            string tokenCacheFile = "MultiTenantTokenCache.json",
            string authority = null,
            bool enableBroker = false)
        {
            const string clientIdInFile = "1d18b3b0-251b-4714-a02a-9956cec86c2d";

            var pcaBuilder = PublicClientApplicationBuilder
                .Create(clientIdInFile)
                .WithLogging((lvl, msg, _) => Trace.WriteLine($"[{lvl}] {msg}"))
                .WithHttpManager(httpManager);

            if (authority != null)
            {
                pcaBuilder = pcaBuilder.WithAuthority(authority);
            }

            if (enableBroker)
            {
#if NET6_WIN && !NET6_0
                pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
#elif !NET6_0
                WamExtension.WithBroker(pcaBuilder, new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
#endif
            }

            var pca = pcaBuilder.BuildConcrete();
            pca.InitializeTokenCacheFromFile(ResourceHelper.GetTestResourceRelativePath(tokenCacheFile), true);
            var expectedRTs = enableBroker ? 0 : 2;
            pca.UserTokenCacheInternal.Accessor.AssertItemCount(3, expectedRTs, 3, 3, 1);
            return pca;
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
                    .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

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
                    .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

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
                .Build();

            // This test is to ensure that the methods we want/need on the IPublicClientApplication exist and compile.  This isn't testing functionality, that's done elsewhere.
            // It's solely to ensure we know that the methods we want/need are available where we expect them since we tend to do most testing on the concrete types.

            var interactiveBuilder = app.AcquireTokenInteractive(TestConstants.s_scope)
               .WithAccount(TestConstants.s_user)
               .WithExtraScopesToConsent(TestConstants.s_scope)
               .WithLoginHint("loginhint")
               .WithPrompt(Prompt.ForceLogin);

#if NETFRAMEWORK
            interactiveBuilder = interactiveBuilder.WithUseEmbeddedWebView(true);
#endif
            CheckBuilderCommonMethods(interactiveBuilder);

            var iwaBuilder = app.AcquireTokenByIntegratedWindowsAuth(TestConstants.s_scope)
               .WithUsername("upn@live.com");
            CheckBuilderCommonMethods(iwaBuilder);

            var usernamePasswordBuilder = app.AcquireTokenByUsernamePassword(TestConstants.s_scope, "upn@live.com", "");
            CheckBuilderCommonMethods(usernamePasswordBuilder);

            var deviceCodeBuilder = app.AcquireTokenWithDeviceCode(TestConstants.s_scope, _ => Task.FromResult(0))
               .WithDeviceCodeResultCallback(_ => Task.FromResult(0));
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
                    .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithCorrelationId(correlationId)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;

                Assert.IsNotNull(result.CorrelationId);
                Assert.AreEqual(correlationId.ToString(), result.CorrelationId.ToString());
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
            }
        }

        public static void CheckBuilderCommonMethods<T>(AbstractAcquireTokenParameterBuilder<T> builder) where T : AbstractAcquireTokenParameterBuilder<T>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            builder.WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, true)
                .WithAuthority(AzureCloudInstance.AzureChina, AadAuthorityAudience.AzureAdMultipleOrgs, true)
                .WithAuthority(AzureCloudInstance.AzurePublic, Guid.NewGuid(), true)
                .WithAuthority(AzureCloudInstance.AzureChina, Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture), true)
                .WithAuthority(TestConstants.AuthorityCommonTenant, Guid.NewGuid(), true)
                .WithAuthority(TestConstants.AuthorityCommonTenant, Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture), true)
                .WithAuthority(TestConstants.AuthorityGuestTenant, true)
                .WithAdfsAuthority(TestConstants.AuthorityGuestTenant, true)
                .WithB2CAuthority(TestConstants.B2CAuthority)
                .WithTenantId(TestConstants.TenantId)
                .WithExtraQueryParameters(
                    new Dictionary<string, string>
                    {
                        {"key1", "value1"}
                    });
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private Task<IEnumerable<IAccount>> PopulateB2CTokenCacheAsync(string userFlow, PublicClientApplication app)
        {
            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCacheInternal.Accessor, TestConstants.B2CSuSiHomeAccountObjectId,
                TestConstants.Utid, TestConstants.ClientId, TestConstants.B2CEnvironment);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCacheInternal.Accessor, TestConstants.B2CSuSiHomeAccountObjectId,
                TestConstants.Utid, TestConstants.B2CEnvironment);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCacheInternal.Accessor, TestConstants.B2CEditProfileHomeAccountObjectId,
                TestConstants.Utid, TestConstants.ClientId, TestConstants.B2CEnvironment);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCacheInternal.Accessor, TestConstants.B2CEditProfileHomeAccountObjectId,
                TestConstants.Utid, TestConstants.B2CEnvironment);

            TokenCacheHelper.AddRefreshTokenToCache(app.UserTokenCacheInternal.Accessor, TestConstants.B2CProfileWithDotHomeAccountObjectId,
                TestConstants.Utid, TestConstants.ClientId, TestConstants.B2CEnvironment);
            TokenCacheHelper.AddAccountToCache(app.UserTokenCacheInternal.Accessor, TestConstants.B2CProfileWithDotHomeAccountObjectId,
                TestConstants.Utid, TestConstants.B2CEnvironment);

            return app.GetAccountsAsync(userFlow);
        }
    }
}
