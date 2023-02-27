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
    [DeploymentItem("Resources\\ManagedIdentityAzureArcSecret.txt")]
    public class AzureArcTests
    {
        private const string Endpoint = "http://localhost:40342/metadata/identity/oauth2/token";
        private const string Resource = "https://management.azure.com";
        private const string AzureArc = "Azure Arc";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [DataTestMethod]
        [DataRow(Endpoint, "https://management.azure.com")]
        [DataRow(Endpoint, "https://management.azure.com/.default")]
        public async Task AzureArcHappyPathAsync(
            string endpoint, 
            string scope)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(endpoint);

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityWSTrustMockHandler(endpoint,
                    ResourceHelper.GetTestResourceRelativePath("ManagedIdentityAzureArcSecret.txt"));

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySourceType.AzureArc);

                var result = await mia.AcquireTokenForManagedIdentity(scope)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await mia.AcquireTokenForManagedIdentity(scope)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(TestConstants.ClientId)]
        [DataRow("resourceId")]
        public async Task AzureArcUserAssignedManagedIdentityNotSupportedAsync(string userAssignedClientId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(Endpoint);

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithUserAssignedManagedIdentity(userAssignedClientId)
                    .WithHttpManager(httpManager)
                    .Build();

                MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mia.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.UserAssignedManagedIdentityNotSupported, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityUserAssignedNotSupported, AzureArc), ex.Message);
            }
        }

        [TestMethod]
        public async Task AzureArcAuthHeaderMissingAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(Endpoint);

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityWSTrustMockHandler(Endpoint);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mia.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityNoChallengeError, ex.Message);
            }
        }

        [TestMethod]
        public async Task AzureArcAuthHeaderInvalidAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(Endpoint);

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityWSTrustMockHandler(Endpoint, "somevalue=filepath");

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mia.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityInvalidChallenge, ex.Message);
            }
        }

        [DataTestMethod]
        [DataRow("user.read")]
        [DataRow("https://management.core.windows.net//user_impersonation")]
        [DataRow("s")]
        public async Task AzureArcTestWrongScopeAsync(string resource)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(Endpoint);
                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityWSTrustMockHandler(Endpoint, 
                    ResourceHelper.GetTestResourceRelativePath("ManagedIdentityAzureArcSecret.txt"));
                httpManager.AddManagedIdentityMockHandler(Endpoint, resource, MockHelpers.GetMsiErrorResponse(), 
                    ManagedIdentitySourceType.AzureArc, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityWSTrustMockHandler(Endpoint, 
                    ResourceHelper.GetTestResourceRelativePath("ManagedIdentityAzureArcSecret.txt"));
                httpManager.AddManagedIdentityMockHandler(Endpoint, resource, MockHelpers.GetMsiErrorResponse(), 
                    ManagedIdentitySourceType.AzureArc, statusCode: HttpStatusCode.InternalServerError);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mia.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task AzureArcErrorResponseNoPayloadTestAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(Endpoint);

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityMockHandler(Endpoint, "scope", "", ManagedIdentitySourceType.AzureArc, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(Endpoint, "scope", "", ManagedIdentitySourceType.AzureArc, statusCode: HttpStatusCode.InternalServerError);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mia.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityNoResponseReceived, ex.Message);
            }
        }

        [TestMethod]
        public async Task AzureArcNullResponseAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(Endpoint);

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityMockHandler(Endpoint, Resource, "", ManagedIdentitySourceType.AzureArc, statusCode: HttpStatusCode.OK);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mia.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityInvalidResponse, ex.Message);
            }
        }

        [TestMethod]
        public async Task AzureArcInvalidEndpointAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables("localhost/token");

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mia.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "IDENTITY_ENDPOINT", "localhost/token", AzureArc), ex.Message);
            }
        }

        private void SetEnvironmentVariables(string endpoint)
        {
            Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
            Environment.SetEnvironmentVariable("IMDS_ENDPOINT", "http://localhost:40342");
        }
    }
}
