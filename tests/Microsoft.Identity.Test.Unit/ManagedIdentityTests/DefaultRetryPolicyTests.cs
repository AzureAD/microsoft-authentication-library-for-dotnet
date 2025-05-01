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
    /// <summary>
    /// The Default Retry Policy applies to:
    ///    ESTS (Azure AD)
    ///    Managed Identity Sources: App Service, Azure Arc, Cloud Shell, Machine Learning, Service Fabric
    /// </summary>
    [TestClass]
    public class DefaultRetryPolicyTests : TestBase
    {
        private static int _originalManagedIdentityRetryDelay;
        private static int _originalEstsRetryDelay;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            // Backup original retry delay values
            _originalManagedIdentityRetryDelay = HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_RETRY_DELAY_MS;
            _originalEstsRetryDelay = HttpManagerFactory.DEFAULT_ESTS_RETRY_DELAY_MS;

            // Speed up retry delays by 100x
            HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_RETRY_DELAY_MS = (int)(_originalManagedIdentityRetryDelay * TestConstants.ONE_HUNDRED_TIMES_FASTER);
            HttpManagerFactory.DEFAULT_ESTS_RETRY_DELAY_MS = (int)(_originalEstsRetryDelay * TestConstants.ONE_HUNDRED_TIMES_FASTER);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Restore retry policy values after each test
            HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_RETRY_DELAY_MS = _originalManagedIdentityRetryDelay;
            HttpManagerFactory.DEFAULT_ESTS_RETRY_DELAY_MS = _originalEstsRetryDelay;
        }

        [DataTestMethod] // see test class header: all sources that allow UAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task UAMIFails500OnceThenSucceeds200Async(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                using (var httpManager = new MockHttpManager())
                {
                    string userAssignedId = TestConstants.ClientId;
                    UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.ClientId;

                    ManagedIdentityId managedIdentityId = ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    // Initial request fails with 500
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        ManagedIdentityTests.Resource,
                        "",
                        managedIdentitySource,
                        statusCode: HttpStatusCode.InternalServerError,
                        userAssignedId: userAssignedId,
                        userAssignedIdentityId: userAssignedIdentityId);

                    // Final success
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        managedIdentitySource,
                        userAssignedId: userAssignedId,
                        userAssignedIdentityId: userAssignedIdentityId);

                    var stopwatch = Stopwatch.StartNew();

                    var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                         .ExecuteAsync()
                                         .ConfigureAwait(false);

                    stopwatch.Stop();

                    // linear backoff (1 second * 1 retry)
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds >= HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_RETRY_DELAY_MS);

                    // ensure that exactly 2 requests were made: initial request + 1 retry
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, 1);
                    Assert.AreEqual(httpManager.QueueSize, 0);

                    Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
                }
            }
        }

        [DataTestMethod] // see test class header: all sources that allow UAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task UAMIFails500PermanentlyAsync(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                using (var httpManager = new MockHttpManager())
                {
                    string userAssignedId = TestConstants.ClientId;
                    UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.ClientId;

                    ManagedIdentityId managedIdentityId = ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    // Simulate permanent 500s (to trigger the maximum number of retries)
                    const int NUM_500 = HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES + 1; // initial request + maximum number of retries (3)
                    for (int i = 0; i < NUM_500; i++)
                    {
                        httpManager.AddManagedIdentityMockHandler(
                            endpoint,
                            ManagedIdentityTests.Resource,
                            "",
                            managedIdentitySource,
                            statusCode: HttpStatusCode.InternalServerError,
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

                    // linear backoff (1 second * 3 retries)
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_RETRY_DELAY_MS * HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES));

                    // ensure that exactly 4 requests were made: initial request + 3 retries
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES);
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500OnceWithNoRetryAfterHeaderThenSucceeds200Async(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                using (var httpManager = new MockHttpManager())
                {
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    // Initial request fails with 500
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        ManagedIdentityTests.Resource,
                        "",
                        managedIdentitySource,
                        statusCode: HttpStatusCode.InternalServerError);

                    // Final success
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        managedIdentitySource);

                    var stopwatch = Stopwatch.StartNew();

                    var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                         .ExecuteAsync()
                                         .ConfigureAwait(false);

                    stopwatch.Stop();

                    // linear backoff (1 second * 1 retry)
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds >= HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_RETRY_DELAY_MS);

                    // ensure that exactly 2 requests were made: initial request + 1 retry
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, 1);
                    Assert.AreEqual(httpManager.QueueSize, 0);

                    Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
                }
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500OnceWithRetryAfterHeader3SecondsThenSucceeds200Async(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                using (var httpManager = new MockHttpManager())
                {
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    // make it one hundred times faster so the test completes quickly
                    double retryAfterSeconds = 3 * TestConstants.ONE_HUNDRED_TIMES_FASTER;

                    // Initial request fails with 500
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        ManagedIdentityTests.Resource,
                        "",
                        managedIdentitySource,
                        statusCode: HttpStatusCode.InternalServerError,
                        retryAfterHeader: retryAfterSeconds.ToString());

                    // Final success
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        managedIdentitySource);

                    var stopwatch = Stopwatch.StartNew();

                    var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                         .ExecuteAsync()
                                         .ConfigureAwait(false);

                    stopwatch.Stop();

                    // ensure that the number of seconds in the retry-after header elapsed before the second network request was made
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds >= (retryAfterSeconds * 1000)); // convert to milliseconds

                    // ensure that exactly 2 requests were made: initial request + 1 retry
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, 1);
                    Assert.AreEqual(httpManager.QueueSize, 0);

                    Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
                }
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500OnceWithRetryAfterHeader3SecondsAsHttpDateThenSucceeds200Async(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                using (var httpManager = new MockHttpManager())
                {
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    // this test can not be made one hundred times faster because it is based on a date
                    const int retryAfterMilliseconds = 3000;
                    var retryAfterHttpDate = DateTime.UtcNow.AddMilliseconds(retryAfterMilliseconds).ToString("R");

                    // Initial request fails with 500
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        ManagedIdentityTests.Resource,
                        "",
                        managedIdentitySource,
                        statusCode: HttpStatusCode.InternalServerError,
                        retryAfterHeader: retryAfterHttpDate);

                    // Final success
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        managedIdentitySource);

                    var stopwatch = Stopwatch.StartNew();

                    var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                         .ExecuteAsync()
                                         .ConfigureAwait(false);

                    stopwatch.Stop();

                    // ensure that the number of seconds in the retry-after header elapsed before the second network request was made
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds >= retryAfterMilliseconds);

                    // ensure that exactly 2 requests were made: initial request + 1 retry
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, 1);
                    Assert.AreEqual(httpManager.QueueSize, 0);

                    Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
                }
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500Permanently(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                using (var httpManager = new MockHttpManager())
                {
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    // Simulate permanent 500s (to trigger the maximum number of retries)
                    int NUM_500 = HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES + 1; // initial request + maximum number of retries (3)
                    for (int i = 0; i < NUM_500; i++)
                    {
                        httpManager.AddManagedIdentityMockHandler(
                            endpoint,
                            ManagedIdentityTests.Resource,
                            "",
                            managedIdentitySource,
                            statusCode: HttpStatusCode.InternalServerError);
                    }

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

                    // ensure that the first request was made and retried 3 times
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES);
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500PermanentlyAndRetryPolicyLifeTimeIsPerRequestAsync(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                using (var httpManager = new MockHttpManager())
                {
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    // Simulate permanent 500s (to trigger the maximum number of retries)
                    int NUM_500 = HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES + 1; // initial request + maximum number of retries (3)
                    for (int i = 0; i < NUM_500; i++)
                    {
                        httpManager.AddManagedIdentityMockHandler(
                            endpoint,
                            ManagedIdentityTests.Resource,
                            "",
                            managedIdentitySource,
                            statusCode: HttpStatusCode.InternalServerError);
                    }

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

                    // ensure that the first request was made and retried 3 times
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES);
                    Assert.AreEqual(httpManager.QueueSize, 0);

                    for (int i = 0; i < NUM_500; i++)
                    {
                        httpManager.AddManagedIdentityMockHandler(
                            endpoint,
                            ManagedIdentityTests.Resource,
                            "",
                            managedIdentitySource,
                            statusCode: HttpStatusCode.InternalServerError);
                    }

                    msalException = null;
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

                    // ensure that the second request was made and retried 3 times
                    // (numRetries would be x2 if retry policy was NOT per request)
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES);
                    Assert.AreEqual(httpManager.QueueSize, 0);

                    for (int i = 0; i < NUM_500; i++)
                    {
                        httpManager.AddManagedIdentityMockHandler(
                            endpoint,
                            ManagedIdentityTests.Resource,
                            "",
                            managedIdentitySource,
                            statusCode: HttpStatusCode.InternalServerError);
                    }

                    msalException = null;
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

                    // ensure that the third request was made and retried 3 times
                    // (numRetries would be x3 if retry policy was NOT per request)
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, HttpManagerFactory.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES);
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails400WhichIsNonRetriableAndRetryPolicyIsNotTriggeredAsync(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                using (var httpManager = new MockHttpManager())
                {
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        ManagedIdentityTests.Resource,
                        "",
                        managedIdentitySource,
                        statusCode: HttpStatusCode.BadRequest);

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
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, 0);
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500AndRetryPolicyIsDisabledAndNotTriggeredAsync(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                using (var httpManager = new MockHttpManager(disableInternalRetries: true))
                {
                    var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                        .WithHttpManager(httpManager);

                    // Disable cache to avoid pollution
                    miBuilder.Config.AccessorOptions = null;

                    var mi = miBuilder.Build();

                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        ManagedIdentityTests.Resource,
                        "",
                        managedIdentitySource,
                        statusCode: HttpStatusCode.InternalServerError);

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
                    Assert.AreEqual(DefaultRetryPolicy.numRetries, 0);
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }
    }
}
