// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme;
#if !NET8_0
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
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class AuthenticationOperationTests : TestBase
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
            var authScheme = Substitute.For<IAuthenticationOperation>();
            authScheme.AuthorizationHeaderPrefix.Returns("BearToken");
            authScheme.AccessTokenType.Returns("bearer");
            authScheme.KeyId.Returns("keyid");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });
            // When FormatResult is called, change the AccessToken property 
            authScheme.WhenForAnyArgs(x => x.FormatResult(default)).Do(x => ((AuthenticationResult)x[0]).AccessToken = "enhanced_secret_" + ((AuthenticationResult)x[0]).AccessToken);

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithExperimentalFeatures()
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                // Act
                AuthenticationResult result = await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .WithAuthenticationOperation(authScheme)
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
            var authScheme = Substitute.For<IAuthenticationOperation>();
            authScheme.AuthorizationHeaderPrefix.Returns("BearToken");
            authScheme.KeyId.Returns("keyid");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithExperimentalFeatures()
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                // Act
                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() => app
                     .AcquireTokenInteractive(TestConstants.s_scope)
                     .WithAuthenticationOperation(authScheme)
                     .ExecuteAsync()).ConfigureAwait(false);

                Assert.AreEqual(MsalError.TokenTypeMismatch, ex.ErrorCode);
            }
        }

        private class TestOperation : IAuthenticationOperation2
        {
            public string AuthorizationHeaderPrefix => "TestToken";
            public string KeyId => "keyid";
            public IReadOnlyDictionary<string, string> GetTokenRequestParams()
            {
                return new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } };
            }
            public void FormatResult(AuthenticationResult authenticationResult)
            {
                Assert.Fail("should not be called, FormatResultAsync should be called");
            }

            public Task FormatResultAsync(AuthenticationResult authenticationResult, CancellationToken cancellationToken = default)
            {
                authenticationResult.AccessToken = "IAuthenticationOperation2" + authenticationResult.AccessToken;
                return Task.CompletedTask;
            }

            public Task<bool> ValidateCachedTokenAsync(MsalCacheValidationData cachedTokenData)
            {
                return Task.FromResult(true);
            }

            public int TelemetryTokenType => 5; // Extension
            public string AccessTokenType => "bearer";
        }

        [TestMethod]
        public async Task IAsyncOperation2_Async()
        {
            // Arrang           
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithExperimentalFeatures()
                                                                            .WithClientSecret(TestConstants.ClientSecret)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                httpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                // Act
                var result = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthenticationOperation(new TestOperation())
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsTrue(result.AccessToken.StartsWith("IAuthenticationOperation2"));
                Assert.AreEqual($"TestToken {result.AccessToken}", result.CreateAuthorizationHeader() );
                
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
            IAuthenticationOperation scheme,
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
                builder = builder.WithAuthenticationOperation(scheme);
            }

            return await builder
                .ExecuteAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ValidateCachedTokenAsync_WhenValidationFails_CacheIsIgnoredAsync()
        {
            // Arrange
            var authScheme = Substitute.For<IAuthenticationOperation2>();
            authScheme.AuthorizationHeaderPrefix.Returns("CustomToken");
            authScheme.AccessTokenType.Returns("bearer");
            authScheme.KeyId.Returns("keyid");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });
            
            // Setup FormatResultAsync to just add a prefix
            authScheme.WhenForAnyArgs(x => x.FormatResultAsync(default, default))
                .Do(x => ((AuthenticationResult)x[0]).AccessToken = "validated_" + ((AuthenticationResult)x[0]).AccessToken);
            
            // Setup ValidateCachedTokenAsync to return false (cached token is invalid)
            authScheme.ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                .Returns(Task.FromResult(false));

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // First request - acquire initial token
                httpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                var result1 = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result1);
                Assert.IsTrue(result1.AccessToken.StartsWith("validated_"));
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Second request - validation should fail, so new token should be acquired
                httpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                var result2 = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - validation was called and failed, so a new token was acquired
                await authScheme.Received(1).ValidateCachedTokenAsync(
                    Arg.Any<MsalCacheValidationData>())
                    .ConfigureAwait(false);
                
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        [TestMethod]
        public async Task ValidateCachedTokenAsync_WhenValidationSucceeds_CacheIsUsedAsync()
        {
            // Arrange
            var authScheme = Substitute.For<IAuthenticationOperation2>();
            authScheme.AuthorizationHeaderPrefix.Returns("CustomToken");
            authScheme.AccessTokenType.Returns("bearer");
            authScheme.KeyId.Returns("keyid");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });
            
            // Setup FormatResultAsync
            authScheme.WhenForAnyArgs(x => x.FormatResultAsync(default, default))
                .Do(x => ((AuthenticationResult)x[0]).AccessToken = "validated_" + ((AuthenticationResult)x[0]).AccessToken);
            
            // Setup ValidateCachedTokenAsync to return true (cached token is valid)
            authScheme.ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                .Returns(Task.FromResult(true));

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // First request - acquire initial token
                httpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                var result1 = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result1);
                Assert.IsTrue(result1.AccessToken.StartsWith("validated_"));
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Second request - validation should succeed, so cached token should be returned
                var result2 = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - validation was called and succeeded, so cached token was returned
                await authScheme.Received(1).ValidateCachedTokenAsync(
                    Arg.Any<MsalCacheValidationData>())
                    .ConfigureAwait(false);
                
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(result1.AccessToken, result2.AccessToken);
                Assert.AreEqual(0, httpManager.QueueSize); // No additional token requests
            }
        }

        [TestMethod]
        public async Task ValidateCachedTokenAsync_WithNullCachedItem_ValidationNotCalledAsync()
        {
            // Arrange
            var authScheme = Substitute.For<IAuthenticationOperation2>();
            authScheme.AuthorizationHeaderPrefix.Returns("CustomToken");
            authScheme.AccessTokenType.Returns("bearer");
            authScheme.KeyId.Returns("keyid");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });
            
            authScheme.WhenForAnyArgs(x => x.FormatResultAsync(default, default))
                .Do(x => ((AuthenticationResult)x[0]).AccessToken = "validated_" + ((AuthenticationResult)x[0]).AccessToken);
            
            authScheme.ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                .Returns(Task.FromResult(true));

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // First request with empty cache - no cached token exists
                httpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                var result = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - validation was never called because no cached token existed
                await authScheme.DidNotReceive().ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        [TestMethod]
        public async Task ValidateCachedTokenAsync_OBO_WhenValidationFails_CacheIsIgnoredAsync()
        {
            // Arrange
            var authScheme = Substitute.For<IAuthenticationOperation2>();
            authScheme.AuthorizationHeaderPrefix.Returns("CustomToken");
            authScheme.AccessTokenType.Returns("bearer");
            authScheme.KeyId.Returns("keyid");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });

            authScheme.WhenForAnyArgs(x => x.FormatResultAsync(default, default))
                .Do(x => ((AuthenticationResult)x[0]).AccessToken = "validated_" + ((AuthenticationResult)x[0]).AccessToken);

            // Validation fails - cached token should be ignored
            authScheme.ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                .Returns(Task.FromResult(false));

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // First request - acquire initial token via OBO
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result1 = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result1);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Second request - validation fails, so a new token should be acquired
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                var result2 = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
                await authScheme.Received(1).ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                    .ConfigureAwait(false);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        [TestMethod]
        public async Task ValidateCachedTokenAsync_OBO_WhenValidationSucceeds_CacheIsUsedAsync()
        {
            // Arrange
            var authScheme = Substitute.For<IAuthenticationOperation2>();
            authScheme.AuthorizationHeaderPrefix.Returns("CustomToken");
            authScheme.AccessTokenType.Returns("bearer");
            authScheme.KeyId.Returns("keyid");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });

            authScheme.WhenForAnyArgs(x => x.FormatResultAsync(default, default))
                .Do(x => ((AuthenticationResult)x[0]).AccessToken = "validated_" + ((AuthenticationResult)x[0]).AccessToken);

            // Validation succeeds - cached token should be used
            authScheme.ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                .Returns(Task.FromResult(true));

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // First request - acquire initial token via OBO
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result1 = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result1);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Second request - validation succeeds, cached token should be returned
                var result2 = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
                await authScheme.Received(1).ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                    .ConfigureAwait(false);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        [TestMethod]
        public async Task ValidateCachedTokenAsync_Silent_WhenValidationFails_CacheIsIgnoredAsync()
        {
            // Arrange
            var authScheme = Substitute.For<IAuthenticationOperation2>();
            authScheme.AuthorizationHeaderPrefix.Returns("CustomToken");
            authScheme.AccessTokenType.Returns("bearer");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });

            authScheme.WhenForAnyArgs(x => x.FormatResultAsync(default, default))
                .Do(x => ((AuthenticationResult)x[0]).AccessToken = "validated_" + ((AuthenticationResult)x[0]).AccessToken);

            // Validation fails - cached token should be ignored
            authScheme.ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                .Returns(Task.FromResult(false));

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(new Uri(TestConstants.AuthorityTestTenant), true)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // Populate user token cache with a valid access token and refresh token
                TokenCacheHelper.PopulateCache(cca.UserTokenCacheInternal.Accessor, addSecondAt: false);

                // Silent request - validation should fail, so RT refresh should occur
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityTestTenant);

                var account = (await cca.GetAccountsAsync().ConfigureAwait(false)).Single();

                var result = await cca.AcquireTokenSilent(TestConstants.s_scope, account)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - validation was called and failed, token was refreshed via RT
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                await authScheme.Received(1).ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                    .ConfigureAwait(false);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        [TestMethod]
        public async Task ValidateCachedTokenAsync_Silent_WhenValidationSucceeds_CacheIsUsedAsync()
        {
            // Arrange
            var authScheme = Substitute.For<IAuthenticationOperation2>();
            authScheme.AuthorizationHeaderPrefix.Returns("CustomToken");
            authScheme.AccessTokenType.Returns("bearer");
            authScheme.GetTokenRequestParams().Returns(new Dictionary<string, string>() { { "tokenParam", "tokenParamValue" } });

            authScheme.WhenForAnyArgs(x => x.FormatResultAsync(default, default))
                .Do(x => ((AuthenticationResult)x[0]).AccessToken = "validated_" + ((AuthenticationResult)x[0]).AccessToken);

            // Validation succeeds - cached token should be used
            authScheme.ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                .Returns(Task.FromResult(true));

            using (var httpManager = new MockHttpManager())
            {
                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(new Uri(TestConstants.AuthorityTestTenant), true)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // Populate user token cache with a valid access token and refresh token
                TokenCacheHelper.PopulateCache(cca.UserTokenCacheInternal.Accessor, addSecondAt: false);

                var account = (await cca.GetAccountsAsync().ConfigureAwait(false)).Single();

                // Silent request - validation succeeds, cached token should be returned
                var result = await cca.AcquireTokenSilent(TestConstants.s_scope, account)
                    .WithAuthenticationOperation(authScheme)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - validation was called and succeeded, cached token was returned
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                await authScheme.Received(1).ValidateCachedTokenAsync(Arg.Any<MsalCacheValidationData>())
                    .ConfigureAwait(false);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }
    }
}
