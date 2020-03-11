// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Client.TelemetryCore.Internal.Events.ApiEvent;

namespace Microsoft.Identity.Test.Unit.TelemetryTests
{
    [TestClass]
    public class HttpTelemetryTests : TestBase
    {
        private MockHttpAndServiceBundle _harness;
        private PublicClientApplication _app;
        /// <summary>
        /// 1.  Acquire Token Interactive successfully
        ///        Current_request = 2 | ATI_ID, 0 |
        ///        Last_request = NULL
        /// 2. Acquire token silent with AT served from cache ... no calls to /token endpoint
        ///        
        /// 3. Acquire token silent with AT not served from cache (AT expired)
        ///         Current_request = 2 | ATS_ID, 0 |
        ///         Last_request = 2 | 1 | | |
        ///         
        /// 4. Acquire Token silent with force_refresh = true -> error invalid_client
        /// Sent to server - 
        ///         Current_request = 2 | ATS_ID, 1 |
        ///         Last_request = 2 | 0 | | |
        ///         
        /// State of client after error response is returned – (the successful silent request counter was flushed, last_request is reset, and now we add the error from step 4)
        ///         Last_request = 2 | 0 | ATS_ID, Corr_step_4 | invalid_client |
        /// 
        /// 5. Acquire Token silent with force_refresh = true -> error interaction_required
        /// Sent to the server - 
        ///         Current_request = 2 | ATS_ID, 1 |
        ///         Last_request = 2 | 0 | ATS_ID, corr_step_4 | invalid_client
        /// State of client after response is returned - 
        ///         Last_request = 2 | 0 | ATS_ID, corr_step_5 | interaction_required
        ///         
        /// 6. Acquire Token interactive -> error user_cancelled(i.e.no calls to /token endpoint)
        ///         Current_request = 2 | ATI_ID, 0 |
        ///         Last_request = 2 | 0 |  ATS_ID, corr_step_5, ATI_ID, corr_step-6 | interaction_required, 
        ///        user_cancelled|
        ///               6b. Acquire Token interactive -> HTTP error 503 (Service Unavailable)
        ///
        ///        Current_request = 2 | ATI_ID, 0 |
        ///        Last_request = 2 | 0 |  ATS_ID, corr_step_5, ATI_ID, corr_step-6, | interaction_required, 
        ///       user_cancelled|
        ///
        /// State of the client: 
        ///
        ///        Last_request = 2 | 0 |  ATS_ID, corr_step_5, ATI_ID, corr_step-6, ATI-ID, corr_step-6b | interaction_required, 
        ///       user_cancelled, ServiceUnavailable|
        ///
        ///
        /// 7. Acquire Token interactive -> successful
        ///
        /// Sent to the server - 
        ///        Current_request = 2 | ATI_ID, 0 |
        ///        Last_request = 2 | 0 |  ATS_ID, corr_step_5, ATI_ID, corr_step-6, ATI-ID, corr_step-6b  | interaction_required, 
        ///        user_cancelled, ServiceUnavailable |
        ///
        /// State of the client after response is returned - 
        ///        Last_request = NULL
        ///
        /// 8. Acquire Token Silent with force-refresh -> successful
        /// Sent to the server - 
        ///         Current_request = 2 | ATI_ID, 1 |
        ///         Last_request = NULL
        /// State of the client after response is returned - 
        ///        Last_request = 2 | 1 | | |
        /// </summary>
        [TestMethod]
        public async Task TelemetryAcceptanceTestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);

                _app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                            .WithHttpManager(_harness.HttpManager)
                            .BuildConcrete();

                Trace.WriteLine("Step 1. Acquire Token Interactive successfully");
                var result = await RunAquireTokenInteractiveAsync().ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, forceRefresh: false);
                AssertLastTelemetryIsNull(result.HttpRequest); // TODO: this is still debated, it's more likely that we should send a header with silent_count = 0

                Trace.WriteLine("Step 2. Acquire Token Silent successfully - AT served from cache");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessFromCache).ConfigureAwait(false);
                Assert.IsNull(result.HttpRequest, "No calls are made to the token endpoint");

                Trace.WriteLine("Step 3. Acquire Token Silent successfully - via refresh_token flow");
                _harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityUtidTenant);
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessViaRefreshGrant).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, forceRefresh: false);
                AssertLastTelemetry(result.HttpRequest, 1, null, null, null);

                Trace.WriteLine("Step 4. Acquire Token Silent with force_refresh = true and failure = invalid_client");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.FailInvalidGrant, forceRefresh: true).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, forceRefresh: true); // Current_request = 2 | ATS_ID, 1 |
                // we've already sent the number of silent calls in step 3, don't send it again
                AssertLastTelemetry(result.HttpRequest, 0, null, null, null); // Last_request = 2 | 0 | | | 
                Guid step4Correlationid = result.Correlationid;

                Trace.WriteLine("Step 5. Acquire Token Silent with force_refresh = true and failure = interaction_required");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.FailInteractionRequired, forceRefresh: true).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, forceRefresh: true);// Current_request = 2 | ATS_ID, 1 |
                AssertLastTelemetry(
                    result.HttpRequest, 
                    expectedeSilentCount: 0, 
                    expectedFailedApiIds: new[] { ApiIds.AcquireTokenSilent }, // from step 4
                    expectedCorrelationIds: new[] { step4Correlationid }, 
                    expectedErrors: new[] { "invalid_client" } ); 
            }
        }

        private enum AcquireTokenSilentOutcome
        {
            SuccessFromCache,
            SuccessViaRefreshGrant,
            FailInvalidGrant, 
            FailInteractionRequired,
        }

        private async Task<(HttpRequestMessage HttpRequest, Guid Correlationid)> RunAquireTokenInteractiveAsync()
        {
            MsalMockHelpers.ConfigureMockWebUI(
                _app.ServiceBundle.PlatformProxy,
                AuthorizationResult.FromUri(_app.AppConfig.RedirectUri + "?code=some-code"));

            MockHttpMessageHandler tokenRequest = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();
            var res = await _app
                .AcquireTokenInteractive(TestConstants.s_scope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(0, _harness.HttpManager.QueueSize);

            return (tokenRequest.ActualRequestMessage, res.CorrelationId);
        }

        private async Task<(HttpRequestMessage HttpRequest, Guid Correlationid)> RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome outcome, bool forceRefresh = false)
        {
            MockHttpMessageHandler tokenRequest = null;
            Guid correlationId = Guid.Empty;

            var account = (await _app.GetAccountsAsync().ConfigureAwait(false)).Single();
            var request = _app
                      .AcquireTokenSilent(TestConstants.s_scope, account)
                      .WithForceRefresh(forceRefresh);

            switch (outcome)
            {
                case AcquireTokenSilentOutcome.SuccessFromCache:
                    var authResult = await request
                       .ExecuteAsync()
                       .ConfigureAwait(false);

                    correlationId = authResult.CorrelationId;
                    break;
                case AcquireTokenSilentOutcome.SuccessViaRefreshGrant:
                    // let's remove the AT so that they can't be used 
                    _app.UserTokenCacheInternal.Accessor.ClearAccessTokens();
                    tokenRequest = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityUtidTenant);
                    authResult = await request
                      .ExecuteAsync()
                      .ConfigureAwait(false);
                    correlationId = authResult.CorrelationId;

                    break;
                case AcquireTokenSilentOutcome.FailInvalidGrant:
                    _app.UserTokenCacheInternal.Accessor.ClearAccessTokens();
                    tokenRequest = _harness.HttpManager.AddFailureTokenEndpointResponse("invalid_grant", TestConstants.AuthorityUtidTenant);

                    var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => request.ExecuteAsync())
                        .ConfigureAwait(false);

                    correlationId = Guid.Parse(ex.CorrelationId);

                    break;
                case AcquireTokenSilentOutcome.FailInteractionRequired:
                    _app.UserTokenCacheInternal.Accessor.ClearAccessTokens();
                    tokenRequest = _harness.HttpManager.AddFailureTokenEndpointResponse("interaction_required", TestConstants.AuthorityUtidTenant);

                    ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => request.ExecuteAsync())
                        .ConfigureAwait(false);

                    correlationId = Guid.Parse(ex.CorrelationId);
                    break;
                default:
                    throw new NotImplementedException();
            }

            Assert.AreEqual(0, _harness.HttpManager.QueueSize);
            return (tokenRequest?.ActualRequestMessage, correlationId);
        }

        private static void AssertLastTelemetryIsNull(HttpRequestMessage requestMessage)
        {
            string lastTelemetryHeader = requestMessage.Headers.GetValues(
               TelemetryConstants.XClientLastTelemetry).Single();

            Assert.IsTrue(string.IsNullOrEmpty(lastTelemetryHeader));
        }

        private static void AssertCurrentTelemetry(
            HttpRequestMessage requestMessage,
            ApiIds apiId,
            bool forceRefresh)
        {
            string actualCurrentTelemetry = requestMessage.Headers.GetValues(
                TelemetryConstants.XClientCurrentTelemetry).Single();

            var actualTelemetryParts = actualCurrentTelemetry.Split("|");
            Assert.AreEqual(3, actualTelemetryParts.Length);

            Assert.AreEqual(TelemetryConstants.HttpTelemetrySchemaVersion2, actualTelemetryParts[0]); // version

            Assert.AreEqual(
                ((int)apiId).ToString(CultureInfo.InvariantCulture),
                actualTelemetryParts[1].Split(",")[0]); // current_api_id

            Assert.AreEqual(
                forceRefresh ? "1" : "0",
                actualTelemetryParts[1].Split(",")[1]); // force_refresh flag
        }


        private static void AssertLastTelemetry(
           HttpRequestMessage requestMessage,
           int expectedeSilentCount,
           ApiIds[] expectedFailedApiIds,
           Guid[] expectedCorrelationIds,
           string[] expectedErrors)
        {
            expectedFailedApiIds = expectedFailedApiIds ?? new ApiIds[0];
            expectedCorrelationIds = expectedCorrelationIds ?? new Guid[0];
            expectedErrors = expectedErrors ?? new string[0];

            string lastTelemetryHeader = requestMessage.Headers.GetValues(
                TelemetryConstants.XClientLastTelemetry).Single();

            // schema_version | silent_succesful_count | failed_requests | errors | platform_fields
            // where a failed_request is "api_id, correlation_id"
            var lastTelemetryParts = lastTelemetryHeader.Split("|");

            Assert.AreEqual(5, lastTelemetryParts.Length);
            string actualSchemaVersion = lastTelemetryParts[0];
            int actualSucesfullSilentCount = int.Parse(lastTelemetryParts[1], CultureInfo.InvariantCulture);

            string[] actualFailedApiIds = lastTelemetryParts[2]
                .Split(",")
                .Where((item, index) => index % 2 == 0)
                .ToArray();
            string[] correlationIds = lastTelemetryParts[2].Split(",").Where((item, index) => index % 2 != 0).ToArray();
            string[] actualErrors = lastTelemetryParts[3].Split(",");

            CoreAssert.AreEqual(actualFailedApiIds.Length, correlationIds.Length, actualFailedApiIds.Length);

            Assert.AreEqual(TelemetryConstants.HttpTelemetrySchemaVersion2, actualSchemaVersion); // version
            Assert.AreEqual(expectedeSilentCount, actualSucesfullSilentCount); // silent count

            CollectionAssert.AreEqual(
                expectedFailedApiIds.Select(apiId => ((int)apiId).ToString(CultureInfo.InvariantCulture)).ToArray(),
                actualFailedApiIds); // failed api IDs
            CollectionAssert.AreEqual(expectedCorrelationIds.Select(g => g.ToString()).ToArray(), correlationIds);

            CollectionAssert.AreEqual(expectedErrors, actualErrors); // errors

        }
    }
}
