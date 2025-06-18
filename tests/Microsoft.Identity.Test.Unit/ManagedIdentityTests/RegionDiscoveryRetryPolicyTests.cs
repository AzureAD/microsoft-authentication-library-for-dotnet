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
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class RegionDiscoveryRetryPolicyTests : TestBase
    {
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();
        private IRegionDiscoveryProvider _regionDiscoveryProvider;
        private MockHttpManager _httpManager;
        private RequestContext _requestContext;
        private MockHttpAndServiceBundle _harness;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _harness = base.CreateTestHarness();
            _harness.ServiceBundle.Config.RetryPolicyFactory = _testRetryPolicyFactory;
            _httpManager = _harness.HttpManager;
            _regionDiscoveryProvider = new RegionAndMtlsDiscoveryProvider(_httpManager, true);
            _requestContext = new RequestContext(
                _harness.ServiceBundle,
                Guid.NewGuid(),
                null,
                CancellationToken.None);
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            _harness?.Dispose();
            base.TestCleanup();
        }

        [TestMethod]
        public async Task RegionDiscoveryFails500OnceThenSucceeds200Async()
        {
            // Initial request fails with 500
            _httpManager.AddRegionDiscoveryMockHandlerWithError(
                statusCode: HttpStatusCode.InternalServerError);

            // Final success
            _httpManager.AddRegionDiscoveryMockHandler(
                TestConstants.Region);

            var metadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"),
                _requestContext).ConfigureAwait(false);

            Assert.AreEqual(TestConstants.Region, metadata.PreferredNetwork.Split('.')[0]);

            const int NumRequests = 2; // initial request + 1 retry
            int requestsMade = NumRequests - _httpManager.QueueSize;
            Assert.AreEqual(NumRequests, requestsMade);
        }

        [TestMethod]
        public async Task RegionDiscoveryFails500PermanentlyAsync()
        {
            // Simulate permanent 500s (to trigger the maximum number of retries)
            const int Num500Errors = 1 + RegionDiscoveryRetryPolicy.NumRetries; // initial request + maximum number of retries
            for (int i = 0; i < Num500Errors; i++)
            {
                _httpManager.AddRegionDiscoveryMockHandlerWithError(
                    statusCode: HttpStatusCode.InternalServerError);
            }

            MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(
                async () => await _regionDiscoveryProvider.GetMetadataAsync(
                    new Uri("https://login.microsoftonline.com/common/"),
                    _requestContext).ConfigureAwait(false))
                .ConfigureAwait(false);

            Assert.IsNotNull(ex);

            int requestsMade = Num500Errors - _httpManager.QueueSize;
            Assert.AreEqual(Num500Errors, requestsMade);
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.NotFound, "404 Not Found")]
        [DataRow(HttpStatusCode.RequestTimeout, "408 Request Timeout")]
        public async Task RegionDiscoveryDoesNotRetryOnNonRetryableStatusCodesAsync(HttpStatusCode statusCode, string description)
        {
            // Add response with non-retryable status code
            _httpManager.AddRegionDiscoveryMockHandlerWithError(
                statusCode: statusCode);

            MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(
                async () => await _regionDiscoveryProvider.GetMetadataAsync(
                    new Uri("https://login.microsoftonline.com/common/"),
                    _requestContext).ConfigureAwait(false))
                .ConfigureAwait(false);

            Assert.IsNotNull(ex);
            Assert.AreEqual((int)statusCode, ex.StatusCode);

            const int NumRequests = 1; // initial request + 0 retries (non-retryable status codes should not trigger retry)
            int requestsMade = NumRequests - _httpManager.QueueSize;
            Assert.AreEqual(NumRequests, requestsMade, $"Expected single request without retry for {description}");
        }
    }
}
