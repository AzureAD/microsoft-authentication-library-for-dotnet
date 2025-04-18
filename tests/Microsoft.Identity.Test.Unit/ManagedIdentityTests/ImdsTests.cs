// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsTests : TestBase
    {
        private const int IMDS_EXPONENTIAL_STRATEGY_TWO_RETRIES_IN_MS = 3000; // 1 second -> 2 seconds
        private const int IMDS_EXPONENTIAL_STRATEGY_MAX_RETRIES_IN_MS = 7000; // 1 second -> 2 seconds -> 4 seconds

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
            ImdsRetryPolicy.MIN_EXPONENTIAL_BACKOFF_MS = (int)(_originalMinBackoff * TestConstants.ONE_HUNDRED_TIMES_FASTER);
            ImdsRetryPolicy.MAX_EXPONENTIAL_BACKOFF_MS = (int)(_originalMaxBackoff * TestConstants.ONE_HUNDRED_TIMES_FASTER);
            ImdsRetryPolicy.EXPONENTIAL_DELTA_BACKOFF_MS = (int)(_originalDeltaBackoff * TestConstants.ONE_HUNDRED_TIMES_FASTER);
            ImdsRetryPolicy.HTTP_STATUS_GONE_RETRY_AFTER_MS = (int)(_originalGoneRetryAfter * TestConstants.ONE_HUNDRED_TIMES_FASTER);
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
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);

                using (var httpManager = new MockHttpManager(isManagedIdentity: true))
                {
                    ManagedIdentityId managedIdentityId = userAssignedId == null
                        ? ManagedIdentityId.SystemAssigned
                        : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

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

                    var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                         .ExecuteAsync()
                                         .ConfigureAwait(false);

                    stopwatch.Stop();

                    // exponential backoff (1 second -> 2 seconds)
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (IMDS_EXPONENTIAL_STRATEGY_TWO_RETRIES_IN_MS * TestConstants.ONE_HUNDRED_TIMES_FASTER));

                    // ensure that exactly 3 requests were made: initial request + 2 retries
                    Assert.AreEqual(httpManager.QueueSize, 0);

                    Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
                }
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails410FourTimesThenSucceeds200Async(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);

                using (var httpManager = new MockHttpManager(isManagedIdentity: true))
                {
                    ManagedIdentityId managedIdentityId = userAssignedId == null
                        ? ManagedIdentityId.SystemAssigned
                        : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

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

                    var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                         .ExecuteAsync()
                                         .ConfigureAwait(false);

                    stopwatch.Stop();

                    // linear backoff (10 seconds * 4 retries)
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (ImdsRetryPolicy.HTTP_STATUS_GONE_RETRY_AFTER_MS * NUM_410 * TestConstants.ONE_HUNDRED_TIMES_FASTER));

                    // ensure that exactly 5 requests were made: initial request + 4 retries
                    Assert.AreEqual(httpManager.QueueSize, 0);

                    Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
                }
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails410PermanentlyAsync(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);

                using (var httpManager = new MockHttpManager(isManagedIdentity: true))
                {
                    ManagedIdentityId managedIdentityId = userAssignedId == null
                        ? ManagedIdentityId.SystemAssigned
                        : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    // Simulate permanent 410s (to trigger the maximum number of retries)
                    const int NUM_410 = ImdsRetryPolicy.LINEAR_STRATEGY_NUM_RETRIES + 1; // initial request + maximum number of retries (7)
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
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (ImdsRetryPolicy.HTTP_STATUS_GONE_RETRY_AFTER_MS * ImdsRetryPolicy.LINEAR_STRATEGY_NUM_RETRIES * TestConstants.ONE_HUNDRED_TIMES_FASTER));

                    // ensure that exactly 8 requests were made: initial request + 7 retries
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails504PermanentlyAsync(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);

                using (var httpManager = new MockHttpManager(isManagedIdentity: true))
                {
                    ManagedIdentityId managedIdentityId = userAssignedId == null
                        ? ManagedIdentityId.SystemAssigned
                        : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    // Simulate permanent 504s (to trigger the maximum number of retries)
                    const int NUM_504 = ImdsRetryPolicy.EXPONENTIAL_STRATEGY_NUM_RETRIES + 1; // initial request + maximum number of retries (3)
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
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (IMDS_EXPONENTIAL_STRATEGY_MAX_RETRIES_IN_MS * TestConstants.ONE_HUNDRED_TIMES_FASTER));

                    // ensure that exactly 4 requests were made: initial request + 3 retries
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails500PermanentlyAndRetryPolicyLifeTimeIsPerRequestAsync(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);

                using (var httpManager = new MockHttpManager(isManagedIdentity: true))
                {
                    ManagedIdentityId managedIdentityId = userAssignedId == null
                        ? ManagedIdentityId.SystemAssigned
                        : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    // Simulate permanent 500s (to trigger the maximum number of retries)
                    const int NUM_500 = ImdsRetryPolicy.EXPONENTIAL_STRATEGY_NUM_RETRIES + 1; // initial request + maximum number of retries (3)
                    for (int i = 0; i < NUM_500; i++)
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

                    // ensure that the first request was made and retried 3 times
                    Assert.AreEqual(httpManager.QueueSize, 0);

                    for (int i = 0; i < NUM_500; i++)
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

                    msalException = null;
                    stopwatch = Stopwatch.StartNew();
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

                    // ensure that the second request was made and retried 3 times
                    Assert.AreEqual(httpManager.QueueSize, 0);

                    for (int i = 0; i < NUM_500; i++)
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

                    msalException = null;
                    stopwatch = Stopwatch.StartNew();
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

                    // ensure that the third request was made and retried 3 times
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails400WhichIsNonRetriableAndRetryPolicyIsNotTriggeredAsync(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);

                using (var httpManager = new MockHttpManager(isManagedIdentity: true))
                {
                    ManagedIdentityId managedIdentityId = userAssignedId == null
                        ? ManagedIdentityId.SystemAssigned
                        : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.ImdsEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(),
                        ManagedIdentitySource.Imds,
                        statusCode: HttpStatusCode.BadRequest,
                        userAssignedId: userAssignedId,
                        userAssignedIdentityId: userAssignedIdentityId);

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

                    // ensure that only the initial request was made
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task ImdsFails500AndRetryPolicyIsDisabledAndNotTriggeredAsync(string userAssignedId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);

                using (var httpManager = new MockHttpManager(isManagedIdentity: true))
                {
                    ManagedIdentityId managedIdentityId = userAssignedId == null
                        ? ManagedIdentityId.SystemAssigned
                        : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.ImdsEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(),
                        ManagedIdentitySource.Imds,
                        statusCode: HttpStatusCode.InternalServerError,
                        userAssignedId: userAssignedId,
                        userAssignedIdentityId: userAssignedIdentityId);

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

                    // ensure that only the initial request was made
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }
    }
}
