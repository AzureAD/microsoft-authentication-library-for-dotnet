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

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{

    [TestClass]
    public class OTelTests
    {
        private static MeterProvider s_meterProvider;
        private static TracerProvider s_activityProvider;

        [TestCleanup]
        public void TestCleanup()
        {
            s_meterProvider?.Dispose();
            s_activityProvider?.Dispose();

            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        [DataRow(TargetFrameworks.NetFx |  TargetFrameworks.NetCore | TargetFrameworks.NetStandard )]
        public async Task WithCertificate_TestAsync(TargetFrameworks runOn)
        {
            var exportedMetrics = new List<Metric>();
            var exportedActivities = new List<Activity>();

            runOn.AssertFramework();
            ExportMetricsAndActivity(exportedMetrics, exportedActivities);
            await RunClientCredsAsync().ConfigureAwait(false);

            Thread.Sleep(70000);

            VerifyMetrics(exportedMetrics, 4);
            VerifyActivity(exportedActivities, 15);
        }

        private void ExportMetricsAndActivity(List<Metric> exportedMetrics, List<Activity> exportedActivities)
        {
            int collectionPeriodMilliseconds = 30000;

            s_meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter(OtelInstrumentation.MeterName)
                .AddInMemoryExporter(exportedMetrics, metricReaderOptions =>
                {
                    metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = collectionPeriodMilliseconds;
                    metricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                })
                .Build();

            s_activityProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(OtelInstrumentation.ActivitySourceName)
                .AddInMemoryExporter(exportedActivities)
                .Build();
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

        private void VerifyActivity(List<Activity> exportedActivities, int expectedTagCount)
        {
            s_activityProvider.ForceFlush();

            Assert.AreEqual(1, exportedActivities.Count);
            foreach (var activity in exportedActivities)
            {
                Assert.AreEqual(OtelInstrumentation.ActivitySourceName, activity.Source.Name);
                Assert.AreEqual(expectedTagCount, activity.Tags.Count());
            }
        }

        private void VerifyMetrics(List<Metric> exportedMetrics, int expectedMetricCount)
        {
            s_meterProvider.ForceFlush();

            Assert.AreEqual(expectedMetricCount, exportedMetrics.Count);

            foreach (Metric exportedItem in exportedMetrics)
            {
                Assert.AreEqual(OtelInstrumentation.MeterName, exportedItem.MeterName);


                switch (exportedItem.Name)
                {
                    case "MsalSuccess":
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);
                        break;
                    case "MsalFailed":
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);
                        break;

                    case "MsalTotalDuration.1A":
                    case "MsalDurationInCache.1A":
                    case "MsalDurationInHttp.1A":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);
                        break;

                    default:
                        Assert.Fail("Unexpected metrics logged.");
                        break;

                }
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
