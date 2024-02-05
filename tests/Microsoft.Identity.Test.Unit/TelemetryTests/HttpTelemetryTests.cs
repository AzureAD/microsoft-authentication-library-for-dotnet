// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Http;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.Identity.Test.Unit.Throttling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;
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

        [TestCleanup]
        public override void TestCleanup()
        {
            _harness?.Dispose();
            base.TestCleanup();
        }

        /// <summary>
        /// 1.  Acquire Token Interactive successfully
        ///        Current_request = 4 | ATI_ID, 0 | 0
        ///        Last_request = 4 | 0 | | |
        /// 
        /// 2. Acquire token silent with AT served from cache ... no calls to /token endpoint
        ///        
        /// 3. Acquire token silent with AT not served from cache (AT expired)
        ///         Current_request = 4 | ATS_ID, 2 | 0
        ///         Last_request = 4 | 1 | | |
        ///         
        /// 4. Acquire Token silent with force_refresh = true -> error invalid_client
        /// Sent to server - 
        ///         Current_request = 4 | ATS_ID, 1 | 0
        ///         Last_request = 4 | 0 | | |
        ///         
        /// State of client after error response is returned – (the successful silent request counter was flushed, last_request is reset, and now we add the error from step 4)
        ///         Last_request = 4 | 0 | ATS_ID, Corr_step_4 | invalid_client |
        /// 
        /// 5. Acquire Token silent with force_refresh = true -> error interaction_required
        /// Sent to the server - 
        ///         Current_request = 4 | ATS_ID, 1 | 0
        ///         Last_request = 4 | 0 | ATS_ID, corr_step_4 | invalid_client
        /// State of client after response is returned - 
        ///         Last_request = 4 | 0 | ATS_ID, corr_step_5 | interaction_required
        ///         
        /// 6. Acquire Token interactive -> error user_cancelled (i.e. no calls to /token endpoint)
        ///       No calls to token endpoint
        /// 
        /// 7. Acquire Token interactive -> HTTP error 503 (Service Unavailable)
        ///
        ///        Current_request = 4 | ATI_ID, 0 | 0
        ///        Last_request = 4 | 0 |  ATS_ID, corr_step_5, ATI_ID, corr_step-6, | interaction_required, 
        ///       authentication_canceled|
        ///
        /// State of the client: 
        ///
        ///        Last_request = 4 | 0 |  ATS_ID, corr_step_5, ATI_ID, corr_step-6, ATI-ID, corr_step-6b | interaction_required, 
        ///       authentication_canceled, ServiceUnavailable|
        ///
        /// 8. Acquire Token interactive -> successful
        ///
        /// Sent to the server - 
        ///        Current_request = 4 | ATI_ID, 0 | 0
        ///        Last_request = 4 | 0 |  ATS_ID, corr_step_5, ATI_ID, corr_step-6, ATI-ID, corr_step-6b  | interaction_required, 
        ///        authentication_canceled, ServiceUnavailable |
        ///
        /// State of the client after response is returned - 
        ///        Last_request = NULL
        ///
        /// 9. Acquire Token Silent with force-refresh false -> successful
        /// Sent to the server - 
        ///         Current_request = 4 | ATI_ID, 2 | 0
        ///         Last_request = NULL
        /// State of the client after response is returned - 
        ///        Last_request = 4 | 1 | | |
        /// </summary>
        [TestMethod]
        public async Task TelemetryAcceptanceTestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();

                _app = CreatePublicClientApp();

                Trace.WriteLine("Step 1. Acquire Token Interactive successful");
                var result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.Success).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, CacheRefreshReason.NotApplicable);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);

                Trace.WriteLine("Step 2. Acquire Token Silent successful - AT served from cache");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessFromCache).ConfigureAwait(false);
                Assert.IsNull(result.HttpRequest, "No calls are made to the token endpoint");

                Trace.WriteLine("Step 3. Acquire Token Silent successful - via refresh_token flow");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessViaRefreshGrant).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, CacheRefreshReason.NoCachedAccessToken);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 1);

                Trace.WriteLine("Step 4. Acquire Token Silent with force_refresh = true and failure = invalid_grant");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.FailInvalidGrant, forceRefresh: true).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, CacheRefreshReason.ForceRefreshOrClaims);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);

                // invalid grant error puts MSAL in a throttled state - simulate some time passing for this
                _harness.ServiceBundle.ThrottlingManager.SimulateTimePassing(
                    UiRequiredProvider.s_uiRequiredExpiration.Add(TimeSpan.FromSeconds(1)));

                Guid step4Correlationid = result.Correlationid;
                Trace.WriteLine("Step 5. Acquire Token Silent with force_refresh = true and failure = interaction_required");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.FailInteractionRequired, forceRefresh: true).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, CacheRefreshReason.ForceRefreshOrClaims);
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
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, CacheRefreshReason.NotApplicable);
                AssertPreviousTelemetry(
                    result.HttpRequest,
                    expectedSilentCount: 0,
                    expectedFailedApiIds: new[] { ApiIds.AcquireTokenSilent, ApiIds.AcquireTokenInteractive },
                    expectedCorrelationIds: new[] { step5CorrelationId, step6CorrelationId },
                    expectedErrors: new[] { "interaction_required", "user_cancelled" });

                // the 5xx error puts MSAL in a throttling state, so "wait" until this clears
                _harness.ServiceBundle.ThrottlingManager.SimulateTimePassing(
                    HttpStatusProvider.s_throttleDuration.Add(TimeSpan.FromSeconds(1)));

                Trace.WriteLine("Step 8. Acquire Token Interactive -> Success");
                result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.Success).ConfigureAwait(false);

                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, CacheRefreshReason.NotApplicable);
                AssertPreviousTelemetry(
                    result.HttpRequest,
                    expectedSilentCount: 0,
                    expectedFailedApiIds: new[] { ApiIds.AcquireTokenSilent, ApiIds.AcquireTokenInteractive, ApiIds.AcquireTokenInteractive },
                    expectedCorrelationIds: new[] { step5CorrelationId, step6CorrelationId, step7CorrelationId },
                    expectedErrors: new[] { "interaction_required", "user_cancelled", "service_not_available" });

                Trace.WriteLine("Step 9. Acquire Token Silent with force-refresh false -> successful");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessViaRefreshGrant, false).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, CacheRefreshReason.NoCachedAccessToken);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);
            }
        }

        /// <summary>
        /// 1.  Acquire Token Interactive successfully
        ///        Current_request = 4 |ATS_ID, 0 | , , 0, , , , 1
        ///        Last_request = 4 | 0 | | |
        /// 
        /// 2. Acquire token silent with AT served from cache ... no calls to /token endpoint
        ///        
        /// 3. Acquire token silent with AT expired
        ///         Current_request = 4 | ATS_ID, 3 | , , 1, , , , 1
        ///         Last_request = 4 | 1 | | |
        ///         
        /// 4. Acquire Token silent with refresh on
        ///         Current_request = 4 | ATS_ID, 4 | , , 1, , , , 1
        ///         Last_request = 4 | 0 | | |
        /// 
        /// 5. Acquire Token silent with force_refresh = true 
        ///         Current_request = 4 | ATS_ID, 1 | , , 1, , , , 1
        ///         Last_request = 4 | 0 | | |
        ///         
        /// </summary>
        [TestMethod]
        public async Task TelemetryCacheRefreshTestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();

                _app = CreatePublicClientApp();

                TokenCacheHelper.PopulateCache(_app.UserTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("Step 1. Acquire Token Interactive successful");
                var result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.Success).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, CacheRefreshReason.NotApplicable);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);

                Trace.WriteLine("Step 2. Acquire Token Silent successful - AT served from cache");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessFromCache).ConfigureAwait(false);
                Assert.IsNull(result.HttpRequest, "No calls are made to the token endpoint");

                Trace.WriteLine("Step 3. Acquire Token Silent successful - via expired token");
                TestCommon.UpdateATWithRefreshOn(_app.UserTokenCacheInternal.Accessor, expired: true);
                TokenCacheAccessRecorder cacheAccess = _app.UserTokenCache.RecordAccess();
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessViaCacheRefresh).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, CacheRefreshReason.Expired, isCacheSerialized: true);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 1);

                Trace.WriteLine("Step 4. Acquire Token Silent successful - via refresh on");
                TestCommon.UpdateATWithRefreshOn(_app.UserTokenCacheInternal.Accessor);
                cacheAccess = _app.UserTokenCache.RecordAccess();
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessViaCacheRefresh).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, CacheRefreshReason.ProactivelyRefreshed, isCacheSerialized: true);

                // Use reflection to get the value and wait till achieved
                HttpTelemetryManager httpTelemetryManager = (HttpTelemetryManager)_app.ServiceBundle.HttpTelemetryManager;
                Type httpTeleMgrType = typeof(HttpTelemetryManager);
                FieldInfo field = httpTeleMgrType.GetField("_successfullSilentCallCount", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsTrue(TestCommon.YieldTillSatisfied(() =>
                {
                    var actual = (int)field.GetValue(httpTelemetryManager);
                    return actual == 0;
                }));

                Trace.WriteLine("Step 5. Acquire Token Silent with force_refresh = true");
                result = await RunAcquireTokenSilentAsync(AcquireTokenSilentOutcome.SuccessViaCacheRefresh, forceRefresh: true).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenSilent, CacheRefreshReason.ForceRefreshOrClaims, isCacheSerialized: true);
                Assert.IsTrue(TestCommon.YieldTillSatisfied(() =>
                {
                    var actual = (int)field.GetValue(httpTelemetryManager);
                    return actual == 0;
                }));
            }
        }

        /// <summary>
        /// Acquire token with serialized token cache successfully
        ///    Current_request = 4 | ATC_ID, 0 | 1
        ///    Last_request = 4 | 0 | | |
        /// </summary>
        [TestMethod]
        public async Task TelemetryTestSerializedTokenCacheAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();

                _app = CreatePublicClientApp();

                var inMemoryTokenCache = new InMemoryTokenCache();
                inMemoryTokenCache.Bind(_app.UserTokenCache);

                Trace.WriteLine("Step 1. Acquire Token Interactive successful");
                var result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.Success).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, CacheRefreshReason.NotApplicable, isCacheSerialized: true);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);
            }
        }

        [TestMethod]
        public async Task TelemetryTestExceptionLogAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();

                _app = CreatePublicClientApp();

                Trace.WriteLine("Acquire token Interactive with OperationCanceledException.");
                var result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.TaskCanceledException).ConfigureAwait(false);
                var previousCorrelationId = result.Correlationid;

                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                Trace.WriteLine("Acquire token interactive successful.");
                result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.Success).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, CacheRefreshReason.NotApplicable);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0,
                    expectedFailedApiIds: new ApiIds[] { ApiIds.AcquireTokenInteractive },
                    expectedCorrelationIds: new Guid[] { previousCorrelationId },
                    expectedErrors: new string[] { "TaskCanceledException" });
            }
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task LegacyCacheEnabledTelemetryTestAsync(bool isLegacyCacheEnabled)
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();

                _app = CreatePublicClientApp(isLegacyCacheEnabled);

                var result = await RunAcquireTokenInteractiveAsync(AcquireTokenInteractiveOutcome.Success).ConfigureAwait(false);
                AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenInteractive, CacheRefreshReason.NotApplicable, isLegacyCacheEnabled: isLegacyCacheEnabled);
                AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);
            }
        }

        [TestMethod]
        public async Task CorrectApiIdSet_OnBehalfOf_TestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var requestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                var cca = CreateConfidentialClientApp();

                await cca.AcquireTokenOnBehalfOf(TestConstants.s_scope, new UserAssertion(TestConstants.DefaultAccessToken))
                    .ExecuteAsync().ConfigureAwait(false);

                AssertCurrentTelemetry(requestHandler.ActualRequestMessage, ApiIds.AcquireTokenOnBehalfOf, CacheRefreshReason.NoCachedAccessToken);
            }
        }

        [TestMethod]
        public async Task CorrectApiIdSet_LongRunningOnBehalfOf_TestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var requestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                var cca = CreateConfidentialClientApp();

                var cacheKey = string.Empty;
                await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref cacheKey)
                    .ExecuteAsync().ConfigureAwait(false);

                AssertCurrentTelemetry(requestHandler.ActualRequestMessage, ApiIds.InitiateLongRunningObo, CacheRefreshReason.NotApplicable);

                // AcquireTokenInLongRunningProcess goes to AAD only in a refresh flow
                requestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, cacheKey)
                    .WithForceRefresh(true)
                    .ExecuteAsync().ConfigureAwait(false);

                AssertCurrentTelemetry(requestHandler.ActualRequestMessage, ApiIds.AcquireTokenInLongRunningObo, CacheRefreshReason.ForceRefreshOrClaims);
            }
        }

        [TestMethod]
        public async Task CorrectApiIdSet_ClientCredentials_TestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var requestHandler = _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var cca = CreateConfidentialClientApp();

                await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                AssertCurrentTelemetry(requestHandler.ActualRequestMessage, ApiIds.AcquireTokenForClient, CacheRefreshReason.NoCachedAccessToken);
            }
        }

        [TestMethod]
        public async Task CorrectApiIdSet_AuthCode_TestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var requestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                var cca = CreateConfidentialClientApp();

                await cca.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .ExecuteAsync().ConfigureAwait(false);

                AssertCurrentTelemetry(requestHandler.ActualRequestMessage, ApiIds.AcquireTokenByAuthorizationCode, CacheRefreshReason.NotApplicable);
            }
        }

        [TestMethod]
        public async Task CorrectApiIdSet_RefreshToken_TestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var requestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                var cca = CreateConfidentialClientApp();

                await ((IByRefreshToken)cca).AcquireTokenByRefreshToken(TestConstants.s_scope, TestConstants.RefreshToken)
                    .ExecuteAsync().ConfigureAwait(false);

                AssertCurrentTelemetry(requestHandler.ActualRequestMessage, ApiIds.AcquireTokenByRefreshToken, CacheRefreshReason.NotApplicable);
            }
        }

        [TestMethod]
        public async Task CorrectApiIdSet_UsernamePassword_TestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddWsTrustMockHandler();
                var requestHandler = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                var pca = CreatePublicClientApp();

                await pca.AcquireTokenByUsernamePassword(TestConstants.s_scope, "username", TestConstants.DefaultPassword)
                    .ExecuteAsync().ConfigureAwait(false);

                AssertCurrentTelemetry(requestHandler.ActualRequestMessage, ApiIds.AcquireTokenByUsernamePassword, CacheRefreshReason.NotApplicable);
            }
        }

        private PublicClientApplication CreatePublicClientApp(bool isLegacyCacheEnabled = true)
        {
            return PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithHttpManager(_harness.HttpManager)
                    .WithDefaultRedirectUri()
                    .WithLogging((lvl, msg, _) => Trace.WriteLine($"[MSAL_LOG][{lvl}] {msg}"))
                    .WithLegacyCacheCompatibility(isLegacyCacheEnabled)
                    .BuildConcrete();
        }

        private ConfidentialClientApplication CreateConfidentialClientApp()
        {
            return ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithHttpManager(_harness.HttpManager)
                .BuildConcrete();
        }

        private enum AcquireTokenSilentOutcome
        {
            SuccessFromCache,
            SuccessViaRefreshGrant,
            SuccessViaCacheRefresh,
            FailInvalidGrant,
            FailInteractionRequired
        }

        private enum AcquireTokenInteractiveOutcome
        {
            Success,

            /// <summary>
            /// An error occurs at the /authorization endpoint, for example
            /// the user closes the embedded browser or AAD complains about a bad redirect URI being configured            
            /// </summary>
            AuthorizationError,

            /// <summary>
            /// An error occurs at the /token endpoint. HTTP 5xx errors and 429 denote that AAD is down.
            /// In this case the telemetry will not have been recorded and MSAL needs to keep it around.
            /// </summary>
            AADUnavailableError,

            TaskCanceledException
        }

        private async Task<(HttpRequestMessage HttpRequest, Guid Correlationid)> RunAcquireTokenInteractiveAsync(
            AcquireTokenInteractiveOutcome outcome)
        {
            MockHttpMessageHandler tokenRequestHandler = null;
            Guid correlationId;

            switch (outcome)
            {
                case AcquireTokenInteractiveOutcome.Success:

                    _app.ServiceBundle.ConfigureMockWebUI();

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
                    _app.ServiceBundle.ConfigureMockWebUI(ui);

                    var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                        _app
                        .AcquireTokenInteractive(TestConstants.s_scope)
                        .WithCorrelationId(correlationId)
                        .ExecuteAsync())
                        .ConfigureAwait(false);

                    break;
                case AcquireTokenInteractiveOutcome.AADUnavailableError:
                    correlationId = Guid.NewGuid();

                    _app.ServiceBundle.ConfigureMockWebUI();

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

                case AcquireTokenInteractiveOutcome.TaskCanceledException:
                    correlationId = Guid.NewGuid();

                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Cancel(true);
                    CancellationToken token = cts.Token;

                    var operationCanceledException = await AssertException.TaskThrowsAsync<TaskCanceledException>(() =>
                        _app
                        .AcquireTokenInteractive(TestConstants.s_scope)
                        .WithCorrelationId(correlationId)
                        .ExecuteAsync(token))
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
            Guid correlationId;

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
                    tokenRequest = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityUtidTenant,
                        responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(refreshToken: Guid.NewGuid().ToString()));
                    authResult = await request
                      .ExecuteAsync()
                      .ConfigureAwait(false);
                    correlationId = authResult.CorrelationId;

                    break;
                case AcquireTokenSilentOutcome.SuccessViaCacheRefresh:
                    tokenRequest = _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityUtidTenant,
                        responseMessage: MockHelpers.CreateSuccessTokenResponseMessage(refreshToken: Guid.NewGuid().ToString()));
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

            TestCommon.YieldTillSatisfied(() => _harness.HttpManager.QueueSize == 0);
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

        private void AssertCurrentTelemetry(
            HttpRequestMessage requestMessage,
            ApiIds apiId,
            CacheRefreshReason cacheInfo,
            bool isCacheSerialized = false,
            bool isLegacyCacheEnabled = true)
        {
            string[] telemetryCategories = requestMessage.Headers.GetValues(
                TelemetryConstants.XClientCurrentTelemetry).Single().Split('|');

            Assert.AreEqual(3, telemetryCategories.Length);
            Assert.AreEqual(1, telemetryCategories[0].Split(',').Length); // version
            Assert.AreEqual(5, telemetryCategories[1].Split(',').Length); // api_id, cache_info, region_used, region_source, region_outcome
            Assert.AreEqual(3, telemetryCategories[2].Split(',').Length); // platform_fields

            Assert.AreEqual(TelemetryConstants.HttpTelemetrySchemaVersion, telemetryCategories[0]); // version

            Assert.AreEqual(
                apiId.ToString("D"),
                telemetryCategories[1].Split(',')[0]); // current_api_id

            Assert.AreEqual(cacheInfo.ToString("D"), telemetryCategories[1].Split(',')[1]); // cache_info

            Assert.AreEqual(isCacheSerialized ? "1" : "0", telemetryCategories[2].Split(',')[0]); // is_cache_serialized

            Assert.AreEqual(isLegacyCacheEnabled ? "1" : "0", telemetryCategories[2].Split(',')[1]); // is_legacy_cache_enabled

            Assert.AreEqual(TokenType.Bearer.ToString("D"), telemetryCategories[2].Split(',')[2]);
        }

        private void AssertPreviousTelemetry(
           HttpRequestMessage requestMessage,
           int expectedSilentCount,
           ApiIds[] expectedFailedApiIds = null,
           Guid[] expectedCorrelationIds = null,
           string[] expectedErrors = null)
        {
            expectedFailedApiIds ??= Array.Empty<ApiIds>();
            expectedCorrelationIds ??= Array.Empty<Guid>();
            expectedErrors ??= Array.Empty<string>();

            var actualHeader = ParseLastRequestHeader(requestMessage);
            TestCommon.YieldTillSatisfied(() => actualHeader.SilentCount == expectedSilentCount);
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

        private (int SilentCount, string[] FailedApis, string[] CorrelationIds, string[] Errors) ParseLastRequestHeader(HttpRequestMessage requestMessage)
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
                .Where((_, index) => index % 2 == 0)
                .Where(it => !string.IsNullOrEmpty(it))
                .ToArray();
            string[] correlationIds = lastRequestParts[2]
                .Split(',')
                .Where((_, index) => index % 2 != 0)
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
