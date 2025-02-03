// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class CacheKeyExtensionTests : TestBase
    {
        private byte[] _serializedCache;
        private Dictionary<string, string> _additionalCacheKeys1 = new Dictionary<string, string>
                                                                            {
                                                                                { "key1", "value1" },
                                                                                { "key2", "value2" }
                                                                            };
        private Dictionary<string, string> _additionalCacheKeys2 = new Dictionary<string, string>
                                                                            {
                                                                                { "key3", "value3" },
                                                                                { "key4", "value4" }
                                                                            };
        private Dictionary<string, string> _additionalCacheKeys3 = new Dictionary<string, string>
                                                                            {
                                                                                { "key2", "value2" },
                                                                                { "key1", "value1" }
                                                                            };

        [TestMethod]
        public async Task CacheExtWithInMemoryTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures()
                                                              .BuildConcrete();

                await RunHappyPathTest(app, httpManager).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CacheExtWithSerializedCacheTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures()
                                                              .BuildConcrete();

                app.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
                app.AppTokenCache.SetAfterAccess(AfterCacheAccess);
                await RunHappyPathTest(app, httpManager).ConfigureAwait(false);
            }
        }

        private async Task RunHappyPathTest(ConfidentialClientApplication app, MockHttpManager httpManager)
        {
            var appCacheAccess = app.AppTokenCache.RecordAccess();

            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

            var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                    .WithAdditionalCacheKeyComponents(_additionalCacheKeys1)
                                    .ExecuteAsync(CancellationToken.None)
                                    .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("header.payload.signature", result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            //Ensure that the order of the keys does not matter
            result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                .WithAdditionalCacheKeyComponents(_additionalCacheKeys3)
                                .ExecuteAsync(CancellationToken.None)
                                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("header.payload.signature", result.AccessToken);
            Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            //Ensure that tokens are not ovverriden
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

            result = await app.AcquireTokenForClient(TestConstants.s_scopeForAnotherResource)
                                .WithAdditionalCacheKeyComponents(_additionalCacheKeys2)
                                .ExecuteAsync(CancellationToken.None)
                                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("header.payload.signature", result.AccessToken);
            Assert.AreEqual(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        public async Task CacheExtNullOrEmptyTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures()
                                                              .BuildConcrete();

                var exception = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                            .WithAdditionalCacheKeyComponents(null)
                                            .ExecuteAsync(CancellationToken.None)
                                            .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.AreEqual(MsalErrorMessage.CryptographicError, exception.Message);

                exception = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                            .WithAdditionalCacheKeyComponents(new Dictionary<string, string> { })
                                            .ExecuteAsync(CancellationToken.None)
                                            .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.AreEqual(MsalErrorMessage.CryptographicError, exception.Message);
            }
        }

        [TestMethod]
        public async Task CacheExtEnsureCacheMisIsLoggedTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                StringBuilder logMessages = new StringBuilder();
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                              .WithRedirectUri(TestConstants.RedirectUri)
                                              .WithClientSecret(TestConstants.ClientSecret)
                                              .WithHttpManager(httpManager)
                                              .WithLogging((LogLevel level, string message, bool containsPii) =>
                                              {
                                                  logMessages.AppendLine(message);
                                              })
                                              .WithExperimentalFeatures()
                                              .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .WithAdditionalCacheKeyComponents(_additionalCacheKeys1)
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);

                //Ensure that the cache miss event is logged
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                    .WithAdditionalCacheKeyComponents(_additionalCacheKeys2)
                                    .ExecuteAsync(CancellationToken.None)
                                    .ConfigureAwait(false);

                Assert.IsTrue(logMessages.ToString().Contains("No tokens found that match the provided key components."));
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            }
        }

        private void BeforeCacheAccess(TokenCacheNotificationArgs args)
        {
            args.TokenCache.DeserializeMsalV3(_serializedCache);
        }

        private void AfterCacheAccess(TokenCacheNotificationArgs args)
        {
            _serializedCache = args.TokenCache.SerializeMsalV3();
        }
    }
}
