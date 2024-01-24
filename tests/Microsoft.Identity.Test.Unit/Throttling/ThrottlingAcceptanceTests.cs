// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.Throttling
{

    [TestClass]
    public class ThrottlingAcceptanceTests : TestBase
    {
        private const int RetryAfterDurationSeconds = 15;
        private static readonly IDictionary<string, string> s_throttlingHeader = new Dictionary<string, string>()
        {
            { ThrottleCommon.ThrottleRetryAfterHeaderName, ThrottleCommon.ThrottleRetryAfterHeaderValue}
        };

        /// <summary>
        /// 400 with Retry After with N seconds, the entry should stay in cache for N seconds
        /// </summary>
        /// <remarks>This test will actually sleep for N seconds to simulate the real world scenario</remarks>
        [TestMethod]
        public async Task Http400_RetryAfter_ThrottleForDuration_AcceptanceTest_Async()
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAcquireOnceAsync(httpManagerAndBundle, 400, 2, TokenResponseType.InvalidClient).ConfigureAwait(false);
                var httpManager = httpManagerAndBundle.HttpManager;
                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                Trace.WriteLine("3. Second call - request is throttled");
                var ex = await AssertException.TaskThrowsAsync<MsalThrottledServiceException>(
                   () => app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync())
                       .ConfigureAwait(false);
                AssertInvalidClientEx(ex);

                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                Trace.WriteLine("4. Time passes, the throttling cache will have expired");
                await Task.Delay(2100).ConfigureAwait(false);
                // cache isn't clear just by time passing
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                Trace.WriteLine("5. Third call - no more throttling");
                httpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows, s_throttlingHeader);
                await app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync()
                       .ConfigureAwait(false);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 0);
            }
        }

        #region Retry-After acceptance tests
        /// <summary>
        /// 429 and 503 with Retry After with 15 seconds, the entry should stay in cache for 15 seconds
        /// </summary>
        [TestMethod]
        [Ignore] // Unstable test, fails on the CI build but passes on local or PR build. https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3587
        [DataRow(429)]
        [DataRow(503)]
        public async Task Http429_RetryAfter_ThrottleForDuration_Async(int httpStatusCode)
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAcquireOnceAsync(
                    httpManagerAndBundle,
                    httpStatusCode,
                    RetryAfterDurationSeconds,
                    TokenResponseType.InvalidClient).ConfigureAwait(false);

                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);
                TimeSpan retryAfterDuration = TimeSpan.FromSeconds(RetryAfterDurationSeconds);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                Trace.WriteLine("3. Second call - request is throttled");
                var ex = await AssertException.TaskThrowsAsync<MsalThrottledServiceException>(
                   () => app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync())
                       .ConfigureAwait(false);
                Assert.AreEqual(httpStatusCode, ex.StatusCode);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                Trace.WriteLine("4. Simulate Time passes, the throttling cache will have expired");
                throttlingManager.SimulateTimePassing(retryAfterDuration);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                Trace.WriteLine("5. Third call - no more throttling");
                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows, s_throttlingHeader);
                await app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync()
                       .ConfigureAwait(false);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 0);
            }
        }

        /// <summary>
        /// If a request had the response with Retry After header, then a different request with the same strict thumbprint should be throttled as well
        /// If a request had the response with Retry After header, then a different request with a different strict thumbprint should work
        /// </summary>
        [TestMethod]
        public async Task SimilarRequests_AreThrottled_RetryAfter_Async()
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAcquireOnceAsync(
                    httpManagerAndBundle,
                    400,
                    RetryAfterDurationSeconds,
                    TokenResponseType.InvalidClient).ConfigureAwait(false);
                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                Trace.WriteLine("A similar request, e.g. with a claims challenge, will be throttled");
                var ex = await AssertException.TaskThrowsAsync<MsalThrottledServiceException>(
                   () => app.AcquireTokenSilent(TestConstants.s_scope, account)
                        .WithClaims(TestConstants.Claims) // claims are not part of the strict thumbprint
                        .ExecuteAsync())
                       .ConfigureAwait(false);
                Assert.AreEqual(400, ex.StatusCode);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                Trace.WriteLine("A different request, e.g. with other scopes, will not be throttled");
                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows, s_throttlingHeader);
                await app.AcquireTokenSilent(new[] { "Other.Scopes" }, account).ExecuteAsync()
                       .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 400 with Retry After with 2 hours, should be throttled on DefaultThrottling timeout
        /// </summary>
        [TestMethod]
        public async Task RetryAfter_LargeTimeoutHeader_DefaultTimeout_IsUsed_Async()
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAcquireOnceAsync(
                    httpManagerAndBundle,
                    400,
                    7200,
                    TokenResponseType.InvalidClient).ConfigureAwait(false);
                
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                var (retryAfterProvider, _, _) = throttlingManager.GetTypedThrottlingProviders();
                var singleEntry = retryAfterProvider.ThrottlingCache.CacheForTest.Single().Value;
                TimeSpan actualExpiration = singleEntry.ExpirationTime - singleEntry.CreationTime;
                Assert.AreEqual(actualExpiration, RetryAfterProvider.MaxRetryAfter);
            }
        }

        [TestMethod]
        public async Task RetryAfter_ConfidentialClient_Async()
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var httpManager = httpManagerAndBundle.HttpManager;
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);
                var (retryAfterProvider, _, _) = throttlingManager.GetTypedThrottlingProviders();

                httpManager.AddInstanceDiscoveryMockHandler();
                var tokenResponse = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                tokenResponse.ResponseMessage.StatusCode = (HttpStatusCode)429;
                const int RetryAfterInSeconds = 10;
                UpdateStatusCodeAndHeaders(tokenResponse.ResponseMessage, 429, RetryAfterInSeconds);

                var ex = await AssertException.TaskThrowsAsync<MsalServiceException>( 
                    () => app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(429, ex.StatusCode);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                var ex2 = await AssertException.TaskThrowsAsync<MsalThrottledServiceException>(
                   () => app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync())
                   .ConfigureAwait(false);
                Assert.AreEqual(429, ex2.StatusCode);
                Assert.AreSame(ex, ex2.OriginalServiceException);

                throttlingManager.SimulateTimePassing(TimeSpan.FromSeconds(RetryAfterInSeconds + 1));
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                await app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task RetryAfter_ConfidentialClient_ErrorMessage_Async()
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var httpManager = httpManagerAndBundle.HttpManager;
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                var tokenResponse = httpManager.AddMockHandlerForThrottledResponseMessage();

                var serverEx = await AssertException.TaskThrowsAsync<MsalThrottledServiceException>(
                    () => app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(serverEx.StatusCode, 429);
                Assert.AreEqual(serverEx.ErrorCode, MsalError.RequestThrottled);
                Assert.AreEqual(serverEx.Message, MsalErrorMessage.AadThrottledError);
                Assert.AreEqual(serverEx.ResponseBody, MockHelpers.TooManyRequestsContent);
            }
        }
        #endregion

        #region HTTP 5xx acceptance test

        /// <summary>
        /// 429 and 503 with Retry After with 15 seconds, the entry should stay in cache for 15 seconds
        /// </summary>
        [TestMethod]
        [Ignore] // Unstable test, fails on the CI build but passes on local or PR build. https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3587
        [DataRow(429)]
        [DataRow(503)]
        public async Task Http429_And503_WithoutRetryAfter_AreThrottled_ByDefaultTimeout_Async(int httpStatusCode)
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAcquireOnceAsync(httpManagerAndBundle, httpStatusCode, null, TokenResponseType.InvalidClient).ConfigureAwait(false);

                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);

                Trace.WriteLine("3. Second call - request is throttled");
                var ex = await AssertException.TaskThrowsAsync<MsalThrottledServiceException>(
                   () => app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync())
                       .ConfigureAwait(false);
                Assert.AreEqual(httpStatusCode, ex.StatusCode);
                AssertThrottlingCacheEntryCount(throttlingManager, httpStatusEntryCount: 1);

                Trace.WriteLine("4. Simulate Time passes, the throttling cache will have expired");
                throttlingManager.SimulateTimePassing(HttpStatusProvider.s_throttleDuration);
                AssertThrottlingCacheEntryCount(throttlingManager, httpStatusEntryCount: 1);

                Trace.WriteLine("5. Third call - no more throttling");
                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);
                await app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync()
                       .ConfigureAwait(false);
                AssertThrottlingCacheEntryCount(throttlingManager, httpStatusEntryCount: 0);
            }
        }

        /// <summary>
        /// If a request had the response with Retry After header, then a different request with the same strict thumbprint should be throttled as well
        /// If a request had the response with Retry After header, then a different request with a different strict thumbprint should work
        /// </summary>
        [TestMethod]
        public async Task SimilarRequests_AreThrottled_HttpStatus_Async()
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAcquireOnceAsync(
                    httpManagerAndBundle,
                    429,
                    null,
                    TokenResponseType.InvalidClient).ConfigureAwait(false);
                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);
                AssertThrottlingCacheEntryCount(throttlingManager, httpStatusEntryCount: 1);

                Trace.WriteLine("A similar request, e.g. with a claims challenge, will be throttled");
                var ex = await AssertException.TaskThrowsAsync<MsalThrottledServiceException>(
                   () => app.AcquireTokenSilent(TestConstants.s_scope, account)
                        .WithClaims(TestConstants.Claims) // claims are not part of the strict thumbprint
                        .ExecuteAsync())
                       .ConfigureAwait(false);
                Assert.AreEqual(429, ex.StatusCode);
                AssertThrottlingCacheEntryCount(throttlingManager, httpStatusEntryCount: 1);

                Trace.WriteLine("A different request, e.g. with other scopes, will not be throttled");
                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);
                await app.AcquireTokenSilent(new[] { "Other.Scopes" }, account).ExecuteAsync()
                       .ConfigureAwait(false);

                Trace.WriteLine("Create a new PCA and try a different flow");
                var pca2 = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithHttpManager(httpManagerAndBundle.HttpManager)
                    .BuildConcrete();
                TokenCacheHelper.PopulateCache(
                    pca2.UserTokenCacheInternal.Accessor,
                    expiredAccessTokens: true);

                await AssertException.TaskThrowsAsync<MsalThrottledServiceException>(
                 () => pca2.AcquireTokenSilent(TestConstants.s_scope, account)
                      .ExecuteAsync())
                     .ConfigureAwait(false);
            }
        }

        #endregion

        #region UiRequired test

        /// <summary>
        /// UI required cache for the same request (invalid_grant)
        /// Create a request that should have the response with HTTP status 400 and OAuth error invalid_grant.
        /// Repeat the same request immediately - expectation is the result should be returned from UI required cache.
        /// Sleep for at least DefaultUIRequired + 1 seconds.
        /// Repeat the same request again - expectation is the request should be issued on the server.
        /// If a request had the response with Retry After header, then a different request with a different strict thumbprint should work
        /// </summary>
        [TestMethod]
        public async Task UiRequiredThrottling_Async()
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAcquireOnceAsync(
                    httpManagerAndBundle,
                    400,
                    null,
                    TokenResponseType.InvalidGrant).ConfigureAwait(false);

                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);
                AssertThrottlingCacheEntryCount(throttlingManager, uiRequiredEntryCount: 1);

                Trace.WriteLine("A similar request will be throttled");
                var ex = await AssertException.TaskThrowsAsync<MsalThrottledUiRequiredException>(
                   () => app.AcquireTokenSilent(TestConstants.s_scope, account)
                        .ExecuteAsync())
                       .ConfigureAwait(false);
                Assert.AreEqual(400, ex.StatusCode);
                AssertThrottlingCacheEntryCount(throttlingManager, uiRequiredEntryCount: 1);

                Trace.WriteLine("And again...");

                ex = await AssertException.TaskThrowsAsync<MsalThrottledUiRequiredException>(
                  () => app.AcquireTokenSilent(TestConstants.s_scope, account)
                       .ExecuteAsync())
                      .ConfigureAwait(false);
                Assert.AreEqual(400, ex.StatusCode);
                AssertThrottlingCacheEntryCount(throttlingManager, uiRequiredEntryCount: 1);

                Trace.WriteLine("Time passes, the same request should now pass");
                throttlingManager.SimulateTimePassing(
                    UiRequiredProvider.s_uiRequiredExpiration +
                    TimeSpan.FromSeconds(1));

                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);
                await app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync()
                       .ConfigureAwait(false);

                AssertThrottlingCacheEntryCount(throttlingManager, uiRequiredEntryCount: 0);
            }
        }

        /// <summary>
        /// Scenarios: 
        /// 1. Application is in throttled state but then goes interactive. Throttling should no longer block AcquireTokenSilent.
        /// 2. FOCI apps where one app goes interactive. The other apps should no longer be blocked.
        /// 
        /// These scenarios are solved by UI Required Throttling cache being scoped on RT. Requests with different RTs 
        /// will bypass the cache.
        /// </summary>
        [TestMethod]
        public async Task UiRequired_BypassRt_Async()
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAcquireOnceAsync(
                    httpManagerAndBundle,
                    400,
                    null,
                    TokenResponseType.InvalidGrant).ConfigureAwait(false);

                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);
                AssertThrottlingCacheEntryCount(throttlingManager, uiRequiredEntryCount: 1);

                Trace.WriteLine("A similar request will be throttled");
                var ex = await AssertException.TaskThrowsAsync<MsalThrottledUiRequiredException>(
                   () => app.AcquireTokenSilent(TestConstants.s_scope, account)
                        .ExecuteAsync())
                       .ConfigureAwait(false);
                Assert.AreEqual(400, ex.StatusCode);
                AssertThrottlingCacheEntryCount(throttlingManager, uiRequiredEntryCount: 1);

                Trace.WriteLine("If RT changes, the request passes through");
                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);
                MsalRefreshTokenCacheItem rt = app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
                rt.Secret = "other_secret";
                app.UserTokenCacheInternal.Accessor.SaveRefreshToken(rt);

                await app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync()
                       .ConfigureAwait(false);
                AssertThrottlingCacheEntryCount(throttlingManager, uiRequiredEntryCount: 1);
            }
        }

        [TestMethod]
        public async Task UiRequired_MultipleEntries_Async()
        {
            Assert.AreEqual(2, TestConstants.s_scope.Count, "test error - expecting at least 2 scopes");

            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);

                var app = await SetupAndAcquireOnceAsync(
                    httpManagerAndBundle,
                    400,
                    null,
                    TokenResponseType.InvalidGrant).ConfigureAwait(false);

                Assert.AreEqual(0, httpManagerAndBundle.HttpManager.QueueSize);
                AssertThrottlingCacheEntryCount(throttlingManager, uiRequiredEntryCount: 1);

                httpManagerAndBundle.HttpManager.AddTokenResponse(
                    TokenResponseType.InvalidGrant, s_throttlingHeader);

                Trace.WriteLine("Make another failing request, but with fewer scopes - should not be throttled.");
                var singleScope = TestConstants.s_scope.Take(1);
                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                  () => app.AcquireTokenSilent(singleScope, account).ExecuteAsync())
                      .ConfigureAwait(false);

                Assert.AreEqual(0, httpManagerAndBundle.HttpManager.QueueSize);
                AssertThrottlingCacheEntryCount(throttlingManager, uiRequiredEntryCount: 2);

                Trace.WriteLine("Time passes, the same requests should now pass");
                throttlingManager.SimulateTimePassing(
                    UiRequiredProvider.s_uiRequiredExpiration +
                    TimeSpan.FromSeconds(1));

                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);
                await app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync()
                       .ConfigureAwait(false);
                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);

                await app.AcquireTokenSilent(singleScope, account).WithForceRefresh(true).ExecuteAsync()
                      .ConfigureAwait(false);

                // there will still be an entry here because the refresh token written by the first successful ATS
                // is different from the initial RT
                AssertThrottlingCacheEntryCount(throttlingManager, uiRequiredEntryCount: 1);
            }
        }
        #endregion

        private async Task<PublicClientApplication> SetupAndAcquireOnceAsync(
            MockHttpAndServiceBundle httpManagerAndBundle,
            int httpStatusCode,
            int? retryAfterInSeconds,
            TokenResponseType tokenResponseType)
        {
            Trace.WriteLine("1. Setup test");
            var httpManager = httpManagerAndBundle.HttpManager;

            PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                        .WithHttpManager(httpManager)
                                                                        .BuildConcrete();
            TokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, expiredAccessTokens: true);

            var tokenResponse = httpManager.AddAllMocks(tokenResponseType);
            UpdateStatusCodeAndHeaders(tokenResponse.ResponseMessage, httpStatusCode, retryAfterInSeconds);

            if (httpStatusCode >= 500 && httpStatusCode < 600 && !retryAfterInSeconds.HasValue)
            {
                var response2 = httpManager.AddTokenResponse(
                    tokenResponseType, s_throttlingHeader);
                UpdateStatusCodeAndHeaders(response2.ResponseMessage, httpStatusCode, retryAfterInSeconds);
            }

            var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();

            Trace.WriteLine("2. First failing call ");
            var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                () => app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync(),
                     allowDerived: true)
                    .ConfigureAwait(false);

            Assert.AreEqual(0, httpManager.QueueSize, "No more requests expected");
            Assert.AreEqual(httpStatusCode, ex.StatusCode);
            Assert.AreEqual(tokenResponseType == TokenResponseType.InvalidGrant, ex is MsalUiRequiredException);

            return app;
        }

        private static void UpdateStatusCodeAndHeaders(
            HttpResponseMessage tokenResponse,
            int httpStatusCode,
            int? retryAfterInSeconds)
        {
            tokenResponse.StatusCode = (HttpStatusCode)httpStatusCode;
            if (retryAfterInSeconds.HasValue)
            {
                tokenResponse.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                    TimeSpan.FromSeconds(retryAfterInSeconds.Value));
            }
        }

        private static void AssertThrottlingCacheEntryCount(
            SingletonThrottlingManager throttlingManager,
            int retryAfterEntryCount = 0,
            int httpStatusEntryCount = 0,
            int uiRequiredEntryCount = 0)
        {
            var (retryAfterProvider, httpStatusProvider, uiRequiredProvider) =
                throttlingManager.GetTypedThrottlingProviders();

            Assert.AreEqual(retryAfterEntryCount, retryAfterProvider.ThrottlingCache.CacheForTest.Count);
            Assert.AreEqual(httpStatusEntryCount, httpStatusProvider.ThrottlingCache.CacheForTest.Count);
            Assert.AreEqual(uiRequiredEntryCount, uiRequiredProvider.ThrottlingCache.CacheForTest.Count);
        }

        private static void AssertInvalidClientEx(MsalServiceException ex)
        {
            Assert.AreEqual("invalid_client", ex.ErrorCode);
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ex.StatusCode);
            Assert.IsNotNull(ex.Headers.RetryAfter);
        }
    }
}
