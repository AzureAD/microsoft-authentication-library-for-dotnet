// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Throttling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Client.TelemetryCore.Internal.Events.ApiEvent;

namespace Microsoft.Identity.Test.Unit.TelemetryTests
{
    [TestClass]
    public class RegionalTelemetryTests : TestBase
    {
        private MockHttpAndServiceBundle _harness;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();

            _harness = CreateTestHarness();

        }

        [TestCleanup]
        public override void TestCleanup()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, null);
            _harness?.Dispose();
            base.TestCleanup();
        }

        /// <summary>
        /// 1.  Acquire Token For Client with Region successfully
        ///        Current_request = 4 | ATC_ID, 0, centralus, 3, 4 | 
        ///        Last_request = 4 | 0 | | |
        /// 
        /// 2. Acquire Token for client with Region -> HTTP error 503 (Service Unavailable)
        ///
        ///        Current_request = 4 | ATC_ID, 1, centralus, 2, 4 |
        ///        Last_request = 4 | 0 | | |
        ///
        /// 3. Acquire Token For Client with Region -> successful
        ///
        /// Sent to the server - 
        ///        Current_request = 4 | ATC_ID, 1, centralus, 2, 4 |
        /// </summary>
        [TestMethod]
        public async Task TelemetryAcceptanceTestAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            Trace.WriteLine("Step 1. Acquire Token For Client with region successful");
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.Success).ConfigureAwait(false);
            AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.EnvVariable.ToString("D"),
                RegionOutcome.AutodetectSuccess.ToString("D"));

            Trace.WriteLine("Step 2. Acquire Token For Client -> HTTP 5xx error (i.e. AAD is down)");
            result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.AADUnavailableError).ConfigureAwait(false);

            // we can assert telemetry here, as it will be sent to AAD. However, AAD is down, so it will not record it.
            AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.Cache.ToString("D"),
                RegionOutcome.AutodetectSuccess.ToString("D"));          

            // the 5xx error puts MSAL in a throttling state, so "wait" until this clears
            _harness.ServiceBundle.ThrottlingManager.SimulateTimePassing(
                HttpStatusProvider.s_throttleDuration.Add(TimeSpan.FromSeconds(1)));

            Trace.WriteLine("Step 3. Acquire Token For Client -> Success");
            result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.Success, true).ConfigureAwait(false);

            AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.Cache.ToString("D"),
                RegionOutcome.AutodetectSuccess.ToString("D"));
        }

        /// <summary>
        /// Acquire token for client with serialized token cache successfully
        ///    Current_request = 4 | ATC_ID, 0, centralus, 3, 4 |
        ///    Last_request = 4 | 0 | | |
        /// </summary>
        [TestMethod]
        public async Task TelemetrySerializedTokenCacheTestAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            Trace.WriteLine("Acquire token for client with token serialization.");
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.Success, serializeCache: true)
                .ConfigureAwait(false);
            AssertCurrentTelemetry(result.HttpRequest,
                ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.EnvVariable.ToString("D"),
                RegionOutcome.AutodetectSuccess.ToString("D"),
                isCacheSerialized: true);
        }

        [TestMethod]
        public async Task TelemetryAutoDiscoveryFailsTestsAsync()
        {
            Trace.WriteLine("Acquire token for client with region detection fails.");
            _harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Get, TestConstants.ImdsUrl);
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.FallbackToGlobal).ConfigureAwait(false);
            AssertCurrentTelemetry(
                result.HttpRequest,
                ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.FailedAutoDiscovery.ToString("D"),
                RegionOutcome.FallbackToGlobal.ToString("D"),
                isCacheSerialized: false,
                region: "");
        }

        [TestMethod]
        public async Task TelemetryUserProvidedRegionAutoDiscoveryFailsTestsAsync()
        {
            Trace.WriteLine("Acquire token for client with region provided by user and region detection fails.");
            _harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Get, TestConstants.ImdsUrl);
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.UserProvidedRegion).ConfigureAwait(false);
            AssertCurrentTelemetry(
                result.HttpRequest,
                ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.FailedAutoDiscovery.ToString("D"),
                RegionOutcome.UserProvidedAutodetectionFailed.ToString("D"),
                isCacheSerialized: false,
                region: TestConstants.Region);
        }

        /// <summary>
        /// Acquire token for client with regionToUse when auto region discovery passes with region same as regionToUse
        ///    Current_request = 4 | ATC_ID, 0, centralus, 3, 1 |
        ///    Last_request = 4 | 0 | | |
        /// </summary>
        [TestMethod]
        public async Task TelemetryUserProvidedRegionAutoDiscoverRegionSameTestsAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            Trace.WriteLine("Acquire token for client with region provided by user and region detected is same as regionToUse.");
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.UserProvidedRegion).ConfigureAwait(false);
            AssertCurrentTelemetry(result.HttpRequest,
                ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.EnvVariable.ToString("D"),
                RegionOutcome.UserProvidedValid.ToString("D"),
                isCacheSerialized: false,
                region: TestConstants.Region);
        }

        /// <summary>
        /// Acquire token for client with regionToUse when auto region discovery passes with region different from regionToUse
        ///    Current_request = 4 | ATC_ID, 0, centralus, 3, 1 |
        ///    Last_request = 4 | 0 | | |
        /// </summary>
        [TestMethod]
        public async Task TelemetryUserProvidedRegionAutoDiscoverRegionMismatchTestsAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            Trace.WriteLine("Acquire token for client with region provided by user and region detected mismatches regionToUse.");
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.UserProvidedInvalidRegion).ConfigureAwait(false);
            AssertCurrentTelemetry(result.HttpRequest,
                ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.EnvVariable.ToString("D"),
                RegionOutcome.UserProvidedInvalid.ToString("D"),
                isCacheSerialized: false,
                region: TestConstants.InvalidRegion);
        }

        private enum AcquireTokenForClientOutcome
        {
            Success,
            UserProvidedRegion,
            UserProvidedInvalidRegion,
            AADUnavailableError,
            FallbackToGlobal
        }

        private async Task<(HttpRequestMessage HttpRequest, Guid Correlationid)> RunAcquireTokenForClientAsync(
            AcquireTokenForClientOutcome outcome, bool forceRefresh = false, bool serializeCache = false)
        {
            MockHttpMessageHandler tokenRequestHandler;
            Guid correlationId;

            switch (outcome)
            {
                case AcquireTokenForClientOutcome.Success:

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                       .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.TenantId, false)
                       .WithClientSecret(TestConstants.ClientSecret)
                       .WithHttpManager(_harness.HttpManager)
                       .WithAzureRegion()
                       .WithExperimentalFeatures(true)
                       .BuildConcrete();

                    if (serializeCache)
                    {
                        InMemoryTokenCache mem = new InMemoryTokenCache();
                        mem.Bind(app.AppTokenCache);
                    }

                    tokenRequestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                        authority: TestConstants.AuthorityRegional,
                        responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());
                    var authResult = await app
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithForceRefresh(forceRefresh)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                    correlationId = authResult.CorrelationId;
                    break;

                case AcquireTokenForClientOutcome.FallbackToGlobal:
                    _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                    tokenRequestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                        authority: TestConstants.AuthorityTenant,
                        responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                    var app2 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                     .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.TenantId, false)
                     .WithClientSecret(TestConstants.ClientSecret)
                     .WithHttpManager(_harness.HttpManager)
                     .WithAzureRegion()
                     .WithExperimentalFeatures(true)
                     .BuildConcrete();

                    authResult = await app2
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithForceRefresh(forceRefresh)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                    correlationId = authResult.CorrelationId;
                    break;

                case AcquireTokenForClientOutcome.UserProvidedRegion:

                    tokenRequestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                        authority: TestConstants.AuthorityRegional,
                        responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                    var app3 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                     .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.TenantId, false)
                     .WithClientSecret(TestConstants.ClientSecret)
                     .WithHttpManager(_harness.HttpManager)
                     .WithAzureRegion(TestConstants.Region)
                     .WithExperimentalFeatures(true)
                     .BuildConcrete();
                    authResult = await app3
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithForceRefresh(forceRefresh)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                    correlationId = authResult.CorrelationId;
                    break;

                case AcquireTokenForClientOutcome.UserProvidedInvalidRegion:

                    tokenRequestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost
                        (authority: TestConstants.AuthorityRegionalInvalidRegion,
                        responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                    var app4 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                     .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.TenantId, false)
                     .WithClientSecret(TestConstants.ClientSecret)
                     .WithHttpManager(_harness.HttpManager)
                     .WithAzureRegion(TestConstants.InvalidRegion)
                     .WithExperimentalFeatures(true)
                     .BuildConcrete();
                    authResult = await app4
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithForceRefresh(forceRefresh)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                    correlationId = authResult.CorrelationId;
                    break;

                case AcquireTokenForClientOutcome.AADUnavailableError:
                    correlationId = Guid.NewGuid();

                    var app5 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.TenantId, false)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(_harness.HttpManager)
                    .WithAzureRegion()
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                    tokenRequestHandler = new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateFailureMessage(
                           System.Net.HttpStatusCode.GatewayTimeout, "gateway timeout")
                    };
                    var tokenRequestHandler2 = new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateFailureMessage(
                         System.Net.HttpStatusCode.GatewayTimeout, "gateway timeout")
                    };

                    // 2 of these are needed because MSAL has a "retry once" policy for 5xx errors
                    _harness.HttpManager.AddMockHandler(tokenRequestHandler2);
                    _harness.HttpManager.AddMockHandler(tokenRequestHandler);

                    var serviceEx = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                        app5
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithForceRefresh(true)
                        .WithCorrelationId(correlationId)
                        .ExecuteAsync())
                        .ConfigureAwait(false);

                    break;

                default:
                    throw new NotImplementedException();
            }

            Assert.AreEqual(0, _harness.HttpManager.QueueSize);

            return (tokenRequestHandler?.ActualRequestMessage, correlationId);
        }

        private static void AssertCurrentTelemetry(
            HttpRequestMessage requestMessage,
            ApiIds apiId,
            string regionSource,
            string regionOutcome,
            string region = TestConstants.Region,
            bool isCacheSerialized = false)
        {
            string[] telemetryCategories = requestMessage.Headers.GetValues(
                TelemetryConstants.XClientCurrentTelemetry).Single().Split('|');

            Assert.AreEqual(3, telemetryCategories.Length);
            Assert.AreEqual(1, telemetryCategories[0].Split(',').Length); // version
            Assert.AreEqual(5, telemetryCategories[1].Split(',').Length); // api_id, cache_info, region_used, region_source, region_outcome
            Assert.AreEqual(5, telemetryCategories[2].Split(',').Length); // platform_fields

            Assert.AreEqual(TelemetryConstants.HttpTelemetrySchemaVersion.ToString(), telemetryCategories[0]); // version

            Assert.AreEqual(
                apiId.ToString("D"),
                telemetryCategories[1].Split(',')[0]); // current_api_id
            Assert.AreEqual(
                region,
                telemetryCategories[1].Split(',')[2]); // region_used
            Assert.AreEqual(
                regionSource,
                telemetryCategories[1].Split(',')[3]); // region_source
            Assert.AreEqual(
                regionOutcome,
                telemetryCategories[1].Split(',')[4]); // region_outcome

            Assert.AreEqual(isCacheSerialized ? "1" : "0", telemetryCategories[2].Split(',')[0]);
        }
    }
}
