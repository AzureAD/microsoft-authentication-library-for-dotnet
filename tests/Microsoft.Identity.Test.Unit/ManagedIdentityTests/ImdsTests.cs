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
    [TestClass]
    public class ImdsTests : TestBase
    {
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();

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
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                IManagedIdentityApplication mi = miBuilder.Build();

                ManagedIdentityTests.MockImdsV1Probe(httpManager, ManagedIdentitySource.Imds, userAssignedIdentityId, userAssignedId);

                // Simulate two 404s (to trigger retries), then a successful response
                const int Num404Errors = 2;
                for (int i = 0; i < Num404Errors; i++)
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

                AuthenticationResult result =
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);

                const int NumRequests = 1 + Num404Errors; // initial request + 2 retries
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade);
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
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                IManagedIdentityApplication mi = miBuilder.Build();

                ManagedIdentityTests.MockImdsV1Probe(httpManager, ManagedIdentitySource.Imds, userAssignedIdentityId, userAssignedId);

                // Simulate four 410s (to trigger retries), then a successful response
                const int Num410Errors = 4;
                for (int i = 0; i < Num410Errors; i++)
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

                AuthenticationResult result =
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                Assert.AreEqual(result.AccessToken, TestConstants.ATSecret);

                const int NumRequests = 1 + Num410Errors; // initial request + 4 retries
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade);
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
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                IManagedIdentityApplication mi = miBuilder.Build();

                ManagedIdentityTests.MockImdsV1Probe(httpManager, ManagedIdentitySource.Imds, userAssignedIdentityId, userAssignedId);

                // Simulate permanent 410s (to trigger the maximum number of retries)
                const int Num410Errors = 1 + TestImdsRetryPolicy.LinearStrategyNumRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num410Errors; i++)
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

                int requestsMade = Num410Errors - httpManager.QueueSize;
                Assert.AreEqual(Num410Errors, requestsMade);
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
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                IManagedIdentityApplication mi = miBuilder.Build();

                ManagedIdentityTests.MockImdsV1Probe(httpManager, ManagedIdentitySource.Imds, userAssignedIdentityId, userAssignedId);

                // Simulate permanent 504s (to trigger the maximum number of retries)
                const int Num504Errors = 1 + TestImdsRetryPolicy.ExponentialStrategyNumRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num504Errors; i++)
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

                int requestsMade = Num504Errors - httpManager.QueueSize;
                Assert.AreEqual(Num504Errors, requestsMade);
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
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                IManagedIdentityApplication mi = miBuilder.Build();

                ManagedIdentityTests.MockImdsV1Probe(httpManager, ManagedIdentitySource.Imds, userAssignedIdentityId, userAssignedId);

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
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                IManagedIdentityApplication mi = miBuilder.Build();

                ManagedIdentityTests.MockImdsV1Probe(httpManager, ManagedIdentitySource.Imds, userAssignedIdentityId, userAssignedId);

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

        [TestMethod]
        
        public async Task ImdsRetryPolicyLifeTimeIsPerRequestAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                IManagedIdentityApplication mi = miBuilder.Build();

                ManagedIdentityTests.MockImdsV1Probe(httpManager, ManagedIdentitySource.Imds);

                // Simulate permanent errors (to trigger the maximum number of retries)
                const int Num504Errors = 1 + TestImdsRetryPolicy.ExponentialStrategyNumRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num504Errors; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.ImdsEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(),
                        ManagedIdentitySource.Imds,
                        statusCode: HttpStatusCode.GatewayTimeout);
                }

                MsalServiceException ex =
                    await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                        await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .ExecuteAsync()
                        .ConfigureAwait(false))
                    .ConfigureAwait(false);
                Assert.IsNotNull(ex);

                int requestsMade = Num504Errors - httpManager.QueueSize;
                Assert.AreEqual(Num504Errors, requestsMade);

                for (int i = 0; i < Num504Errors; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.ImdsEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(),
                        ManagedIdentitySource.Imds,
                        statusCode: HttpStatusCode.GatewayTimeout);
                }

                ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                        await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                .ExecuteAsync()
                                .ConfigureAwait(false))
                    .ConfigureAwait(false);
                Assert.IsNotNull(ex);

                // 3 retries (requestsMade would be 6 if retry policy was NOT per request)
                requestsMade = Num504Errors - httpManager.QueueSize;
                Assert.AreEqual(Num504Errors, requestsMade);

                for (int i = 0; i < Num504Errors; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.ImdsEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(),
                        ManagedIdentitySource.Imds,
                        statusCode: HttpStatusCode.GatewayTimeout);
                }

                ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                        await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                                .ExecuteAsync()
                                .ConfigureAwait(false))
                    .ConfigureAwait(false);
                Assert.IsNotNull(ex);

                // 3 retries (requestsMade would be 9 if retry policy was NOT per request)
                requestsMade = Num504Errors - httpManager.QueueSize;
                Assert.AreEqual(Num504Errors, requestsMade);
            }
        }
    }
}
