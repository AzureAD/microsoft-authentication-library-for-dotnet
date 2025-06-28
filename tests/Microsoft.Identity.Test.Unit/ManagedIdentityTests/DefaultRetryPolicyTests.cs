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
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();

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
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                MockHelpers.AddCredentialEndpointNotFoundHandlers(managedIdentitySource, httpManager);

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
                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);

                const int NumRequests = 2; // initial request + 1 retry
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade);
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
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                MockHelpers.AddCredentialEndpointNotFoundHandlers(managedIdentitySource, httpManager);

                // Simulate permanent 500s (to trigger the maximum number of retries)
                const int Num500Errors = 1 + TestDefaultRetryPolicy.DefaultManagedIdentityMaxRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num500Errors; i++)
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

                int requestsMade = Num500Errors - httpManager.QueueSize;
                Assert.AreEqual(Num500Errors, requestsMade);
            }
        }

        [DataTestMethod]
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint, null)]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint, null)]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint, null)]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint, null)]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint, null)]
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint, "3")]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint, "3")]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint, "3")]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint, "3")]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint, "3")]
        [DataRow(ManagedIdentitySource.AppService, TestConstants.AppServiceEndpoint, "date")]
        [DataRow(ManagedIdentitySource.AzureArc, TestConstants.AzureArcEndpoint, "date")]
        [DataRow(ManagedIdentitySource.CloudShell, TestConstants.CloudShellEndpoint, "date")]
        [DataRow(ManagedIdentitySource.MachineLearning, TestConstants.MachineLearningEndpoint, "date")]
        [DataRow(ManagedIdentitySource.ServiceFabric, TestConstants.ServiceFabricEndpoint, "date")]
        public async Task SAMIFails500OnceWithVariousRetryAfterHeaderValuesThenSucceeds200Async(
            ManagedIdentitySource managedIdentitySource,
            string endpoint,
            string retryAfterHeader)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                MockHelpers.AddCredentialEndpointNotFoundHandlers(managedIdentitySource, httpManager);

                // Initial request fails with 500
                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    ManagedIdentityTests.Resource,
                    "",
                    managedIdentitySource,
                    statusCode: HttpStatusCode.InternalServerError,
                    retryAfterHeader: retryAfterHeader == "date" ? DateTime.UtcNow.AddSeconds(3).ToString("R") : retryAfterHeader);

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
                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);

                const int NumRequests = 2; // initial request + 1 retry
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade);
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
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                MockHelpers.AddCredentialEndpointNotFoundHandlers(managedIdentitySource, httpManager);

                // Simulate permanent 500s (to trigger the maximum number of retries)
                int Num500Errors = 1 + TestDefaultRetryPolicy.DefaultManagedIdentityMaxRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num500Errors; i++)
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

                int requestsMade = Num500Errors - httpManager.QueueSize;
                Assert.AreEqual(Num500Errors, requestsMade);
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
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                MockHelpers.AddCredentialEndpointNotFoundHandlers(managedIdentitySource, httpManager);

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

                const int NumRequests = 1; // initial request + 0 retries
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade);
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
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                MockHelpers.AddCredentialEndpointNotFoundHandlers(managedIdentitySource, httpManager);

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

                const int NumRequests = 1; // initial request + 0 retries
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade);
            }
        }
    }
}
