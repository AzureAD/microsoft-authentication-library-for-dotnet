// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
    public class AppServiceTests
    {
        private const string DefaultResource = "https://management.azure.com";

        [DataTestMethod]
        [DataRow("http://127.0.0.1:41564/msi/token/", "https://management.azure.com", null)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com", null)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", null)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", "resource_id", UserAssignedIdentityId.ResourceId)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", "", UserAssignedIdentityId.None)]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", "  ", UserAssignedIdentityId.None)]
        public async Task AppServiceHappyPathAsync(
            string endpoint,
            string scope,
            string userAssignedClientIdOrResourceId,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(endpoint);

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithUserAssignedManagedIdentity(userAssignedClientIdOrResourceId)
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    DefaultResource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySourceType.AppService,
                    userAssignedClientIdOrResourceId: userAssignedClientIdOrResourceId,
                    userAssignedIdentityId: userAssignedIdentityId);

                var result = await mia.AcquireTokenForManagedIdentity(scope).ExecuteAsync().ConfigureAwait(false);

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
        [DataRow("user.read")]
        [DataRow("https://management.core.windows.net//user_impersonation")]
        [DataRow("s")]
        public async Task AppServiceTestWrongScopeAsync(string resource)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables("http://127.0.0.1:41564/msi/token");

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", resource, MockHelpers.GetMsiErrorResponse(),
                    ManagedIdentitySourceType.AppService, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", resource, MockHelpers.GetMsiErrorResponse(),
                    ManagedIdentitySourceType.AppService, statusCode: HttpStatusCode.InternalServerError);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mia.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task AppServiceErrorResponseNoPayloadTestAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables("http://127.0.0.1:41564/msi/token");

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", "scope", "",
                    ManagedIdentitySourceType.AppService, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", "scope", "",
                    ManagedIdentitySourceType.AppService, statusCode: HttpStatusCode.InternalServerError);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mia.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
            }

        }

        [TestMethod]
        public async Task AppServiceNullResponseAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables("http://127.0.0.1:41564/msi/token");

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityMockHandler(
                    "http://127.0.0.1:41564/msi/token",
                    DefaultResource,
                    "",
                    ManagedIdentitySourceType.AppService,
                    statusCode: HttpStatusCode.OK);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mia.AcquireTokenForManagedIdentity(DefaultResource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityInvalidResponse, ex.Message);
            }

        }

        [TestMethod]
        public async Task AppServiceInvalidEndpointAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables("127.0.0.1:41564/msi/token");

                IManagedIdentityApplication mia = ManagedIdentityApplicationBuilder
                    .Create()
                    .WithHttpManager(httpManager)
                    .Build();

                MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mia.AcquireTokenForManagedIdentity(DefaultResource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
            }
        }

        private void SetEnvironmentVariables(string endpoint, string secret = "secret")
        {
            Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
            Environment.SetEnvironmentVariable("IDENTITY_HEADER", secret);
        }
    }
}
