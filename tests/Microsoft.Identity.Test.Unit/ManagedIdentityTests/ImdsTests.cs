// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsTests : TestBase
    {
        private const double ONE_HUNDRED_TIMES_FASTER = 0.01;
        private const int IMDS_EXPONENTIAL_STRATEGY_TWO_RETRIES_IN_MS = 3000; // 1 -> 2

        private static int _originalMinBackoff;
        private static int _originalMaxBackoff;
        private static int _originalDeltaBackoff;
        private static int _originalGoneRetryAfter;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            // Backup original retry delay values
            _originalMinBackoff = ImdsRetryPolicy.MIN_EXPONENTIAL_BACKOFF_MS;
            _originalMaxBackoff = ImdsRetryPolicy.MAX_EXPONENTIAL_BACKOFF_MS;
            _originalDeltaBackoff = ImdsRetryPolicy.EXPONENTIAL_DELTA_BACKOFF_MS;
            _originalGoneRetryAfter = ImdsRetryPolicy.HTTP_STATUS_GONE_RETRY_AFTER_MS;

            // Speed up retry delays by 100x
            ImdsRetryPolicy.MIN_EXPONENTIAL_BACKOFF_MS = (int)(_originalMinBackoff * ONE_HUNDRED_TIMES_FASTER);
            ImdsRetryPolicy.MAX_EXPONENTIAL_BACKOFF_MS = (int)(_originalMaxBackoff * ONE_HUNDRED_TIMES_FASTER);
            ImdsRetryPolicy.EXPONENTIAL_DELTA_BACKOFF_MS = (int)(_originalDeltaBackoff * ONE_HUNDRED_TIMES_FASTER);
            ImdsRetryPolicy.HTTP_STATUS_GONE_RETRY_AFTER_MS = (int)(_originalGoneRetryAfter * ONE_HUNDRED_TIMES_FASTER);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Restore retry policy values after each test
            ImdsRetryPolicy.MIN_EXPONENTIAL_BACKOFF_MS = _originalMinBackoff;
            ImdsRetryPolicy.MAX_EXPONENTIAL_BACKOFF_MS = _originalMaxBackoff;
            ImdsRetryPolicy.EXPONENTIAL_DELTA_BACKOFF_MS = _originalDeltaBackoff;
            ImdsRetryPolicy.HTTP_STATUS_GONE_RETRY_AFTER_MS = _originalGoneRetryAfter;
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails404TwiceThenSucceeds200Async(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsHost);

                ManagedIdentityId managedIdentityId = userAssignedId == null
                    ? ManagedIdentityId.SystemAssigned
                    : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                var miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                    .WithHttpManager(httpManager);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                // Simulate two 404s (to trigger retries), then a successful response
                const int NUM_404S = 2;
                for (int i = 0; i < NUM_404S; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.ImdsEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(),
                        ManagedIdentitySource.Imds,
                        statusCode: HttpStatusCode.NotFound,
                        userAssignedId: userAssignedId,
                        userAssignedIdentityId: userAssignedIdentityId);
                }

                // Final success
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    userAssignedId: userAssignedId,
                    userAssignedIdentityId: userAssignedIdentityId);

                var stopwatch = Stopwatch.StartNew();

                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                     .ExecuteAsync()
                                     .ConfigureAwait(false);

                stopwatch.Stop();

                // ensure that each retry followed the exponential backoff strategy
                // 2 x exponential backoff (1 second -> 2 seconds)
                Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (IMDS_EXPONENTIAL_STRATEGY_TWO_RETRIES_IN_MS * ONE_HUNDRED_TIMES_FASTER));

                Assert.AreEqual(httpManager.ExecutedRequestCount, 3); // request + 2 retries
                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
            }
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.BadRequest, ImdsManagedIdentitySource.IdentityUnavailableError, 1, DisplayName = "BadRequest - Identity Unavailable")]
        [DataRow(HttpStatusCode.BadGateway, ImdsManagedIdentitySource.GatewayError, 1, DisplayName = "BadGateway - Gateway Error")]
        [DataRow(HttpStatusCode.GatewayTimeout, ImdsManagedIdentitySource.GatewayError, 4, DisplayName = "GatewayTimeout - Gateway Error Retries")]
        public async Task ImdsErrorHandlingTestAsync(HttpStatusCode statusCode, string expectedErrorSubstring, int expectedAttempts)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, "http://169.254.169.254");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                // Adding multiple mock handlers to simulate retries for GatewayTimeout
                for (int i = 0; i < expectedAttempts; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(ManagedIdentityTests.ImdsEndpoint, ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(), ManagedIdentitySource.Imds, statusCode: statusCode);
                }

                // Expecting a MsalServiceException indicating an error
                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.Imds.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.IsTrue(ex.Message.Contains(expectedErrorSubstring), $"The error message is not as expected. Error message: {ex.Message}. Expected message should contain: {expectedErrorSubstring}");
            }
        }
    }
}
