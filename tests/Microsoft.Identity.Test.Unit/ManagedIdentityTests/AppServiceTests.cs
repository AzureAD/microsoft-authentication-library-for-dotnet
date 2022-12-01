// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class AppServiceTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            SetEnvironmentVariables(null, null);
        }

        [DataTestMethod]
        [DataRow("http://127.0.0.1:41564/msi/token/", "https://management.azure.com", "https://management.azure.com", null)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com", "https://management.azure.com", null)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", "https://management.azure.com", null)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", "https://management.azure.com", TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", "https://management.azure.com", "resource_id", UserAssignedIdentityId.ResourceId)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", "https://management.azure.com", "", UserAssignedIdentityId.None)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", "https://management.azure.com", "  ", UserAssignedIdentityId.None)]
        public async Task AppServiceHappyPathAsync(
            string endpoint, 
            string scope, 
            string resource, 
            string userAssignedClientIdOrResourceId, 
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None)
        {
            try
            {
                SetEnvironmentVariables(endpoint);

                using (var httpManager = new MockHttpManager())
                {
                    IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                        .Create("clientId")
                        .WithHttpManager(httpManager)
                        .WithExperimentalFeatures()
                        .Build();

                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        userAssignedClientIdOrResourceId: userAssignedClientIdOrResourceId,
                        userAssignedIdentityId: userAssignedIdentityId);

                    var result = await cca.AcquireTokenForClient(new string[] { scope })
                        .WithManagedIdentity(userAssignedClientIdOrResourceId)
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
            finally
            {
                SetEnvironmentVariables(null, null);
            }
        }

        [DataTestMethod]
        [DataRow("user.read")]
        [DataRow("https://management.core.windows.net//user_impersonation")]
        [DataRow("s")]
        public async Task AppServiceTestWrongScopeAsync(string resource)
        {
            try
            {
                SetEnvironmentVariables("http://127.0.0.1:41564/msi/token");

                using (var httpManager = new MockHttpManager())
                {
                    IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                        .Create("clientId")
                        .WithHttpManager(httpManager)
                        .WithExperimentalFeatures()
                        .Build();

                    httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", resource, MockHelpers.GetMsiErrorResponse(), statusCode: HttpStatusCode.InternalServerError);
                    httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", resource, MockHelpers.GetMsiErrorResponse(), statusCode: HttpStatusCode.InternalServerError);

                    MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                        await cca.AcquireTokenForClient(new string[] { resource })
                        .WithManagedIdentity()
                        .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                    Assert.IsNotNull(ex);
                    Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                }
            }
            finally 
            {
                SetEnvironmentVariables(null, null);
            }
        }

        [TestMethod]
        public async Task AppServiceErrorResponseNoPayloadTestAsync()
        {
            try
            {
                SetEnvironmentVariables("http://127.0.0.1:41564/msi/token");

                using (var httpManager = new MockHttpManager())
                {
                    IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                        .Create("clientId")
                        .WithHttpManager(httpManager)
                        .WithExperimentalFeatures()
                        .Build();

                    httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", "scope", "", statusCode: HttpStatusCode.InternalServerError);
                    httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", "scope", "", statusCode: HttpStatusCode.InternalServerError);

                    MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                        await cca.AcquireTokenForClient(new string[] { "scope" })
                        .WithManagedIdentity()
                        .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                    Assert.IsNotNull(ex);
                    Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                    Assert.AreEqual("[Managed Identity] Empty error response received.", ex.Message);
                }
            }
            finally
            {
                SetEnvironmentVariables(null, null);
            }
        }

        [TestMethod]
        public async Task AppServiceNullResponseAsync()
        {
            try
            {
                SetEnvironmentVariables("http://127.0.0.1:41564/msi/token");

                using (var httpManager = new MockHttpManager())
                {
                    IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                        .Create("clientId")
                        .WithHttpManager(httpManager)
                        .WithExperimentalFeatures()
                        .Build();

                    httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", "https://management.azure.com", "", statusCode: HttpStatusCode.OK);

                    MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                        await cca.AcquireTokenForClient(new string[] { "https://management.azure.com" })
                        .WithManagedIdentity()
                        .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                    Assert.IsNotNull(ex);
                    Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                    Assert.AreEqual(MsalErrorMessage.AuthenticationResponseInvalidFormatError, ex.Message);
                }
            }
            finally
            {
                SetEnvironmentVariables(null, null);
            }
        }

        [TestMethod]
        public async Task AppServiceInvalidEndpointAsync()
        {
            try
            {
                SetEnvironmentVariables("127.0.0.1:41564/msi/token");

                using (var httpManager = new MockHttpManager())
                {
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
                }
            }
            finally 
            { 
                SetEnvironmentVariables(null, null); 
            }
        }

        private void SetEnvironmentVariables(string endpoint, string secret = "secret")
        {
            Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
            Environment.SetEnvironmentVariable("IDENTITY_HEADER", secret);
        }
    }
}
