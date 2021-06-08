using System;
using System.Linq;
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
            Metrics.TotalDurationInMs = 0;
        }

        [TestMethod]
        public async Task MetricsUpdatedSucessfully_AcquireTokenForClient_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), false)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(harness.HttpManager)
                                                              .BuildConcrete();

                // Act - AcquireTokenForClient
                var result = await cca.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationInCacheInMs > 0);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
                Assert.AreEqual(1, Metrics.TotalAccessTokensFromIdP);
                Assert.AreEqual(0, Metrics.TotalAccessTokensFromCache);

                // Act - AcquireTokenForClient returns result from cache
                result = await cca.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationInCacheInMs > 0);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationInHttpInMs == 0);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
                Assert.AreEqual(1, Metrics.TotalAccessTokensFromIdP);
                Assert.AreEqual(1, Metrics.TotalAccessTokensFromCache);
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

                PublicClientApplication pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), false)
                                            .WithHttpManager(harness.HttpManager)
                                            .BuildConcrete();

                InMemoryTokenCache memoryTokenCache = new InMemoryTokenCache(withOperationDelay: true, shouldClearExistingCache: false);
                memoryTokenCache.Bind(pca.UserTokenCache);

                pca.ServiceBundle.ConfigureMockWebUI(
                    AuthorizationResult.FromUri(pca.AppConfig.RedirectUri + "?code=some-code"));

                var result = await pca
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationInCacheInMs > 0);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
                Assert.AreEqual(1, Metrics.TotalAccessTokensFromIdP);
                Assert.AreEqual(0, Metrics.TotalAccessTokensFromCache);
                Assert.IsTrue(Metrics.TotalDurationInMs > 0);
            }
        }

        [TestMethod]
        public async Task MetricsUpdatedSucessfully_AcquireTokenSilent_Async()
        {
            using (var harness = CreateTestHarness())
            {
                PublicClientApplication pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), false)
                                            .WithHttpManager(harness.HttpManager)
                                            .BuildConcrete();

                TokenCacheHelper.PopulateCache(pca.UserTokenCacheInternal.Accessor);
                InMemoryTokenCache memoryTokenCache = new InMemoryTokenCache(withOperationDelay: true, shouldClearExistingCache: false);
                memoryTokenCache.Bind(pca.UserTokenCache);

                var result = await pca.AcquireTokenSilent(
                    TestConstants.s_scope.ToArray(),
                    TestConstants.DisplayableId)
                    .WithAuthority(pca.Authority, false)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationInCacheInMs > 0);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationInHttpInMs == 0);
                Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
                Assert.AreEqual(0, Metrics.TotalAccessTokensFromIdP);
                Assert.AreEqual(1, Metrics.TotalAccessTokensFromCache);
                Assert.IsTrue(Metrics.TotalDurationInMs > 0);
            }
        }
    }
}
