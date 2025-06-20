// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Unit.CoreTests;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class RegionDiscoveryRetryPolicyTests : RegionDiscoveryProviderTests
    {
        [TestMethod]
        public async Task RegionDiscoveryFails500OnceThenSucceeds200Async()
        {
            // Configure the test to use region auto-discovery  
            GetTestRequestContext().ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Initial request fails with 500
            AddMockedResponse(MockHelpers.CreateFailureMessage(HttpStatusCode.InternalServerError, "Internal Server Error"));

            // Final success
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region));

            var metadata = await GetRegionDiscoveryProvider().GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"),
                GetTestRequestContext()).ConfigureAwait(false);

            Assert.IsNotNull(metadata, "Metadata should not be null");
            Assert.AreEqual(TestConstants.Region, metadata.PreferredNetwork.Split('.')[0]);

            const int NumRequests = 2; // initial request + 1 retry
            int requestsMade = NumRequests - GetHttpManager().QueueSize;
            Assert.AreEqual(NumRequests, requestsMade);
        }

        [TestMethod]
        public async Task RegionDiscoveryFails500PermanentlyAsync()
        {
            // Configure the test to use region auto-discovery
            GetTestRequestContext().ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Simulate permanent 500s (to trigger the maximum number of retries)
            const int Num500Errors = 1 + RegionDiscoveryRetryPolicy.NumRetries; // initial request + maximum number of retries
            for (int i = 0; i < Num500Errors; i++)
            {
                AddMockedResponse(
                    MockHelpers.CreateFailureMessage(HttpStatusCode.InternalServerError, "Internal Server Error"),
                    throwException: i == Num500Errors - 1); // Throw exception on the last retry
            }

            var response = await GetRegionDiscoveryProvider().GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"),
                GetTestRequestContext()).ConfigureAwait(false);

            Assert.IsNull(response, "Response should be null after failing all retries");

            const int ExpectedRequests = RegionDiscoveryRetryPolicy.NumRetries + 1; // retries + initial request
            int requestsMade = ExpectedRequests - GetHttpManager().QueueSize;
            Assert.AreEqual(ExpectedRequests, requestsMade, "Number of requests should match retry policy");
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.NotFound, "404 Not Found")]
        [DataRow(HttpStatusCode.RequestTimeout, "408 Request Timeout")]
        public async Task RegionDiscoveryDoesNotRetryOnNonRetryableStatusCodesAsync(HttpStatusCode statusCode, string description)
        {
            // Configure the test to use region auto-discovery
            GetTestRequestContext().ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Add response with non-retryable status code
            AddMockedResponse(MockHelpers.CreateFailureMessage(statusCode, $"Error {(int)statusCode}"));

            MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(
                async () => await GetRegionDiscoveryProvider().GetMetadataAsync(
                    new Uri("https://login.microsoftonline.com/common/"),
                    GetTestRequestContext()).ConfigureAwait(false))
                .ConfigureAwait(false);

            Assert.IsNotNull(ex);
            Assert.AreEqual((int)statusCode, ex.StatusCode);

            const int NumRequests = 1; // initial request + 0 retries (non-retryable status codes should not trigger retry)
            int requestsMade = NumRequests - GetHttpManager().QueueSize;
            Assert.AreEqual(NumRequests, requestsMade, $"Expected single request without retry for {description}");
        }
    }
}
