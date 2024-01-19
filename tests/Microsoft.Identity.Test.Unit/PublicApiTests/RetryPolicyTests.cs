// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class RetryPolicyTests : TestBase
    {
// This test is expensive, as it has to wait 1 second - run it only on latest .NET
#if NET6_0_OR_GREATER 
        [TestMethod]        
        public async Task RetryPolicyAsync()
        {
            using (var httpManager = new MockHttpManager(retryOnce: false))
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithHttpClientFactory(
                                                                httpClientFactory: null,
                                                                retryOnceOn5xx: false)
                                                              .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddResiliencyMessageMockHandler(
                    HttpMethod.Post,
                    HttpStatusCode.InternalServerError, retryAfter: 1);
                httpManager.AddResiliencyMessageMockHandler(
                    HttpMethod.Post,
                    HttpStatusCode.InternalServerError);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Retry policy at token request level
                TimeSpan retryAfter = TimeSpan.Zero;

                var retryPolicy = Policy.Handle<Exception>(ex =>
                {
                    return IsMsalRetryableException(ex, out retryAfter);
                }).RetryAsync(5,
                    async (exception, retryCount, _) =>
                    {
                        IsMsalRetryableException(exception, out retryAfter);
                        switch (retryCount)
                        {
                            case 1:
                                Assert.AreEqual(1, retryAfter.TotalSeconds);
                                // MSAL enforces Retry-After via throttling, so the test must wait 
                                await Task.Delay(1 * 1100).ConfigureAwait(false); 
                                break;
                            case 2:
                                Assert.AreEqual(
                                    0, 
                                    retryAfter.TotalSeconds, 
                                    $"Exception should not have Retry-After and should not be a throttling exception - {exception}");
                                break;
                            default:
                                Assert.Fail("3rd attempt should succeed");
                                break;
                        }
                    });
                int attempts = 0;

                // Act
                var result = await retryPolicy.ExecuteAsync(() =>
                {
                    attempts++;
                    return app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync();
                })
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(3, attempts);
            }
        }

        /// <summary>
        ///  Retry any MsalException marked as retryable - see IsRetryable property and HttpRequestException
        ///  If Retry-After header is present, return the value.
        /// </summary>
        /// <remarks>
        /// In MSAL 4.47.2 IsRetryable includes HTTP 408, 429 and 5xx AAD errors but may be expanded to transient AAD errors in the future. 
        /// </remarks>
        private static bool IsMsalRetryableException(Exception ex, out TimeSpan retryAfter)
        {
            retryAfter = TimeSpan.Zero;

            if (ex is HttpRequestException)
                return true;

            if (ex is MsalException msalException && msalException.IsRetryable)
            {
                if (msalException is MsalServiceException msalServiceException)
                {
                    retryAfter = GetRetryAfterValue(msalServiceException.Headers);
                }

                return true;
            }

            return false;
        }

        private static TimeSpan GetRetryAfterValue(HttpResponseHeaders headers)
        {
            var date = headers?.RetryAfter?.Date;
            if (date.HasValue)
            {
                return date.Value - DateTimeOffset.Now;
            }

            var delta = headers?.RetryAfter?.Delta;
            if (delta.HasValue)
            {
                return delta.Value;
            }

            return TimeSpan.Zero;
        }
#endif
    }
}
