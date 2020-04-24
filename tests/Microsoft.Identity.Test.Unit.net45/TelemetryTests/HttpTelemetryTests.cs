// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Microsoft.Identity.Test.Unit.Throttling;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
        ///        Last_request = 2 | 0 | | |
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
        /// 6. Acquire Token interactive -> error user_cancelled (i.e. no calls to /token endpoint)
        ///       No calls to token endpoint
        /// 
        /// 7. Acquire Token interactive -> HTTP error 503 (Service Unavailable)
        ///
        ///        Current_request = 2 | ATI_ID, 0 |
        ///        Last_request = 2 | 0 |  ATS_ID, corr_step_5, ATI_ID, corr_step-6, | interaction_required, 
        ///       authentication_canceled|
        ///
        /// State of the client: 
        ///
        ///        Last_request = 2 | 0 |  ATS_ID, corr_step_5, ATI_ID, corr_step-6, ATI-ID, corr_step-6b | interaction_required, 
        ///       authentication_canceled, ServiceUnavailable|
        ///
        /// 8. Acquire Token interactive -> successful
        ///
        /// Sent to the server - 
        ///        Current_request = 2 | ATI_ID, 0 |
        ///        Last_request = 2 | 0 |  ATS_ID, corr_step_5, ATI_ID, corr_step-6, ATI-ID, corr_step-6b  | interaction_required, 
        ///        authentication_canceled, ServiceUnavailable |
        ///
        /// State of the client after response is returned - 
        ///        Last_request = NULL
        ///
        /// 9. Acquire Token Silent with force-refresh -> successful
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
                            .WithDefaultRedirectUri()
                            .WithLogging((lvl,msg,pii) => Trace.WriteLine($"[MSAL_LOG][{lvl}] {msg}"))
                            .BuildConcrete();

                Trace.WriteLine("Step 1. Acquire Token Interactive successful");
                var result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.Success).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, forceRefresh: false);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0); // Previous_request = 2|0|||

                Trace.WriteLine("Step 2. Acquire Token Silent successful - AT served from cache");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessFromCache).ConfigureAwait(false);
                Assert.IsNull(result.HttpRequest, "No calls are made to the token endpoint");

                Trace.WriteLine("Step 3. Acquire Token Silent successful - via refresh_token flow");
                _harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityUtidTenant);
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessViaRefreshGrant).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, forceRefresh: false);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 1); // Previous_request = 2|1|||

                Trace.WriteLine("Step 4. Acquire Token Silent with force_refresh = true and failure = invalid_grant");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.FailInvalidGrant, forceRefresh: true).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, forceRefresh: true); // Current_request = 2 | ATS_ID, 1 |
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0); // Previous_request = 2|0|||

                Guid step4Correlationid = result.Correlationid;
                Trace.WriteLine("Step 5. Acquire Token Silent with force_refresh = true and failure = interaction_required");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.FailInteractionRequired, forceRefresh: true).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, forceRefresh: true);// Current_request = 2 | ATS_ID, 1 |
                AssertPreviousTelemetry(
                    result.HttpRequest,
                    expectedSilentCount: 0,
                    expectedFailedApiIds: new[] { ApiIds.AcquireTokenSilent }, // from step 4
                    expectedCorrelationIds: new[] { step4Correlationid },
                    expectedErrors: new[] { "invalid_grant" });
                Guid step5CorrelationId = result.Correlationid;

                Trace.WriteLine("Step 6. Acquire Token Interactive -  some /authorization error  -> token endpoint not hit");
                result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.AuthorizationError).ConfigureAwait(false);
                Assert.IsNull(result.HttpRequest, "No calls are made to the token endpoint");
                Guid step6CorrelationId = result.Correlationid;

                Trace.WriteLine("Step 7. Acquire Token Interactive -> HTTP 5xx error (i.e. AAD is down)");
                result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.AADUnavailableError).ConfigureAwait(false);
                Guid step7CorrelationId = result.Correlationid;

                // we can assert telemetry here, as it will be sent to AAD. However, AAD is down, so it will not record it.
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, forceRefresh: false);
                AssertPreviousTelemetry(
                    result.HttpRequest,
                    expectedSilentCount: 0,
                    expectedFailedApiIds: new[] { ApiIds.AcquireTokenSilent, ApiIds.AcquireTokenInteractive },
                    expectedCorrelationIds: new[] { step5CorrelationId, step6CorrelationId },
                    expectedErrors: new[] { "interaction_required", "user_cancelled" });

                // the 5xx error puts MSAL in a throttling state, so "wait" until this clears
                _harness.ServiceBundle.ThrottlingManager.SimulateTimePassing(
                    HttpStatusProvider.s_throttleDuration);

                Trace.WriteLine("Step 8. Acquire Token Interactive -> Success");
                result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.Success).ConfigureAwait(false);

                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, forceRefresh: false);
                AssertPreviousTelemetry(
                    result.HttpRequest,
                    expectedSilentCount: 0,
                    expectedFailedApiIds: new[] { ApiIds.AcquireTokenSilent, ApiIds.AcquireTokenInteractive, ApiIds.AcquireTokenInteractive },
                    expectedCorrelationIds: new[] { step5CorrelationId, step6CorrelationId, step7CorrelationId },
                    expectedErrors: new[] { "interaction_required", "user_cancelled", "service_not_available" });

                Trace.WriteLine("Step 9. Acquire Token Silent with force-refresh -> successful");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessViaRefreshGrant, false).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, forceRefresh: false);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);
            }
        }

        private enum AcquireTokenSilentOutcome
        {
            SuccessFromCache,
            SuccessViaRefreshGrant,
            FailInvalidGrant,
            FailInteractionRequired
        }

        private enum AcquireTokenInteractiveOutcome
        {
            Success,

            /// <summary>
            /// An error occurs at the /authorization endpoint, for example
            /// the user closes the embedded browser or AAD complains about a bad redirect uri being configured            
            /// </summary>
            AuthorizationError,

            /// <summary>
            /// An error occurs at the /token endpoint. HTTP 5xx errors and 429 denote that AAD is down.
            /// In this case the telemetry will not have been recorded and MSAL needs to keep it around.
            /// </summary>
            AADUnavailableError
        }

        private async Task<(HttpRequestMessage HttpRequest, Guid Correlationid)> RunAcquireTokenInteractiveAsync(
            AcquireTokenInteractiveOutcome outcome)
        {
            MockHttpMessageHandler tokenRequestHandler = null;
            Guid correlationId = default;

            switch (outcome)
            {
                case AcquireTokenInteractiveOutcome.Success:
                    MsalMockHelpers.ConfigureMockWebUI(
                        _app.ServiceBundle.PlatformProxy,
                         AuthorizationResult.FromUri(_app.AppConfig.RedirectUri + "?code=some-code"));
                    tokenRequestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();
                    var authResult = await _app
                        .AcquireTokenInteractive(TestConstants.s_scope)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                    correlationId = authResult.CorrelationId;
                    break;
                case AcquireTokenInteractiveOutcome.AuthorizationError:
                    correlationId = Guid.NewGuid();

                    var ui = Substitute.For<IWebUI>();
                    ui.UpdateRedirectUri(Arg.Any<Uri>()).Returns(new Uri("http://localhost:1234"));
                    ui.AcquireAuthorizationAsync(null, null, null, default).ThrowsForAnyArgs(
                        new MsalClientException("user_cancelled"));
                    MsalMockHelpers.ConfigureMockWebUI(_app.ServiceBundle.PlatformProxy, ui);
                    
                    var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                        _app
                        .AcquireTokenInteractive(TestConstants.s_scope)
                        .WithCorrelationId(correlationId)
                        .ExecuteAsync())
                        .ConfigureAwait(false);

                    break;
                case AcquireTokenInteractiveOutcome.AADUnavailableError:
                    correlationId = Guid.NewGuid();

                    MsalMockHelpers.ConfigureMockWebUI(
                       _app.ServiceBundle.PlatformProxy,
                        AuthorizationResult.FromUri(_app.AppConfig.RedirectUri + "?code=some-code"));

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
                        _app
                        .AcquireTokenInteractive(TestConstants.s_scope)
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

        private async Task<(HttpRequestMessage HttpRequest, Guid Correlationid)> RunAcquireTokenSilentAsync(
            AcquireTokenSilentOutcome outcome, 
            bool forceRefresh = false)
        {
            MockHttpMessageHandler tokenRequest = null;
            Guid correlationId = Guid.Empty;

            var account = (await _app.GetAccountsAsync().ConfigureAwait(false)).Single();
            AcquireTokenSilentParameterBuilder request = _app
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
                    (tokenRequest, correlationId) = await RunSilentFlowWithTokenErrorAsync(request, "invalid_grant")
                        .ConfigureAwait(false);

                    break;
                case AcquireTokenSilentOutcome.FailInteractionRequired:
                    (tokenRequest, correlationId) = await RunSilentFlowWithTokenErrorAsync(request, "interaction_required")
                        .ConfigureAwait(false);
                    break;

                default:
                    throw new NotImplementedException();
            }

            Assert.AreEqual(0, _harness.HttpManager.QueueSize);
            return (tokenRequest?.ActualRequestMessage, correlationId);
        }

        private async Task<(MockHttpMessageHandler MockHttpHandler, Guid Correlationid)> RunSilentFlowWithTokenErrorAsync(AcquireTokenSilentParameterBuilder request, string errorCode)
        {
            _app.UserTokenCacheInternal.Accessor.ClearAccessTokens();

            var correlationId = Guid.NewGuid();
            var tokenRequest = _harness.HttpManager.AddFailureTokenEndpointResponse(
                errorCode,
                TestConstants.AuthorityUtidTenant,
                correlationId.ToString());

            var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                () => request.WithCorrelationId(correlationId).ExecuteAsync(), 
                allowDerived: true)
                .ConfigureAwait(false);

            Assert.AreEqual(
                correlationId, 
                Guid.Parse(ex.CorrelationId), 
                "Test error - Exception correlation ID does not match WithCorrelationId value");

            return (tokenRequest, correlationId);
        }

        private static void AssertCurrentTelemetry(
            HttpRequestMessage requestMessage, 
            ApiIds apiId, 
            bool forceRefresh)
        {
            string actualCurrentTelemetry = requestMessage.Headers.GetValues(
                TelemetryConstants.XClientCurrentTelemetry).Single();

            var actualTelemetryParts = actualCurrentTelemetry.Split('|');
            Assert.AreEqual(3, actualTelemetryParts.Length);

            Assert.AreEqual(TelemetryConstants.HttpTelemetrySchemaVersion2, actualTelemetryParts[0]); // version

            Assert.AreEqual(
                ((int)apiId).ToString(CultureInfo.InvariantCulture),
                actualTelemetryParts[1].Split(',')[0]); // current_api_id

            Assert.IsTrue(actualTelemetryParts[1].EndsWith(forceRefresh ? "1" : "0")); // force_refresh flag
        }

        private static void AssertPreviousTelemetry(
           HttpRequestMessage requestMessage,
           int expectedSilentCount,
           ApiIds[] expectedFailedApiIds = null,
           Guid[] expectedCorrelationIds = null,
           string[] expectedErrors = null)
        {
            expectedFailedApiIds = expectedFailedApiIds ?? new ApiIds[0];
            expectedCorrelationIds = expectedCorrelationIds ?? new Guid[0];
            expectedErrors = expectedErrors ?? new string[0];

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
        }

        private static (int SilentCount, string[] FailedApis, string[] CorrelationIds, string[] Errors) ParseLastRequestHeader(HttpRequestMessage requestMessage)
        {
            // schema_version | silent_succesful_count | failed_requests | errors | platform_fields
            // where a failed_request is "api_id, correlation_id"
            string lastTelemetryHeader = requestMessage.Headers.GetValues(
               TelemetryConstants.XClientLastTelemetry).Single();
            var lastRequestParts =  lastTelemetryHeader.Split('|');

            Assert.AreEqual(5, lastRequestParts.Length); //  2 | 1 | | |
            Assert.AreEqual(TelemetryConstants.HttpTelemetrySchemaVersion2, lastRequestParts[0]); // version

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

            return (actualSuccessfullSilentCount, actualFailedApiIds, correlationIds, actualErrors);
        }
    }
}
