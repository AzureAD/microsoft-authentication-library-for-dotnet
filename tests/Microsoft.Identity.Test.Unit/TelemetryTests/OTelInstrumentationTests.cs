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
using Microsoft.IdentityModel.Abstractions;
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
        private static TracerProvider s_activityProvider;
        private readonly List<Metric> _exportedMetrics = new();
        private readonly List<Activity> _exportedActivities = new();

        [TestCleanup]
        public override void TestCleanup()
        {
            s_meterProvider?.Dispose();
            s_activityProvider?.Dispose();

            _exportedMetrics.Clear();
            _exportedActivities.Clear();

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

            s_activityProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(OtelInstrumentation.ActivitySourceName)
                .AddInMemoryExporter(_exportedActivities)
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
                s_activityProvider.ForceFlush();
                OTelInstrumentationUtil.VerifyMetrics(5, _exportedMetrics);
                OTelInstrumentationUtil.VerifyActivity(5, _exportedActivities);
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

            //Test for MsalClientException
            MsalClientException exClient = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => _cca.AcquireTokenForClient(null) // null scope -> client exception
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

            Assert.IsNotNull(exClient);
            Assert.IsNotNull(exClient.ErrorCode);
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
    }
}
