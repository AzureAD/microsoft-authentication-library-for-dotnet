// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Platforms.Features.OpenTelemetry;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.AuthExtension;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class OTelInstrumentationTests : TestBase
    {
        private MockHttpAndServiceBundle _harness;
        private ConfidentialClientApplication _cca;
        private static MeterProvider s_meterProvider;
        private readonly List<Metric> _exportedMetrics = new();

        private const string callerSdkId = "123";
        private const string callerSdkVersion = "1.1.1.1";
        private Dictionary<string, (string, bool)> extraQueryParams = new()
            {
                { "caller-sdk-id", (callerSdkId, false) },
                { "caller-sdk-ver", (callerSdkVersion, false) }
            };

        [TestCleanup]
        public override void TestCleanup()
        {
            s_meterProvider?.Dispose();

            _exportedMetrics.Clear();

            base.TestCleanup();
        }

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _harness = CreateTestHarness();

            s_meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter(OtelInstrumentation.MeterName)
                .AddInMemoryExporter(_exportedMetrics)
                .Build();
        }

        [TestMethod]
        public async Task AcquireTokenOTelTestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication();
                await AcquireTokenSuccessAsync().ConfigureAwait(false);
                await AcquireTokenMsalServiceExceptionAsync().ConfigureAwait(false);
                await AcquireTokenMsalClientExceptionAsync().ConfigureAwait(false);

                s_meterProvider.ForceFlush();
                var metricNames = _exportedMetrics.Select(m => m.Name).ToList();

                // V1 histograms emitted, V2 must not be
                CollectionAssert.Contains(metricNames, "MsalTotalDuration.1A");
                CollectionAssert.Contains(metricNames, "MsalDurationInHttp.1A");
                CollectionAssert.DoesNotContain(metricNames, "MsalTotalDurationV2.1A");
                CollectionAssert.DoesNotContain(metricNames, "MsalDurationInHttpV2.1A");

                VerifyMetrics(7, _exportedMetrics, 2, 2);
            }
        }

        [TestMethod]
        public async Task AcquireTokenOTelTestWithExtensionAsync()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication();
                await AcquireTokenSuccessAsync(true).ConfigureAwait(false);
                await AcquireTokenMsalServiceExceptionAsync().ConfigureAwait(false);
                await AcquireTokenMsalClientExceptionAsync().ConfigureAwait(false);

                s_meterProvider.ForceFlush();
                var metricNames = _exportedMetrics.Select(m => m.Name).ToList();

                // V1 histograms emitted, V2 must not be
                CollectionAssert.Contains(metricNames, "MsalTotalDuration.1A");
                CollectionAssert.Contains(metricNames, "MsalDurationInHttp.1A");
                CollectionAssert.DoesNotContain(metricNames, "MsalTotalDurationV2.1A");
                CollectionAssert.DoesNotContain(metricNames, "MsalDurationInHttpV2.1A");

                VerifyMetrics(7, _exportedMetrics, 2, 2);
            }
        }

        [TestMethod]
        [Description("MSAL_ENABLE_EXTENDED_TOKEN_METRICS opt-in emits MsalTotalDurationV2.1A and MsalDurationInHttpV2.1A instead of V1 equivalents.")]
        public async Task AcquireToken_WithExtendedMetrics_EmitsV2HistogramsAsync()
        {
            using (new EnvVariableContext())
            using (_harness = CreateTestHarness())
            {
                Environment.SetEnvironmentVariable(OtelInstrumentation.EnableExtendedTokenMetricsEnvVariable, "true");

                CreateApplication();
                await AcquireTokenSuccessAsync().ConfigureAwait(false);

                _harness.HttpManager.AddTokenResponse(TokenResponseType.InvalidClient);
                await AssertException.TaskThrowsAsync<MsalServiceException>(
                    () => _cca.AcquireTokenForClient(TestConstants.s_scopeForAnotherResource)
                        .WithExtraQueryParameters(extraQueryParams)
                        .WithTenantId(TestConstants.Utid)
                        .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                await AcquireTokenMsalClientExceptionAsync().ConfigureAwait(false);

                s_meterProvider.ForceFlush();
                var metricNames = _exportedMetrics.Select(m => m.Name).ToList();

                // V2 histograms emitted instead of V1
                CollectionAssert.Contains(metricNames, "MsalTotalDurationV2.1A");
                CollectionAssert.Contains(metricNames, "MsalDurationInHttpV2.1A");
                CollectionAssert.DoesNotContain(metricNames, "MsalTotalDuration.1A");
                CollectionAssert.DoesNotContain(metricNames, "MsalDurationInHttp.1A");

                // MsalTotalDurationV2.1A has Succeeded=true for the IDP success and Succeeded=false for the failure
                var totalDurationV2 = _exportedMetrics.Single(m => m.Name == "MsalTotalDurationV2.1A");
                bool hasSuccessPoint = false, hasFailurePoint = false;
                foreach (var point in totalDurationV2.GetMetricPoints())
                {
                    if ((bool)GetTagValue(point.Tags, TelemetryConstants.Succeeded))
                    {
                        hasSuccessPoint = true;
                    }
                    else
                    {
                        hasFailurePoint = true;
                        // No token was acquired on failure, so TokenSource is empty.
                        Assert.AreEqual(string.Empty, GetTagValue(point.Tags, TelemetryConstants.TokenSource),
                            "MsalTotalDurationV2.1A failure point should have an empty TokenSource");
                    }
                }
                Assert.IsTrue(hasSuccessPoint, "MsalTotalDurationV2.1A should have a point with Succeeded=true");
                Assert.IsTrue(hasFailurePoint, "MsalTotalDurationV2.1A should have a point with Succeeded=false");

                // MsalDurationInHttpV2.1A has HttpStatusCode=200 from the IDP success
                var httpDurationV2 = _exportedMetrics.Single(m => m.Name == "MsalDurationInHttpV2.1A");
                bool has200 = false;
                foreach (var point in httpDurationV2.GetMetricPoints())
                {
                    if ((int)GetTagValue(point.Tags, TelemetryConstants.HttpStatusCode) == 200)
                        has200 = true;
                }
                Assert.IsTrue(has200, "MsalDurationInHttpV2.1A should have a point with HttpStatusCode=200 for the IDP success");

                VerifyMetrics(7, _exportedMetrics, 2, 2);
            }
        }

        [TestMethod]
        [Description("Foreground failure without an HTTP call (MsalClientException from validation) records V2 total " +
            "with Succeeded=false and empty TokenSource, but does NOT record V2 HTTP duration (no HTTP exchange occurred).")]
        public async Task ForegroundFailure_NoHttpCall_WithExtendedMetrics_RecordsV2TotalButNotV2HttpAsync()
        {
            using (new EnvVariableContext())
            using (_harness = CreateTestHarness())
            {
                Environment.SetEnvironmentVariable(OtelInstrumentation.EnableExtendedTokenMetricsEnvVariable, "true");
                CreateApplication();

                // null scope triggers MsalClientException from parameter validation — no HTTP exchange happens.
                await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => _cca.AcquireTokenForClient(null)
                        .WithExtraQueryParameters(extraQueryParams)
                        .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                s_meterProvider.ForceFlush();

                // V2 total must record the failure point with empty TokenSource.
                var totalDurationV2 = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalTotalDurationV2.1A");
                Assert.IsNotNull(totalDurationV2,
                    "MsalTotalDurationV2.1A should be recorded for foreground failure even without an HTTP call.");

                bool hasFailurePoint = false;
                foreach (var point in totalDurationV2.GetMetricPoints())
                {
                    if (!(bool)GetTagValue(point.Tags, TelemetryConstants.Succeeded))
                    {
                        hasFailurePoint = true;
                        Assert.AreEqual(string.Empty, GetTagValue(point.Tags, TelemetryConstants.TokenSource),
                            "Failure point should have empty TokenSource (no token was acquired).");
                    }
                }
                Assert.IsTrue(hasFailurePoint, "MsalTotalDurationV2.1A should have a Succeeded=false point.");

                // V2 HTTP must NOT be recorded — no HTTP exchange occurred, so DurationInHttpInMs is 0.
                var httpDurationV2 = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalDurationInHttpV2.1A");
                Assert.IsNull(httpDurationV2,
                    "MsalDurationInHttpV2.1A should not be recorded when no HTTP call was made.");
            }
        }

        [TestMethod]
        [Description("Foreground failure with sub-millisecond total duration is still recorded on V2 total.")]
        public async Task ForegroundFailure_SubMillisecond_WithExtendedMetrics_RecordsV2TotalAsync()
        {
            using (new EnvVariableContext())
            using (_harness = CreateTestHarness())
            {
                Environment.SetEnvironmentVariable(OtelInstrumentation.EnableExtendedTokenMetricsEnvVariable, "true");
                CreateApplication();

                // MsalClientException from null scope is essentially synchronous and typically sub-ms.
                await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => _cca.AcquireTokenForClient(null)
                        .WithExtraQueryParameters(extraQueryParams)
                        .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                s_meterProvider.ForceFlush();

                var totalDurationV2 = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalTotalDurationV2.1A");
                Assert.IsNotNull(totalDurationV2,
                    "MsalTotalDurationV2.1A must be recorded for sub-ms foreground failures (no totalDurationInMs > 0 gate).");

                bool hasFailurePoint = false;
                foreach (var point in totalDurationV2.GetMetricPoints())
                {
                    if (!(bool)GetTagValue(point.Tags, TelemetryConstants.Succeeded))
                        hasFailurePoint = true;
                }
                Assert.IsTrue(hasFailurePoint,
                    "MsalTotalDurationV2.1A should have a Succeeded=false point even when totalDurationInMs is 0.");
            }
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD responds well to Refresh.")]
        public async Task ProactiveTokenRefresh_ValidResponse_ClientCredential_Async()
        {
            // Arrange
            using (_harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app");
                CreateApplication();

                _harness.HttpManager.AddAllMocks(TokenResponseType.Valid_ClientCredentials);
                AuthenticationResult result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                // Assert
                Assert.IsNotNull(result);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                var refreshOn = TestCommon.UpdateATWithRefreshOn(_cca.AppTokenCacheInternal.Accessor).RefreshOn;

                Trace.WriteLine("3. Configure AAD to respond with valid token to the refresh RT flow");
                _harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                // Act
                Trace.WriteLine("4. ATS - should perform an RT refresh");
                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Trace.WriteLine(result.AuthenticationResultMetadata.DurationTotalInMs);

                // Assert
                TestCommon.YieldTillSatisfied(() => _harness.HttpManager.QueueSize == 0);
                Assert.IsNotNull(result);
                Assert.AreEqual(0, _harness.HttpManager.QueueSize,
                    "MSAL should have refreshed the token because the original AT was marked for refresh");
                Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, result.AuthenticationResultMetadata.CacheRefreshReason);
                Assert.AreEqual(refreshOn, result.AuthenticationResultMetadata.RefreshOn);

                Trace.WriteLine("5. ATS - should not perform an RT refresh, as the token is still valid");
                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Trace.WriteLine(result.AuthenticationResultMetadata.DurationTotalInMs);

                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);

                s_meterProvider.ForceFlush();
                VerifyMetrics(6, _exportedMetrics, 4, 0);
            }
        }

        [TestMethod]
        [Description("Background proactive refresh success records HTTP duration in MsalDurationInHttp.1A " +
            "even when the foreground request was served from cache with no HTTP call.")]
        public async Task ProactiveTokenRefresh_ValidResponse_ClientCredential_RecordsBackgroundHttpDurationAsync()
        {
            using (_harness = base.CreateTestHarness())
            {
                CreateApplication();

                // Pre-populate cache so the foreground request is a cache hit with no HTTP call
                TokenCacheHelper.PopulateCache(_cca.AppTokenCacheInternal.Accessor, addSecondAt: false);
                TestCommon.UpdateATWithRefreshOn(_cca.AppTokenCacheInternal.Accessor);

                // Background refresh will hit the IDP
                _harness.HttpManager.AddAllMocks(TokenResponseType.Valid_ClientCredentials);

                // Foreground: served from cache (no HTTP), fires background refresh
                var result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                // Poll until the background metric appears — flush on each iteration to avoid races
                TestCommon.YieldTillSatisfied(() =>
                {
                    s_meterProvider.ForceFlush();
                    return _exportedMetrics.Any(m => m.Name == "MsalDurationInHttp.1A");
                });

                // MsalDurationInHttp.1A must be present — it can only come from the background IDP call
                // since the foreground request made no HTTP call. V2 must not be emitted.
                var metricNames = _exportedMetrics.Select(m => m.Name).ToList();
                CollectionAssert.Contains(metricNames, "MsalDurationInHttp.1A",
                    "Background refresh HTTP duration must be recorded in MsalDurationInHttp.1A");
                CollectionAssert.DoesNotContain(metricNames, "MsalDurationInHttpV2.1A");
            }
        }

        [TestMethod]
        [Description("Background proactive refresh success with extended metrics records HTTP duration in MsalDurationInHttpV2.1A " +
            "even when the foreground request was served from cache with no HTTP call.")]
        public async Task ProactiveTokenRefresh_Success_WithExtendedMetrics_RecordsV2HttpDurationAsync()
        {
            using (new EnvVariableContext())
            using (_harness = base.CreateTestHarness())
            {
                Environment.SetEnvironmentVariable(OtelInstrumentation.EnableExtendedTokenMetricsEnvVariable, "true");

                CreateApplication();

                // Pre-populate cache so the foreground request is a cache hit with no HTTP call
                TokenCacheHelper.PopulateCache(_cca.AppTokenCacheInternal.Accessor, addSecondAt: false);
                TestCommon.UpdateATWithRefreshOn(_cca.AppTokenCacheInternal.Accessor);

                // Background refresh will hit the IDP
                _harness.HttpManager.AddAllMocks(TokenResponseType.Valid_ClientCredentials);

                // Foreground: served from cache (no HTTP), fires background refresh
                var result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                // Poll until the background metric appears — flush on each iteration to avoid races
                TestCommon.YieldTillSatisfied(() =>
                {
                    s_meterProvider.ForceFlush();
                    return _exportedMetrics.Any(m => m.Name == "MsalDurationInHttpV2.1A");
                });

                // MsalDurationInHttpV2.1A must be present — it can only come from the background IDP call
                // since the foreground request made no HTTP call. V1 must not be emitted.
                var httpDurationV2 = _exportedMetrics.SingleOrDefault(m => m.Name == "MsalDurationInHttpV2.1A");
                Assert.IsNotNull(httpDurationV2, "Background refresh HTTP duration must be recorded in MsalDurationInHttpV2.1A");
                CollectionAssert.DoesNotContain(
                    _exportedMetrics.Select(m => m.Name).ToList(),
                    "MsalDurationInHttp.1A");

                bool has200 = false;
                foreach (var point in httpDurationV2.GetMetricPoints())
                {
                    if ((int)GetTagValue(point.Tags, TelemetryConstants.HttpStatusCode) == 200)
                        has200 = true;
                }
                Assert.IsTrue(has200, "MsalDurationInHttpV2.1A should have a point with HttpStatusCode=200 from the background success");
            }
        }

        [TestMethod]
        [Description("Setup AT in cache, needs refresh. MSI responds well to Refresh.")]
        public async Task ProactiveTokenRefresh_ValidResponse_MSI_Async()
        {
            string appServiceEndpoint = "http://127.0.0.1:41564/msi/token";
            string resource = "https://management.azure.com/";

            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ManagedIdentityTestUtil.SetEnvironmentVariables(ManagedIdentitySource.AppService, appServiceEndpoint);

                Trace.WriteLine("1. Setup an app with a token cache with one AT");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                var mi = miBuilder.BuildConcrete();

                httpManager.AddManagedIdentityMockHandler(
                        appServiceEndpoint,
                        resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        ManagedIdentitySource.AppService);

                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(resource)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                var refreshOn = TestCommon.UpdateATWithRefreshOn(mi.AppTokenCacheInternal.Accessor).RefreshOn;

                Trace.WriteLine("3. Configure MSI to respond with a valid token");
                httpManager.AddManagedIdentityMockHandler(
                        appServiceEndpoint,
                        resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        ManagedIdentitySource.AppService);

                // Act
                Trace.WriteLine("4. ATM - should perform an RT refresh");
                result = await mi.AcquireTokenForManagedIdentity(resource)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                TestCommon.YieldTillSatisfied(() => httpManager.QueueSize == 0);

                Assert.IsNotNull(result);

                Assert.AreEqual(0, httpManager.QueueSize,
                    "MSAL should have refreshed the token because the original AT was marked for refresh");
                
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, result.AuthenticationResultMetadata.CacheRefreshReason);
                Assert.AreEqual(refreshOn, result.AuthenticationResultMetadata.RefreshOn);

                result = await mi.AcquireTokenForManagedIdentity(resource)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);

                s_meterProvider.ForceFlush();
                VerifyMetrics(6, _exportedMetrics, 4, 0);
            }
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD responds well to Refresh.")]
        public async Task ProactiveTokenRefresh_ValidResponse_OBO_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                Trace.WriteLine("1. Setup an app with a token cache with one AT");
                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                string oboCacheKey = "obo-cache-key";
                var result = await cca.InitiateLongRunningProcessInWebApi(TestConstants.s_scope, TestConstants.DefaultAccessToken, ref oboCacheKey)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                TestCommon.UpdateATWithRefreshOn(cca.UserTokenCacheInternal.Accessor);

                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                Trace.WriteLine("3. Configure AAD to respond with a valid token");
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, result.AuthenticationResultMetadata.CacheRefreshReason);

                // Add delay to let the proactive refresh happen
                Thread.Sleep(1000);

                Trace.WriteLine("4. Fetch token from cache");
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);

                s_meterProvider.ForceFlush();
                VerifyMetrics(6, _exportedMetrics, 4, 0);
            }
        }

        [TestMethod]
        [Description("AT in cache, needs refresh. AAD is unavailable when refreshing.")]
        public async Task ProactiveTokenRefresh_AadUnavailableResponse_Async()
        {
            // Arrange
            using (_harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app ");
                CreateApplication();
                TokenCacheHelper.PopulateCache(_cca.AppTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                TestCommon.UpdateATWithRefreshOn(_cca.AppTokenCacheInternal.Accessor);

                TokenCacheAccessRecorder cacheAccess = _cca.AppTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with an error");
                _harness.HttpManager.AddAllMocks(TokenResponseType.Invalid_AADUnavailable503);
                _harness.HttpManager.AddTokenResponse(TokenResponseType.Invalid_AADUnavailable503);

                // Act
                AuthenticationResult result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result, "ClientCredentials should still succeeds even though AAD is unavailable");
                TestCommon.YieldTillSatisfied(() => _harness.HttpManager.QueueSize == 0);
                Assert.AreEqual(0, _harness.HttpManager.QueueSize);
                cacheAccess.WaitTo_AssertAcessCounts(1, 0); // the refresh failed, no new data is written to the cache

                // Now let AAD respond with tokens
                _harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.IsNotNull(result);

                cacheAccess.WaitTo_AssertAcessCounts(2, 1); // new tokens written to cache

                Thread.Sleep(1000);

                s_meterProvider.ForceFlush();
                VerifyMetrics(6, _exportedMetrics, 3, 1);
            }
        }

        [TestMethod]
        [Description("Background proactive refresh failure with extended metrics records MsalDurationInHttpV2.1A " +
            "with the HTTP status code but does not emit MsalTotalDurationV2.1A for the background failure path.")]
        public async Task ProactiveTokenRefresh_AadUnavailable_WithExtendedMetrics_RecordsHttpStatusCodeNotTotalDurationAsync()
        {
            using (new EnvVariableContext())
            using (_harness = base.CreateTestHarness())
            {
                Environment.SetEnvironmentVariable(OtelInstrumentation.EnableExtendedTokenMetricsEnvVariable, "true");

                Trace.WriteLine("1. Setup an app ");
                CreateApplication();
                TokenCacheHelper.PopulateCache(_cca.AppTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                TestCommon.UpdateATWithRefreshOn(_cca.AppTokenCacheInternal.Accessor);

                TokenCacheAccessRecorder cacheAccess = _cca.AppTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with an error");
                _harness.HttpManager.AddAllMocks(TokenResponseType.Invalid_AADUnavailable503);
                _harness.HttpManager.AddTokenResponse(TokenResponseType.Invalid_AADUnavailable503);

                // Act
                AuthenticationResult result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result, "ClientCredentials should still succeeds even though AAD is unavailable");
                TestCommon.YieldTillSatisfied(() => _harness.HttpManager.QueueSize == 0);
                Assert.AreEqual(0, _harness.HttpManager.QueueSize);
                cacheAccess.WaitTo_AssertAcessCounts(1, 0); // the refresh failed, no new data is written to the cache

                // Now let AAD respond with tokens
                _harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.IsNotNull(result);

                cacheAccess.WaitTo_AssertAcessCounts(2, 1); // new tokens written to cache

                Thread.Sleep(1000);

                s_meterProvider.ForceFlush();
                VerifyMetrics(6, _exportedMetrics, 3, 1);

                // MsalTotalDurationV2.1A should have no Succeeded=false point from the background path —
                // background failures call LogBackgroundFailureMetrics, which does not record total duration
                // (the foreground user already received their token from cache).
                var totalDurationV2 = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalTotalDurationV2.1A");
                if (totalDurationV2 != null)
                {
                    foreach (var point in totalDurationV2.GetMetricPoints())
                    {
                        Assert.IsTrue(
                            (bool)GetTagValue(point.Tags, TelemetryConstants.Succeeded),
                            "MsalTotalDurationV2.1A should not record Succeeded=false from the background path");
                    }
                }

                // MsalDurationInHttpV2.1A should have a point with HttpStatusCode=503 from the background failure
                var httpDurationV2 = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalDurationInHttpV2.1A");
                Assert.IsNotNull(httpDurationV2, "MsalDurationInHttpV2.1A should be recorded for background failure with an HTTP status code");
                bool has503 = false;
                foreach (var point in httpDurationV2.GetMetricPoints())
                {
                    if ((int)GetTagValue(point.Tags, TelemetryConstants.HttpStatusCode) == 503)
                        has503 = true;
                }
                Assert.IsTrue(has503, "MsalDurationInHttpV2.1A should have a point with HttpStatusCode=503 from the background failure");
            }
        }

        [TestMethod]
        [Description("Background proactive refresh failure on V1 (default — extended metrics disabled) increments " +
            "MsalFailure but does not emit either V2 histogram. Guards against V2 leakage in the default configuration.")]
        public async Task ProactiveTokenRefresh_BackgroundFailure_V1Default_NoV2MetricsAsync()
        {
            using (_harness = base.CreateTestHarness())
            {
                CreateApplication();
                TokenCacheHelper.PopulateCache(_cca.AppTokenCacheInternal.Accessor, addSecondAt: false);
                TestCommon.UpdateATWithRefreshOn(_cca.AppTokenCacheInternal.Accessor);

                // Background refresh will fail with 503.
                _harness.HttpManager.AddAllMocks(TokenResponseType.Invalid_AADUnavailable503);
                _harness.HttpManager.AddTokenResponse(TokenResponseType.Invalid_AADUnavailable503);

                // Foreground served from cache succeeds; background refresh fails fire-and-forget.
                AuthenticationResult result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.IsNotNull(result, "Foreground request should succeed from cache even though background refresh fails.");

                TestCommon.YieldTillSatisfied(() => _harness.HttpManager.QueueSize == 0);
                Thread.Sleep(1000);

                s_meterProvider.ForceFlush();

                // MsalFailure must include the background failure.
                var failureMetric = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalFailure");
                Assert.IsNotNull(failureMetric, "MsalFailure should be incremented by the background failure.");

                // V2 histograms must NOT be emitted because extended metrics are disabled.
                var metricNames = _exportedMetrics.Select(m => m.Name).ToList();
                CollectionAssert.DoesNotContain(metricNames, "MsalTotalDurationV2.1A",
                    "MsalTotalDurationV2.1A must not be emitted when MSAL_ENABLE_EXTENDED_TOKEN_METRICS is unset.");
                CollectionAssert.DoesNotContain(metricNames, "MsalDurationInHttpV2.1A",
                    "MsalDurationInHttpV2.1A must not be emitted when MSAL_ENABLE_EXTENDED_TOKEN_METRICS is unset.");
            }
        }

        private async Task AcquireTokenSuccessAsync(bool withExtension = false)
        {
            _harness.HttpManager.AddInstanceDiscoveryMockHandler();
            AuthenticationResult result;

            if (withExtension)
            {
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "someAccessTokenType");
                MsalAuthenticationExtension authExtension = new MsalAuthenticationExtension()
                {
                    AuthenticationOperation = new MsalTestAuthenticationOperation()
                };

                // Acquire token for client with scope
                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .WithAuthenticationExtension(authExtension)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);

                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .WithAuthenticationExtension(authExtension)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
            } 
            else
            {
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                // Acquire token for client with scope
                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);

                // Acquire token from the cache
                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
            }
        }

        private async Task AcquireTokenMsalServiceExceptionAsync()
        {
            _harness.HttpManager.AddTokenResponse(TokenResponseType.InvalidClient);

            //Test for MsalServiceException
            MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                () => _cca.AcquireTokenForClient(TestConstants.s_scopeForAnotherResource)
                .WithExtraQueryParameters(extraQueryParams)
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

            Assert.IsNotNull(ex);
            Assert.IsNotNull(ex.ErrorCode);
        }

        private async Task AcquireTokenMsalClientExceptionAsync()
        {
            //Test for MsalClientException
            MsalClientException exClient = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => _cca.AcquireTokenForClient(null) // null scope -> client exception
                .WithExtraQueryParameters(extraQueryParams)
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

            Assert.IsNotNull(exClient);
            Assert.IsNotNull(exClient.ErrorCode);
        }

        private void CreateApplication(CacheOptions cacheOptions = null)
        {
            var builder = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithExperimentalFeatures()
                        .WithAuthority(TestConstants.AuthorityUtidTenant)
                        .WithClientSecret(TestConstants.ClientSecret)
                        .WithHttpManager(_harness.HttpManager);

            if (cacheOptions != null)
            {
                builder = builder.WithCacheOptions(cacheOptions);
            }

            _cca = builder.BuildConcrete();
        }

        [TestMethod]
        public async Task DisableInternalCacheOptions_AcquireTokenForClient_OTelEmitsCacheDisabledReason_Async()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication(cacheOptions: CacheOptions.DisableInternalCacheOptions);

                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                AuthenticationResult result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(CacheRefreshReason.CacheDisabled, result.AuthenticationResultMetadata.CacheRefreshReason);

                s_meterProvider.ForceFlush();

                var msalSuccess = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalSuccess");
                Assert.IsNotNull(msalSuccess, "MsalSuccess metric should be emitted.");

                foreach (var metricPoint in msalSuccess.GetMetricPoints())
                {
                    AssertTagValue(metricPoint.Tags, TelemetryConstants.CacheRefreshReason, CacheRefreshReason.CacheDisabled);
                }
            }
        }

        [TestMethod]
        public async Task DisableInternalCacheOptions_AcquireTokenOnBehalfOf_OTelEmitsCacheDisabledReason_Async()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(_harness.HttpManager)
                    .WithCacheOptions(CacheOptions.DisableInternalCacheOptions)
                    .BuildConcrete();

                AuthenticationResult result = await cca.AcquireTokenOnBehalfOf(
                    TestConstants.s_scope, new UserAssertion(TestConstants.DefaultAccessToken))
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(CacheRefreshReason.CacheDisabled, result.AuthenticationResultMetadata.CacheRefreshReason);

                s_meterProvider.ForceFlush();

                var msalSuccess = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalSuccess");
                Assert.IsNotNull(msalSuccess, "MsalSuccess metric should be emitted.");

                foreach (var metricPoint in msalSuccess.GetMetricPoints())
                {
                    AssertTagValue(metricPoint.Tags, TelemetryConstants.CacheRefreshReason, CacheRefreshReason.CacheDisabled);
                }
            }
        }

        private void AssertTagValue(ReadOnlyTagCollection tags, string tagKey, object expectedValue)
        {
            IDictionary<string, object> tagDictionary = new Dictionary<string, object>();
            foreach (var tag in tags)
            {
                tagDictionary[tag.Key] = tag.Value;
            }
            Assert.IsTrue(tagDictionary.ContainsKey(tagKey), $"Tag '{tagKey}' is missing from metric point.");
            Assert.AreEqual(expectedValue, tagDictionary[tagKey], $"Tag '{tagKey}' has unexpected value.");
        }

        [TestMethod]
        public async Task MsalFailure_ServiceException_RawStsErrorCodeTag_IncludedAsync()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication();

                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddTokenResponse(TokenResponseType.InvalidClient);

                MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    () => _cca.AcquireTokenForClient(TestConstants.s_scopeForAnotherResource)
                        .WithExtraQueryParameters(extraQueryParams)
                        .WithTenantId(TestConstants.Utid)
                        .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                Assert.IsNotNull(ex.ErrorCodes, "ErrorCodes should be populated from IDP response.");

                s_meterProvider.ForceFlush();

                var failureMetric = _exportedMetrics.First(m => m.Name == "MsalFailure");
                foreach (var metricPoint in failureMetric.GetMetricPoints())
                {
                    var tags = GetTagDictionary(metricPoint.Tags);
                    Assert.IsTrue(tags.ContainsKey(TelemetryConstants.RawStsErrorCode),
                        "RawStsErrorCode tag should be present when the IDP response contains error_codes.");
                    Assert.AreEqual(ex.ErrorCodes.FirstOrDefault(), tags[TelemetryConstants.RawStsErrorCode]);
                }
            }
        }

        [TestMethod]
        public async Task MsalFailure_ClientException_RawStsErrorCodeTag_NotIncludedAsync()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication();

                // Null scope triggers MsalClientException before any HTTP call — no ErrorCodes
                MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => _cca.AcquireTokenForClient(null)
                        .WithExtraQueryParameters(extraQueryParams)
                        .WithTenantId(TestConstants.Utid)
                        .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                Assert.IsNotNull(ex);

                s_meterProvider.ForceFlush();

                var failureMetric = _exportedMetrics.First(m => m.Name == "MsalFailure");
                foreach (var metricPoint in failureMetric.GetMetricPoints())
                {
                    var tags = GetTagDictionary(metricPoint.Tags);
                    Assert.IsFalse(tags.ContainsKey(TelemetryConstants.RawStsErrorCode),
                        "RawStsErrorCode tag should not be present for non-service exceptions.");
                }
            }
        }

        [TestMethod]
        [Description("WithOtelTagsEnricher adds caller-supplied tags to MSAL's success metrics and receives a populated ExecutionResult.")]
        public async Task WithOtelTagsEnricher_SuccessfulAcquisition_AddsCustomTagAndReceivesResultAsync()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication();
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                bool? capturedSuccessful = null;
                bool capturedHasResult = false;

                // Do not assert inside the enricher — exceptions there are swallowed by design.
                AuthenticationResult result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .WithOtelTagsEnricher((executionResult, tags) =>
                    {
                        capturedSuccessful = executionResult.Successful;
                        capturedHasResult = executionResult.Result != null;
                        tags.Add(new KeyValuePair<string, object>("CustomTag", "CustomValue"));
                    })
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                s_meterProvider.ForceFlush();

                Assert.IsTrue(capturedSuccessful.HasValue, "Enricher should have been invoked.");
                Assert.IsTrue(capturedSuccessful.Value, "ExecutionResult.Successful should be true for a successful acquisition.");
                Assert.IsTrue(capturedHasResult, "ExecutionResult.Result should be populated for a successful acquisition.");

                var msalSuccess = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalSuccess");
                Assert.IsNotNull(msalSuccess, "MsalSuccess metric should be emitted.");

                bool foundCustomTag = false;
                foreach (var metricPoint in msalSuccess.GetMetricPoints())
                {
                    var tags = GetTagDictionary(metricPoint.Tags);
                    if (tags.TryGetValue("CustomTag", out var value) && (string)value == "CustomValue")
                        foundCustomTag = true;
                }
                Assert.IsTrue(foundCustomTag, "MsalSuccess should include the custom tag added by the enricher.");
            }
        }

        [TestMethod]
        [Description("WithOtelTagsEnricher adds caller-supplied tags to MSAL's failure metrics and receives an ExecutionResult carrying the exception.")]
        public async Task WithOtelTagsEnricher_FailedAcquisition_AddsCustomTagAndReceivesExceptionAsync()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication();
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddTokenResponse(TokenResponseType.InvalidClient);

                bool? capturedSuccessful = null;
                bool capturedHasException = false;

                await AssertException.TaskThrowsAsync<MsalServiceException>(
                    () => _cca.AcquireTokenForClient(TestConstants.s_scopeForAnotherResource)
                        .WithExtraQueryParameters(extraQueryParams)
                        .WithTenantId(TestConstants.Utid)
                        .WithOtelTagsEnricher((executionResult, tags) =>
                        {
                            capturedSuccessful = executionResult.Successful;
                            capturedHasException = executionResult.Exception != null;
                            tags.Add(new KeyValuePair<string, object>("CustomTag", "CustomValue"));
                        })
                        .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                s_meterProvider.ForceFlush();

                Assert.IsTrue(capturedSuccessful.HasValue, "Enricher should have been invoked.");
                Assert.IsFalse(capturedSuccessful.Value, "ExecutionResult.Successful should be false for a failed acquisition.");
                Assert.IsTrue(capturedHasException, "ExecutionResult.Exception should be populated for a failed acquisition.");

                var failureMetric = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalFailure");
                Assert.IsNotNull(failureMetric, "MsalFailure metric should be emitted.");

                bool foundCustomTag = false;
                foreach (var metricPoint in failureMetric.GetMetricPoints())
                {
                    var tags = GetTagDictionary(metricPoint.Tags);
                    if (tags.TryGetValue("CustomTag", out var value) && (string)value == "CustomValue")
                        foundCustomTag = true;
                }
                Assert.IsTrue(foundCustomTag, "MsalFailure should include the custom tag added by the enricher.");
            }
        }

        [TestMethod]
        [Description("For a non-MSAL failure the enricher still receives an ExecutionResult carrying a wrapper MsalException with failure metadata, while the original exception propagates unchanged to the caller.")]
        public async Task WithOtelTagsEnricher_NonMsalFailure_ReceivesWrappedExceptionWithMetadataAsync()
        {
            using (_harness = CreateTestHarness())
            {
                // A non-MSAL exception (e.g. a transport failure while a federated-credential callback fetches
                // the client assertion) propagates out of MSAL unwrapped. This exercises RequestBase's generic
                // catch, which must still hand the OTel enricher a populated ExecutionResult.Exception.
                var transportException = new HttpRequestException("Simulated transport failure while fetching the federated credential assertion.");

                Func<CancellationToken, Task<string>> throwingAssertion = async _ =>
                {
                    await Task.Yield();
                    throw transportException;
                };

                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .WithClientAssertion(throwingAssertion)
                    .WithHttpManager(_harness.HttpManager)
                    .BuildConcrete();

                _harness.HttpManager.AddInstanceDiscoveryMockHandler();

                bool? capturedSuccessful = null;
                Exception capturedException = null;

                var thrown = await AssertException.TaskThrowsAsync<HttpRequestException>(
                    () => cca.AcquireTokenForClient(TestConstants.s_scope)
                        .WithExtraQueryParameters(extraQueryParams)
                        .WithOtelTagsEnricher((executionResult, tags) =>
                        {
                            capturedSuccessful = executionResult.Successful;
                            capturedException = executionResult.Exception;
                            tags.Add(new KeyValuePair<string, object>("CustomTag", "CustomValue"));
                        })
                        .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                s_meterProvider.ForceFlush();

                // The caller must observe the ORIGINAL exception, never the telemetry-only wrapper.
                Assert.AreSame(transportException, thrown, "The original non-MSAL exception must propagate unchanged to the caller.");

                Assert.IsTrue(capturedSuccessful.HasValue, "Enricher should have been invoked.");
                Assert.IsFalse(capturedSuccessful.Value, "ExecutionResult.Successful should be false for a failed acquisition.");
                Assert.IsNotNull(capturedException, "ExecutionResult.Exception should be populated even for a non-MSAL failure.");

                var wrapper = capturedException as MsalException;
                Assert.IsNotNull(wrapper, "ExecutionResult.Exception should be surfaced as an MsalException wrapper for the enricher.");
                Assert.AreEqual(typeof(HttpRequestException).FullName, wrapper.ErrorCode, "The wrapper's ErrorCode should capture the originating exception type.");
                Assert.AreSame(transportException, wrapper.InnerException, "The original exception should be preserved as the wrapper's InnerException.");
                Assert.AreEqual(transportException.Message, wrapper.Message, "The wrapper should retain the original exception message.");

                Assert.IsNotNull(wrapper.AuthenticationResultMetadata, "The wrapper should carry failure metadata for the enricher.");

                var failureMetric = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalFailure");
                Assert.IsNotNull(failureMetric, "MsalFailure metric should be emitted.");

                bool foundCustomTag = false;
                foreach (var metricPoint in failureMetric.GetMetricPoints())
                {
                    var tags = GetTagDictionary(metricPoint.Tags);
                    if (tags.TryGetValue("CustomTag", out var value) && (string)value == "CustomValue")
                        foundCustomTag = true;
                }
                Assert.IsTrue(foundCustomTag, "MsalFailure should include the custom tag added by the enricher.");
            }
        }

        [TestMethod]
        [Description("A throwing OTel tags enricher must not break the token acquisition or telemetry recording, and a warning is logged.")]
        public async Task WithOtelTagsEnricher_ThrowingEnricher_DoesNotBreakAcquisitionAndLogsWarningAsync()
        {
            using (_harness = CreateTestHarness())
            {
                var warnings = new List<string>();
                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(_harness.HttpManager)
                    .WithLogging((level, message, containsPii) =>
                    {
                        if (level == LogLevel.Warning)
                        {
                            lock (warnings) { warnings.Add(message); }
                        }
                    })
                    .BuildConcrete();

                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                AuthenticationResult result = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .WithOtelTagsEnricher((executionResult, tags) => throw new InvalidOperationException("boom"))
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result, "Acquisition should succeed even if the enricher throws.");

                s_meterProvider.ForceFlush();
                var msalSuccess = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalSuccess");
                Assert.IsNotNull(msalSuccess, "MsalSuccess metric should still be emitted when the enricher throws.");

                lock (warnings)
                {
                    int enricherWarnings = warnings.Count(m => m.Contains("OTel tags enricher threw an exception"));
                    Assert.AreEqual(1, enricherWarnings,
                        "A throwing enricher runs once per acquisition and must log exactly one warning, not one per metric instrument.");
                }
            }
        }

        [TestMethod]
        [Description("An enricher that tries to clear or mutate the tag list it receives cannot remove MSAL's canonical tags — " +
            "the enricher only ever sees its own additions list, so the canonical metric set is append-only.")]
        public async Task WithOtelTagsEnricher_AttemptsToRemoveCanonicalTags_CanonicalTagsArePreservedAsync()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication();
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                AuthenticationResult result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .WithOtelTagsEnricher((executionResult, tags) =>
                    {
                        // A hostile/buggy enricher tries to wipe and overwrite the canonical tags.
                        tags.Clear();
                        tags.Add(new KeyValuePair<string, object>("CustomTag", "CustomValue"));
                    })
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                s_meterProvider.ForceFlush();

                var msalSuccess = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalSuccess");
                Assert.IsNotNull(msalSuccess, "MsalSuccess metric should be emitted.");

                foreach (var metricPoint in msalSuccess.GetMetricPoints())
                {
                    var tags = GetTagDictionary(metricPoint.Tags);

                    // Canonical tags survive despite the enricher's Clear().
                    Assert.IsTrue(tags.ContainsKey(TelemetryConstants.MsalVersion), "Canonical MsalVersion tag must be preserved.");
                    Assert.IsTrue(tags.ContainsKey(TelemetryConstants.Platform), "Canonical Platform tag must be preserved.");
                    Assert.IsTrue(tags.ContainsKey(TelemetryConstants.ApiId), "Canonical ApiId tag must be preserved.");

                    // The enricher's own addition is still applied on top.
                    Assert.IsTrue(tags.TryGetValue("CustomTag", out var value) && (string)value == "CustomValue",
                        "The enricher's added tag should still be present.");
                }
            }
        }

        [TestMethod]
        [Description("The OTel tags enricher is invoked exactly once per acquisition, not once per metric instrument, " +
            "even though several instruments are recorded for a single successful acquisition.")]
        public async Task WithOtelTagsEnricher_InvokedOncePerAcquisitionAsync()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication();
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                int invocationCount = 0;

                AuthenticationResult result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .WithOtelTagsEnricher((executionResult, tags) =>
                    {
                        Interlocked.Increment(ref invocationCount);
                        tags.Add(new KeyValuePair<string, object>("CustomTag", "CustomValue"));
                    })
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                s_meterProvider.ForceFlush();

                // A single IDP success records several instruments (success counter, total duration, HTTP duration,
                // extension duration, remaining token lifetime). The enricher must still run only once.
                Assert.AreEqual(1, invocationCount,
                    "Enricher must be invoked exactly once per acquisition, regardless of how many instruments are recorded.");

                // The single materialized tag set is still merged into every recorded instrument.
                var msalSuccess = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalSuccess");
                Assert.IsNotNull(msalSuccess, "MsalSuccess metric should be emitted.");
                bool foundCustomTag = false;
                foreach (var metricPoint in msalSuccess.GetMetricPoints())
                {
                    var tags = GetTagDictionary(metricPoint.Tags);
                    if (tags.TryGetValue("CustomTag", out var value) && (string)value == "CustomValue")
                        foundCustomTag = true;
                }
                Assert.IsTrue(foundCustomTag, "The single materialized tag set should be merged into the recorded metrics.");
            }
        }

        [TestMethod]
        [Description("An enricher tag whose key collides with a canonical tag key is dropped (the canonical value wins), " +
            "and tags with null/empty keys are skipped, so the enricher cannot override or corrupt the canonical metric set.")]
        public async Task WithOtelTagsEnricher_CollidingAndInvalidKeys_AreDroppedAsync()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication();
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                AuthenticationResult result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(extraQueryParams)
                    .WithOtelTagsEnricher((executionResult, tags) =>
                    {
                        // Collides with a canonical key — must NOT override MSAL's value.
                        tags.Add(new KeyValuePair<string, object>(TelemetryConstants.ApiId, "BOGUS_OVERRIDE"));
                        // Invalid keys — must be skipped without breaking recording.
                        tags.Add(new KeyValuePair<string, object>(null, "nullKey"));
                        tags.Add(new KeyValuePair<string, object>(string.Empty, "emptyKey"));
                        // A normal tag still gets through.
                        tags.Add(new KeyValuePair<string, object>("CustomTag", "CustomValue"));
                    })
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result, "Acquisition should succeed even when the enricher adds colliding/invalid tags.");

                s_meterProvider.ForceFlush();

                var msalSuccess = _exportedMetrics.FirstOrDefault(m => m.Name == "MsalSuccess");
                Assert.IsNotNull(msalSuccess, "MsalSuccess metric should be emitted.");

                foreach (var metricPoint in msalSuccess.GetMetricPoints())
                {
                    var tags = GetTagDictionary(metricPoint.Tags);

                    // Canonical ApiId is preserved — the colliding enricher value is dropped.
                    Assert.AreNotEqual("BOGUS_OVERRIDE", tags[TelemetryConstants.ApiId],
                        "The enricher must not override the canonical ApiId tag.");

                    // The empty-key tag is skipped (a null-key tag is likewise skipped before recording).
                    Assert.IsFalse(tags.ContainsKey(string.Empty), "Empty-key tag should be skipped.");

                    // The valid custom tag still made it through.
                    Assert.IsTrue(tags.TryGetValue("CustomTag", out var value) && (string)value == "CustomValue",
                        "A valid custom tag should still be recorded.");
                }
            }
        }

        [TestMethod]
        [Description("MsalMetricsCatalog is internally consistent: it maps every metric-name constant it declares, " +
            "and every canonical tag it references is one of the declared tag-name constants.")]
        public void MsalMetricsCatalog_IsInternallyConsistent()
        {
            var metricNames = new[]
            {
                MsalMetricsCatalog.SuccessCounterName,
                MsalMetricsCatalog.FailureCounterName,
                MsalMetricsCatalog.TotalDurationHistogramName,
                MsalMetricsCatalog.TotalDurationV2HistogramName,
                MsalMetricsCatalog.DurationInL1CacheHistogramName,
                MsalMetricsCatalog.DurationInL2CacheHistogramName,
                MsalMetricsCatalog.DurationInHttpHistogramName,
                MsalMetricsCatalog.DurationInHttpV2HistogramName,
                MsalMetricsCatalog.DurationInExtensionHistogramName,
                MsalMetricsCatalog.RemainingTokenLifetimeHistogramName,
            };

            foreach (var name in metricNames)
            {
                Assert.IsTrue(MsalMetricsCatalog.CanonicalTagsByMetric.ContainsKey(name),
                    $"Metric-name constant '{name}' has no canonical-tag entry in MsalMetricsCatalog.CanonicalTagsByMetric.");
            }

            Assert.HasCount(metricNames.Length, MsalMetricsCatalog.CanonicalTagsByMetric,
                "CanonicalTagsByMetric has an entry for a metric not declared as a metric-name constant (or vice versa).");

            var knownTags = new HashSet<string>(StringComparer.Ordinal)
            {
                TelemetryConstants.MsalVersion,
                TelemetryConstants.MsalVersionPlatform,
                TelemetryConstants.Platform,
                TelemetryConstants.ApiId,
                TelemetryConstants.CallerSdkId,
                TelemetryConstants.TokenSource,
                TelemetryConstants.CacheRefreshReason,
                TelemetryConstants.CacheLevel,
                TelemetryConstants.TokenType,
                TelemetryConstants.ErrorCode,
                TelemetryConstants.RawStsErrorCode,
                TelemetryConstants.Succeeded,
                TelemetryConstants.HttpStatusCode,
            };

            foreach (var entry in MsalMetricsCatalog.CanonicalTagsByMetric)
            {
                foreach (var tag in entry.Value)
                {
                    Assert.Contains(tag, knownTags,
                        $"Metric '{entry.Key}' references canonical tag '{tag}' that is not a declared tag-name constant.");
                }
            }

            // The meter name is owned by OtelInstrumentation (it constructs the Meter); the catalog only
            // declares the metric-to-canonical-tag mapping.
        }

        [TestMethod]
        [Description("MsalMetricsCatalog stays in sync with what MSAL actually emits: every emitted metric has a catalog " +
            "entry, and every tag MSAL emits for a metric is declared as a canonical tag for that metric in the catalog.")]
        public async Task MsalMetricsCatalog_MatchesEmittedMetricsAndTagsAsync()
        {
            using (_harness = CreateTestHarness())
            {
                CreateApplication();
                await AcquireTokenSuccessAsync().ConfigureAwait(false);
                await AcquireTokenMsalServiceExceptionAsync().ConfigureAwait(false);
                await AcquireTokenMsalClientExceptionAsync().ConfigureAwait(false);

                s_meterProvider.ForceFlush();

                Assert.IsGreaterThan(0, _exportedMetrics.Count, "Expected at least one metric to be exported.");

                foreach (Metric metric in _exportedMetrics)
                {
                    Assert.IsTrue(
                        MsalMetricsCatalog.CanonicalTagsByMetric.TryGetValue(metric.Name, out var canonicalTags),
                        $"Metric '{metric.Name}' is emitted by MSAL but missing from MsalMetricsCatalog.CanonicalTagsByMetric.");

                    foreach (var metricPoint in metric.GetMetricPoints())
                    {
                        foreach (var tag in metricPoint.Tags)
                        {
                            Assert.IsTrue(
                                canonicalTags.Contains(tag.Key),
                                $"Metric '{metric.Name}' emitted tag '{tag.Key}', which is not declared as a canonical tag in MsalMetricsCatalog.");
                        }
                    }
                }
            }
        }

        private static IDictionary<string, object> GetTagDictionary(ReadOnlyTagCollection tags)
        {
            var dict = new Dictionary<string, object>();
            foreach (var tag in tags)
                dict[tag.Key] = tag.Value;
            return dict;
        }

        private void VerifyMetrics(int expectedMetricCount, List<Metric> exportedMetrics,
            long expectedSuccessfulRequests, long expectedFailedRequests)
        {
            Assert.HasCount(expectedMetricCount, exportedMetrics, "Count of metrics recorded is not as expected.");

            foreach (Metric exportedItem in exportedMetrics)
            {
                List<string> expectedTags = new List<string>();

                Assert.AreEqual(OtelInstrumentation.MeterName, exportedItem.MeterName);

                switch (exportedItem.Name)
                {
                    case "MsalSuccess":
                        Trace.WriteLine("Verify the metrics captured for MsalSuccess counter.");
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.CallerSdkId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);
                        expectedTags.Add(TelemetryConstants.CacheLevel);
                        expectedTags.Add(TelemetryConstants.TokenType);

                        long totalSuccessfulRequests = 0;
                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            totalSuccessfulRequests += metricPoint.GetSumLong();
                            AssertTags(metricPoint.Tags, expectedTags, true);
                        }

                        Assert.AreEqual(expectedSuccessfulRequests, totalSuccessfulRequests);

                        break;
                    case "MsalFailure":
                        Trace.WriteLine("Verify the metrics captured for MsalFailure counter.");
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ErrorCode);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.CallerSdkId);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);
                        expectedTags.Add(TelemetryConstants.TokenType);

                        long totalFailedRequests = 0;
                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            totalFailedRequests += metricPoint.GetSumLong();
                            var pointExpectedTags = new List<string>(expectedTags);
                            if (GetTagDictionary(metricPoint.Tags).ContainsKey(TelemetryConstants.RawStsErrorCode))
                                pointExpectedTags.Add(TelemetryConstants.RawStsErrorCode);
                            AssertTags(metricPoint.Tags, pointExpectedTags, true);
                        }

                        Assert.AreEqual(expectedFailedRequests, totalFailedRequests);

                        break;

                    case "MsalTotalDuration.1A":
                        Trace.WriteLine("Verify the metrics captured for MsalTotalDuration.1A histogram.");
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);
                        expectedTags.Add(TelemetryConstants.TokenType);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, expectedTags);
                        }

                        break;

                    case "MsalDurationInL1CacheInUs.1B":
                        Trace.WriteLine("Verify the metrics captured for MsalDurationInL1CacheInUs.1B histogram.");
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, expectedTags);
                        }

                        break;

                    case "MsalDurationInL2Cache.1A":
                        Trace.WriteLine("Verify the metrics captured for MsalDurationInL2Cache.1A histogram.");
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, expectedTags);
                        }

                        break;

                    case "MsalDurationInHttp.1A":
                        Trace.WriteLine("Verify the metrics captured for MsalDurationInHttp.1A histogram.");
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenType);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, expectedTags);
                        }

                        break;

                    case "MsalDurationInExtensionInMs.1B":
                        Trace.WriteLine("Verify the metrics captured for MsalDurationInExtensionInMs.1B histogram.");
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);
                        expectedTags.Add(TelemetryConstants.TokenType);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, expectedTags);
                        }

                        break;

                    case "MsalTotalDurationV2.1A":
                        Trace.WriteLine("Verify the metrics captured for MsalTotalDurationV2.1A histogram.");
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersionPlatform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);
                        expectedTags.Add(TelemetryConstants.TokenType);
                        expectedTags.Add(TelemetryConstants.ErrorCode);
                        expectedTags.Add(TelemetryConstants.Succeeded);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, expectedTags);
                        }

                        break;

                    case "MsalDurationInHttpV2.1A":
                        Trace.WriteLine("Verify the metrics captured for MsalDurationInHttpV2.1A histogram.");
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersionPlatform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenType);
                        expectedTags.Add(TelemetryConstants.HttpStatusCode);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, expectedTags);
                        }

                        break;

                    case "MsalDurationBearerOperation.1B":
                        Trace.WriteLine("Verify the metrics captured for MsalDurationBearerOperation.1B histogram.");
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);
                        expectedTags.Add(TelemetryConstants.TokenType);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, expectedTags);
                        }

                        break;

                    case "MsalRemainingTokenLifetime.1A":
                        Trace.WriteLine("Verify the metrics captured for MsalRemainingTokenLifetime.1A histogram.");
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersionPlatform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);
                        expectedTags.Add(TelemetryConstants.TokenType);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, expectedTags);
                            Assert.IsGreaterThan((long)0, metricPoint.GetHistogramCount(), "Histogram should have at least one recorded value.");
                            Assert.IsGreaterThanOrEqualTo(0.0, metricPoint.GetHistogramSum(), "Remaining token lifetime should be non-negative.");
                        }

                        break;
                    default:
                        Assert.Fail("Unexpected metrics logged.");
                        break;
                }
            }
        }

        private static object GetTagValue(ReadOnlyTagCollection tags, string key)
        {
            foreach (var tag in tags)
                if (tag.Key == key) return tag.Value;
            return null;
        }

        private void AssertTags(ReadOnlyTagCollection tags, List<string> expectedTags, bool expectCallerSdkDetails = false)
        {
            Assert.AreEqual(expectedTags.Count, tags.Count);
            IDictionary<string, object> tagDictionary = new Dictionary<string, object>();

            foreach (var tag in tags)
            {
                tagDictionary[tag.Key] = tag.Value;
            }

            if (expectCallerSdkDetails)
            {
                Assert.AreEqual(callerSdkId, tagDictionary[TelemetryConstants.CallerSdkId]);
            }

            foreach (var expectedTag in expectedTags)
            {
                Assert.IsNotNull(tagDictionary[expectedTag], $"Tag {expectedTag} is missing.");
            }
        }
    }
}
