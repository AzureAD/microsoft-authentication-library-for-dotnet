// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    [DeploymentItem("Resources\\local-imds-error-response.json")]
    [DeploymentItem("Resources\\local-imds-error-response-versions-missing.json")]
    public class RegionDiscoveryProviderTests : TestBase
    {
        private MockHttpAndServiceBundle _harness;
        private MockHttpManager _httpManager;
        private RequestContext _testRequestContext;
        private ApiEvent _apiEvent;
        private CancellationTokenSource _userCancellationTokenSource;
        private IRegionDiscoveryProvider _regionDiscoveryProvider;
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();

            _harness = base.CreateTestHarness();
            _harness.ServiceBundle.Config.RetryPolicyFactory = _testRetryPolicyFactory;
            _httpManager = _harness.HttpManager;
            _userCancellationTokenSource = new CancellationTokenSource();
            _testRequestContext = new RequestContext(
                _harness.ServiceBundle,
                Guid.NewGuid(),
                null,
                _userCancellationTokenSource.Token);
            _apiEvent = new ApiEvent(Guid.NewGuid());
            _apiEvent.ApiId = ApiEvent.ApiIds.AcquireTokenForClient;
            _testRequestContext.ApiEvent = _apiEvent;
            _regionDiscoveryProvider = new RegionAndMtlsDiscoveryProvider(_httpManager);
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "");
            _harness?.Dispose();
            _regionDiscoveryProvider = new RegionAndMtlsDiscoveryProvider(_httpManager);
            _httpManager.Dispose();
            base.TestCleanup();
        }

        [TestMethod]
        [DataRow("eastus", true, DisplayName = "Lowercase letters")]
        [DataRow("eastus2", true, DisplayName = "Letters and digits")]
        [DataRow("EastUs", true, DisplayName = "Mixed case")]
        [DataRow("TryAutoDetect", true, DisplayName = "Auto-detect sentinel is alphanumeric")]
        [DataRow("fake.com/x", false, DisplayName = "Path separator rejected")]
        [DataRow("fake.com?x", false, DisplayName = "Query separator rejected")]
        [DataRow("fake.com#x", false, DisplayName = "Fragment separator rejected")]
        [DataRow("east us", false, DisplayName = "Embedded space rejected")]
        [DataRow("east.us", false, DisplayName = "Dot rejected")]
        [DataRow("east@us", false, DisplayName = "At sign rejected")]
        [DataRow("eastus\n", false, DisplayName = "Trailing newline rejected")]
        [DataRow("eastus\r\n", false, DisplayName = "Trailing CRLF rejected")]
        [DataRow("east\nus", false, DisplayName = "Embedded newline rejected")]
        [DataRow("eastus\u00B2", false, DisplayName = "Unicode superscript digit rejected")]
        [DataRow("eastus\uFF10", false, DisplayName = "Unicode fullwidth digit rejected")]
        [DataRow("", false, DisplayName = "Empty rejected")]
        public void IsValidRegionName_EnforcesAlphanumericOnly(string region, bool expected)
        {
            // Act
            bool actual = RegionManager.IsValidRegionName(region);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task RegionWithSpecialCharactersFromEnvironmentVariableIsRejectedAsync()
        {
            // Arrange - a poisoned REGION_NAME must not be used as the region
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "fake.com/x");
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;
            _httpManager.AddRegionDiscoveryMockHandlerWithError(HttpStatusCode.NotFound);

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext)
                .ConfigureAwait(false);

            // Assert - the invalid env region is ignored, discovery falls through and fails over to global
            Assert.IsNull(regionalMetadata);
            Assert.AreNotEqual("fake.com/x", _testRequestContext.ApiEvent.RegionUsed);
        }

        [TestMethod]
        public async Task SuccessfulResponseFromEnvironmentVariableAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            _testRequestContext.ServiceBundle.Config.AzureRegion = null; // not configured

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext)
                .ConfigureAwait(false);

            Assert.IsNull(regionalMetadata);
        }

        [TestMethod]
        public async Task SuccessfulResponseFromLocalImdsAsync()
        {
            AddMockedResponse(CreateImdsComputeResponse(TestConstants.Region));

            _testRequestContext.ServiceBundle.Config.AzureRegion =
                ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
        }

        [TestMethod]
        public void MultiThreadSuccessfulResponseFromLocalImds_HasOnlyOneImdsCall()
        {
            const int MaxThreadCount = 5;
            // add the mock response only once and call it 5 times on multiple threads
            // if the http mock is called more than once, it will fail in dispose as queue will be non-empty
            AddMockedResponse(CreateImdsComputeResponse(TestConstants.Region));
            int threadCount = MaxThreadCount;
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates - acceptable risk (crash the test proj)
            var result = Parallel.For(0, MaxThreadCount, async (i) =>
            {
                try
                {
                    _testRequestContext.ServiceBundle.Config.AzureRegion =
                        ConfidentialClientApplication.AttemptRegionDiscovery;

                    InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                        new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

                    ValidateInstanceMetadata(regionalMetadata);
                }
                catch (Exception ex)
                {
#pragma warning disable MSTEST0040 // Assert inside async void - acceptable risk (crash the test process)
                    Assert.Fail(ex.Message);
#pragma warning restore MSTEST0040
                }
                finally
                {
                    Interlocked.Decrement(ref threadCount);
                }
            });
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates

            while (threadCount != 0)
            {
                Thread.Sleep(100);
                Thread.Yield();
            }
            Assert.IsTrue(result.IsCompleted);
        }

        [TestMethod]
        public async Task FetchRegionFromLocalImdsThenGetMetadataFromCacheAsync()
        {
            AddMockedResponse(CreateImdsComputeResponse(TestConstants.Region));

            _testRequestContext.ServiceBundle.Config.AzureRegion =
               ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata =
                await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);

            //get metadata from the instance metadata cache
            regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
        }

        [TestMethod]
        public async Task SuccessfulResponseFromUserProvidedRegionDoesNotCallImdsAsync()
        {
            _testRequestContext.ServiceBundle.Config.AzureRegion = TestConstants.Region;
            RegionManager.ResetStaticCacheForTest();
            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionAndMtlsDiscoveryProvider(_httpManager);
            InstanceDiscoveryMetadataEntry regionalMetadata = await regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual($"centralus.{RegionAndMtlsDiscoveryProvider.PublicEnvForRegional}", regionalMetadata.PreferredNetwork);

            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.None, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.None, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsNull(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason);

            // Verify no IMDS request was made for the explicit region.
            Assert.AreEqual(0, _httpManager.QueueSize);
        }

        [TestMethod]
        public async Task ResponseFromUserProvidedRegionSkipsEnvDetectionAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);
            _testRequestContext.ServiceBundle.Config.AzureRegion = TestConstants.Region;

            //            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager);
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual($"centralus.{RegionAndMtlsDiscoveryProvider.PublicEnvForRegional}", regionalMetadata.PreferredNetwork);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.None, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.None, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsNull(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason);
        }

        [TestMethod]
        public async Task ResponseFromUserProvidedRegionSkipsRegionMismatchDetectionAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "detectedregion");
            _testRequestContext.ServiceBundle.Config.AzureRegion = "userregion";

            //IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager, new NetworkCacheMetadataProvider());
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"),
                _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual($"userregion.{RegionAndMtlsDiscoveryProvider.PublicEnvForRegional}", regionalMetadata.PreferredNetwork);
            Assert.AreEqual("userregion", _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.None, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.None, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsNull(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason);
        }

        [TestMethod]
        public async Task RegionInEnvVariableIsProperlyTransformedAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "Region With Spaces");
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"),
                _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual($"regionwithspaces.{RegionAndMtlsDiscoveryProvider.PublicEnvForRegional}", regionalMetadata.PreferredNetwork);
            Assert.AreEqual("regionwithspaces", _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.EnvVariable, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.AutodetectSuccess, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsNull(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason);
        }

        [TestMethod]
        public async Task SuccessfulResponseFromRegionalizedAuthorityAsync()
        {
            var regionalizedAuthority = new Uri($"https://{TestConstants.Region}.{RegionAndMtlsDiscoveryProvider.PublicEnvForRegional}/common/");
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            // In the instance discovery flow, GetMetadataAsync is always called with a known authority first, then with regionalized.
            await _regionDiscoveryProvider.GetMetadataAsync(new Uri(TestConstants.AuthorityCommonTenant), _testRequestContext).ConfigureAwait(false);
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(regionalizedAuthority, _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.EnvVariable, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.AutodetectSuccess, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsNull(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason);
        }

        [TestMethod]
        public async Task InvalidRegionEnvVariableAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "invalid`region");

            AddMockedResponse(CreateImdsComputeResponse(TestConstants.Region)); // IMDS will return a valid region

            _testRequestContext.ServiceBundle.Config.AzureRegion =
                ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
        }

        [TestMethod]
        [DataRow("Region with spaces")]
        [DataRow("invalid`region")]
        public async Task InvalidImdsAsync(string region)
        {
            AddMockedResponse(CreateImdsComputeResponse(region)); // IMDS will return an invalid region

            _testRequestContext.ServiceBundle.Config.AzureRegion =
                ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");
        }

        [TestMethod]
        public async Task NonPublicCloudTestAsync()
        {
            // Arrange
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.someenv.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual("centralus.login.someenv.com", regionalMetadata.PreferredNetwork);
        }

        [TestMethod]
        public async Task ResponseMissingRegionFromLocalImdsAsync()
        {
            // Arrange
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(string.Empty));
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");
            Assert.IsNull(_testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.Contains(TestConstants.RegionAutoDetectOkFailureMessage, _testRequestContext.ApiEvent.RegionDiscoveryFailureReason);
        }

        [TestMethod]
        [DataRow("{\"vmId\":\"11111111-1111-1111-1111-111111111111\"}", DisplayName = "Missing location field")]
        [DataRow("{\"location\":null}", DisplayName = "Null location field")]
        [DataRow("{ this is not valid json", DisplayName = "Malformed JSON")]
        public async Task ResponseWithUnusableBodyFromLocalImdsAsync(string responseBody)
        {
            // Arrange - 200 OK with a non-empty but unusable body (missing/null location or malformed JSON)
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(responseBody));
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");
            Assert.IsNull(_testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);
            // Unusable bodies funnel into the same "status code OK or an empty response" failure reason.
            Assert.Contains(TestConstants.RegionAutoDetectOkFailureMessage, _testRequestContext.ApiEvent.RegionDiscoveryFailureReason);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.NotFound, 0, TestConstants.RegionAutoDetectNotFoundFailureMessage)]  // No retries for 404 errors
        [DataRow(HttpStatusCode.InternalServerError, TestRegionDiscoveryRetryPolicy.NumRetries, TestConstants.RegionAutoDetectInternalServerErrorFailureMessage)]
        public async Task ErrorResponseFromLocalImdsAsync(
            HttpStatusCode statusCode,
            int expectedRetries,
            string expectedFailureMessage)
        {
            for (int i = 0; i < (1 + expectedRetries); i++)
            {
                AddMockedResponse(MockHelpers.CreateNullMessage(statusCode));
            }
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.
                 GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext)
                 .ConfigureAwait(false);

            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");

            Assert.IsNull(_testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.Contains(expectedFailureMessage, _testRequestContext.ApiEvent.RegionDiscoveryFailureReason);

            // Verify all mock responses were consumed
            Assert.AreEqual(0, _httpManager.QueueSize);
        }

        [TestMethod]
        public async Task UpdateImdsApiVersionWhenCurrentVersionExpiresForImdsAsync()
        {
            // Arrange
            // Two different api-versions appear by design:
            //   1. The first call uses the default api-version (2021-02-01) and is rejected with 400 BadRequest.
            //   2. MSAL then probes IMDS for supported versions; the error response's "newest-versions"
            //      yields 2020-10-01, and the retry succeeds using that negotiated api-version.
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest));
            AddMockedResponse(MockHelpers.CreateFailureMessage(System.Net.HttpStatusCode.BadRequest, File.ReadAllText(
                        ResourceHelper.GetTestResourceRelativePath("local-imds-error-response.json"))), expectedParams: false);
            AddMockedResponse(CreateImdsComputeResponse(TestConstants.Region), apiVersion: "2020-10-01");
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            ValidateInstanceMetadata(regionalMetadata);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.Imds, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.AutodetectSuccess, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsNull(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason);
        }

        [TestMethod]
        public async Task UpdateApiversionFailsWithEmptyResponseBodyAsync()
        {
            // Arrange
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest));
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest), expectedParams: false);
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");
            Assert.IsNull(_testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.Contains(TestConstants.RegionDiscoveryNotSupportedErrorMessage, _testRequestContext.ApiEvent.RegionDiscoveryFailureReason);
        }

        [TestMethod]
        public async Task UpdateApiversionFailsWithNoNewestVersionsAsync()
        {
            // Arrange
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest));
            AddMockedResponse(MockHelpers.CreateFailureMessage(System.Net.HttpStatusCode.BadRequest, File.ReadAllText(
                        ResourceHelper.GetTestResourceRelativePath("local-imds-error-response-versions-missing.json"))), expectedParams: false);
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");

            Assert.IsNull(_testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.Contains(TestConstants.RegionDiscoveryNotSupportedErrorMessage, _testRequestContext.ApiEvent.RegionDiscoveryFailureReason);
        }

        [TestMethod]
        public async Task RegionDiscoveryFails500OnceThenSucceeds200Async()
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(HttpStatusCode.InternalServerError));
            AddMockedResponse(CreateImdsComputeResponse(TestConstants.Region));

            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.Imds, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.AutodetectSuccess, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsNull(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason);

            const int NumRequests = 2; // initial request + one retry
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
                AddMockedResponse(MockHelpers.CreateNullMessage(HttpStatusCode.InternalServerError));
            }

            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNull(regionalMetadata, "Discovery should fail after max retries");
            Assert.IsNull(_testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);

            const int NumRequests = Num500Errors; // initial request + three retries
            int requestsMade = NumRequests - _httpManager.QueueSize;
            Assert.AreEqual(NumRequests, requestsMade);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.NotFound)]
        [DataRow(HttpStatusCode.RequestTimeout)]
        public async Task RegionDiscoveryDoesNotRetryOnNonRetryableStatusCodesAsync(HttpStatusCode statusCode)
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(statusCode));

            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNull(regionalMetadata, "Discovery should fail and not retry");
            Assert.IsNull(_testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);

            const int NumRequests = 1; // initial request + 0 retries (non-retryable status codes should not trigger retry)
            int requestsMade = NumRequests - _httpManager.QueueSize;
            Assert.AreEqual(NumRequests, requestsMade);
        }

        private void AddMockedResponse(HttpResponseMessage responseMessage, string apiVersion = "2021-02-01", bool expectedParams = true)
        {
            var queryParams = new Dictionary<string, string>();

            if (expectedParams)
            {
                queryParams.Add("api-version", apiVersion);

                _httpManager.AddMockHandler(
                   new MockHttpMessageHandler
                   {
                       ExpectedMethod = HttpMethod.Get,
                       ExpectedUrl = TestConstants.ImdsUrl,
                       ExpectedRequestHeaders = new Dictionary<string, string>
                        {
                            { "Metadata", "true" }
                        },
                       ExpectedQueryParams = queryParams,
                       ResponseMessage = responseMessage
                   });
            }
            else
            {
                _httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = TestConstants.ImdsUrl,
                        ExpectedRequestHeaders = new Dictionary<string, string>
                            {
                            { "Metadata", "true" }
                            },
                        ResponseMessage = responseMessage
                    });
            }
        }

        private static HttpResponseMessage CreateImdsComputeResponse(string location)
        {
            return MockHelpers.CreateSuccessResponseMessage($"{{\"location\":\"{location}\"}}");
        }

        private void ValidateInstanceMetadata(InstanceDiscoveryMetadataEntry entry, string region = "centralus")
        {
            InstanceDiscoveryMetadataEntry expectedEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { $"{region}.{RegionAndMtlsDiscoveryProvider.PublicEnvForRegional}", "login.microsoftonline.com" },
                PreferredCache = "login.microsoftonline.com",
                PreferredNetwork = $"{region}.{RegionAndMtlsDiscoveryProvider.PublicEnvForRegional}"
            };

            CollectionAssert.AreEquivalent(expectedEntry.Aliases, entry.Aliases);
            Assert.AreEqual(expectedEntry.PreferredCache, entry.PreferredCache);
            Assert.AreEqual(expectedEntry.PreferredNetwork, entry.PreferredNetwork);
        }
    }
}
