// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ServiceFabricTests
    {
        private const string Endpoint = "http://localhost:40342/metadata/identity/oauth2/token";
        private const string Resource = "https://management.azure.com";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [DataTestMethod]
        [DataRow(Endpoint, "https://management.azure.com")]
        [DataRow(Endpoint, "https://management.azure.com/.default")]
        [DataRow(Endpoint, "https://management.azure.com", TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow(Endpoint, "https://management.azure.com", "resource_id", UserAssignedIdentityId.ResourceId)]
        [DataRow(Endpoint, "https://management.azure.com", "", UserAssignedIdentityId.None)]
        public async Task ServiceFabricHappyPathAsync(
            string endpoint, 
            string scope,
            string userAssignedId = null,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(endpoint);

                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .Build();

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySourceType.ServiceFabric,
                    userAssignedClientIdOrResourceId: userAssignedId,
                    userAssignedIdentityId: userAssignedIdentityId);

                var result = await cca.AcquireTokenForClient(new string[] { scope })
                    .WithManagedIdentity(userAssignedId)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await cca.AcquireTokenForClient(new string[] { scope })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow("user.read")]
        [DataRow("https://management.core.windows.net//user_impersonation")]
        [DataRow("s")]
        public async Task ServiceFabricTestWrongScopeAsync(string resource)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(Endpoint);
                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .Build();

                httpManager.AddManagedIdentityMockHandler(Endpoint, resource, MockHelpers.GetMsiErrorResponse(), 
                    ManagedIdentitySourceType.ServiceFabric, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(Endpoint, resource, MockHelpers.GetMsiErrorResponse(), 
                    ManagedIdentitySourceType.ServiceFabric, statusCode: HttpStatusCode.InternalServerError);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await cca.AcquireTokenForClient(new string[] { resource })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task ServiceFabricErrorResponseNoPayloadTestAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(Endpoint);

                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .Build();

                httpManager.AddManagedIdentityMockHandler(Endpoint, "scope", "", ManagedIdentitySourceType.ServiceFabric, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(Endpoint, "scope", "", ManagedIdentitySourceType.ServiceFabric, statusCode: HttpStatusCode.InternalServerError);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await cca.AcquireTokenForClient(new string[] { "scope" })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityNoResponseReceived, ex.Message);
            }
        }

        [TestMethod]
        public async Task ServiceFabricNullResponseAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(Endpoint);

                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .Build();

                httpManager.AddManagedIdentityMockHandler(Endpoint, "https://management.azure.com", "", ManagedIdentitySourceType.ServiceFabric, statusCode: HttpStatusCode.OK);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await cca.AcquireTokenForClient(new string[] { "https://management.azure.com" })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityInvalidResponse, ex.Message);
            }
        }

        [TestMethod]
        public async Task ServiceFabricInvalidEndpointAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables("localhost/token");

                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .Build();

                MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await cca.AcquireTokenForClient(new string[] { "https://management.azure.com" })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "IDENTITY_ENDPOINT", "localhost/token", "Service Fabric"), ex.Message);
            }
        }

        private void SetEnvironmentVariables(string endpoint)
        {
            Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
            Environment.SetEnvironmentVariable("IDENTITY_HEADER", "secret");
            Environment.SetEnvironmentVariable("IDENTITY_SERVER_THUMBPRINT", "thumbprint");

        }
    }
}
