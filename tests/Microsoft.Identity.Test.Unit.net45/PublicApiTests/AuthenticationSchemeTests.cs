// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class AuthenticationSchemeTests : TestBase
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
        }

        [TestMethod]
        public async Task Interactive_WithCustomAuthScheme_ThenSilent_Async()
        {
            // Arrange
            var authScheme = Substitute.For<IAuthenticationScheme>();
            authScheme.AuthorizationHeaderPrefix.Returns("BearToken");
            authScheme.KeyId.Returns("keyid");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });
            authScheme.FormatAccessToken(default).ReturnsForAnyArgs(x => "enhanced_secret_" + ((MsalAccessTokenCacheItem)x[0]).Secret);

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

                // Act
                AuthenticationResult result = await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithAuthenticationScheme(authScheme)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                ValidateEnhancedToken(httpManager, result);

                // Act again - silent call with the same auth scheme - will retrieve existing AT
                var silentResult = await RunSilentCallAsync(
                   httpManager,
                   app,
                   scheme: authScheme,
                   expectRtRefresh: false).ConfigureAwait(false);
                ValidateEnhancedToken(httpManager, silentResult);

                // Act again - silent call with a different scheme - the existing AT will not be returned
                authScheme.KeyId.Returns("other_keyid");
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityTestTenant);

                silentResult = await RunSilentCallAsync(
                   httpManager,
                   app,
                   scheme: authScheme,
                   expectRtRefresh: true).ConfigureAwait(false);
                ValidateEnhancedToken(httpManager, silentResult);

                // silent call with no key id (i.e. BearerScheme) - the existing AT will not be returned
                silentResult = await RunSilentCallAsync(
                    httpManager, 
                    app,
                    scheme: null,
                    expectRtRefresh: true).ConfigureAwait(false);
                ValidateBearerToken(httpManager, silentResult);
            }
        }

        private static void ValidateBearerToken(MockHttpManager httpManager, AuthenticationResult result)
        {
            Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
            Assert.AreEqual("Bearer", result.TokenType, "Token Type is read from EVO");
            Assert.AreEqual("Bearer " + TestConstants.ATSecret, result.CreateAuthorizationHeader());
            Assert.AreEqual(0, httpManager.QueueSize);
        }

        private static void ValidateEnhancedToken(MockHttpManager httpManager, AuthenticationResult result)
        {
            string expectedAt = "enhanced_secret_" + TestConstants.ATSecret;

            Assert.AreEqual(expectedAt, result.AccessToken);
            Assert.AreEqual("Bearer", result.TokenType, "Token Type is read from EVO");
            Assert.AreEqual("BearToken " + expectedAt, result.CreateAuthorizationHeader());
            Assert.AreEqual(0, httpManager.QueueSize);
        }

        private static async Task<AuthenticationResult> RunSilentCallAsync(
            MockHttpManager httpManager,
            PublicClientApplication app,
            IAuthenticationScheme scheme, 
            bool expectRtRefresh)
        {
            if (expectRtRefresh)
            {
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityTestTenant);
            }

            var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();

            var builder = app.AcquireTokenSilent(TestConstants.s_scope, account);
            if (scheme != null)
            {
                builder = builder.WithAuthenticationScheme(scheme);
            }

            return await builder
                .ExecuteAsync().ConfigureAwait(false);

           
        }
    }
}
