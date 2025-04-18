// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
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

                using (var httpManager = new MockHttpManager(isManagedIdentity: true))
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

                using (var httpManager = new MockHttpManager(isManagedIdentity: true))
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
                            MockHelpers.GetMsiImdsErrorResponse(),
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
                    Assert.AreEqual(httpManager.QueueSize, 0);
                }
            }
        }
    }
}
