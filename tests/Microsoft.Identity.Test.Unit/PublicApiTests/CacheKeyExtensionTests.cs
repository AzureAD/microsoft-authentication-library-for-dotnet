// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        private Dictionary<string, Func<CancellationToken, Task<string>>> _additionalCacheKeysAsync1 = new Dictionary<string, Func<CancellationToken, Task<string>>>
                                                                            {
                                                                                { "key1", (CancellationToken ct) => { return Task.FromResult("value1"); } },
                                                                                { "key2", (CancellationToken ct) => { return Task.FromResult("value2"); } }
                                                                            };
        private Dictionary<string, Func<CancellationToken, Task<string>>> _additionalCacheKeysAsync2 = new Dictionary<string, Func<CancellationToken, Task<string>>>
                                                                            {
                                                                                { "key3", (CancellationToken ct) => { return Task.FromResult("value3"); } },
                                                                                { "key4", (CancellationToken ct) => { return Task.FromResult("value4"); } }
                                                                            };
        private Dictionary<string, Func<CancellationToken, Task<string>>> _additionalCacheKeysAsync3 = new Dictionary<string, Func<CancellationToken, Task<string>>>
                                                                            {
                                                                                { "key2", (CancellationToken ct) => { return Task.FromResult("value2"); } },
                                                                                { "key1", (CancellationToken ct) => { return Task.FromResult("value1"); } }
                                                                            };

        private Dictionary<string, Func<CancellationToken, Task<string>>> _additionalCacheKeysCombinedAsync = new Dictionary<string, Func<CancellationToken, Task<string>>>
                                                                            {
                                                                                { "key1", (CancellationToken ct) => { return Task.FromResult("value1"); } },
                                                                                { "key2", (CancellationToken ct) => { return Task.FromResult("value2"); } },
                                                                                { "key3", (CancellationToken ct) => { return Task.FromResult("value3"); } },
                                                                                { "key4", (CancellationToken ct) => { return Task.FromResult("value4"); } }
                                                                            };

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
            string expectedCacheKey1 = "-login.windows.net-atext-d3adb33f-c0de-ed0c-c0de-deadb33fc0d3-common-r1/scope1 r1/scope2-latlwkpewb_a0rcsmjvkecqt0_huumkw4sflzociike";
            string expectedCacheKey2 = "-login.windows.net-atext-d3adb33f-c0de-ed0c-c0de-deadb33fc0d3-common-r1/scope1 r1/scope2-jjoe9jgfmdtnj0rzuetsqy7kzs2m1xfnjjxwsfxsrxq";

            string expectedCacheKeyHash = string.Empty;
            var appCacheAccess = app.AppTokenCache.RecordAccess((args) =>
            {
                if (expectedCacheKeyHash != null)
                {
                    Assert.Contains(expectedCacheKeyHash, args.SuggestedCacheKey);
                }
            });

            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

            expectedCacheKeyHash = CoreHelpers.ComputeAccessTokenExtCacheKey(new(_additionalCacheKeys1));
            var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                    .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
                                    .ExecuteAsync(CancellationToken.None)
                                    .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("header.payload.signature", result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First(), _additionalCacheKeys1, expectedCacheKey1);

            //Ensure that the order of the keys does not matter
            expectedCacheKeyHash = CoreHelpers.ComputeAccessTokenExtCacheKey(new(_additionalCacheKeys3));
            result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync3)
                                .ExecuteAsync(CancellationToken.None)
                                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("header.payload.signature", result.AccessToken);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First(), _additionalCacheKeys3, expectedCacheKey1);

            //Ensure that tokens are not overridden
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
            expectedCacheKeyHash = null;
            result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync2)
                                .ExecuteAsync(CancellationToken.None)
                                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("header.payload.signature", result.AccessToken);
            Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Where(x => x.CacheKey.Contains("latlwkpewb_a0rcsmjvkecqt0_huumkw4sflzociike")).FirstOrDefault(), _additionalCacheKeys1, expectedCacheKey1);
            ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Where(x => x.CacheKey.Contains("jjoe9jgfmdtnj0rzuetsqy7kzs2m1xfnjjxwsfxsrxq")).FirstOrDefault(), _additionalCacheKeys2, expectedCacheKey2);
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
                                    .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
                                    .ExecuteAsync(CancellationToken.None)
                                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());

                //Ensure that default tokens are retrivable
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                //Ensure that extended tokens are retrivable
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
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
                                        .WithAdditionalCacheKeyComponents(new SortedList<string, Func<CancellationToken, Task<string>>>())
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
                string expectedPopCacheKey = "-login.windows.net-atext-d3adb33f-c0de-ed0c-c0de-deadb33fc0d3-my-utid-r1/scope1 r1/scope2-pop-latlwkpewb_a0rcsmjvkecqt0_huumkw4sflzociike";
                string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";

                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithExperimentalFeatures()
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
                    .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
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
                    .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
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
            string expectedPopCacheKey = "-login.windows.net-atext-d3adb33f-c0de-ed0c-c0de-deadb33fc0d3-common-r1/scope1 r1/scope2-hjvvw1vwz3vtsfowyllfgwoevbbhkazpbm1rgwklj0u";
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
                                        .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
                                        .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync2)
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);
                ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First(), _additionalCacheKeysCombined, expectedPopCacheKey);

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
                                        .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync2)
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);
                ValidateCacheKeyComponents(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First(), _additionalCacheKeysCombined, expectedPopCacheKey);
            }
        }

        [TestMethod]
        public async Task CacheExt_WithExtraQueryParameters_NoConflictTestAsync()
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

                // Test 1: Use WithAdditionalCacheKeyComponents only
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_with_additional_components");
                var result1 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                        .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.AreEqual("token_with_additional_components", result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());

                // Test 2: Use tuple-based WithExtraQueryParameters only
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_with_tuple_params");
                var result2 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                     .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                     {
                                 { "param1", ("value1", true) },
                                 { "param2", ("value2", true) }
                                     })
                                     .ExecuteAsync()
                                     .ConfigureAwait(false);

                Assert.AreEqual("token_with_tuple_params", result2.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
                Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());

                // Test 3: Use both APIs together - should create a different cache entry
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_with_both_apis");
                var result3 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                     .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
                                     .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                     {
                                 { "param1", ("value1", true) },
                                 { "param2", ("value2", true) }
                                     })
                                     .ExecuteAsync()
                                     .ConfigureAwait(false);

                Assert.AreEqual("token_with_both_apis", result3.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result3.AuthenticationResultMetadata.TokenSource);
                Assert.HasCount(3, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());

                // Test 4: Retrieve from cache using the same combination
                var result4 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                     .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
                                     .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                     {
                                 { "param1", ("value1", true) },
                                 { "param2", ("value2", true) }
                                     })
                                     .ExecuteAsync()
                                     .ConfigureAwait(false);

                Assert.AreEqual("token_with_both_apis", result4.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result4.AuthenticationResultMetadata.TokenSource);
                Assert.HasCount(3, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());

                // Test 5: Test with non-cached parameters
                var result5 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                     .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
                                     .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                     {
                                 { "param1", ("value1", true) },
                                 { "param2", ("value2", true) },
                                 { "non_cached_param", ("some_value", false) } // This should not affect cache key
                                     })
                                     .ExecuteAsync()
                                     .ConfigureAwait(false);

                Assert.AreEqual("token_with_both_apis", result5.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result5.AuthenticationResultMetadata.TokenSource);
                Assert.HasCount(3, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());

                // Test 6: Change a parameter that is included in cache key - should get a new token
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_with_changed_param");
                var result6 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                     .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync1)
                                     .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                     {
                                 { "param1", ("different_value", true) }, // Changed value with includeInCacheKey=true
                                 { "param2", ("value2", true) }
                                     })
                                     .ExecuteAsync()
                                     .ConfigureAwait(false);

                Assert.AreEqual("token_with_changed_param", result6.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result6.AuthenticationResultMetadata.TokenSource);
                Assert.HasCount(4, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());

                // Test 7: Now try with includeInCacheKey=false for a parameter
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_with_non_cached_param");
                var result7 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                     .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync2) // Different additional components
                                     .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                     {
                                 { "param3", ("value3", false) } // Not included in cache key
                                     })
                                     .ExecuteAsync()
                                     .ConfigureAwait(false);

                Assert.AreEqual("token_with_non_cached_param", result7.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result7.AuthenticationResultMetadata.TokenSource);
                Assert.HasCount(5, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());

                // Test 8: Repeat with same config but change the non-cached parameter value
                var result8 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                     .WithAdditionalCacheKeyComponents(_additionalCacheKeysAsync2)
                                     .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                     {
                                 { "param3", ("different_value3", false) } // Changed value but not in cache key
                                     })
                                     .ExecuteAsync()
                                     .ConfigureAwait(false);

                Assert.AreEqual("token_with_non_cached_param", result8.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result8.AuthenticationResultMetadata.TokenSource);
                Assert.HasCount(5, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
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

        #region ComputeAccessTokenExtCacheKey collision-resistance tests

        // These tests pin the length-prefix (netstring) encoding used to hash the additional
        // cache-key components. The encoding is <byteLen(key)>:<key><byteLen(value)>:<value>
        // per (sorted) entry, using UTF-8 byte length. It is byte-identical to the parallel
        // MSAL Go/Java/Python/JS fixes, so the golden vectors below double as a cross-SDK guard.

        [TestMethod]
        public void ComputeAccessTokenExtCacheKey_BoundaryAmbiguity_ProducesDifferentKeys()
        {
            // Arrange
            // The pre-fix delimiter-less concatenation mapped both of these to "fmi_pathvalue".
            var a1 = new SortedList<string, string> { { "fmi_path", "value" } };
            var a2 = new SortedList<string, string> { { "fmi_pat", "hvalue" } };

            // Multi-entry boundary: pre-fix both concatenated to "abcde".
            var b1 = new SortedList<string, string> { { "a", "b" }, { "cd", "e" } };
            var b2 = new SortedList<string, string> { { "ab", "c" }, { "d", "e" } };

            // A value that itself contains the colon/digit characters used by the encoding.
            var c1 = new SortedList<string, string> { { "k", "3:xy" } };
            var c2 = new SortedList<string, string> { { "k3", ":xy" } };

            // Act
            string ka1 = CoreHelpers.ComputeAccessTokenExtCacheKey(a1);
            string ka2 = CoreHelpers.ComputeAccessTokenExtCacheKey(a2);
            string kb1 = CoreHelpers.ComputeAccessTokenExtCacheKey(b1);
            string kb2 = CoreHelpers.ComputeAccessTokenExtCacheKey(b2);
            string kc1 = CoreHelpers.ComputeAccessTokenExtCacheKey(c1);
            string kc2 = CoreHelpers.ComputeAccessTokenExtCacheKey(c2);

            // Assert
            Assert.AreNotEqual(ka1, ka2, "key/value boundary ambiguity must not collide");
            Assert.AreNotEqual(kb1, kb2, "multi-entry boundary ambiguity must not collide");
            Assert.AreNotEqual(kc1, kc2, "colon/digit values must not collide");
        }

        [TestMethod]
        public void ComputeAccessTokenExtCacheKey_InjectivityFuzz_NoCollisions()
        {
            // Arrange
            // Adversarial alphabet: digits, the encoding delimiter ':', pipe, backslash, the
            // empty string, and multi-byte code points (accented, combining, emoji).
            string[] atoms =
            {
                "", "1", "12", ":", "1:", ":1", "|", "\\", "e", "\u00e9", "e\u0301", "\U0001F642"
            };

            var inputs = new List<SortedList<string, string>>();

            // Single-entry component sets.
            foreach (string key in atoms)
            {
                foreach (string value in atoms)
                {
                    inputs.Add(new SortedList<string, string> { { key, value } });
                }
            }

            // Two-entry component sets (keys must differ for a valid SortedList). Use an
            // ordinal comparer so byte-distinct keys (e.g. "é" vs "e" + combining accent) are
            // treated as distinct rather than collapsed by the default culture-aware comparer.
            foreach (string k1 in atoms)
            {
                foreach (string k2 in atoms)
                {
                    if (string.Equals(k1, k2, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    inputs.Add(new SortedList<string, string>(StringComparer.Ordinal)
                    {
                        { k1, "v" }, { k2, "v" }
                    });
                }
            }

            // De-duplicate inputs using a separator that cannot appear in the adversarial
            // alphabet, so equal canonical forms mean genuinely equal inputs.
            const char sep = '\u0001';
            var distinctInputs = new HashSet<string>();
            var hashToInput = new Dictionary<string, string>();

            // Act + Assert
            foreach (var input in inputs)
            {
                string canonical = string.Join(sep.ToString(),
                    input.Select(kvp => kvp.Key + sep + kvp.Value));

                if (!distinctInputs.Add(canonical))
                {
                    continue; // already covered
                }

                string hash = CoreHelpers.ComputeAccessTokenExtCacheKey(input);

                if (hashToInput.TryGetValue(hash, out string previous))
                {
                    Assert.Fail(
                        $"Cache-key collision between distinct inputs: [{previous}] and [{canonical}] both hashed to {hash}");
                }

                hashToInput[hash] = canonical;
            }

            // Sanity: every distinct input produced a distinct hash.
            Assert.HasCount(distinctInputs.Count, hashToInput);
        }

        [TestMethod]
        public void ComputeAccessTokenExtCacheKey_IsInputOrderIndependent()
        {
            // Arrange
            // Same components, different insertion order into the underlying Dictionary.
            var forward = new Dictionary<string, string>
            {
                { "alpha", "1" }, { "beta", "2" }, { "gamma", "3" }
            };
            var reverse = new Dictionary<string, string>
            {
                { "gamma", "3" }, { "beta", "2" }, { "alpha", "1" }
            };

            // Act
            string keyForward = CoreHelpers.ComputeAccessTokenExtCacheKey(new SortedList<string, string>(forward));
            string keyReverse = CoreHelpers.ComputeAccessTokenExtCacheKey(new SortedList<string, string>(reverse));

            // Assert
            Assert.AreEqual(keyForward, keyReverse, "cache key must not depend on input insertion order");
        }

        [TestMethod]
        public void ComputeAccessTokenExtCacheKey_UsesUtf8ByteLength_NotStringLength()
        {
            // Arrange
            // 'é' is U+00E9: 1 UTF-16 code unit but 2 UTF-8 bytes.
            var components = new SortedList<string, string> { { "\u00e9", "\u00e9" } };

            // Correct (byte-length) serialization: "2:é2:é".
            string byteLengthSerialized = BuildByteLengthNetstring(components);
            string expectedByteLength = Sha256Base64Url(byteLengthSerialized);

            // Wrong (UTF-16 string.Length) serialization would be "1:é1:é".
            string stringLengthSerialized = BuildStringLengthNetstring(components);
            string wrongStringLength = Sha256Base64Url(stringLengthSerialized);

            // Act
            string actual = CoreHelpers.ComputeAccessTokenExtCacheKey(components);

            // Assert
            Assert.AreEqual(expectedByteLength, actual, "must use UTF-8 GetByteCount for the length prefix");
            Assert.AreNotEqual(wrongStringLength, actual, "must NOT use string.Length (UTF-16 units)");
        }

        [TestMethod]
        public void ComputeAccessTokenExtCacheKey_EmptyAndSingleEntryEdges()
        {
            // Arrange + Act + Assert
            Assert.AreEqual(string.Empty, CoreHelpers.ComputeAccessTokenExtCacheKey(null));
            Assert.AreEqual(string.Empty, CoreHelpers.ComputeAccessTokenExtCacheKey(new SortedList<string, string>()));

            string single = CoreHelpers.ComputeAccessTokenExtCacheKey(
                new SortedList<string, string> { { "k", "v" } });
            Assert.IsFalse(string.IsNullOrEmpty(single));

            // An empty value is distinct from moving those characters into the key.
            string emptyValue = CoreHelpers.ComputeAccessTokenExtCacheKey(
                new SortedList<string, string> { { "k", string.Empty } });
            string emptyKey = CoreHelpers.ComputeAccessTokenExtCacheKey(
                new SortedList<string, string> { { string.Empty, "k" } });
            Assert.IsFalse(string.IsNullOrEmpty(emptyValue));
            Assert.AreNotEqual(emptyValue, emptyKey);
        }

        [TestMethod]
        public void ComputeAccessTokenExtCacheKey_GoldenVectors_MatchCrossSdk()
        {
            // The MSAL SDK family (Go/Java/Python/JS) emits byte-identical hashes, lowercased
            // and padding-free. .NET's Base64Url encoder preserves case, so compare
            // case-insensitively after stripping any padding.
            AssertGoldenVector(
                new SortedList<string, string> { { "fmi_path", "agent-app-id" } },
                "a0ry_zl4gccsdp7gnw927x8s0mrmnodv6tyilt0u07m");
            AssertGoldenVector(
                new SortedList<string, string> { { "a", "b" }, { "cd", "e" } },
                "cybgactkrvlzlen1aiwzwl3ay5krkyixommrobc-ri4");
            AssertGoldenVector(
                new SortedList<string, string> { { "fmi_path", "value" } },
                "n_lucewkadzv_nybtg-2wtorgf2nrns6ihlfa7vbuzg");
            AssertGoldenVector(
                new SortedList<string, string> { { "fmi_pat", "hvalue" } },
                "tjtm16m-suk2_bkniblr25lyuki40qyceco7knuyu0k");
            AssertGoldenVector(
                new SortedList<string, string> { { "\u00e9", "\u00e9" } },
                "xskzaoz4ibr3mznftyxctvg1ptuh-0fuzpty7ndbfls");
        }

        private static void AssertGoldenVector(SortedList<string, string> components, string expectedLowerNoPad)
        {
            string actual = CoreHelpers.ComputeAccessTokenExtCacheKey(components);

            // Confirm the output is URL-safe and padding-free.
            Assert.DoesNotContain("=", actual, "Base64Url output must be padding-free");
            Assert.DoesNotContain("+", actual, "Base64Url output must be URL-safe");
            Assert.DoesNotContain("/", actual, "Base64Url output must be URL-safe");

            Assert.IsTrue(
                string.Equals(actual.TrimEnd('='), expectedLowerNoPad, StringComparison.OrdinalIgnoreCase),
                $"cross-SDK golden vector mismatch. expected (case-insensitive): {expectedLowerNoPad}, actual: {actual}");
        }

        private static string BuildByteLengthNetstring(SortedList<string, string> components)
        {
            var sb = new StringBuilder();
            foreach (var component in components)
            {
                sb.Append(Encoding.UTF8.GetByteCount(component.Key)).Append(':').Append(component.Key);
                sb.Append(Encoding.UTF8.GetByteCount(component.Value)).Append(':').Append(component.Value);
            }
            return sb.ToString();
        }

        private static string BuildStringLengthNetstring(SortedList<string, string> components)
        {
            var sb = new StringBuilder();
            foreach (var component in components)
            {
                sb.Append(component.Key.Length).Append(':').Append(component.Key);
                sb.Append(component.Value.Length).Append(':').Append(component.Value);
            }
            return sb.ToString();
        }

        private static string Sha256Base64Url(string serialized)
        {
            using (SHA256 hash = SHA256.Create())
            {
                return Base64UrlHelpers.Encode(hash.ComputeHash(Encoding.UTF8.GetBytes(serialized)));
            }
        }

        #endregion
    }
}
