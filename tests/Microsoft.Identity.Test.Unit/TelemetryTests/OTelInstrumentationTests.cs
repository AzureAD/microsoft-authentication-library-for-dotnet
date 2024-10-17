// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
                VerifyMetrics(5, _exportedMetrics, 2, 2);
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
                VerifyMetrics(6, _exportedMetrics, 2, 2);
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
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
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
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
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
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Trace.WriteLine(result.AuthenticationResultMetadata.DurationTotalInMs);

                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);

                s_meterProvider.ForceFlush();
                VerifyMetrics(4, _exportedMetrics, 4, 0);
            }
        }

        [TestMethod]
        [Description("Setup AT in cache, needs refresh. MSI responds well to Refresh.")]
        public async Task ProactiveTokenRefresh_ValidResponse_MSI_Async()
        {
            string appServiceEndpoint = "http://127.0.0.1:41564/msi/token";
            string resource = "https://management.azure.com/";

            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityTestUtil.SetEnvironmentVariables(ManagedIdentitySource.AppService, appServiceEndpoint);

                Trace.WriteLine("1. Setup an app with a token cache with one AT");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.BuildConcrete();

                httpManager.AddManagedIdentityMockHandler(
                        appServiceEndpoint,
                        resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        ManagedIdentitySource.AppService);

                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(resource)
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
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
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
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
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);

                s_meterProvider.ForceFlush();
                VerifyMetrics(4, _exportedMetrics, 4, 0);
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
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                TestCommon.UpdateATWithRefreshOn(cca.UserTokenCacheInternal.Accessor);

                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                Trace.WriteLine("3. Configure AAD to respond with a valid token");
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey)
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, result.AuthenticationResultMetadata.CacheRefreshReason);

                // Add delay to let the proactive refresh happen
                Thread.Sleep(1000);

                Trace.WriteLine("4. Fetch token from cache");
                result = await cca.AcquireTokenInLongRunningProcess(TestConstants.s_scope, oboCacheKey)
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TestConstants.ATSecret, result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);

                s_meterProvider.ForceFlush();
                VerifyMetrics(4, _exportedMetrics, 4, 0);
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
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
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
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.IsNotNull(result);

                cacheAccess.WaitTo_AssertAcessCounts(2, 1); // new tokens written to cache

                Thread.Sleep(1000);

                s_meterProvider.ForceFlush();
                VerifyMetrics(3, _exportedMetrics, 3, 1);
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
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
                    .WithAuthenticationExtension(authExtension)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);

                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
                    .WithAuthenticationExtension(authExtension)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
            } 
            else
            {
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                // Acquire token for client with scope
                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);

                // Acquire token from the cache
                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
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
                .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
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
                .WithExtraQueryParameters(new Dictionary<string, string> { { "caller-sdk-id", callerSdkId }, { "caller-sdk-ver", callerSdkVersion } })
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

            Assert.IsNotNull(exClient);
            Assert.IsNotNull(exClient.ErrorCode);
        }

        private void CreateApplication()
        {
            _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithExperimentalFeatures()
                        .WithAuthority(TestConstants.AuthorityUtidTenant)
                        .WithClientSecret(TestConstants.ClientSecret)
                        .WithHttpManager(_harness.HttpManager)
                        .BuildConcrete();
        }

        private void VerifyMetrics(int expectedMetricCount, List<Metric> exportedMetrics, 
            long expectedSuccessfulRequests, long expectedFailedRequests)
        {
            Assert.AreEqual(expectedMetricCount, exportedMetrics.Count, "Count of metrics recorded is not as expected.");

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
                        expectedTags.Add(TelemetryConstants.CallerSdkVersion);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

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
                        expectedTags.Add(TelemetryConstants.CallerSdkVersion);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);

                        long totalFailedRequests = 0;
                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            totalFailedRequests += metricPoint.GetSumLong();
                            AssertTags(metricPoint.Tags, expectedTags, true);
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

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, expectedTags);
                        }

                        break;

                    default:
                        Assert.Fail("Unexpected metrics logged.");
                        break;
                }
            }
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
                Assert.AreEqual(callerSdkVersion, tagDictionary[TelemetryConstants.CallerSdkVersion]);
            }

            foreach (var expectedTag in expectedTags)
            {
                Assert.IsNotNull(tagDictionary[expectedTag], $"Tag {expectedTag} is missing.");
            }
        }
    }
}
