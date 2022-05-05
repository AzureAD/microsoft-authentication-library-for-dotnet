// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Throttling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Client.TelemetryCore.Internal.Events.ApiEvent;

namespace Microsoft.Identity.Test.Unit.TelemetryTests
{
    [TestClass]
    public class RegionalTelemetryTests : BaseRegionTelemetryTests
    {
        /// <summary>
        /// 1.  Acquire Token For Client with Region successfully
        ///        Current_request = 4 | ATC_ID, 0, centralus, 3, 4 | 
        ///        Last_request = 4 | 0 | | |
        /// 
        /// 2. Acquire Token for client with Region -> HTTP error 503 (Service Unavailable)
        ///
        ///        Current_request = 4 | ATC_ID, 1, centralus, 2, 4 |
        ///        Last_request = 4 | 0 | | |
        ///
        /// 3. Acquire Token For Client with Region -> successful
        ///
        /// Sent to the server - 
        ///        Current_request = 4 | ATC_ID, 1, centralus, 2, 4 |
        ///        Last_request = 4 | 0 |  ATC_ID, corr_step_2  | ServiceUnavailable | centralus, 3
        /// </summary>
        [TestMethod]
        public async Task TelemetryAcceptanceTestAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            Trace.WriteLine("Step 1. Acquire Token For Client with region successful");
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.Success).ConfigureAwait(false);
            AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.EnvVariable.ToString("D"),
                RegionOutcome.AutodetectSuccess.ToString("D"));
            AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);

            Trace.WriteLine("Step 2. Acquire Token For Client -> HTTP 5xx error (i.e. AAD is down)");
            result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.AADUnavailableError).ConfigureAwait(false);
            Guid step2CorrelationId = result.Correlationid;

            // we can assert telemetry here, as it will be sent to AAD. However, AAD is down, so it will not record it.
            AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.Cache.ToString("D"),
                RegionOutcome.AutodetectSuccess.ToString("D"));
            AssertPreviousTelemetry(
                result.HttpRequest,
                expectedSilentCount: 0);

            // the 5xx error puts MSAL in a throttling state, so "wait" until this clears
            _harness.ServiceBundle.ThrottlingManager.SimulateTimePassing(
                HttpStatusProvider.s_throttleDuration.Add(TimeSpan.FromSeconds(1)));

            Trace.WriteLine("Step 3. Acquire Token For Client -> Success");
            result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.Success, true).ConfigureAwait(false);

            AssertCurrentTelemetry(result.HttpRequest, ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.Cache.ToString("D"),
                RegionOutcome.AutodetectSuccess.ToString("D"));
        }

        /// <summary>
        /// Acquire token for client with serialized token cache successfully
        ///    Current_request = 4 | ATC_ID, 0, centralus, 3, 4 |
        ///    Last_request = 4 | 0 | | |
        /// </summary>
        [TestMethod]
        public async Task TelemetrySerializedTokenCacheTestAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            Trace.WriteLine("Acquire token for client with token serialization.");
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.Success, serializeCache: true)
                .ConfigureAwait(false);
            AssertCurrentTelemetry(result.HttpRequest,
                ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.EnvVariable.ToString("D"),
                RegionOutcome.AutodetectSuccess.ToString("D"),
                isCacheSerialized: true);
            AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);
        }

        [TestMethod]
        public async Task TelemetryAutoDiscoveryFailsTestsAsync()
        {
            Trace.WriteLine("Acquire token for client with region detection fails.");
            _harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Get, TestConstants.ImdsUrl);
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.FallbackToGlobal).ConfigureAwait(false);
            AssertCurrentTelemetry(
                result.HttpRequest,
                ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.FailedAutoDiscovery.ToString("D"),
                RegionOutcome.FallbackToGlobal.ToString("D"),
                isCacheSerialized: false,
                region: "");
            AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);
        }

        [TestMethod]
        public async Task TelemetryUserProvidedRegionAutoDiscoveryFailsTestsAsync()
        {
            Trace.WriteLine("Acquire token for client with region provided by user and region detection fails.");
            _harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Get, TestConstants.ImdsUrl);
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.UserProvidedRegion).ConfigureAwait(false);
            AssertCurrentTelemetry(
                result.HttpRequest,
                ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.FailedAutoDiscovery.ToString("D"),
                RegionOutcome.UserProvidedAutodetectionFailed.ToString("D"),
                isCacheSerialized: false,
                region: TestConstants.Region);
            AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);
        }

        /// <summary>
        /// Acquire token for client with regionToUse when auto region discovery passes with region same as regionToUse
        ///    Current_request = 4 | ATC_ID, 0, centralus, 3, 1 |
        ///    Last_request = 4 | 0 | | |
        /// </summary>
        [TestMethod]
        public async Task TelemetryUserProvidedRegionAutoDiscoverRegionSameTestsAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            Trace.WriteLine("Acquire token for client with region provided by user and region detected is same as regionToUse.");
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.UserProvidedRegion).ConfigureAwait(false);
            AssertCurrentTelemetry(result.HttpRequest,
                ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.EnvVariable.ToString("D"),
                RegionOutcome.UserProvidedValid.ToString("D"),
                isCacheSerialized: false,
                region: TestConstants.Region);
            AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);
        }

        /// <summary>
        /// Acquire token for client with regionToUse when auto region discovery passes with region different from regionToUse
        ///    Current_request = 4 | ATC_ID, 0, centralus, 3, 1 |
        ///    Last_request = 4 | 0 | | |
        /// </summary>
        [TestMethod]
        public async Task TelemetryUserProvidedRegionAutoDiscoverRegionMismatchTestsAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            Trace.WriteLine("Acquire token for client with region provided by user and region detected mismatches regionToUse.");
            var result = await RunAcquireTokenForClientAsync(AcquireTokenForClientOutcome.UserProvidedInvalidRegion).ConfigureAwait(false);
            AssertCurrentTelemetry(result.HttpRequest,
                ApiIds.AcquireTokenForClient,
                RegionAutodetectionSource.EnvVariable.ToString("D"),
                RegionOutcome.UserProvidedInvalid.ToString("D"),
                isCacheSerialized: false,
                region: TestConstants.InvalidRegion);
            AssertPreviousTelemetry(result.HttpRequest, expectedSilentCount: 0);
        }
    }
}
