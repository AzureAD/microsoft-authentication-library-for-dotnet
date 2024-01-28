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
                VerifyMetrics(5, _exportedMetrics);
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

            // Acquire token silently
            var account = (await _cca.GetAccountsAsync().ConfigureAwait(false)).Single();
            result = await _cca.AcquireTokenSilent(TestConstants.s_scope, account)
                .ExecuteAsync().ConfigureAwait(false);
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
            TokenCacheHelper.PopulateCache(_cca.UserTokenCacheInternal.Accessor);
        }

        private void VerifyMetrics(int expectedMetricCount, List<Metric> exportedMetrics)
        {
            Assert.AreEqual(expectedMetricCount, exportedMetrics.Count);

            foreach (Metric exportedItem in exportedMetrics)
            {
                int expectedTagCount = 0;
                var expectedTags = new List<string>();

                Assert.AreEqual(OtelInstrumentation.MeterName, exportedItem.MeterName);

                switch (exportedItem.Name)
                {
                    case "MsalSuccess":
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);

                        expectedTagCount = 6;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheRefreshReason);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

                        break;
                    case "MsalFailure":
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);

                        expectedTagCount = 3;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ErrorCode);

                        break;

                    case "MsalTotalDuration.1A":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTagCount = 5;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

                        break;

                    case "MsalDurationInL1CacheInUs.1B":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTagCount = 5;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

                        break;

                    case "MsalDurationInL2Cache.1A":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTagCount = 3;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);

                        break;

                    case "MsalDurationInHttp.1A":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTagCount = 3;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);

                        break;

                    default:
                        Assert.Fail("Unexpected metrics logged.");
                        break;
                }

                foreach (var metricPoint in exportedItem.GetMetricPoints())
                {
                    AssertTags(metricPoint.Tags, expectedTagCount, expectedTags);
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
