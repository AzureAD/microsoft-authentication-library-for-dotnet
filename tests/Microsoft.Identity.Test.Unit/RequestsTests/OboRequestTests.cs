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

        private MockHttpMessageHandler AddMockHandlerAadSuccess(MockHttpManager httpManager, string authority, IList<string> unexpectedHeaders = null)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                UnexpectedRequestHeaders = unexpectedHeaders
            };
            httpManager.AddMockHandler(handler);

            return handler;
        }

        [TestMethod]
        public async Task AcquireTokenByOboAccessTokenExpiredRefreshTokenAvailableAsync()
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
                TokenCacheHelper.ExpireAccessTokens(cca.UserTokenCacheInternal);

                MockHttpMessageHandler mockTokenRequestHttpHandlerRefresh = AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);
                mockTokenRequestHttpHandlerRefresh.ExpectedPostData = new Dictionary<string, string> { { "grant_type", "refresh_token" } };

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByOboMissMatchUserAssertionsAsync()
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

                //Access and refresh tokens are have a different user assertion so MSAL should perform OBO.
                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByOboAccessTokenInCacheTestAsync()
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
        public async Task AcquireTokenByObo_WithCacheKey_TestAsync()
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
                                                         .WithLogging((level, message, pii) => System.Diagnostics.Debug.WriteLine($"MMMMMSAL {message}"), LogLevel.Verbose, true)
                                                         .BuildConcrete();

                var userCacheAccess = cca.UserTokenCache.RecordAccess();

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                string oboCacheKey = "obo-cache-key";
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithCacheKey(oboCacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.IdentityProvider);
                userCacheAccess.AssertAccessCounts(1, 1);

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, oboCacheKey).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(result.AuthenticationResultMetadata.TokenSource, TokenSource.Cache);
                Assert.AreEqual("some-access-token", result.AccessToken);
                userCacheAccess.AssertAccessCounts(2, 1);

                MsalAccessTokenCacheItem cachedAccessToken = cca.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
                MsalRefreshTokenCacheItem cachedRefreshToken = cca.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                Assert.AreEqual(oboCacheKey, cachedAccessToken.OboCacheKey);
                Assert.AreEqual(oboCacheKey, cachedRefreshToken.OboCacheKey);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByObo_WithNullCacheKey_Throws_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                UserAssertion userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);
                await AssertException.TaskThrowsAsync<ArgumentNullException>(
                    () => cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                        .WithCacheKey(null)
                        .ExecuteAsync())
                    .ConfigureAwait(false);

                await AssertException.TaskThrowsAsync<ArgumentNullException>(
                    () => cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, cacheKey: null)
                        .ExecuteAsync())
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task AcquireTokenByOboNullCcsTestAsync()
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
