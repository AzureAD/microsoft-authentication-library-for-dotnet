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
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class ProactiveRefreshTests
    {
        [TestMethod]
        public async Task ProactiveRefreshTriggers_WithTelemetry_Test()
        {
            Trace.WriteLine("Add exporter to test the metrics for proactive token refresh");
            List<Metric> exportedMetrics = new();
            var meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter(OtelInstrumentation.MeterName)
                .AddInMemoryExporter(exportedMetrics)
                .Build();

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.MsalAppAzureAdMultipleOrgs).ConfigureAwait(false);
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string[] appScopes = new[] { "https://vault.azure.net/.default" };

            AuthenticationResult authResult;

            Trace.WriteLine("Create a confidential client application with certificate.");
            ConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority, true)
                .WithCertificate(cert)
                .BuildConcrete();

            Trace.WriteLine("Acquire a token from IDP.");
            authResult = await confidentialApp
                .AcquireTokenForClient(appScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

            Trace.WriteLine("Acquire a token from cache.");
            authResult = await confidentialApp
                .AcquireTokenForClient(appScopes)
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
                .AcquireTokenForClient(appScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, authResult.AuthenticationResultMetadata.CacheRefreshReason);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs < 50);

            meterProvider.ForceFlush();

            TestCommon.YieldTillSatisfied(()=>ValidateSuccessMetrics(meterProvider, exportedMetrics) == 4);  // Wait for the background process to complete

            exportedMetrics.Clear();
            meterProvider.Dispose();
        }

        private long ValidateSuccessMetrics(MeterProvider meterProvider, List<Metric> exportedMetrics)
        {
            Assert.AreEqual(5, exportedMetrics.Count);

            foreach (var metric in exportedMetrics)
            {
                if (metric.Name == "MsalSuccess")
                {
                    long successfulRequests = 0;

                    foreach (var metricPoint in metric.GetMetricPoints())
                    {
                        successfulRequests += metricPoint.GetSumLong();
                    }

                    return successfulRequests;
                }
            }

            return 0;
        }
    }
}
