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

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{

    [TestClass]
    public class OTelTests
    {
        private static MetricReader? _reader;
        private ICollection<Metric> ExportedItems;
        private ICollection<Activity> ExportedActivity;

        [TestCleanup]
        public void TestCleanup()
        {
            _reader?.Shutdown();
            _reader?.Dispose();
            ExportedItems?.Clear();
            ExportedActivity?.Clear();

            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        [DataRow(TargetFrameworks.NetFx | TargetFrameworks.NetCore | TargetFrameworks.NetStandard )]
        public async Task WithCertificate_TestAsync(TargetFrameworks runOn)
        {
            runOn.AssertFramework();
            ExportMetricsAndActivity();
            await RunClientCredsAsync().ConfigureAwait(false);

            await Task.Delay(60000).ConfigureAwait(true);
            VerifyMetrics();
            VerifyActivity();
        }

        private void ExportMetricsAndActivity()
        {
            ExportedItems = new List<Metric>();
            ExportedActivity = new List<Activity>();
            var inMemoryExporter = new InMemoryExporter<Metric>(ExportedItems);
            _reader = new PeriodicExportingMetricReader(inMemoryExporter)
            {
                TemporalityPreference = MetricReaderTemporalityPreference.Delta

            };

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("ID4S_MSAL")
                .AddReader(_reader)
                .Build();

            using var activityProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource("MSAL_Activity")
                .AddInMemoryExporter(ExportedActivity)
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
        }

        private void VerifyActivity()
        {
            Assert.AreEqual(1, ExportedActivity.Count);
            foreach (var activity in ExportedActivity)
            {
                Assert.AreEqual(6, activity.Tags.Count());
            }
        }

        private void VerifyMetrics()
        {
            Assert.AreEqual(4, ExportedItems.Count);

            foreach (var exportedItem in ExportedItems)
            {
                Assert.AreEqual("ID4S_MSAL", exportedItem.MeterName);


                switch (exportedItem.Name)
                {
                    case "MsalSuccess":
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);
                        break;
                    case "MsalFailed":
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);
                        break;

                    case "MsalTotalDurationHistogram":
                    case "MsalDurationInCacheHistogram":
                    case "MsalDurationInHttpHistogram":
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
