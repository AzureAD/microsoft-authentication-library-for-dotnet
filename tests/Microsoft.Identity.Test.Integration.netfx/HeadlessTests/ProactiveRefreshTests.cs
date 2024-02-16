// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Platforms.Features.OpenTelemetry;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class ProactiveRefreshTests
    {
        [TestMethod]
        public async Task TestProactiveTokenWithTelemetry()
        {
            Trace.WriteLine("Add exporter to test the metrics for proactive token refresh");
            List<Metric> exportedMetrics = new();
            var meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter(OtelInstrumentation.MeterName)
                .AddInMemoryExporter(exportedMetrics)
                .Build();

            IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(Cloud.Public);
            settings.UseAppIdUri = true;

            AuthenticationResult authResult;

            Trace.WriteLine("Create a confidential client application with certificate.");
            ConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder
                .Create(settings.ClientId)
                .WithAuthority(settings.Authority, true)
                .WithCertificate(settings.GetCertificate())
                .BuildConcrete();

            Trace.WriteLine("Acquire a token from IDP.");
            authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs > 50);

            Trace.WriteLine("Acquire a token from cache.");
            authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(CacheRefreshReason.NotApplicable, authResult.AuthenticationResultMetadata.CacheRefreshReason);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs < 50);

            Trace.WriteLine("Update the refresh token in the cache to trigger proactive refresh.");
            TestCommon.UpdateATWithRefreshOn(confidentialApp.AppTokenCacheInternal.Accessor);

            Trace.WriteLine("Acquire a token from cache with proactive refresh.");
            authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, authResult.AuthenticationResultMetadata.CacheRefreshReason);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs < 50);

            Thread.Sleep(5000);  // Wait for the background process to complete

            meterProvider.ForceFlush();
            Assert.AreEqual(4, exportedMetrics.Count);

            foreach (var metric in exportedMetrics)
            {
                if (metric.Name == "MsalSuccess")
                {
                    var successfulRequests = 0;
                    foreach (var metricPoint in metric.GetMetricPoints())
                    {
                        successfulRequests += (int)metricPoint.GetSumLong();
                    }

                    Trace.WriteLine("Assert that there are 4 successful requests logged including 1 for background refresh.");
                    Assert.AreEqual(4, successfulRequests);
                }
            }
        }
    }
}
