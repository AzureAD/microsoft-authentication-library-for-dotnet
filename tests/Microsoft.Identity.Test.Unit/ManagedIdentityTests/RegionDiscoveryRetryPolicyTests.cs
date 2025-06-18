// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class RegionDiscoveryRetryPolicyTests : TestBase
    {
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();

        [TestMethod]
        public async Task RegionDiscoveryFails500OnceThenSucceeds200Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Configure the app with RegionDiscovery
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                    .BuildConcrete();

                // Initial request fails with 500
                httpManager.AddRegionDiscoveryMockHandler(
                    statusCode: HttpStatusCode.InternalServerError);

                // Final success
                httpManager.AddRegionDiscoveryMockHandler(
                    TestConstants.Region);

                var result = await app.GetAzureRegionAsync().ConfigureAwait(false);
                Assert.AreEqual(TestConstants.Region, result);

                const int NumRequests = 2; // initial request + 1 retry
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade);
            }
        }

        [TestMethod]
        public async Task RegionDiscoveryFails500PermanentlyAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                    .BuildConcrete();

                // Simulate permanent 500s (to trigger the maximum number of retries)
                const int Num500Errors = 1 + RegionDiscoveryRetryPolicy.NumRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddRegionDiscoveryMockHandler(
                        statusCode: HttpStatusCode.InternalServerError);
                }

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(
                    async () => await app.GetAzureRegionAsync().ConfigureAwait(false))
                    .ConfigureAwait(false);

                Assert.IsNotNull(ex);

                int requestsMade = Num500Errors - httpManager.QueueSize;
                Assert.AreEqual(Num500Errors, requestsMade);
            }
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.NotFound, "404 Not Found")]
        [DataRow(HttpStatusCode.RequestTimeout, "408 Request Timeout")]
        public async Task RegionDiscoveryDoesNotRetryOnNonRetryableStatusCodesAsync(HttpStatusCode statusCode, string description)
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                    .BuildConcrete();

                // Add response with non-retryable status code
                httpManager.AddRegionDiscoveryMockHandler(
                    statusCode: statusCode);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(
                    async () => await app.GetAzureRegionAsync().ConfigureAwait(false))
                    .ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual((int)statusCode, ex.StatusCode);

                const int NumRequests = 1; // initial request + 0 retries (non-retryable status codes should not trigger retry)
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade, $"Expected single request without retry for {description}");
            }
        }
    }
}
