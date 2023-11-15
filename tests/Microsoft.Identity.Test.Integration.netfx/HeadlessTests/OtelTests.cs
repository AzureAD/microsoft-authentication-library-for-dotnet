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

            s_meterProvider.ForceFlush();
            OTelInstrumentationUtil.VerifyMetrics(5, _exportedMetrics);
            VerifyActivity(15);
        }

        private async Task RunClientCredsAsync()
        {
            IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(Cloud.Public);

            AuthenticationResult authResult;

            IConfidentialClientApplication confidentialApp = CreateApp(settings);

            // Acquire token from IDDP
            authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);

            // Acquire token from cache
            authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);

            // Get back a service exception.
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
