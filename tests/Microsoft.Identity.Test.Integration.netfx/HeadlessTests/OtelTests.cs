// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Linq;
using Microsoft.Identity.Client.TelemetryCore.OpenTelemetry;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{

    [TestClass]
    public class OTelTests
    {
        private static MeterProvider s_meterProvider;
        private static TracerProvider s_activityProvider;
        private readonly List<Metric> _exportedMetrics = new();
        private readonly List<Activity> _exportedActivities = new();

        [TestCleanup]
        public void TestCleanup()
        {
            s_meterProvider?.Dispose();
            s_activityProvider?.Dispose();

            TestCommon.ResetInternalStaticCaches();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();

            s_meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter(OtelInstrumentation.MeterName)
                .AddInMemoryExporter(_exportedMetrics)
                .Build();

            s_activityProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(OtelInstrumentation.ActivitySourceName)
                .AddInMemoryExporter(_exportedActivities)
                .Build();
        }

        [TestMethod]
        [DataRow( TargetFrameworks.NetFx | TargetFrameworks.NetCore | TargetFrameworks.NetStandard )]
        public async Task OTelClientCredWithCertificate_TestAsync(TargetFrameworks runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync().ConfigureAwait(false);

            VerifyMetrics(4);
            VerifyActivity(15);
        }

        private async Task RunClientCredsAsync()
        {
            IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(Cloud.Public);

            AuthenticationResult authResult;

            IConfidentialClientApplication confidentialApp = CreateApp(settings);

            authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);

            authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);

            try
            {
                authResult = await confidentialApp
                .AcquireTokenForClient(new string[] { "wrongScope" })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            } catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(MsalServiceException));
            }
            
        }

        private void VerifyActivity(int expectedTagCount)
        {
            s_activityProvider.ForceFlush();

            Assert.AreEqual(1, _exportedActivities.Count);
            foreach (var activity in _exportedActivities)
            {
                Assert.AreEqual(OtelInstrumentation.ActivitySourceName, activity.Source.Name);
                Assert.AreEqual(expectedTagCount, activity.Tags.Count());
            }
        }

        private void VerifyMetrics(int expectedMetricCount)
        {
            
            s_meterProvider.ForceFlush();

            Assert.AreEqual(expectedMetricCount, _exportedMetrics.Count);

            foreach (Metric exportedItem in _exportedMetrics)
            {
                int expectedTagCount = 0;
                List<string> expectedTags = new();

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
                        expectedTags.Add(TelemetryConstants.CacheInfoTelemetry);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

                        break;
                    case "MsalFailed":
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
                    case "MsalDurationInCache.1A":
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

        private static IConfidentialClientApplication CreateApp(IConfidentialAppSettings settings)
        {
            var builder = ConfidentialClientApplicationBuilder
                .Create(settings.ClientId)
                .WithAuthority(settings.Authority, true)
                .WithCertificate(settings.GetCertificate())
                .WithTestLogging();

            var confidentialApp = builder.Build();
            return confidentialApp;
        }

    }
}
