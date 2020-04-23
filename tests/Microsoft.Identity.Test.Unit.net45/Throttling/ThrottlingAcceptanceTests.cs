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
                var app = await SetupAndAquireOnceAsync(httpManagerAndBundle, 400, 2).ConfigureAwait(false);
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
                await Task.Delay(2 * 1000).ConfigureAwait(false);
                // cache isn't clear just by time passing
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1); 

                Trace.WriteLine("5. Third call - no more throttling");
                httpManager.AddTokenResponse(TokenResponseType.Valid, s_throttlingHeader);
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
        [DataRow(429)]
        [DataRow(503)]
        public async Task Http429_RetryAfter_ThrottleForDuration_Async(int httpStatusCode)
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAquireOnceAsync(httpManagerAndBundle, httpStatusCode, RetryAfterDurationSeconds).ConfigureAwait(false);

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
                SimulateTimePassing(throttlingManager, retryAfterDuration);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                Trace.WriteLine("5. Third call - no more throttling");
                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid, s_throttlingHeader);
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
                var app = await SetupAndAquireOnceAsync(httpManagerAndBundle, 400, RetryAfterDurationSeconds).ConfigureAwait(false);
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
                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid, s_throttlingHeader);
                await app.AcquireTokenSilent(new[] { "Other.Scopes" }, account).ExecuteAsync()
                       .ConfigureAwait(false);
            }
        }         

        /// <summary>
        /// 400 with Retry After with 2 hours, should be throttled on DefaultThrottlig timeout
        /// </summary>
        [TestMethod]
        public async Task RetryAfter_LargeTimeoutHeader_DefaultTimeout_IsUsed_Async()
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAquireOnceAsync(httpManagerAndBundle, 400, 7200).ConfigureAwait(false);
                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);
                AssertThrottlingCacheEntryCount(throttlingManager, retryAfterEntryCount: 1);

                var (retryAfterProvider, _, _) = GetTypedThrottlingProviders(throttlingManager);
                var singleEntry = retryAfterProvider.Cache.CacheForTest.Single().Value;
                TimeSpan actualExpiration = singleEntry.ExpirationTime - singleEntry.CreationTime;
                Assert.AreEqual(actualExpiration, RetryAfterProvider.MaxRetryAfter);
            }
        }

        #endregion

        #region HTTP 5xx acceptance test

        /// <summary>
        /// 429 and 503 with Retry After with 15 seconds, the entry should stay in cache for 15 seconds
        /// </summary>
        [TestMethod]
        [DataRow(429)]
        [DataRow(503)]
        public async Task Http429_And503_WithoutRetryAfter_AreThrottled_ByDefaultTimeout_Async(int httpStatusCode)
        {
            using (var httpManagerAndBundle = new MockHttpAndServiceBundle())
            {
                var app = await SetupAndAquireOnceAsync(httpManagerAndBundle, httpStatusCode, null).ConfigureAwait(false);

                var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();
                var throttlingManager = (httpManagerAndBundle.ServiceBundle.ThrottlingManager as SingletonThrottlingManager);                

                Trace.WriteLine("3. Second call - request is throttled");
                var ex = await AssertException.TaskThrowsAsync<MsalThrottledServiceException>(
                   () => app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync())
                       .ConfigureAwait(false);
                Assert.AreEqual(httpStatusCode, ex.StatusCode);
                AssertThrottlingCacheEntryCount(throttlingManager, httpStatusEntryCount: 1);

                Trace.WriteLine("4. Simulate Time passes, the throttling cache will have expired");
                SimulateTimePassing(throttlingManager, HttpStatusProvider.s_throttleDuration);
                AssertThrottlingCacheEntryCount(throttlingManager, httpStatusEntryCount: 1);

                Trace.WriteLine("5. Third call - no more throttling");
                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid);
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
                var app = await SetupAndAquireOnceAsync(httpManagerAndBundle, 429, null).ConfigureAwait(false);
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
                httpManagerAndBundle.HttpManager.AddTokenResponse(TokenResponseType.Valid);
                await app.AcquireTokenSilent(new[] { "Other.Scopes" }, account).ExecuteAsync()
                       .ConfigureAwait(false);

                Trace.WriteLine("Create a new PCA and try a different flow");
                var pca2 = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithHttpManager(httpManagerAndBundle.HttpManager)
                    .BuildConcrete();
                new TokenCacheHelper().PopulateCache(
                    pca2.UserTokenCacheInternal.Accessor, 
                    expiredAccessTokens: true);

                await AssertException.TaskThrowsAsync<MsalThrottledServiceException>(
                 () => pca2.AcquireTokenSilent(TestConstants.s_scope, account)
                      .ExecuteAsync())
                     .ConfigureAwait(false);
            }
        }

        #endregion

        private async Task<IPublicClientApplication> SetupAndAquireOnceAsync(
             MockHttpAndServiceBundle httpManagerAndBundle,
            int httpStatusCode,
            int? retryAfterInSeconds)
        {
            Trace.WriteLine("1. Setup test");
            var httpManager = httpManagerAndBundle.HttpManager;

            PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                        .WithHttpManager(httpManager)
                                                                        .BuildConcrete();
            new TokenCacheHelper().PopulateCache(app.UserTokenCacheInternal.Accessor, expiredAccessTokens: true);

            var tokenResponse = httpManager.AddAllMocks(TokenResponseType.InvalidClient);
            UpdateStatusCodeAndHeaders(tokenResponse.ResponseMessage, httpStatusCode, retryAfterInSeconds);

            if (httpStatusCode >= 500 && httpStatusCode < 600 && !retryAfterInSeconds.HasValue)
            {
                var response2 = httpManager.AddTokenResponse(
                    TokenResponseType.InvalidClient, s_throttlingHeader);
                UpdateStatusCodeAndHeaders(response2.ResponseMessage, httpStatusCode, retryAfterInSeconds);
            }

            var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();

            Trace.WriteLine("2. First failing call ");
            var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                () => app.AcquireTokenSilent(TestConstants.s_scope, account).ExecuteAsync())
                    .ConfigureAwait(false);
            
            Assert.AreEqual(0, httpManager.QueueSize, "No more requests expected");
            Assert.AreEqual(httpStatusCode, ex.StatusCode);

            return app;
        }

        private static void UpdateStatusCodeAndHeaders(HttpResponseMessage tokenResponse, int httpStatusCode, int? retryAfterInSeconds)
        {
            tokenResponse.StatusCode = (HttpStatusCode)httpStatusCode;
            if (retryAfterInSeconds.HasValue)
            {
                tokenResponse.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                    TimeSpan.FromSeconds(retryAfterInSeconds.Value));
            }
        }

        private static void SimulateTimePassing(SingletonThrottlingManager throttlingManager, TimeSpan delay)
        {
            var (retryAfterProvider, httpStatusProvider, uiRequiredProvider) = GetTypedThrottlingProviders(throttlingManager);
            MoveToPast(delay, retryAfterProvider.Cache.CacheForTest);
            MoveToPast(delay, httpStatusProvider.Cache.CacheForTest);
            MoveToPast(delay, uiRequiredProvider.Cache.CacheForTest);
        }

        private static void MoveToPast(TimeSpan delay, ConcurrentDictionary<string, ThrottlingCacheEntry> cacheDictionary)
        {
            foreach (var kvp in cacheDictionary)
            {
                // move time forward by moving creation and expiration time back
                cacheDictionary[kvp.Key] = new ThrottlingCacheEntry(
                    kvp.Value.Exception,
                    kvp.Value.CreationTime - delay,
                    kvp.Value.ExpirationTime - delay);
            }
        }

        private static void AssertThrottlingCacheEntryCount(
            SingletonThrottlingManager throttlingManager, 
            int retryAfterEntryCount = 0, 
            int httpStatusEntryCount = 0, 
            int uiExceptionEntryCount = 0)
        {
            var (retryAfterProvider, httpStatusProvider, uiRequiredProvider) = GetTypedThrottlingProviders(throttlingManager);

            Assert.AreEqual(retryAfterEntryCount, retryAfterProvider.Cache.CacheForTest.Count);
            Assert.AreEqual(httpStatusEntryCount, httpStatusProvider.Cache.CacheForTest.Count);
            Assert.AreEqual(uiExceptionEntryCount, uiRequiredProvider.Cache.CacheForTest.Count);
        }

        private static (RetryAfterProvider, HttpStatusProvider, UiRequiredProvider) GetTypedThrottlingProviders(
            SingletonThrottlingManager throttlingManager)
        {
            return (
                throttlingManager.ThrottlingProvidersForTest.Single(p => p is RetryAfterProvider) as RetryAfterProvider,
                throttlingManager.ThrottlingProvidersForTest.Single(p => p is HttpStatusProvider) as HttpStatusProvider,
                throttlingManager.ThrottlingProvidersForTest.Single(p => p is UiRequiredProvider) as UiRequiredProvider);
        }

        private static void AssertInvalidClientEx(MsalServiceException ex)
        {
            Assert.AreEqual("invalid_client", ex.ErrorCode);
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ex.StatusCode);
            Assert.IsNotNull(ex.Headers.RetryAfter);
        }
    }
}
