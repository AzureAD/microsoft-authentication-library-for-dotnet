// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme;
#if !NET6_0
using Microsoft.Identity.Client.Platforms.Features.RuntimeBroker;
#endif
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.Identity.Test.Unit.BrokerTests;
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
            authScheme.AccessTokenType.Returns("bearer");
            authScheme.KeyId.Returns("keyid");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });
            authScheme.FormatAccessToken(default).ReturnsForAnyArgs(x => "enhanced_secret_" + ((MsalAccessTokenCacheItem)x[0]).Secret);

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

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

        [TestMethod]
        public async Task PopBrokerAuthSchemeTestAsync_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var mockBroker = Substitute.For<IBroker>();
                mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(true);
                mockBroker.IsPopSupported.Returns(true);
                mockBroker.AcquireTokenInteractiveAsync(
                    Arg.Any<AuthenticationRequestParameters>(),
                    Arg.Any<AcquireTokenInteractiveParameters>()).Returns(
                    MockHelpers.CreateMsalRunTimeBrokerTokenResponse(null, Constants.PoPAuthHeaderPrefix));
                mockBroker.AcquireTokenSilentAsync(
                    Arg.Any<AuthenticationRequestParameters>(), 
                    Arg.Any<AcquireTokenSilentParameters>()).Returns(
                    MockHelpers.CreateMsalRunTimeBrokerTokenResponse(null, Constants.PoPAuthHeaderPrefix));

                var pcaBuilder = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithTestBroker(mockBroker)
                    .WithHttpManager(harness.HttpManager);

#if NET6_WIN
                pcaBuilder = pcaBuilder.WithBroker(true);
#else
                pcaBuilder = pcaBuilder.WithBroker();
#endif
                var pca = pcaBuilder.BuildConcrete();

                TokenCacheHelper.PopulateCache(pca.UserTokenCacheInternal.Accessor);
                TokenCacheHelper.ExpireAllAccessTokens(pca.UserTokenCacheInternal);

                pca.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

                var resultForATI = await pca.AcquireTokenInteractive(TestConstants.s_graphScopes)
                    .WithProofOfPossession(TestConstants.Nonce, HttpMethod.Get, new Uri(TestConstants.AuthorityCommonTenant))
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Act
                var resultForATS = await pca.AcquireTokenSilent(TestConstants.s_graphScopes, TestConstants.DisplayableId)
                    .WithProofOfPossession(TestConstants.Nonce, HttpMethod.Get, new Uri(TestConstants.AuthorityCommonTenant))
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                //Assert

                //Validate that access token from broker is not wrapped
                Assert.AreEqual(TestConstants.UserAccessToken, resultForATI.AccessToken);
                Assert.AreEqual(Constants.PoPAuthHeaderPrefix, resultForATI.TokenType);
                Assert.AreEqual(TokenSource.Broker, resultForATI.AuthenticationResultMetadata.TokenSource);
                //Validate that the pop broker auth scheme returns the correct token type for ATI
                Assert.AreEqual(Constants.PoPAuthHeaderPrefix + " " + TestConstants.UserAccessToken, resultForATI.CreateAuthorizationHeader());

                //Validate that access token from broker is not wrapped
                Assert.AreEqual(TestConstants.UserAccessToken, resultForATS.AccessToken);
                Assert.AreEqual(Constants.PoPAuthHeaderPrefix, resultForATS.TokenType);
                Assert.AreEqual(TokenSource.Broker, resultForATS.AuthenticationResultMetadata.TokenSource);
                //Validate that the pop broker auth scheme returns the correct token type for ATS
                Assert.AreEqual(Constants.PoPAuthHeaderPrefix + " " + TestConstants.UserAccessToken, resultForATS.CreateAuthorizationHeader());
            }
        }

        [TestMethod]
        public async Task WrongTokenType_Async()
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

                app.ServiceBundle.ConfigureMockWebUI();

                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                // Act
                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() => app
                     .AcquireTokenInteractive(TestConstants.s_scope)
                     .WithAuthenticationScheme(authScheme)
                     .ExecuteAsync()).ConfigureAwait(false);

                Assert.AreEqual(MsalError.TokenTypeMismatch, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task MissingAccessTokenTypeInResponse_Throws_Async()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityCommonTenant);
                var tokenClient = new TokenClient(requestParams);

                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var fakeResponse = TestConstants.CreateMsalTokenResponse();
                fakeResponse.TokenType = null;
                harness.HttpManager.AddResponseMockHandlerForPost(MockHelpers.CreateSuccessResponseMessage(JsonHelper.SerializeToJson(fakeResponse)));
                await requestParams.AuthorityManager.RunInstanceDiscoveryAndValidationAsync().ConfigureAwait(false);

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                    () => tokenClient.SendTokenRequestAsync(new Dictionary<string, string>())).ConfigureAwait(false);
                Assert.AreEqual(MsalError.AccessTokenTypeMissing, ex.ErrorCode);
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
