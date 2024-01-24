// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class LongRunningOnBehalfOfTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public async Task LongRunningObo_RunsSuccessfully_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager);

                var cca = BuildCCA(httpManager);

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                string oboCacheKey = "obo-cache-key";
                var result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                // Token's not in cache, searched by user assertion hash, retrieved from IdP, saved with the provided OBO cache key
                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
                userCacheAccess.AssertAccessCounts(0, 1);

                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

                // Token is in the cache, retrieved by the provided OBO cache key
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                userCacheAccess.AssertAccessCounts(1, 1);

                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);
                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid2,
                        accessToken: TestConstants.ATSecret2,
                        refreshToken: TestConstants.RTSecret2));

                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

                // Cached AT is expired, RT used to retrieve new AT
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(TestConstants.ATSecret2, result.AccessToken);
                userCacheAccess.AssertAccessCounts(2, 2);
            }
        }

        [TestMethod]
        public async Task ProactiveRefresh_CancelsSuccessfully_Async()
        {
            bool wasErrorLogged = false;

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            AddMockHandlerAadSuccess(httpManager);

            var cca = BuildCCA(httpManager, LocalLogCallback);

            string oboCacheKey = "obo-cache-key";
            var result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                .ExecuteAsync().ConfigureAwait(false);

            TestCommon.UpdateATWithRefreshOn(cca.UserTokenCacheInternal.Accessor);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            cts.Cancel();
            cts.Dispose();

            result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync(cancellationToken).ConfigureAwait(false);

            Assert.IsTrue(TestCommon.YieldTillSatisfied(() => wasErrorLogged));

            void LocalLogCallback(LogLevel level, string message, bool containsPii)
            {
                if (level == LogLevel.Warning &&
                    message.Contains(SilentRequestHelper.ProactiveRefreshCancellationError))
                {
                    wasErrorLogged = true;
                }
            }
        }

        [TestMethod]
        public async Task InitiateLongRunningObo_WithExistingKeyAndToken_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager);

                var cca = BuildCCA(httpManager);

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                string oboCacheKey = null;
                var result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                // Token's not in cache, searched by user assertion hash, retrieved from IdP, saved with the provided OBO cache key
                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
                userCacheAccess.AssertAccessCounts(0, 1);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(accessToken: TestConstants.ATSecret2));

                //Initiate another process using the same cache key
                //MSAL should ignore the token in the cache, fetch a new token and overwrite the existing one
                result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret2, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
                Assert.AreEqual(TestConstants.ATSecret2, cachedAccessToken.Secret);
                userCacheAccess.AssertAccessCounts(0, 2);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(accessToken: TestConstants.ATSecret3));

                //Initiate another process using the same cache key but a different user assertion
                //MSAL should ignore the token in the cache, fetch a new token.
                result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, $"{TestConstants.DefaultAccessToken}2", ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret3, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
                Assert.AreEqual(TestConstants.ATSecret3, cachedAccessToken.Secret);
                userCacheAccess.AssertAccessCounts(0, 3);
            }
        }

        [TestMethod]
        public async Task InitiateLongRunningObo_CacheKeyAlreadyExists_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = BuildCCA(httpManager);

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.UniqueId,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        accessToken: TestConstants.ATSecret,
                        refreshToken: TestConstants.RTSecret));

                string oboCacheKey = "obo-cache-key";
                var result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                // Cache is empty or token with the same scopes, OBO cache key, etc. not in cache -> AT and RT are retrieved from IdP and saved
                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
                Assert.AreEqual(TestConstants.RTSecret, cachedRefreshToken.Secret);
                userCacheAccess.AssertAccessCounts(0, 1);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        $"{TestConstants.UniqueId}2",
                        $"{TestConstants.DisplayableId}2",
                        new string[] { "differentscope" },
                        utid: TestConstants.Utid2,
                        accessToken: TestConstants.ATSecret2,
                        refreshToken: TestConstants.RTSecret2));

                // Token with the same OBO cache key but different scopes, etc. exists in the cache -> save AT and RT in the cache since it's a different token
                // This mirrors the current behavior with AcquireTokenOnBehalfOf - two different tokens with the same user assertion can exist in the cache
                result = await cca.InitiateLongRunningProcessInWebApi(new string[] { "differentscope" }, $"{TestConstants.DefaultAccessToken}2", ref oboCacheKey)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret2, result.AccessToken);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().FirstOrDefault(at => at.Secret.Equals(TestConstants.ATSecret2));
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().FirstOrDefault(rt => rt.Secret.Equals(TestConstants.RTSecret2));
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
                Assert.AreEqual(TestConstants.RTSecret2, cachedRefreshToken.Secret);
                userCacheAccess.AssertAccessCounts(0, 2);
            }
        }

        // See https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4124
        [TestMethod]
        public async Task InitiateLongRunningObo_WithIgnoreCachedAssertion_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager);

                var cca = BuildCCA(httpManager);

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                string oboCacheKey = null;

                // Token's not in the cache, searched by user assertion hash, retrieved from IdP, saved with the provided OBO cache key
                var result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);
                userCacheAccess.AssertAccessCounts(0, 1);

                // Initiate with different user assertion and IgnoreCachedAssertion flag
                // MSAL will not match on the assertion and return cached token
                result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, $"{TestConstants.DefaultAccessToken}2", ref oboCacheKey)
                    .WithSearchInCacheForLongRunningProcess()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);
                userCacheAccess.AssertAccessCounts(1, 1);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(accessToken: TestConstants.ATSecret2, refreshToken: TestConstants.RTSecret2),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken } });
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                // Initiate with different user assertion and IgnoreCachedAssertion flag
                // MSAL will not match on the assertion, find expired cached token, and use cached refresh token
                result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, $"{TestConstants.DefaultAccessToken}2", ref oboCacheKey)
                    .WithSearchInCacheForLongRunningProcess()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret2, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
                Assert.AreEqual(TestConstants.ATSecret2, cachedAccessToken.Secret);
                Assert.AreEqual(TestConstants.RTSecret2, cachedRefreshToken.Secret);
                Assert.AreEqual(CacheRefreshReason.Expired, result.AuthenticationResultMetadata.CacheRefreshReason);
                userCacheAccess.AssertAccessCounts(2, 2);
            }
        }

        [TestMethod]
        public async Task InitiateLongRunningObo_WithIgnoreCachedAssertionAndWithForceRefresh_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager);

                var cca = BuildCCA(httpManager);

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                string oboCacheKey = null;

                // Token's not in the cache, searched by user assertion hash, retrieved from IdP, saved with the provided OBO cache key
                var result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);
                userCacheAccess.AssertAccessCounts(0, 1);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(accessToken: TestConstants.ATSecret2, refreshToken: TestConstants.RTSecret2),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken } });

                // Initiate with different user assertion; and IgnoreCachedAssertion and ForceRefresh flags
                // MSAL will not return a cached AT, but use RT to refresh
                result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, $"{TestConstants.DefaultAccessToken}2", ref oboCacheKey)
                    .WithSearchInCacheForLongRunningProcess()
                    .WithForceRefresh(true)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.ForceRefreshOrClaims, result.AuthenticationResultMetadata.CacheRefreshReason);
                userCacheAccess.AssertAccessCounts(1, 2);
            }
        }

        [TestMethod]
        public async Task AcquireTokenInLongRunningObo_CacheKeyDoesNotExist_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = BuildCCA(httpManager);

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                string oboCacheKey = "obo-cache-key";

                // If token with OBO cache key provided does not exist in the cache throw error
                var exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope.ToArray(), oboCacheKey)
                    .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.OboCacheKeyNotInCacheError, exception.ErrorCode);
                userCacheAccess.AssertAccessCounts(1, 0);
            }
        }

        [TestMethod]
        public async Task WithNullCacheKey_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager);

                var cca = BuildCCA(httpManager);

                string cacheKey = null;
                await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref cacheKey)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // If not provided, cache key is defaulted to the assertion hash.
                Assert.AreEqual(new UserAssertion(TestConstants.DefaultAccessToken).AssertionHash, cacheKey);

                // Cache key is required in this method
                await AssertException.TaskThrowsAsync<ArgumentNullException>(
                    () => cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, null)
                        .ExecuteAsync())
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Tests the behavior when calling both, long-running and normal OBO methods.
        /// Long-running OBO method return cached long-running tokens.
        /// Normal OBO method return cached normal tokens.
        /// Should be different partitions: by user-provided and by assertion hash 
        /// (if the user-provided key is not assertion hash)
        /// </summary>
        [TestMethod]
        public async Task NormalOboAndLongRunningObo_WithDifferentKeys_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = BuildCCA(httpManager);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-long-running",
                        refreshToken: "refresh-token-long-running"));

                string oboCacheKey = "obo-cache-key";
                string longRunningUserToken = "long-running-assertion";
                var result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, longRunningUserToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                // Cache has 1 partition (user-provided key) with 1 token
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-long-running", result.AccessToken);
                Assert.AreEqual("access-token-long-running", cachedAccessToken.Secret);
                Assert.AreEqual("refresh-token-long-running", cachedRefreshToken.Secret);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-normal",
                        refreshToken: "refresh-token-normal"));

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                // Cache has 2 partitions (user-provided key, assertion) with 1 token each
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(2, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(userAssertion.AssertionHash));
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().FirstOrDefault(t => t.OboCacheKey.Equals(userAssertion.AssertionHash));
                Assert.AreEqual("access-token-normal", result.AccessToken);
                Assert.AreEqual("access-token-normal", cachedAccessToken.Secret);
                Assert.IsNull(cachedRefreshToken);

                // Returns long-running token
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("access-token-long-running", result.AccessToken);

                // Returns normal token
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("access-token-normal", result.AccessToken);
            }
        }

        /// <summary>
        /// Tests the behavior when calling both, long-running and normal OBO methods.
        /// Both methods should return the same tokens, since the cache key is the same.
        /// Should be the same partition: by assertion hash.
        /// </summary>
        [TestMethod]
        public async Task NormalOboAndLongRunningObo_WithTheSameKey_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = BuildCCA(httpManager);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-1",
                        refreshToken: "refresh-token-1"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer } });

                string oboCacheKey = null;
                string userToken = "user-token";
                UserAssertion userAssertion = new UserAssertion(userToken);

                // InitiateLR - Empty cache - AT via OBO flow (new AT, RT cached)
                var result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, userToken, ref oboCacheKey)
                                        .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-1", result.AccessToken);
                Assert.AreEqual("access-token-1", cachedAccessToken.Secret);
                Assert.AreEqual("refresh-token-1", cachedRefreshToken.Secret);

                // AcquireLR - AT from cache
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("access-token-1", result.AccessToken);

                // AcquireNormal - AT from cache
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("access-token-1", result.AccessToken);

                // Expire AT
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-2",
                        refreshToken: "refresh-token-2"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer } });

                // InitiateLR - AT from IdP via RT flow(new AT, RT cached)
                result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, userToken, ref oboCacheKey)
                                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-2", result.AccessToken);
                Assert.AreEqual("access-token-2", cachedAccessToken.Secret);
                Assert.AreEqual("refresh-token-2", cachedRefreshToken.Secret);

                // Expire AT
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-3",
                        refreshToken: "refresh-token-3"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken } });

                // AcquireLR - AT from IdP via RT flow (new AT, RT cached)
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-3", result.AccessToken);
                Assert.AreEqual("access-token-3", cachedAccessToken.Secret);
                Assert.AreEqual("refresh-token-3", cachedRefreshToken.Secret);

                // Expire AT
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-4",
                        refreshToken: "refresh-token-4"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer } });

                // AcquireNormal - AT from IdP via OBO flow (only new AT cached, old RT still left in cache)
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-4", result.AccessToken);
                Assert.AreEqual("access-token-4", cachedAccessToken.Secret);
                Assert.AreEqual("refresh-token-3", cachedRefreshToken.Secret);
            }
        }

        /// <summary>
        /// Tests the behavior when calling both, long-running and normal OBO methods.
        /// Both methods should return the same tokens, since the cache key is the same.
        /// Should be the same partition: by assertion hash.
        /// </summary>
        [TestMethod]
        public async Task NormalOboThenLongRunningAcquire_WithTheSameKey_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = BuildCCA(httpManager);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-1",
                        refreshToken: "refresh-token-1"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer } });

                string userToken = "user-token";
                UserAssertion userAssertion = new UserAssertion(userToken);
                string oboCacheKey = userAssertion.AssertionHash;

                // AcquireNormal - AT from IdP via OBO flow (only new AT cached, no RT in cache)
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().FirstOrDefault(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-1", result.AccessToken);
                Assert.AreEqual("access-token-1", cachedAccessToken.Secret);

                // AcquireLR - AT from cache
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("access-token-1", result.AccessToken);

                // Expire AT
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                // AcquireLR - throws because no RT
                var exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope.ToArray(), oboCacheKey)
                    .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.OboCacheKeyNotInCacheError, exception.ErrorCode);

                // Expire AT
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-2",
                        refreshToken: "refresh-token-2"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer } });

                // InitiateLR - AT from IdP via OBO flow (new AT, RT cached)
                result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, userToken, ref oboCacheKey)
                        .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-2", result.AccessToken);
                Assert.AreEqual("access-token-2", cachedAccessToken.Secret);
                Assert.AreEqual("refresh-token-2", cachedRefreshToken.Secret);

                // Expire AT
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-3",
                        refreshToken: "refresh-token-3"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken } });

                // AcquireLR - AT from IdP via RT flow (new AT, RT cached)
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-3", result.AccessToken);
                Assert.AreEqual("access-token-3", cachedAccessToken.Secret);
                Assert.AreEqual("refresh-token-3", cachedRefreshToken.Secret);
            }
        }

        /// <summary>
        /// Tests the behavior when calling both, long-running and normal OBO methods.
        /// Both methods should return the same tokens, since the cache key is the same.
        /// Should be the same partition: by assertion hash.
        /// </summary>
        [TestMethod]
        public async Task NormalOboThenLongRunningInitiate_WithTheSameKey_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = BuildCCA(httpManager);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-1",
                        refreshToken: "refresh-token-1"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer } });

                string userToken = "user-token";
                UserAssertion userAssertion = new UserAssertion(userToken);
                string oboCacheKey = userAssertion.AssertionHash;

                // AcquireNormal - AT from IdP via OBO flow(only new AT cached, no RT in cache)
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().FirstOrDefault(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-1", result.AccessToken);
                Assert.AreEqual("access-token-1", cachedAccessToken.Secret);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-1",
                        refreshToken: "refresh-token-1"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer } });

                // InitiateLR - AT from cache
                result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, userToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("access-token-1", result.AccessToken);

                // Expire AT
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-2",
                        refreshToken: "refresh-token-2"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer } });

                // InitiateLR - AT via OBO flow (new AT, RT cached)
                result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, userToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-2", result.AccessToken);
                Assert.AreEqual("access-token-2", cachedAccessToken.Secret);
                Assert.AreEqual("refresh-token-2", cachedRefreshToken.Secret);

                // Expire AT
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-3",
                        refreshToken: "refresh-token-3"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken } });

                // AcquireLR - AT from IdP via RT flow(new AT, RT cached)
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().First(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-3", result.AccessToken);
                Assert.AreEqual("access-token-3", cachedAccessToken.Secret);
                Assert.AreEqual("refresh-token-3", cachedRefreshToken.Secret);
            }
        }

        /// <summary>
        /// This test performs an initiation of a long running OBO process, then validated that the remove long running process api deletes the tokens
        /// in the cache associated with the provided OBO cache key.
        /// </summary>
        [TestMethod]
        public async Task LongRunningThenRemoveTokens_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                string oboCacheKey = "someCacheKey";
                bool validateCacheProperties = false;

                var cca = BuildCCA(httpManager);
                var oboCca = cca as ILongRunningWebApi;

                cca.UserTokenCache.SetBeforeAccess((args) =>
                {
                    if (validateCacheProperties)
                    {
                        Assert.AreEqual(true, args.HasTokens);
                        Assert.AreEqual(false, args.HasStateChanged);
                        Assert.AreEqual(oboCacheKey, args.SuggestedCacheKey);
                    }
                });

                cca.UserTokenCache.SetAfterAccess((args) =>
                {
                    if (validateCacheProperties)
                    {
                        Assert.AreEqual(true, args.HasStateChanged);
                        Assert.AreEqual(false, args.HasTokens);
                        Assert.AreEqual(oboCacheKey, args.SuggestedCacheKey);
                    }

                    validateCacheProperties = false;
                });

                AddMockHandlerAadSuccess(httpManager,
                    responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(
                        TestConstants.Uid,
                        TestConstants.DisplayableId,
                        TestConstants.s_scope.ToArray(),
                        utid: TestConstants.Utid,
                        accessToken: "access-token-1",
                        refreshToken: "refresh-token-1"),
                    expectedPostData: new Dictionary<string, string> { { OAuth2Parameter.GrantType, OAuth2GrantType.JwtBearer } });

                string userToken = "user-token";
                UserAssertion userAssertion = new UserAssertion(userToken);

                // InitiateLR - Empty cache - AT via OBO flow (new AT, RT cached)
                var result = await oboCca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, userToken, ref oboCacheKey)
                                        .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single(t => t.OboCacheKey.Equals(oboCacheKey));
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single(t => t.OboCacheKey.Equals(oboCacheKey));
                Assert.AreEqual("access-token-1", result.AccessToken);
                Assert.AreEqual("access-token-1", cachedAccessToken.Secret);
                Assert.AreEqual("refresh-token-1", cachedRefreshToken.Secret);

                //remove tokens
                validateCacheProperties = true;
                var tokensRemoved = await oboCca.StopLongRunningProcessInWebApiAsync(oboCacheKey).ConfigureAwait(false);

                var cachedAccessTokens = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens();
                var cachedRefreshTokens = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens();

                Assert.AreEqual(0, cachedAccessTokens.Count);
                Assert.AreEqual(0, cachedRefreshTokens.Count);
                Assert.IsTrue(tokensRemoved);

                //validate that no more tokens are removed
                tokensRemoved = await oboCca.StopLongRunningProcessInWebApiAsync(oboCacheKey).ConfigureAwait(false);

                cachedAccessTokens = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens();
                cachedRefreshTokens = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens();

                Assert.AreEqual(0, cachedAccessTokens.Count);
                Assert.AreEqual(0, cachedRefreshTokens.Count);
                Assert.IsFalse(tokensRemoved);
            }
        }

        [TestMethod]
        public async Task SuggestedCacheExpiry_ShouldNotExist_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                AddMockHandlerAadSuccess(httpManager);

                var app = BuildCCA(httpManager);

                InMemoryTokenCache cache = new InMemoryTokenCache();
                cache.Bind(app.UserTokenCache);

                (app.UserTokenCache as TokenCache).AfterAccess += (args) =>
                {
                    if (args.HasStateChanged)
                    {
                        Assert.IsFalse(args.SuggestedCacheExpiry.HasValue);
                    }
                };

                string oboCacheKey = "obo-cache-key";
                await app.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);
                await app.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);
            }
        }

        private MockHttpMessageHandler AddMockHandlerAadSuccess(
            MockHttpManager httpManager,
            string authority = TestConstants.AuthorityCommonTenant,
            IList<string> unexpectedHeaders = null,
            HttpResponseMessage responseMessage = null,
            IDictionary<string, string> expectedPostData = null)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = responseMessage ?? MockHelpers.CreateSuccessTokenResponseMessage(),
                UnexpectedRequestHeaders = unexpectedHeaders,
            };
            if (expectedPostData != null)
            {
                handler.ExpectedPostData = expectedPostData;
            }
            httpManager.AddMockHandler(handler);

            return handler;
        }

        private ConfidentialClientApplication BuildCCA(IHttpManager httpManager, LogCallback logCallback = null)
        {
            var builder = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithClientSecret(TestConstants.ClientSecret)
                            .WithAuthority(TestConstants.AuthorityCommonTenant)
                            .WithHttpManager(httpManager);
            if (logCallback != null)
            {
                builder.WithLogging(logCallback);
            }
            return builder.BuildConcrete();
        }
    }
}
