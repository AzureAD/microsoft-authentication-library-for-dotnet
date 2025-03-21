// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache;
using System.Net.Http;
using Microsoft.Identity.Client.AppConfig;

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

        private Dictionary<string, string> _additionalCacheKeysCombined = new Dictionary<string, string>
                                                                            {
                                                                                { "key1", "value1" },
                                                                                { "key2", "value2" },
                                                                                { "key3", "value3" },
                                                                                { "key4", "value4" }
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
                                                              .BuildConcrete();

                app.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
                app.AppTokenCache.SetAfterAccess(AfterCacheAccess);
                await RunHappyPathTest(app, httpManager).ConfigureAwait(false);
            }
        }

        private async Task RunHappyPathTest(ConfidentialClientApplication app, MockHttpManager httpManager)
        {
            string expectedCacheKey1 = "-login.windows.net-atext-d3adb33f-c0de-ed0c-c0de-deadb33fc0d3-common-r1/scope1 r1/scope2-bns2ytmx5hxkh4fnfixridmezpbbayhnmuh6t4bbghi";
            string expectedCacheKey2 = "-login.windows.net-atext-d3adb33f-c0de-ed0c-c0de-deadb33fc0d3-common-r1/scope1 r1/scope2-3-rg6_wyjx5bcy0c3cqq7gajtzgsqy3oxqpwj4y8k4u";

            string expectedCacheKeyHash = string.Empty;
            var appCacheAccess = app.AppTokenCache.RecordAccess((args) =>
            {
                if (expectedCacheKeyHash != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedCacheKeyHash));
                }
            });

            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

            expectedCacheKeyHash = CoreHelpers.ComputeAccessTokenExtCacheKey(new(_additionalCacheKeys1));
            var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                    .WithAdditionalCacheKeyComponents(_additionalCacheKeys1)
                                    .ExecuteAsync(CancellationToken.None)
                                    .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("header.payload.signature", result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First(), _additionalCacheKeys1, expectedCacheKey1);

            //Ensure that the order of the keys does not matter
            expectedCacheKeyHash = CoreHelpers.ComputeAccessTokenExtCacheKey(new(_additionalCacheKeys3));
            result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                .WithAdditionalCacheKeyComponents(_additionalCacheKeys3)
                                .ExecuteAsync(CancellationToken.None)
                                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("header.payload.signature", result.AccessToken);
            Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First(), _additionalCacheKeys3, expectedCacheKey1);

            //Ensure that tokens are not overridden
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
            expectedCacheKeyHash = null;
            result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                .WithAdditionalCacheKeyComponents(_additionalCacheKeys2)
                                .ExecuteAsync(CancellationToken.None)
                                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("header.payload.signature", result.AccessToken);
            Assert.AreEqual(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Where(x => x.CacheKey.Contains("bns2ytmx5hxkh4fnfixridmezpbbayhnmuh6t4bbghi")).FirstOrDefault(), _additionalCacheKeys1, expectedCacheKey1);
            ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Where(x => x.CacheKey.Contains("3-rg6_wyjx5bcy0c3cqq7gajtzgsqy3oxqpwj4y8k4u")).FirstOrDefault(), _additionalCacheKeys2, expectedCacheKey2);
        }

        private void ValidateCacheKeyComponents(MsalAccessTokenCacheItem msalAccessTokenCacheItem,
            Dictionary<string, string> expectedAdditionalCacheKeyComponents,
            string expectedCacheKey)
        {
            if (CollectionHelpers.AreDictionariesEqual(
                msalAccessTokenCacheItem.AdditionalCacheKeyComponents,
                expectedAdditionalCacheKeyComponents)
                && msalAccessTokenCacheItem.CacheKey.Equals(expectedCacheKey)
                && msalAccessTokenCacheItem.CredentialType == StorageJsonValues.CredentialTypeAccessTokenExtended)
            {
                return;
            }

            Assert.Fail("Cache key components not found in the cached tokens");
        }

        [TestMethod]
        public async Task CacheExtEnsureStandardTokensDoNotClashTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                StringBuilder logMessages = new StringBuilder();
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                              .WithRedirectUri(TestConstants.RedirectUri)
                                              .WithClientSecret(TestConstants.ClientSecret)
                                              .WithHttpManager(httpManager)
                                              .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);

                //Ensure that the cache miss event is logged
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                    .WithAdditionalCacheKeyComponents(_additionalCacheKeys1)
                                    .ExecuteAsync(CancellationToken.None)
                                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);

                //Ensure that default tokens are retrivable
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                //Ensure that extended tokens are retrivable
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAdditionalCacheKeyComponents(_additionalCacheKeys1)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task CacheExtEnsureNoComponentsAreAddedWithEmptyArrayTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                              .WithRedirectUri(TestConstants.RedirectUri)
                                              .WithClientSecret(TestConstants.ClientSecret)
                                              .WithHttpManager(httpManager)
                                              .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .WithAdditionalCacheKeyComponents(new SortedList<string, string>())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);
                Assert.IsNull(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First().AdditionalCacheKeyComponents);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .WithForceRefresh(true)
                                        .WithAdditionalCacheKeyComponents(null)
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);
                Assert.IsNull(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First().AdditionalCacheKeyComponents);
            }
        }

        [TestMethod]
        public async Task CacheExtEnsurePopKeysFunctionAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                string expectedPopCacheKey = "-login.windows.net-atext-d3adb33f-c0de-ed0c-c0de-deadb33fc0d3-my-utid-r1/scope1 r1/scope2-pop-bns2ytmx5hxkh4fnfixridmezpbbayhnmuh6t4bbghi";
                string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";

                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));
                var popConfig = new PoPAuthenticationConfiguration(request);
                var cacheAccess = app.AppTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                //Ensure that pop tokens are cached with the correct key and the key components are correct
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithSignedHttpRequestProofOfPossession(popConfig)
                    .WithAdditionalCacheKeyComponents(_additionalCacheKeys1)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First(), _additionalCacheKeys1, expectedPopCacheKey);

                //Ensure pop token can be retrieved from cache with the cache key components
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithSignedHttpRequestProofOfPossession(popConfig)
                    .WithAdditionalCacheKeyComponents(_additionalCacheKeys1)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First(), _additionalCacheKeys1, expectedPopCacheKey);
            }
        }

        [TestMethod]
        public async Task CacheExtEnsureInputKeysAddedCorrectlyTestAsync()
        {
            string expectedPopCacheKey = "-login.windows.net-atext-d3adb33f-c0de-ed0c-c0de-deadb33fc0d3-common-r1/scope1 r1/scope2-ap4mvs3cq7ewsb5cl17miymk5r1nqogqh11uzwqjvw4";
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                              .WithRedirectUri(TestConstants.RedirectUri)
                                              .WithClientSecret(TestConstants.ClientSecret)
                                              .WithHttpManager(httpManager)
                                              .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                //Ensure cache key components are added correctly
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .WithForceRefresh(true)
                                        .WithAdditionalCacheKeyComponents(_additionalCacheKeys1)
                                        .WithAdditionalCacheKeyComponents(_additionalCacheKeys2)
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);
                ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First(), _additionalCacheKeysCombined, expectedPopCacheKey);

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .WithAdditionalCacheKeyComponents(_additionalCacheKeys1)
                                        .WithAdditionalCacheKeyComponents(_additionalCacheKeys2)
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);
                ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First(), _additionalCacheKeysCombined, expectedPopCacheKey);
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
