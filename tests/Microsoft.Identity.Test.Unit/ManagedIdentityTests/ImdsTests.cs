// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Http.Retry;
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
        private static int s_originalMinBackoff;
        private static int s_originalMaxBackoff;
        private static int s_originalDeltaBackoff;
        private static int s_originalGoneRetryAfter;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            // Backup original retry delay values
            s_originalMinBackoff = ImdsRetryPolicy.MinExponentialBackoffMs;
            s_originalMaxBackoff = ImdsRetryPolicy.MaxExponentialBackoffMs;
            s_originalDeltaBackoff = ImdsRetryPolicy.ExponentialDeltaBackoffMs;
            s_originalGoneRetryAfter = ImdsRetryPolicy.HttpStatusGoneRetryAfterMs;

            // Speed up retry delays by 100x
            ImdsRetryPolicy.MinExponentialBackoffMs = (int)(s_originalMinBackoff * TestConstants.ONE_HUNDRED_TIMES_FASTER);
            ImdsRetryPolicy.MaxExponentialBackoffMs = (int)(s_originalMaxBackoff * TestConstants.ONE_HUNDRED_TIMES_FASTER);
            ImdsRetryPolicy.ExponentialDeltaBackoffMs = (int)(s_originalDeltaBackoff * TestConstants.ONE_HUNDRED_TIMES_FASTER);
            ImdsRetryPolicy.HttpStatusGoneRetryAfterMs = (int)(s_originalGoneRetryAfter * TestConstants.ONE_HUNDRED_TIMES_FASTER);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Restore retry policy values after each test
            ImdsRetryPolicy.MinExponentialBackoffMs = s_originalMinBackoff;
            ImdsRetryPolicy.MaxExponentialBackoffMs = s_originalMaxBackoff;
            ImdsRetryPolicy.ExponentialDeltaBackoffMs = s_originalDeltaBackoff;
            ImdsRetryPolicy.HttpStatusGoneRetryAfterMs = s_originalGoneRetryAfter;
        }

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();

            ImdsRetryPolicy.NumRetries = 0;
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails404TwiceThenSucceeds200Async(
            string userAssignedId,
            UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);

                ManagedIdentityId managedIdentityId = userAssignedId == null
                    ? ManagedIdentityId.SystemAssigned
                    : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                    .WithHttpManager(httpManager);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Simulate two 404s (to trigger retries), then a successful response
                const int NUM_404 = 2;
                for (int i = 0; i < NUM_404; i++)
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

                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                        .ExecuteAsync()
                                        .ConfigureAwait(false);

                stopwatch.Stop();

                // exponential backoff (1 second -> 2 seconds)
                const int ImdsExponentialStrategyTwoRetriesInMs = 3000;
                Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (ImdsExponentialStrategyTwoRetriesInMs * TestConstants.ONE_HUNDRED_TIMES_FASTER));

                // ensure that exactly 3 requests were made: initial request + 2 retries
                Assert.AreEqual(ImdsRetryPolicy.NumRetries, NUM_404);
                Assert.AreEqual(httpManager.QueueSize, 0);

                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails410FourTimesThenSucceeds200Async(
            string userAssignedId,
            UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);
                
                ManagedIdentityId managedIdentityId = userAssignedId == null
                    ? ManagedIdentityId.SystemAssigned
                    : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                    .WithHttpManager(httpManager);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Simulate four 410s (to trigger retries), then a successful response
                const int NUM_410 = 4;
                for (int i = 0; i < NUM_410; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.ImdsEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(),
                        ManagedIdentitySource.Imds,
                        statusCode: HttpStatusCode.Gone,
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

                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                        .ExecuteAsync()
                                        .ConfigureAwait(false);

                stopwatch.Stop();

                // linear backoff (10 seconds * 4 retries)
                Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (ImdsRetryPolicy.HttpStatusGoneRetryAfterMs * NUM_410 * TestConstants.ONE_HUNDRED_TIMES_FASTER));

                // ensure that exactly 5 requests were made: initial request + 4 retries
                Assert.AreEqual(ImdsRetryPolicy.NumRetries, NUM_410);
                Assert.AreEqual(httpManager.QueueSize, 0);

                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails410PermanentlyAsync(
            string userAssignedId,
            UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);
                
                ManagedIdentityId managedIdentityId = userAssignedId == null
                    ? ManagedIdentityId.SystemAssigned
                    : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                    .WithHttpManager(httpManager);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Simulate permanent 410s (to trigger the maximum number of retries)
                const int NUM_410 = ImdsRetryPolicy.LinearStrategyNumRetries + 1; // initial request + maximum number of retries (7)
                for (int i = 0; i < NUM_410; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.ImdsEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(),
                        ManagedIdentitySource.Imds,
                        statusCode: HttpStatusCode.Gone,
                        userAssignedId: userAssignedId,
                        userAssignedIdentityId: userAssignedIdentityId);
                }

                MsalServiceException msalException = null;
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .ExecuteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    msalException = ex as MsalServiceException;
                }
                stopwatch.Stop();
                Assert.IsNotNull(msalException);

                // linear backoff (10 seconds * 7 retries)
                Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (ImdsRetryPolicy.HttpStatusGoneRetryAfterMs * ImdsRetryPolicy.LinearStrategyNumRetries * TestConstants.ONE_HUNDRED_TIMES_FASTER));

                // ensure that exactly 8 requests were made: initial request + 7 retries
                Assert.AreEqual(ImdsRetryPolicy.NumRetries, ImdsRetryPolicy.LinearStrategyNumRetries);
                Assert.AreEqual(httpManager.QueueSize, 0);
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails504PermanentlyAsync(
            string userAssignedId,
            UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);
                
                ManagedIdentityId managedIdentityId = userAssignedId == null
                    ? ManagedIdentityId.SystemAssigned
                    : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                    .WithHttpManager(httpManager);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Simulate permanent 504s (to trigger the maximum number of retries)
                const int NUM_504 = ImdsRetryPolicy.ExponentialStrategyNumRetries + 1; // initial request + maximum number of retries (3)
                for (int i = 0; i < NUM_504; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.ImdsEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(),
                        ManagedIdentitySource.Imds,
                        statusCode: HttpStatusCode.GatewayTimeout,
                        userAssignedId: userAssignedId,
                        userAssignedIdentityId: userAssignedIdentityId);
                }

                MsalServiceException msalException = null;
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .ExecuteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    msalException = ex as MsalServiceException;
                }
                stopwatch.Stop();
                Assert.IsNotNull(msalException);

                // exponential backoff (1 second -> 2 seconds -> 4 seconds)
                const int ImdsExponentialStrategyMaxRetriesInMs = 7000;
                Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (ImdsExponentialStrategyMaxRetriesInMs * TestConstants.ONE_HUNDRED_TIMES_FASTER));

                // ensure that exactly 4 requests were made: initial request + 3 retries
                Assert.AreEqual(ImdsRetryPolicy.NumRetries, ImdsRetryPolicy.ExponentialStrategyNumRetries);
                Assert.AreEqual(httpManager.QueueSize, 0);
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails400WhichIsNonRetriableAndRetryPolicyIsNotTriggeredAsync(
            string userAssignedId,
            UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);
                
                ManagedIdentityId managedIdentityId = userAssignedId == null
                    ? ManagedIdentityId.SystemAssigned
                    : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                    .WithHttpManager(httpManager);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiImdsErrorResponse(),
                    ManagedIdentitySource.Imds,
                    statusCode: HttpStatusCode.BadRequest,
                    userAssignedId: userAssignedId,
                    userAssignedIdentityId: userAssignedIdentityId);

                MsalServiceException msalException = null;
                try
                {
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .ExecuteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    msalException = ex as MsalServiceException;
                }
                Assert.IsNotNull(msalException);

                // ensure that only the initial request was made
                Assert.AreEqual(ImdsRetryPolicy.NumRetries, 0);
                Assert.AreEqual(httpManager.QueueSize, 0);
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails500AndRetryPolicyIsDisabledAndNotTriggeredAsync(
            string userAssignedId,
            UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager(disableInternalRetries: true))
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);
                
                ManagedIdentityId managedIdentityId = userAssignedId == null
                    ? ManagedIdentityId.SystemAssigned
                    : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                    .WithHttpManager(httpManager);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiImdsErrorResponse(),
                    ManagedIdentitySource.Imds,
                    statusCode: HttpStatusCode.InternalServerError,
                    userAssignedId: userAssignedId,
                    userAssignedIdentityId: userAssignedIdentityId);

                MsalServiceException msalException = null;
                try
                {
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .ExecuteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    msalException = ex as MsalServiceException;
                }
                Assert.IsNotNull(msalException);

                // ensure that only the initial request was made
                Assert.AreEqual(ImdsRetryPolicy.NumRetries, 0);
                Assert.AreEqual(httpManager.QueueSize, 0);
            }
        }
    }
}
