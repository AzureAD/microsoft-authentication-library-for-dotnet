// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Platforms.Features.OpenTelemetry;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
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
        [Description("AT in cache, needs refresh. AAD responds well to Refresh.")]
        public async Task ProactiveTokenRefresh_ValidResponse_Async()
        {
            // Arrange
            using (_harness = base.CreateTestHarness())
            {
                Trace.WriteLine("1. Setup an app");
                CreateApplication();
                TokenCacheHelper.PopulateCache(_cca.AppTokenCacheInternal.Accessor, addSecondAt: false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                var refreshOn = TestCommon.UpdateATWithRefreshOn(_cca.AppTokenCacheInternal.Accessor).RefreshOn;

                TokenCacheAccessRecorder cacheAccess = _cca.AppTokenCache.RecordAccess();

                Trace.WriteLine("3. Configure AAD to respond with valid token to the refresh RT flow");
                _harness.HttpManager.AddAllMocks(TokenResponseType.Valid_ClientCredentials);

                // Act
                Trace.WriteLine("4. ATS - should perform an RT refresh");
                AuthenticationResult result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                TestCommon.YieldTillSatisfied(() => _harness.HttpManager.QueueSize == 0);
                Assert.IsNotNull(result);
                Assert.AreEqual(0, _harness.HttpManager.QueueSize,
                    "MSAL should have refreshed the token because the original AT was marked for refresh");
                cacheAccess.WaitTo_AssertAcessCounts(1, 1);
                Assert.IsTrue(result.AuthenticationResultMetadata.CacheRefreshReason == CacheRefreshReason.ProactivelyRefreshed);
                Assert.IsTrue(result.AuthenticationResultMetadata.RefreshOn == refreshOn);

                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                                    .ExecuteAsync()
                                    .ConfigureAwait(false);

                Assert.IsTrue(result.AuthenticationResultMetadata.CacheRefreshReason == CacheRefreshReason.NotApplicable);

                s_meterProvider.ForceFlush();
                VerifyMetrics(3, _exportedMetrics, 2, 0);
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
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.IsNotNull(result);

                cacheAccess.WaitTo_AssertAcessCounts(2, 1); // new tokens written to cache

                Thread.Sleep(1000);

                s_meterProvider.ForceFlush();
                VerifyMetrics(4, _exportedMetrics, 2, 1);
            }
        }

        private async Task AcquireTokenSuccessAsync()
        {
            _harness.HttpManager.AddInstanceDiscoveryMockHandler();
            _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

            // Acquire token for client with scope
            var result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result);

            // Acquire token from the cache
            //var account = (await _cca.GetAccountsAsync().ConfigureAwait(false)).Single();
            result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result);
        }

        private async Task AcquireTokenMsalServiceExceptionAsync()
        {
            _harness.HttpManager.AddTokenResponse(TokenResponseType.InvalidClient);

            //Test for MsalServiceException
            MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                () => _cca.AcquireTokenForClient(TestConstants.s_scopeForAnotherResource)
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
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

            Assert.IsNotNull(exClient);
            Assert.IsNotNull(exClient.ErrorCode);
        }

        private void CreateApplication()
        {
            _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority(TestConstants.AuthorityUtidTenant)
                        .WithClientSecret(TestConstants.ClientSecret)
                        .WithHttpManager(_harness.HttpManager)
                        .BuildConcrete();
        }

        private void VerifyMetrics(int expectedMetricCount, List<Metric> exportedMetrics, 
            long expectedSuccessfulRequests, long expectedFailedRequests)
        {
            Assert.AreEqual(expectedMetricCount, exportedMetrics.Count);

            foreach (Metric exportedItem in exportedMetrics)
            {
                List<string> expectedTags = new List<string>();

                Assert.AreEqual(OtelInstrumentation.MeterName, exportedItem.MeterName);

                switch (exportedItem.Name)
                {
                    case "MsalSuccess":
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

                        long totalSuccessfulRequests = 0;
                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            totalSuccessfulRequests += metricPoint.GetSumLong();
                            AssertTags(metricPoint.Tags, 6, expectedTags);
                        }

                        Assert.AreEqual(expectedSuccessfulRequests, totalSuccessfulRequests);

                        break;
                    case "MsalFailure":
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ErrorCode);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.IsProactiveRefresh);

                        long totalFailedRequests = 0;
                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            totalFailedRequests += metricPoint.GetSumLong();
                            AssertTags(metricPoint.Tags, 5, expectedTags);
                        }

                        Assert.AreEqual(expectedFailedRequests, totalFailedRequests);

                        break;

                    case "MsalTotalDuration.1A":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, 5, expectedTags);
                        }

                        break;

                    case "MsalDurationInL1CacheInUs.1B":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, 5, expectedTags);
                        }

                        break;

                    case "MsalDurationInL2Cache.1A":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, 3, expectedTags);
                        }

                        break;

                    case "MsalDurationInHttp.1A":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);

                        foreach (var metricPoint in exportedItem.GetMetricPoints())
                        {
                            AssertTags(metricPoint.Tags, 3, expectedTags);
                        }

                        break;

                    default:
                        Assert.Fail("Unexpected metrics logged.");
                        break;
                }

                
            }
        }

        private void AssertTags(ReadOnlyTagCollection tags, int expectedTagCount, List<string> expectedTags)
        {
            Assert.AreEqual(expectedTagCount, tags.Count);
            IDictionary<string, object> tagDictionary = new Dictionary<string, object>();

            foreach (var tag in tags)
            {
                tagDictionary[tag.Key] = tag.Value;
            }

            foreach (var expectedTag in expectedTags)
            {
                Assert.IsNotNull(tagDictionary[expectedTag], $"Tag {expectedTag} is missing.");
            }
        }
    }
}
