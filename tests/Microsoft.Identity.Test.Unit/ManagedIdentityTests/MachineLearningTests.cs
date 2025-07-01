// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class MachineLearningTests : TestBase
    {
        private const string MachineLearning = "Machine learning";
        private const string MachineLearningEndpoint = "http://localhost:7071/msi/token";
        internal const string Resource = "https://management.azure.com";

        [DataTestMethod]
        [DataRow(null, null)]                                              // SAMI
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)] // UAMI
        public async Task MachineLearningUserAssignedHappyPathAndHasCorrectClientIdQueryParameterAsync(
            string userAssignedId,
            UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.MachineLearning, MachineLearningEndpoint);

                ManagedIdentityId managedIdentityId = userAssignedId == null
                    ? ManagedIdentityId.SystemAssigned
                    : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(managedIdentityId)
                    .WithHttpManager(httpManager);

                IManagedIdentityApplication mi = miBuilder.Build();

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                MockHttpMessageHandler mockHandler = httpManager.AddManagedIdentityMockHandler(
                    MachineLearningEndpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.MachineLearning,
                    userAssignedId: userAssignedId,
                    userAssignedIdentityId);

                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(Resource).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);

                // Verify query parameter is "clientid" and not "client_id"
                Assert.IsTrue(mockHandler.ExpectedQueryParams.ContainsKey(Constants.ManagedIdentityClientId2017), "Query parameter should use 'clientid' and not 'client_id'");

                // Verify the clientid value based on identity type
                string expectedClientId = userAssignedId ?? EnvironmentVariables.MachineLearningDefaultClientId;
                Assert.AreEqual(expectedClientId, mockHandler.ExpectedQueryParams[Constants.ManagedIdentityClientId2017],
                    "Clientid value should match the provided user assigned ID for UAMI or environment variable for SAMI");
            }
        }

        [DataTestMethod]
        [DataRow(TestConstants.MiResourceId, UserAssignedIdentityId.ResourceId)]
        [DataRow(TestConstants.MiResourceId, UserAssignedIdentityId.ObjectId)]
        public async Task MachineLearningUserAssignedNonClientIdThrowsAsync(
            string userAssignedId,
            UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.MachineLearning, MachineLearningEndpoint);

                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(userAssignedId, userAssignedIdentityId);
                miBuilder
                    .WithHttpManager(httpManager);

                IManagedIdentityApplication mi = miBuilder.Build();

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.MachineLearning.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.InvalidManagedIdentityIdType, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task MachineLearningTestsInvalidEndpointAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.MachineLearning, "127.0.0.1:41564/msi/token");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
                Assert.AreEqual(ManagedIdentitySource.MachineLearning.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "MSI_ENDPOINT", "127.0.0.1:41564/msi/token", MachineLearning), ex.Message);
            }
        }
    }
}
