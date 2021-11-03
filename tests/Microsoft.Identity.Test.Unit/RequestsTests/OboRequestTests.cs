// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class OboRequestTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        private MockHttpMessageHandler AddMockHandlerAadSuccess(
            MockHttpManager httpManager,
            string authority,
            IList<string> unexpectedHeaders = null,
            HttpResponseMessage responseMessage = null)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = responseMessage ?? MockHelpers.CreateSuccessTokenResponseMessage(),
                UnexpectedRequestHeaders = unexpectedHeaders
            };
            httpManager.AddMockHandler(handler);

            return handler;
        }

        [TestMethod]
        public async Task AcquireTokenByObo_AccessTokenExpiredRefreshTokenAvailable_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);

                //Expire access tokens
                TokenCacheHelper.ExpireAllAccessTokens(cca.UserTokenCacheInternal);

                MockHttpMessageHandler mockTokenRequestHttpHandlerRefresh = AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);
                mockTokenRequestHttpHandlerRefresh.ExpectedPostData = new Dictionary<string, string> { { "grant_type", "refresh_token" } };

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByObo_MissMatchUserAssertions_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);

                //Update user assertions
                TokenCacheHelper.UpdateUserAssertions(cca);

                MockHttpMessageHandler mockTokenRequestHttpHandlerRefresh = AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                //Access and refresh tokens have a different user assertion so MSAL should perform OBO.
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByObo_AccessTokenInCache_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
                userCacheAccess.AssertAccessCounts(1, 1);

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.Cache);
                Assert.AreEqual("some-access-token", result.AccessToken);
                userCacheAccess.AssertAccessCounts(2, 1);

                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                Assert.AreEqual(userAssertion.AssertionHash, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(userAssertion.AssertionHash, cachedRefreshToken.OboCacheKey);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByObo_InLongRunningProcess_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                string oboCacheKey = "obo-cache-key";
                var result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                // Token's not in cache, searched by user assertion hash, retrieved from IdP, saved with the provided OBO cache key
                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
                userCacheAccess.AssertAccessCounts(1, 1);

                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

                // Token is in the cache, retrieved by the provided OBO cache key
                Assert.IsNotNull(result);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.Cache);
                Assert.AreEqual("some-access-token", result.AccessToken);
                userCacheAccess.AssertAccessCounts(2, 1);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByObo_InitiateLongRunningProcessInWebApi_CacheKeyAlreadyExists_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant,
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
                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
                Assert.AreEqual(TestConstants.RTSecret, cachedRefreshToken.Secret);
                userCacheAccess.AssertAccessCounts(1, 1);

                // Token with the same scopes, OBO cache key, etc. exists in the cache -> throw error
                var exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.OboCacheKeyAlreadyInCacheError, exception.ErrorCode);
                userCacheAccess.AssertAccessCounts(2, 1);

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant,
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

                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.ATSecret2, result.AccessToken);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
                cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().FirstOrDefault(at => at.Secret.Equals(TestConstants.ATSecret2));
                cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().FirstOrDefault(rt => rt.Secret.Equals(TestConstants.RTSecret2));
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
                Assert.AreEqual(TestConstants.RTSecret2, cachedRefreshToken.Secret);
                userCacheAccess.AssertAccessCounts(3, 2);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByObo_AcquireTokenInLongRunningProcess_CacheKeyDoesNotExist_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

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
        public async Task AcquireTokenByObo_InLongRunningProcess_WithNullCacheKey_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

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

        [TestMethod]
        public async Task AcquireTokenByObo_NullCcs_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                var extraUnexpectedHeaders = new List<string>() { { Constants.CcsRoutingHintHeader } };
                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant, extraUnexpectedHeaders);

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                                      .WithCcsRoutingHint("")
                                      .WithCcsRoutingHint("", "")
                                      .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                      .WithCcsRoutingHint("")
                      .WithCcsRoutingHint("", "")
                      .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
            }
        }
    }
}
