// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Core.Helpers;
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
                await TestAcquireTokenSilent_Async(pca, expectedTokensFromIdp: 1, expectedTokensFromCache: 1, 0, refreshIn: true).ConfigureAwait(false);
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

            DateTimeOffset expectedDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(2400);
            var remainingTimeMS = result.AuthenticationResultMetadata.RefreshOn.Value - DateTimeOffset.UtcNow;
            DateTimeOffset actualRefreshIn = (new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero)).AddMilliseconds(remainingTimeMS.TotalMilliseconds);

            CoreAssert.IsWithinRange(expectedDateTimeOffset, actualRefreshIn, TimeSpan.FromSeconds(Constants.DefaultJitterRangeInSeconds));
        }

        private async Task TestAcquireTokenSilent_Async(PublicClientApplication pca, int expectedTokensFromIdp = 0, int expectedTokensFromCache = 0, int expectedTokensFromBroker = 0, bool refreshIn = false)
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

            if (refreshIn)
            {
                //Force Refresh In
                var at = pca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                at = TokenCacheHelper.WithRefreshOn(at, DateTimeOffset.UtcNow - TimeSpan.FromSeconds(Constants.DefaultJitterRangeInSeconds));
                at = at.WithExpiresOn(DateTimeOffset.UtcNow);
                pca.UserTokenCacheInternal.Accessor.ClearAccessTokens();
                pca.UserTokenCacheInternal.Accessor.SaveAccessToken(at);

                var at2 = pca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();

                result = await pca.AcquireTokenSilent(
                    TestConstants.s_scope.ToArray(),
                    TestConstants.DisplayableId)
                    .WithAuthority(pca.Authority, false)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var at3 = pca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();

                Assert.IsNotNull(result);
                Assert.IsTrue(result.AuthenticationResultMetadata.CacheRefreshReason == Client.CacheRefreshReason.ProactivelyRefreshed);
            }
        }
    }
}
