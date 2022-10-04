// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.Retry;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class RetryPolicyTests : TestBase
    {
        [TestMethod]
        public async Task RetryPolicyAsync()
        {
            using (var httpManager = new MockHttpManager())
            {

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithHttpClientFactory(
                                                                httpClientFactory: null,
                                                                retryOnceOn5xx: false)
                                                              .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.InternalServerError, retryAfter: 3);
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.InternalServerError, retryAfter: 4);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Retry policy at token request level
                TimeSpan retryAfter = TimeSpan.Zero;
                int retried = 0;

                var retryPolicy = Policy.Handle<Exception>(ex =>
                {
                    return IsMsalRetryableException(ex, out retryAfter);
                }).RetryAsync( 2,                     
                    (exception, retryCount, context) =>
                    {
                        IsMsalRetryableException(exception, out retryAfter);
                        Assert.AreEqual(retryCount + 2, retryAfter.TotalSeconds);
                        retried++;
                    });

                // Act
                var result = await retryPolicy.ExecuteAsync(() => app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync())
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(2, retried);
            }
        }


        /// <summary>
        ///  Retry any MsalException marked as retryable - see IsRetryiable property and HttpRequestException
        ///  If Retry-After header is present, return the value.
        /// </summary>
        /// <remarks>
        /// In MSAL 4.47.2 IsRetryiable includes HTTP 408, 429 and 5xx AAD errors but may be expanded to transient AAD errors in the future. 
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
    }
}
