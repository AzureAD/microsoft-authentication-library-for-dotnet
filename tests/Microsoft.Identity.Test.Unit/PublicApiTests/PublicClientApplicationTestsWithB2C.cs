// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
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
    [TestCategory(TestCategories.B2C)]
    public class PublicClientApplicationTestsWithB2C : TestBase
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
        }

        [TestMethod]
        public void B2CLoginAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

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
        public void B2CAcquireTokenTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

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
        public void B2CAcquireTokenWithValidateAuthorityTrueTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

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
        public async Task B2CAcquireTokenWithValidateAuthorityTrueAndRandomAuthorityTest_Async()
        {
            using (var harness = base.CreateTestHarness())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CCustomDomain), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithCachePartitioningAsserts(harness.ServiceBundle.PlatformProxy)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.B2CCustomDomain);

                AuthenticationResult result = await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                await app.AcquireTokenSilent(TestConstants.s_scope, result.Account).ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        [Description("Test against https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3471")]
        public void B2CIgnoreWithTenantId_Success()
        {
            PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(new Uri(TestConstants.B2CCustomDomain), true)
                .WithTenantId(Guid.NewGuid().ToString("D"))
                .BuildConcrete();
            
            Assert.AreEqual(TestConstants.B2CCustomDomain, app.Authority);

            app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(new Uri(TestConstants.B2CAuthority), true)
                .WithTenantId(Guid.NewGuid().ToString("D"))
                .BuildConcrete();

            Assert.AreEqual(TestConstants.B2CAuthority, app.Authority);
        }

        [TestMethod]
        public void B2CAcquireTokenAuthorityHostMisMatchErrorTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
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

                MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle, ui);

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
        /// If no scopes are passed in, B2C does not return a AT. MSAL must be able to 
        /// persist the data to the cache and return an AuthenticationResult.
        /// This behavior has been seen on B2C, as AAD will return an access token for the implicit scopes.
        /// </summary>
        [TestMethod]
        public async Task B2C_NoScopes_NoAccessToken_Async()
        {

            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                // Arrange 1 - interactive call with 0 scopes
                httpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.B2CLoginAuthority,
                    responseMessage: MockHelpers.CreateSuccessResponseMessage(MockHelpers.B2CTokenResponseWithoutAT));

                // Act 
                AuthenticationResult result = await app
                    .AcquireTokenInteractive(null) // no scopes -> no Access Token!
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                AssertNoAccessToken(result);
                Assert.AreEqual(0, httpManager.QueueSize);

                var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() =>
                  app.AcquireTokenSilent(null, result.Account).ExecuteAsync()
              ).ConfigureAwait(false);

                Assert.AreEqual(MsalError.ScopesRequired, ex.ErrorCode);
                Assert.AreEqual(UiRequiredExceptionClassification.AcquireTokenSilentFailed, ex.Classification);
            }
        }

        [TestMethod]
        public async Task B2CSomeExceptionAsync()
        {

            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.B2CLoginAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle,
                                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                // Arrange AcquireTokenWithUsernamePassword
                httpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.B2CLoginAuthority,
                    responseMessage: MockHelpers.CreateSuccessResponseMessage("non json response"));

                // Act 
                var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => 
                    app.AcquireTokenByUsernamePassword(new[] { "user.read" }, "username", "password" ) // no scopes -> no Access Token!
                    .ExecuteAsync())
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.JsonParseError, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.JsonParseErrorMessage, ex.Message);
            }
        }

        private static void AssertNoAccessToken(AuthenticationResult result)
        {
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
            Assert.IsNotNull(result.IdToken);
            Assert.IsNull(result.AccessToken);
            Assert.IsNull(result.Scopes);
            Assert.IsTrue(result.ExpiresOn == default);
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

            MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle, ui);
            harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(authority);

            var result = app
                .AcquireTokenInteractive(TestConstants.s_scope)
                .ExecuteAsync(CancellationToken.None)
                .Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
        }
    }
}
