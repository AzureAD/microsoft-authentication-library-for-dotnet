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
using OpenTelemetry.Metrics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Microsoft.Identity.Client.Platforms.Features.OpenTelemetry;

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
            s_activityProvider.ForceFlush();
            OTelInstrumentationUtil.VerifyMetrics(5, _exportedMetrics);
            OTelInstrumentationUtil.VerifyActivity(15, _exportedActivities);
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
