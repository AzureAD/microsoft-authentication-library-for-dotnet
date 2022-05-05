// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
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
    public class BaseRegionTelemetryTests : TestBase
    {
        internal MockHttpAndServiceBundle _harness;
        protected bool _isSingleThread = true;

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
            if (_isSingleThread)
            {
                _harness?.Dispose();
            }
            base.TestCleanup();
        }

        protected enum AcquireTokenForClientOutcome
        {
            Success,
            UserProvidedRegion,
            UserProvidedInvalidRegion,
            AADUnavailableError,
            FallbackToGlobal
        }

        protected async Task<(HttpRequestMessage HttpRequest, Guid Correlationid)> RunAcquireTokenForClientAsync(
            AcquireTokenForClientOutcome outcome, bool forceRefresh = false, bool serializeCache = false, LogCallback logCallback = null)
        {
            MockHttpMessageHandler tokenRequestHandler = null;
            Guid correlationId = default;

            switch (outcome)
            {
                case AcquireTokenForClientOutcome.Success:

                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                       .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.TenantId, false)
                       .WithClientSecret(TestConstants.ClientSecret)
                       .WithHttpManager(_harness.HttpManager)
                       .WithAzureRegion()
                       .WithLogging(logCallback)
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

        internal static void AssertCurrentTelemetry(
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
            Assert.AreEqual(2, telemetryCategories[2].Split(',').Length); // platform_fields

            Assert.AreEqual(TelemetryConstants.HttpTelemetrySchemaVersion, telemetryCategories[0]); // version

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

        internal static void AssertPreviousTelemetry(
           HttpRequestMessage requestMessage,
           int expectedSilentCount,
           ApiIds[] expectedFailedApiIds = null,
           Guid[] expectedCorrelationIds = null,
           string[] expectedErrors = null,
           string[] expectedRegions = null,
           string[] expectedRegionSources = null)
        {
            expectedFailedApiIds = expectedFailedApiIds ?? new ApiIds[0];
            expectedCorrelationIds = expectedCorrelationIds ?? new Guid[0];
            expectedErrors = expectedErrors ?? new string[0];
            expectedRegions = expectedRegions ?? new string[0];
            expectedRegionSources = expectedRegionSources ?? new string[0];

            var actualHeader = ParseLastRequestHeader(requestMessage);

            Assert.AreEqual(expectedSilentCount, actualHeader.SilentCount);
            CoreAssert.AreEqual(actualHeader.FailedApis.Length, actualHeader.CorrelationIds.Length, actualHeader.Errors.Length);

            CollectionAssert.AreEqual(
                expectedFailedApiIds.Select(apiId => ((int)apiId).ToString(CultureInfo.InvariantCulture)).ToArray(),
                actualHeader.FailedApis);

            CollectionAssert.AreEqual(
                expectedCorrelationIds.Select(g => g.ToString()).ToArray(),
                actualHeader.CorrelationIds);

            CollectionAssert.AreEqual(
                expectedErrors,
                actualHeader.Errors);

            CollectionAssert.AreEqual(
                expectedRegions,
                actualHeader.Regions);

            CollectionAssert.AreEqual(
                expectedRegionSources,
                actualHeader.RegionSources);
        }

        private static (int SilentCount, string[] FailedApis, string[] CorrelationIds, string[] Errors, string[] Regions, string[] RegionSources) ParseLastRequestHeader(HttpRequestMessage requestMessage)
        {
            // schema_version | silent_succesful_count | failed_requests | errors | platform_fields
            // where a failed_request is "api_id, correlation_id"
            string lastTelemetryHeader = requestMessage.Headers.GetValues(
               TelemetryConstants.XClientLastTelemetry).Single();
            var lastRequestParts = lastTelemetryHeader.Split('|');

            Assert.AreEqual(5, lastRequestParts.Length); //  2 | 1 | | |
            Assert.AreEqual(TelemetryConstants.HttpTelemetrySchemaVersion, lastRequestParts[0]); // version

            int actualSuccessfullSilentCount = int.Parse(lastRequestParts[1], CultureInfo.InvariantCulture);

            string[] actualFailedApiIds = lastRequestParts[2]
                .Split(',')
                .Where((item, index) => index % 2 == 0)
                .Where(it => !string.IsNullOrEmpty(it))
                .ToArray();
            string[] correlationIds = lastRequestParts[2]
                .Split(',')
                .Where((item, index) => index % 2 != 0)
                .Where(it => !string.IsNullOrEmpty(it))
                .ToArray();

            string[] actualErrors = lastRequestParts[3]
                .Split(',')
                .Where(it => !string.IsNullOrEmpty(it))
                .ToArray();

            string[] regions = lastRequestParts[4]
                .Split(',')
                .Where((item, index) => index % 2 == 0)
                .Where(it => !string.IsNullOrEmpty(it))
                .ToArray();
            string[] regionSources = lastRequestParts[4]
                .Split(',')
                .Where((item, index) => index % 2 != 0)
                .Where(it => !string.IsNullOrEmpty(it))
                .ToArray();

            return (actualSuccessfullSilentCount, actualFailedApiIds, correlationIds, actualErrors, regions, regionSources);
        }

    }
}
