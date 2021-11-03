// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class MetricsTests : TestBase
    {
        [TestInitialize]
        public void TestInit()
        {
            Metrics.TotalAccessTokensFromIdP = 0;
            Metrics.TotalAccessTokensFromCache = 0;
            Metrics.TotalAccessTokensFromBroker = 0;
            Metrics.TotalDurationInMs = 0;
        }

        [TestMethod]
        public async Task MetricsUpdatedSucessfully_AcquireTokenForClient_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                ConfidentialClientApplication cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), false)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(harness.HttpManager)
                                                              .BuildConcrete();

                InMemoryTokenCache memoryTokenCache = new InMemoryTokenCache(withOperationDelay: true, shouldClearExistingCache: false);
                memoryTokenCache.Bind(cca.AppTokenCache);

                // Act - AcquireTokenForClient
                AuthenticationResult result = await cca.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationInCacheInMs > 0);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
                Assert.AreEqual(
                    "https://login.microsoftonline.com/common/oauth2/v2.0/token", 
                    result.AuthenticationResultMetadata.TokenEndpoint);
                Assert.AreEqual(1, Metrics.TotalAccessTokensFromIdP);
                Assert.AreEqual(0, Metrics.TotalAccessTokensFromCache);
                Assert.AreEqual(0, Metrics.TotalAccessTokensFromBroker);

                // Act - AcquireTokenForClient returns result from cache
                result = await cca.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationInCacheInMs > 0);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationInHttpInMs == 0);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
                Assert.AreEqual(1, Metrics.TotalAccessTokensFromIdP);
                Assert.AreEqual(1, Metrics.TotalAccessTokensFromCache);
                Assert.AreEqual(0, Metrics.TotalAccessTokensFromBroker);
                Assert.IsTrue(Metrics.TotalDurationInMs > 0);
                Assert.IsNull(result.AuthenticationResultMetadata.TokenEndpoint);

            }
        }

        [TestMethod]
        public async Task RefreshReasonExpired_ConfidentialClient_Async()
        {
            using (var harness = CreateTestHarness())
            {
                #region ClientCredential
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                ConfidentialClientApplication cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), false)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(harness.HttpManager)
                                                              .BuildConcrete();

                // Act - AcquireTokenForClient returns result from IDP. Refresh reason is no access tokens.
                AuthenticationResult result = await cca.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                                       .ExecuteAsync(CancellationToken.None)
                                                       .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NoCachedAccessToken, result.AuthenticationResultMetadata.CacheRefreshReason);

                //expire access tokens
                TokenCacheHelper.ExpireAllAccessTokens(cca.AppTokenCacheInternal);

                // Act - AcquireTokenForClient returns result from IDP because token is expired.
                result = await cca.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.Expired, result.AuthenticationResultMetadata.CacheRefreshReason);

                // Act - AcquireTokenForClient returns result from Cache. Refresh reason is not applicable.
                result = await cca.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);
                #endregion

                #region ObBehalfOf
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act - AcquireTokenForClient returns result from IDP. Refresh reason is no access tokens.
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope.ToArray(), new UserAssertion(TestConstants.UserAssertion))
                                       .ExecuteAsync(CancellationToken.None)
                                       .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NoCachedAccessToken, result.AuthenticationResultMetadata.CacheRefreshReason);

                //expire access tokens
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                // Act - AcquireTokenOnBehalfOf returns result from IDP because access token is expired.
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope.ToArray(), new UserAssertion(TestConstants.UserAssertion))
                       .ExecuteAsync(CancellationToken.None)
                       .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.Expired, result.AuthenticationResultMetadata.CacheRefreshReason);

                // Act - AcquireTokenOnBehalfOf returns result from cache. Refresh reason is not applicable.
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope.ToArray(), new UserAssertion(TestConstants.UserAssertion))
                       .ExecuteAsync(CancellationToken.None)
                       .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);
                #endregion
            }
        }

        [TestMethod]
        public async Task RefreshReasonExpired_AcquireTokenSilent_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray())
                });

                PublicClientApplication pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), false)
                                .WithHttpManager(harness.HttpManager)
                                .BuildConcrete();

                TokenCacheHelper.PopulateCache(pca.UserTokenCacheInternal.Accessor);

                //expire access tokens
                TokenCacheHelper.ExpireAllAccessTokens(pca.UserTokenCacheInternal);

                // Act - AcquireTokenForClient returns result from IDP.
                AuthenticationResult result = await pca.AcquireTokenSilent(
                                                        TestConstants.s_scope.ToArray(),
                                                        TestConstants.DisplayableId)
                                                        .ExecuteAsync()
                                                        .ConfigureAwait(false);

                //Token should have refreshed due to expiration.
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.Expired, result.AuthenticationResultMetadata.CacheRefreshReason);

                // Act - AcquireTokenForClient returns result from Cache.
                result = await pca.AcquireTokenSilent(
                                                        TestConstants.s_scope.ToArray(),
                                                        TestConstants.DisplayableId)
                                                        .ExecuteAsync()
                                                        .ConfigureAwait(false);

                //Token should have come from cache and cache should not have been refreshed.
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);
            }
        }

        [TestMethod]
        public async Task MetricsUpdatedSucessfully_AcquireTokenInteractive_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                PublicClientApplication pca = CreatePca(harness.HttpManager);
                await TestAcquireTokenInteractive_Async(pca, expectedTokensFromIdp: 1, expectedTokensFromCache: 0).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task MetricsUpdatedSucessfully_AcquireTokenSilent_Async()
        {
            using (var harness = CreateTestHarness())
            {
                PublicClientApplication pca = CreatePca(harness.HttpManager, populateUserCache: true);
                await TestAcquireTokenSilent_Async(pca, expectedTokensFromIdp: 0, expectedTokensFromCache: 1).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task MetricsUpdatedSucessfully_AcquireTokenInteractiveAndSilent_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                PublicClientApplication pca = CreatePca(harness.HttpManager);
                await TestAcquireTokenInteractive_Async(pca, expectedTokensFromIdp: 1, expectedTokensFromCache: 0).ConfigureAwait(false);
                await TestAcquireTokenSilent_Async(pca, expectedTokensFromIdp: 1, expectedTokensFromCache: 1).ConfigureAwait(false);
            }
        }

        private PublicClientApplication CreatePca(MockHttpManager httpManager, bool populateUserCache = false)
        {
            PublicClientApplication pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), false)
                            .WithHttpManager(httpManager)
                            .BuildConcrete();
            
            if (populateUserCache)
            {
                TokenCacheHelper.PopulateCache(pca.UserTokenCacheInternal.Accessor);
            }
            InMemoryTokenCache memoryTokenCache = new InMemoryTokenCache(withOperationDelay: true, shouldClearExistingCache: false);
            memoryTokenCache.Bind(pca.UserTokenCache);

            return pca;
        }

        private async Task TestAcquireTokenInteractive_Async(PublicClientApplication pca, int expectedTokensFromIdp = 0, int expectedTokensFromCache = 0, int expectedTokensFromBroker = 0)
        {
            pca.ServiceBundle.ConfigureMockWebUI(
                AuthorizationResult.FromUri(pca.AppConfig.RedirectUri + "?code=some-code"));

            AuthenticationResult result = await pca
                .AcquireTokenInteractive(TestConstants.s_scope)
                .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(result.AuthenticationResultMetadata.DurationInCacheInMs > 0);
            Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.AreEqual(expectedTokensFromIdp, Metrics.TotalAccessTokensFromIdP);
            Assert.AreEqual(expectedTokensFromCache, Metrics.TotalAccessTokensFromCache);
            Assert.AreEqual(expectedTokensFromBroker, Metrics.TotalAccessTokensFromBroker);
            Assert.IsTrue(Metrics.TotalDurationInMs > 0);
        }

        private async Task TestAcquireTokenSilent_Async(PublicClientApplication pca, int expectedTokensFromIdp = 0, int expectedTokensFromCache = 0, int expectedTokensFromBroker = 0)
        {
            AuthenticationResult result = await pca.AcquireTokenSilent(
                TestConstants.s_scope.ToArray(),
                TestConstants.DisplayableId)
                .WithAuthority(pca.Authority, false)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(result.AuthenticationResultMetadata.DurationInCacheInMs > 0);
            Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.AreEqual(expectedTokensFromIdp, Metrics.TotalAccessTokensFromIdP);
            Assert.AreEqual(expectedTokensFromCache, Metrics.TotalAccessTokensFromCache);
            Assert.AreEqual(expectedTokensFromBroker, Metrics.TotalAccessTokensFromBroker);
            Assert.IsTrue(Metrics.TotalDurationInMs > 0);
        }
    }
}
