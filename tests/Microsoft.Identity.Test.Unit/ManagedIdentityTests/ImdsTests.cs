// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Resource;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsTests
    {
        private const string ApiVersion = "2018-02-01";
        private const string ImdsEndpoint = "http://169.254.169.254/metadata/identity/oauth2/token";
        private const string DefaultResource = "https://management.azure.com";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            SetEnvironmentVariables(null);
        }

        [DataTestMethod]
        [DataRow("http://169.254.169.254", DefaultResource, DefaultResource, null)]
        [DataRow(null, DefaultResource, DefaultResource, null)]
        [DataRow("http://169.254.169.254", "https://management.azure.com/.default", DefaultResource, null)]
        [DataRow("http://169.254.169.254", "https://management.azure.com/.default", DefaultResource, TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow("http://169.254.169.254", "https://management.azure.com/.default", DefaultResource, "resource_id", UserAssignedIdentityId.ResourceId)]
        [DataRow("http://169.254.169.254", "https://management.azure.com/.default", DefaultResource, "", UserAssignedIdentityId.None)]
        [DataRow("http://169.254.169.254", "https://management.azure.com/.default", DefaultResource, "  ", UserAssignedIdentityId.None)]
        public async Task ImdsHappyPathAsync(
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
                        ImdsEndpoint,
                        resource,
                        MockHelpers.GetMsiImdsSuccessfulResponse(),
                        ApiVersion,
                        ManagedIdentitySourceType.IMDS,
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
                SetEnvironmentVariables(null);
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
                SetEnvironmentVariables("http://169.254.169.254");

                using (var httpManager = new MockHttpManager())
                {
                    IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                        .Create("clientId")
                        .WithHttpManager(httpManager)
                        .WithExperimentalFeatures()
                        .Build();

                    httpManager.AddManagedIdentityMockHandler(ImdsEndpoint, resource, MockHelpers.GetMsiImdsErrorResponse(), ApiVersion, 
                        ManagedIdentitySourceType.IMDS, statusCode: HttpStatusCode.InternalServerError);
                    httpManager.AddManagedIdentityMockHandler(ImdsEndpoint, resource, MockHelpers.GetMsiImdsErrorResponse(), ApiVersion, 
                        ManagedIdentitySourceType.IMDS, statusCode: HttpStatusCode.InternalServerError);

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
                SetEnvironmentVariables(null);
            }
        }

        [TestMethod]
        public async Task ImdsErrorResponseNoPayloadTestAsync()
        {
            try
            {
                SetEnvironmentVariables("http://169.254.169.254");

                using (var httpManager = new MockHttpManager())
                {
                    IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                        .Create("clientId")
                        .WithHttpManager(httpManager)
                        .WithExperimentalFeatures()
                        .Build();

                    httpManager.AddManagedIdentityMockHandler(ImdsEndpoint, "scope", "", ApiVersion, ManagedIdentitySourceType.IMDS, statusCode: HttpStatusCode.InternalServerError);
                    httpManager.AddManagedIdentityMockHandler(ImdsEndpoint, "scope", "", ApiVersion, ManagedIdentitySourceType.IMDS, statusCode: HttpStatusCode.InternalServerError);

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
                SetEnvironmentVariables(null);
            }
        }

        [TestMethod]
        public async Task ImdsNullResponseAsync()
        {
            try
            {
                SetEnvironmentVariables("http://169.254.169.254");

                using (var httpManager = new MockHttpManager())
                {
                    IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                        .Create("clientId")
                        .WithHttpManager(httpManager)
                        .WithExperimentalFeatures()
                        .Build();

                    httpManager.AddManagedIdentityMockHandler(ImdsEndpoint, "https://management.azure.com", "", ApiVersion, ManagedIdentitySourceType.IMDS, statusCode: HttpStatusCode.OK);

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
                SetEnvironmentVariables(null);
            }
        }

        [TestMethod]
        public async Task ImdsBadRequestTestAsync()
        {
            try
            {
                SetEnvironmentVariables("http://169.254.169.254");

                using (var httpManager = new MockHttpManager())
                {
                    IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                        .Create("clientId")
                        .WithHttpManager(httpManager)
                        .WithExperimentalFeatures()
                    .Build();

                    httpManager.AddManagedIdentityMockHandler(ImdsEndpoint, DefaultResource, MockHelpers.GetMsiImdsErrorResponse(), ApiVersion,
                        ManagedIdentitySourceType.IMDS, statusCode: HttpStatusCode.BadRequest);

                    MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                        await cca.AcquireTokenForClient(new string[] { DefaultResource })
                        .WithManagedIdentity()
                        .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                    Assert.IsNotNull(ex);
                    Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                    Assert.IsTrue(ex.Message.Contains("The requested identity has not been assigned to this resource."));
                }
            }
            finally
            {
                SetEnvironmentVariables(null);
            }
        }

        private void SetEnvironmentVariables(string endpoint)
        {
            Environment.SetEnvironmentVariable("AZURE_POD_IDENTITY_AUTHORITY_HOST", endpoint);
        }
    }
}
