// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// The Default Retry Policy applies to:
    ///    STS (Azure AD) (Tested in HttpManagerTests.cs)
    ///    Managed Identity Sources: App Service, Azure Arc, Cloud Shell, Machine Learning, Service Fabric
    /// </summary>
    [TestClass]
    public class DefaultRetryPolicyTests : TestBase
    {
        private TestRetryPolicyFactory TestRetryPolicyFactory = new TestRetryPolicyFactory();

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();

            TestDefaultRetryPolicy.NumRetries = 0;
        }

        [DataTestMethod] // see test class header: all sources that allow UAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task UAMIFails500OnceThenSucceeds200Async(
            ManagedIdentitySource managedIdentitySource,
            string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                string userAssignedId = TestConstants.ClientId;
                UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.ClientId;

                ManagedIdentityId managedIdentityId = ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(TestRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

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

                AuthenticationResult result =
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .ExecuteAsync()
                            .ConfigureAwait(false);

                // ensure that exactly 2 requests were made: initial request + 1 retry
                Assert.AreEqual(TestDefaultRetryPolicy.NumRetries, 1);
                Assert.AreEqual(httpManager.QueueSize, 0);

                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
            }
        }

        [DataTestMethod] // see test class header: all sources that allow UAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task UAMIFails500PermanentlyAsync(
            ManagedIdentitySource managedIdentitySource,
            string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);
                
                string userAssignedId = TestConstants.ClientId;
                UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.ClientId;

                ManagedIdentityId managedIdentityId = ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(TestRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Simulate permanent 500s (to trigger the maximum number of retries)
                const int NUM_500 = TestDefaultRetryPolicy.DefaultManagedIdentityMaxRetries + 1; // initial request + maximum number of retries (3)
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
                try
                {
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    msalException = ex as MsalServiceException;
                }
                Assert.IsNotNull(msalException);

                // ensure that exactly 4 requests were made: initial request + 3 retries
                Assert.AreEqual(TestDefaultRetryPolicy.NumRetries, TestDefaultRetryPolicy.DefaultManagedIdentityMaxRetries);
                Assert.AreEqual(httpManager.QueueSize, 0);
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500OnceWithNoRetryAfterHeaderThenSucceeds200Async(
            ManagedIdentitySource managedIdentitySource,
            string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(TestRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

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

                AuthenticationResult result =
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .ExecuteAsync()
                            .ConfigureAwait(false);

                // ensure that exactly 2 requests were made: initial request + 1 retry
                Assert.AreEqual(TestDefaultRetryPolicy.NumRetries, 1);
                Assert.AreEqual(httpManager.QueueSize, 0);

                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500OnceWithRetryAfterHeader3SecondsThenSucceeds200Async(
            ManagedIdentitySource managedIdentitySource,
            string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(TestRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

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

                AuthenticationResult result =
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .ExecuteAsync()
                            .ConfigureAwait(false);

                // ensure that exactly 2 requests were made: initial request + 1 retry
                Assert.AreEqual(TestDefaultRetryPolicy.NumRetries, 1);
                Assert.AreEqual(httpManager.QueueSize, 0);

                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500OnceWithRetryAfterHeader3SecondsAsHttpDateThenSucceeds200Async(
            ManagedIdentitySource managedIdentitySource,
            string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(TestRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // this test can not be made one hundred times faster because it is based on a date
                const int retryAfterMilliseconds = 3000;
                // an extra second has been added to account for this date operation
                var retryAfterHttpDate = DateTime.UtcNow.AddMilliseconds(retryAfterMilliseconds + 1000).ToString("R");

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

                AuthenticationResult result =
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .ExecuteAsync()
                            .ConfigureAwait(false);

                // ensure that exactly 2 requests were made: initial request + 1 retry
                Assert.AreEqual(TestDefaultRetryPolicy.NumRetries, 1);
                Assert.AreEqual(httpManager.QueueSize, 0);

                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500Permanently(
            ManagedIdentitySource managedIdentitySource,
            string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(TestRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Simulate permanent 500s (to trigger the maximum number of retries)
                int NUM_500 = TestDefaultRetryPolicy.DefaultManagedIdentityMaxRetries + 1; // initial request + maximum number of retries (3)
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
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    msalException = ex as MsalServiceException;
                }
                Assert.IsNotNull(msalException);

                // ensure that the first request was made and retried 3 times
                Assert.AreEqual(TestDefaultRetryPolicy.NumRetries, TestDefaultRetryPolicy.DefaultManagedIdentityMaxRetries);
                Assert.AreEqual(httpManager.QueueSize, 0);
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails400WhichIsNonRetriableAndRetryPolicyIsNotTriggeredAsync(
            ManagedIdentitySource managedIdentitySource,
            string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(TestRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

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
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    msalException = ex as MsalServiceException;
                }
                Assert.IsNotNull(msalException);

                // ensure that only the initial request was made
                Assert.AreEqual(TestDefaultRetryPolicy.NumRetries, 0);
                Assert.AreEqual(httpManager.QueueSize, 0);
            }
        }

        [DataTestMethod] // see test class header: all sources allow SAMI
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint)]
        public async Task SAMIFails500AndRetryPolicyIsDisabledAndNotTriggeredAsync(
            ManagedIdentitySource managedIdentitySource,
            string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager(disableInternalRetries: true))
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(TestRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

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
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    msalException = ex as MsalServiceException;
                }
                Assert.IsNotNull(msalException);

                // ensure that only the initial request was made
                Assert.AreEqual(TestDefaultRetryPolicy.NumRetries, 0);
                Assert.AreEqual(httpManager.QueueSize, 0);
            }
        }
    }
}
