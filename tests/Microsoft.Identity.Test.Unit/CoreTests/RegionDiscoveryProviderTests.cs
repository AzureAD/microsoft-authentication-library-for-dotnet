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
            _regionDiscoveryProvider = new RegionAndMtlsDiscoveryProvider(_httpManager, true);
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "");
            _harness?.Dispose();
            _regionDiscoveryProvider = new RegionAndMtlsDiscoveryProvider(_httpManager, true);
            _httpManager.Dispose();
            base.TestCleanup();
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
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region));

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
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region));
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
                    Assert.Fail(ex.Message);
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
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region));

            _testRequestContext.ServiceBundle.Config.AzureRegion =
               ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata =
                await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);

            //get metadata from the instance metadata cache
            regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.NotFound, 0, TestConstants.RegionAutoDetectNotFoundFailureMessage)]  // No retries for 404 errors
        [DataRow(HttpStatusCode.InternalServerError, TestRegionDiscoveryRetryPolicy.NumRetries, TestConstants.RegionAutoDetectInternalServerErrorFailureMessage)]
        public async Task SuccessfulResponseFromUserProvidedRegionAsync(
            HttpStatusCode statusCode,
            int expectedRetries,
            string expectedFailureMessage)
        {
            // Add multiple mock responses: initial request + max retry attempts
            for (int i = 0; i < (1 + expectedRetries); i++)
            {
                AddMockedResponse(MockHelpers.CreateNullMessage(statusCode));
            }

            _testRequestContext.ServiceBundle.Config.AzureRegion = TestConstants.Region;

            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionAndMtlsDiscoveryProvider(_httpManager, true);
            InstanceDiscoveryMetadataEntry regionalMetadata = await regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual($"centralus.{RegionAndMtlsDiscoveryProvider.PublicEnvForRegional}", regionalMetadata.PreferredNetwork);

            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.UserProvidedAutodetectionFailed, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsTrue(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason.Contains(expectedFailureMessage));

            // Verify all mock responses were consumed
            Assert.AreEqual(0, _httpManager.QueueSize);
        }

        [TestMethod]
        public async Task ResponseFromUserProvidedRegionSameAsRegionDetectedAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);
            _testRequestContext.ServiceBundle.Config.AzureRegion = TestConstants.Region;

            //            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager);
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual($"centralus.{RegionAndMtlsDiscoveryProvider.PublicEnvForRegional}", regionalMetadata.PreferredNetwork);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.EnvVariable, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.UserProvidedValid, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsNull(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason);
        }

        [TestMethod]
        public async Task ResponseFromUserProvidedRegionDifferentFromRegionDetectedAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "detected_region");
            _testRequestContext.ServiceBundle.Config.AzureRegion = "user_region";

            //IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager, new NetworkCacheMetadataProvider());
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"),
                _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual($"user_region.{RegionAndMtlsDiscoveryProvider.PublicEnvForRegional}", regionalMetadata.PreferredNetwork);
            Assert.AreEqual("user_region", _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.EnvVariable, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.UserProvidedInvalid, _testRequestContext.ApiEvent.RegionOutcome);
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

            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region)); // IMDS will return a valid region

            _testRequestContext.ServiceBundle.Config.AzureRegion =
                ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
        }

        [DataTestMethod]
        [DataRow("Region with spaces")]
        [DataRow("invalid`region")]
        public async Task InvalidImdsAsync(string region)
        {
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(region)); // IMDS will return an invalid region

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
            Assert.AreEqual(null, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsTrue(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason.Contains(TestConstants.RegionAutoDetectOkFailureMessage));
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.NotFound, 0, TestConstants.RegionAutoDetectNotFoundFailureMessage)]  // No retries for 404 errors
        [DataRow(HttpStatusCode.InternalServerError, TestRegionDiscoveryRetryPolicy.NumRetries, TestConstants.RegionAutoDetectInternalServerErrorFailureMessage)]
        public async Task ErrorResponseFromLocalImdsAsync(
            HttpStatusCode statusCode,
            int expectedRetries,
            string expectedFailureMessage)
        {
            // Add multiple mock responses: initial request + max retry attempts
            for (int i = 0; i < (1 + expectedRetries); i++)
            {
                AddMockedResponse(MockHelpers.CreateNullMessage(statusCode));
            }
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.
                 GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext)
                 .ConfigureAwait(false);

            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");

            Assert.AreEqual(null, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsTrue(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason.Contains(expectedFailureMessage));

            // Verify all mock responses were consumed
            Assert.AreEqual(0, _httpManager.QueueSize);
        }

        [TestMethod]
        public async Task UpdateImdsApiVersionWhenCurrentVersionExpiresForImdsAsync()
        {
            // Arrange
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest));
            AddMockedResponse(MockHelpers.CreateFailureMessage(System.Net.HttpStatusCode.BadRequest, File.ReadAllText(
                        ResourceHelper.GetTestResourceRelativePath("local-imds-error-response.json"))), expectedParams: false);
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region), apiVersion: "2020-10-01");
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
            Assert.AreEqual(null, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsTrue(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason.Contains(TestConstants.RegionDiscoveryNotSupportedErrorMessage));
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

            Assert.AreEqual(null, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsTrue(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason.Contains(TestConstants.RegionDiscoveryNotSupportedErrorMessage));
        }

        protected void AddMockedResponse(HttpResponseMessage responseMessage, string apiVersion = "2020-06-01", bool expectedParams = true, bool throwException = false)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ExpectedUrl = TestConstants.ImdsUrl,
                ExpectedRequestHeaders = new Dictionary<string, string>
                {
                    { "Metadata", "true" }
                },
                ResponseMessage = responseMessage
            };

            if (expectedParams)
            {
                var queryParams = new Dictionary<string, string>
                {
                    { "api-version", apiVersion },
                    { "format", "text" }
                };
                handler.ExpectedQueryParams = queryParams;
            }

            if (throwException)
            {
                handler.ExceptionToThrow = new TaskCanceledException();
            }

            _httpManager.AddMockHandler(handler);
        }

        internal RequestContext GetTestRequestContext() => _testRequestContext;
        internal IRegionDiscoveryProvider GetRegionDiscoveryProvider() => _regionDiscoveryProvider;
        internal MockHttpManager GetHttpManager() => _httpManager;

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

        [TestMethod]
        public async Task RegionDiscoveryRetriesOn500ThenSucceedsAsync()
        {
            // Simulate three 500s (initial request + two retries), then a successful response
            const int Num500Errors = 3;
            for (int i = 0; i < Num500Errors; i++)
            {
                AddMockedResponse(MockHelpers.CreateNullMessage(HttpStatusCode.InternalServerError));
            }

            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region));
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.Imds, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.AutodetectSuccess, _testRequestContext.ApiEvent.RegionOutcome);
            Assert.IsNull(_testRequestContext.ApiEvent.RegionDiscoveryFailureReason);

            const int NumRequests = Num500Errors + 1; // initial request + two retries + successful response
            int requestsMade = NumRequests - _httpManager.QueueSize;
            Assert.AreEqual(NumRequests, requestsMade);
        }

        [TestMethod]
        public async Task RegionDiscoveryRetriesMaximumAttemptsAsync()
        {
            // Simulate four 500s (initial request + three retries)
            const int Num500Errors = 1 + RegionDiscoveryRetryPolicy.NumRetries;
            for (int i = 0; i < Num500Errors; i++)
            {
                AddMockedResponse(MockHelpers.CreateNullMessage(HttpStatusCode.InternalServerError));
            }

            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNull(regionalMetadata, "Discovery should fail after max retries");
            Assert.AreEqual(null, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual(RegionAutodetectionSource.FailedAutoDiscovery, _testRequestContext.ApiEvent.RegionAutodetectionSource);
            Assert.AreEqual(RegionOutcome.FallbackToGlobal, _testRequestContext.ApiEvent.RegionOutcome);

            const int NumRequests = Num500Errors; // initial request + three retries
            int requestsMade = NumRequests - _httpManager.QueueSize;
            Assert.AreEqual(NumRequests, requestsMade);
        }
    }
}
