// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{

    [TestClass]
    public class ManagedIdentityTests : TestBase
    {
        internal const string Resource = "https://management.azure.com";
        internal const string ResourceDefaultSuffix = "https://management.azure.com/.default";
        internal const string AppServiceEndpoint = "http://127.0.0.1:41564/msi/token";
        internal const string MachineLearningEndpoint = "http://localhost:7071/msi/token";
        internal const string ImdsEndpoint = "http://169.254.169.254/metadata/identity/oauth2/token";
        internal const string AzureArcEndpoint = "http://localhost:40342/metadata/identity/oauth2/token";
        internal const string CloudShellEndpoint = "http://localhost:40342/metadata/identity/oauth2/token";
        internal const string ServiceFabricEndpoint = "https://localhost:2377/metadata/identity/oauth2/token";
        internal const string ExpectedErrorMessage = "Expected error message.";
        internal const string ExpectedErrorCode = "ErrorCode";
        internal const string ExpectedCorrelationId = "Some GUID";

        [DataTestMethod]
        [DataRow("http://127.0.0.1:41564/msi/token/", ManagedIdentitySource.AppService, ManagedIdentitySource.AppService)]
        [DataRow(AppServiceEndpoint, ManagedIdentitySource.AppService, ManagedIdentitySource.AppService)]
        [DataRow(ImdsEndpoint, ManagedIdentitySource.Imds, ManagedIdentitySource.DefaultToImds)]
        [DataRow(null, ManagedIdentitySource.Imds, ManagedIdentitySource.DefaultToImds)]
        [DataRow(AzureArcEndpoint, ManagedIdentitySource.AzureArc, ManagedIdentitySource.AzureArc)]
        [DataRow(CloudShellEndpoint, ManagedIdentitySource.CloudShell, ManagedIdentitySource.CloudShell)]
        [DataRow(ServiceFabricEndpoint, ManagedIdentitySource.ServiceFabric, ManagedIdentitySource.ServiceFabric)]
        [DataRow(MachineLearningEndpoint, ManagedIdentitySource.MachineLearning, ManagedIdentitySource.MachineLearning)]
        public void GetManagedIdentityTests(
            string endpoint,
            ManagedIdentitySource managedIdentitySource, 
            ManagedIdentitySource expectedManagedIdentitySource)
        {
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                Assert.AreEqual(expectedManagedIdentitySource, ManagedIdentityApplication.GetManagedIdentitySource());
            }
        }

        [DataTestMethod]
        [DataRow("http://127.0.0.1:41564/msi/token/", Resource, ManagedIdentitySource.AppService)]
        [DataRow(AppServiceEndpoint, Resource, ManagedIdentitySource.AppService)]
        [DataRow(AppServiceEndpoint, ResourceDefaultSuffix, ManagedIdentitySource.AppService)]
        [DataRow(ImdsEndpoint, Resource, ManagedIdentitySource.Imds)]
        [DataRow(null, Resource, ManagedIdentitySource.Imds)]
        [DataRow(AzureArcEndpoint, Resource, ManagedIdentitySource.AzureArc)]
        [DataRow(AzureArcEndpoint, ResourceDefaultSuffix, ManagedIdentitySource.AzureArc)]
        [DataRow(CloudShellEndpoint, Resource, ManagedIdentitySource.CloudShell)]
        [DataRow(CloudShellEndpoint, ResourceDefaultSuffix, ManagedIdentitySource.CloudShell)]
        [DataRow(ServiceFabricEndpoint, Resource, ManagedIdentitySource.ServiceFabric)]
        [DataRow(ServiceFabricEndpoint, ResourceDefaultSuffix, ManagedIdentitySource.ServiceFabric)]
        [DataRow(MachineLearningEndpoint, Resource, ManagedIdentitySource.MachineLearning)]
        [DataRow(MachineLearningEndpoint, ResourceDefaultSuffix, ManagedIdentitySource.MachineLearning)]
        public async Task ManagedIdentityHappyPathAsync(
            string endpoint,
            string scope,
            ManagedIdentitySource managedIdentitySource)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();
                
                httpManager.AddManagedIdentityMockHandler(
                endpoint,
                Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                managedIdentitySource);

                var result = await mi.AcquireTokenForManagedIdentity(scope).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await mi.AcquireTokenForManagedIdentity(scope)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(AppServiceEndpoint, ManagedIdentitySource.AppService, TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow(AppServiceEndpoint, ManagedIdentitySource.AppService, TestConstants.MiResourceId, UserAssignedIdentityId.ResourceId)]
        [DataRow(AppServiceEndpoint, ManagedIdentitySource.AppService, TestConstants.ObjectId, UserAssignedIdentityId.ObjectId)]
        [DataRow(ImdsEndpoint, ManagedIdentitySource.Imds, TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow(ImdsEndpoint, ManagedIdentitySource.Imds, TestConstants.MiResourceId, UserAssignedIdentityId.ResourceId)]
        [DataRow(ImdsEndpoint, ManagedIdentitySource.Imds, TestConstants.MiResourceId, UserAssignedIdentityId.ObjectId)]
        [DataRow(ServiceFabricEndpoint, ManagedIdentitySource.ServiceFabric, TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow(ServiceFabricEndpoint, ManagedIdentitySource.ServiceFabric, TestConstants.MiResourceId, UserAssignedIdentityId .ResourceId)]
        [DataRow(ServiceFabricEndpoint, ManagedIdentitySource.ServiceFabric, TestConstants.MiResourceId, UserAssignedIdentityId.ObjectId)]
        [DataRow(MachineLearningEndpoint, ManagedIdentitySource.MachineLearning, TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow(MachineLearningEndpoint, ManagedIdentitySource.MachineLearning, TestConstants.MiResourceId, UserAssignedIdentityId.ResourceId)]
        [DataRow(MachineLearningEndpoint, ManagedIdentitySource.MachineLearning, TestConstants.MiResourceId, UserAssignedIdentityId.ObjectId)]
        public async Task ManagedIdentityUserAssignedHappyPathAsync(
            string endpoint,
            ManagedIdentitySource managedIdentitySource,
            string userAssignedId,
            UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(userAssignedId, userAssignedIdentityId);
                
                miBuilder.WithHttpManager(httpManager);

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    managedIdentitySource,
                    userAssignedId: userAssignedId,
                    userAssignedIdentityId: userAssignedIdentityId);

                var result = await mi.AcquireTokenForManagedIdentity(Resource).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(AppServiceEndpoint, Resource, "https://graph.microsoft.com", ManagedIdentitySource.AppService)]
        [DataRow(ImdsEndpoint, Resource, "https://graph.microsoft.com", ManagedIdentitySource.Imds)]
        [DataRow(AzureArcEndpoint, Resource, "https://graph.microsoft.com", ManagedIdentitySource.AzureArc)]
        [DataRow(CloudShellEndpoint, Resource, "https://graph.microsoft.com", ManagedIdentitySource.CloudShell)]
        [DataRow(ServiceFabricEndpoint, Resource, "https://graph.microsoft.com", ManagedIdentitySource.ServiceFabric)]
        [DataRow(MachineLearningEndpoint, Resource, "https://graph.microsoft.com", ManagedIdentitySource.MachineLearning)]
        public async Task ManagedIdentityDifferentScopesTestAsync(
            string endpoint,
            string scope,
            string anotherScope,
            ManagedIdentitySource managedIdentitySource)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    managedIdentitySource);

                var result = await mi.AcquireTokenForManagedIdentity(scope).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Acquire token for same scope
                result = await mi.AcquireTokenForManagedIdentity(scope)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    anotherScope,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    managedIdentitySource);

                // Acquire token for another scope
                result = await mi.AcquireTokenForManagedIdentity(anotherScope).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(AppServiceEndpoint, Resource, ManagedIdentitySource.AppService)]
        [DataRow(ImdsEndpoint, Resource, ManagedIdentitySource.Imds)]
        [DataRow(AzureArcEndpoint, Resource, ManagedIdentitySource.AzureArc)]
        [DataRow(CloudShellEndpoint, Resource, ManagedIdentitySource.CloudShell)]
        [DataRow(ServiceFabricEndpoint, Resource, ManagedIdentitySource.ServiceFabric)]
        [DataRow(MachineLearningEndpoint, Resource, ManagedIdentitySource.MachineLearning)]
        public async Task ManagedIdentityForceRefreshTestAsync(
            string endpoint,
            string scope,
            ManagedIdentitySource managedIdentitySource)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    managedIdentitySource);

                var result = await mi.AcquireTokenForManagedIdentity(scope).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Acquire token from cache
                result = await mi.AcquireTokenForManagedIdentity(scope)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    scope,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    managedIdentitySource);

                // Acquire token with force refresh
                result = await mi.AcquireTokenForManagedIdentity(scope).WithForceRefresh(true)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(AppServiceEndpoint, Resource, ManagedIdentitySource.AppService)]
        [DataRow(ImdsEndpoint, Resource, ManagedIdentitySource.Imds)]
        [DataRow(AzureArcEndpoint, Resource, ManagedIdentitySource.AzureArc)]
        [DataRow(CloudShellEndpoint, Resource, ManagedIdentitySource.CloudShell)]
        [DataRow(ServiceFabricEndpoint, Resource, ManagedIdentitySource.ServiceFabric)]
        [DataRow(MachineLearningEndpoint, Resource, ManagedIdentitySource.MachineLearning)]
        public async Task ManagedIdentityWithClaimsAndCapabilitiesTestAsync(
            string endpoint,
            string scope,
            ManagedIdentitySource managedIdentitySource)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithClientCapabilities(TestConstants.ClientCapabilities)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    managedIdentitySource);

                var result = await mi.AcquireTokenForManagedIdentity(scope).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Acquire token from cache
                result = await mi.AcquireTokenForManagedIdentity(scope)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    scope,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    managedIdentitySource);

                // Acquire token with force refresh
                result = await mi.AcquireTokenForManagedIdentity(scope).WithClaims(TestConstants.Claims)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(AppServiceEndpoint, Resource, ManagedIdentitySource.AppService)]
        [DataRow(ImdsEndpoint, Resource, ManagedIdentitySource.Imds)]
        [DataRow(AzureArcEndpoint, Resource, ManagedIdentitySource.AzureArc)]
        [DataRow(CloudShellEndpoint, Resource, ManagedIdentitySource.CloudShell)]
        [DataRow(ServiceFabricEndpoint, Resource, ManagedIdentitySource.ServiceFabric)]
        [DataRow(MachineLearningEndpoint, Resource, ManagedIdentitySource.MachineLearning)]
        public async Task ManagedIdentityWithClaimsTestAsync(
            string endpoint,
            string scope,
            ManagedIdentitySource managedIdentitySource)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures(true)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    managedIdentitySource);

                var result = await mi.AcquireTokenForManagedIdentity(scope).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Acquire token from cache
                result = await mi.AcquireTokenForManagedIdentity(scope)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    scope,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    managedIdentitySource);

                // Acquire token with force refresh
                result = await mi.AcquireTokenForManagedIdentity(scope).WithClaims(TestConstants.Claims)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow("user.read", ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [DataRow("https://management.core.windows.net//user_impersonation", ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [DataRow("s", ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [DataRow("user.read", ManagedIdentitySource.Imds, ImdsEndpoint)]
        [DataRow("https://management.core.windows.net//user_impersonation", ManagedIdentitySource.Imds, ImdsEndpoint)]
        [DataRow("s", ManagedIdentitySource.Imds, ImdsEndpoint)]
        [DataRow("user.read", ManagedIdentitySource.AzureArc, AzureArcEndpoint)]
        [DataRow("https://management.core.windows.net//user_impersonation", ManagedIdentitySource.AzureArc, AzureArcEndpoint)]
        [DataRow("s", ManagedIdentitySource.AzureArc, AzureArcEndpoint)]
        [DataRow("user.read", ManagedIdentitySource.CloudShell, CloudShellEndpoint)]
        [DataRow("https://management.core.windows.net//user_impersonation", ManagedIdentitySource.CloudShell, CloudShellEndpoint)]
        [DataRow("s", ManagedIdentitySource.CloudShell, CloudShellEndpoint)]
        [DataRow("user.read", ManagedIdentitySource.ServiceFabric, ServiceFabricEndpoint)]
        [DataRow("https://management.core.windows.net//user_impersonation", ManagedIdentitySource.ServiceFabric, ServiceFabricEndpoint)]
        [DataRow("s", ManagedIdentitySource.ServiceFabric, ServiceFabricEndpoint)]
        [DataRow("user.read", ManagedIdentitySource.MachineLearning, MachineLearningEndpoint)]
        [DataRow("https://management.core.windows.net//user_impersonation", ManagedIdentitySource.MachineLearning, MachineLearningEndpoint)]
        [DataRow("s", ManagedIdentitySource.MachineLearning, MachineLearningEndpoint)]
        public async Task ManagedIdentityTestWrongScopeAsync(string resource, ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(endpoint, resource, MockHelpers.GetMsiErrorResponse(managedIdentitySource),
                    managedIdentitySource, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(endpoint, resource, MockHelpers.GetMsiErrorResponse(managedIdentitySource),
                    managedIdentitySource, statusCode: HttpStatusCode.InternalServerError);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(managedIdentitySource.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.IsFalse(ex.Message.Contains(MsalErrorMessage.ManagedIdentityUnexpectedErrorResponse));
            }
        }

        [DataTestMethod]
        [DataRow("{\"statusCode\":500,\"message\":\"Error message\",\"correlationId\":\"GUID\"}", new string[] { "Error message", "GUID" })]
        [DataRow("{\"message\":\"Error message\",\"correlationId\":\"GUID\"}", new string[] { "Error message", "GUID" })]
        [DataRow("{\"error\":\"errorCode\",\"error_description\":\"Error message\"}", new string[] { "errorCode", "Error message" })]
        [DataRow("{\"error_description\":\"Error message\"}", new string[] { "Error message" })]
        [DataRow("{\"message\":\"Error message\"}", new string[] { "Error message" })]
        [DataRow("{\"error\":{\"code\":\"errorCode\"}}", new string[] { "errorCode" })]
        [DataRow("{\"error\":{\"message\":\"Error message\"}}", new string[] { "Error message" })]
        [DataRow("{\"error\":{\"code\":\"errorCode\",\"message\":\"Error message\"}}", new string[] { "errorCode", "Error message" })]
        [DataRow("{\"error\":{\"code\":\"errorCode\",\"message\":\"Error message\",\"innererror\":{\"trace\":\"trace\"}}}", new string[] { "errorCode", "Error message" })]
        [DataRow("{\"notExpectedJson\":\"someValue\"}", new string[] { MsalErrorMessage.ManagedIdentityUnexpectedErrorResponse })]
        [DataRow("notExpectedJson", new string[] { MsalErrorMessage.ManagedIdentityUnexpectedErrorResponse })]
        public async Task ManagedIdentityTestErrorResponseParsing(string errorResponse, string[] expectedInErrorResponse)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;
                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(AppServiceEndpoint, Resource, errorResponse,
                    ManagedIdentitySource.AppService, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(AppServiceEndpoint, Resource, errorResponse,
                    ManagedIdentitySource.AppService, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(AppServiceEndpoint, Resource, errorResponse,
                    ManagedIdentitySource.AppService, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(AppServiceEndpoint, Resource, errorResponse,
                    ManagedIdentitySource.AppService, statusCode: HttpStatusCode.InternalServerError);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.AppService.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);

                foreach (var expectedErrorSubString in expectedInErrorResponse)
                {
                    Assert.IsTrue(ex.Message.Contains(expectedErrorSubString), 
                        $"Expected to contain string {expectedErrorSubString}. Actual error message: {ex.Message}");
                }
            }
        }

        [DataTestMethod]
        [DataRow("", ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [DataRow(null, ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ManagedIdentityTestNullOrEmptyScopeAsync(string resource, ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager).Build();

                await mi.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync().ConfigureAwait(false);
            }
        }

        [DataTestMethod]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.Imds, ImdsEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, ServiceFabricEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, MachineLearningEndpoint)]
        public async Task ManagedIdentityErrorResponseNoPayloadTestAsync(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(endpoint, "scope", "",
                    managedIdentitySource, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(endpoint, "scope", "",
                    managedIdentitySource, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(endpoint, "scope", "",
                    managedIdentitySource, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(endpoint, "scope", "",
                    managedIdentitySource, statusCode: HttpStatusCode.InternalServerError);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(managedIdentitySource.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityNoResponseReceived, ex.Message);
            }
        }

        [DataTestMethod]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.Imds, ImdsEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, ServiceFabricEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, MachineLearningEndpoint)]
        public async Task ManagedIdentityNullResponseAsync(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    Resource,
                    "",
                    managedIdentitySource,
                    statusCode: HttpStatusCode.OK);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(managedIdentitySource.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityInvalidResponse, ex.Message);
            }
        }

        [DataTestMethod]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.Imds, ImdsEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, ServiceFabricEndpoint)]
        [DataRow(ManagedIdentitySource.MachineLearning, MachineLearningEndpoint)]
        public async Task ManagedIdentityUnreachableNetworkAsync(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddFailingRequest(new HttpRequestException("A socket operation was attempted to an unreachable network.",
                    new SocketException(10051)));

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(managedIdentitySource.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityUnreachableNetwork, ex.ErrorCode);
                Assert.AreEqual("A socket operation was attempted to an unreachable network.", ex.Message);
            }
        }

        [TestMethod] 
        public async Task SystemAssignedManagedIdentityApiIdTestAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
     
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    AppServiceEndpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.AppService);

                var builder = mi.AcquireTokenForManagedIdentity(Resource);
                var result = await builder.ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                Assert.AreEqual(ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity, builder.CommonParameters.ApiId);
            }
        }

        [TestMethod]
        public async Task UserAssignedManagedIdentityApiIdTestAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedClientId(TestConstants.ClientId))
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    AppServiceEndpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.AppService,
                    userAssignedId: TestConstants.ClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                var builder = mi.AcquireTokenForManagedIdentity(Resource);
                var result = await builder.ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                Assert.AreEqual(ApiEvent.ApiIds.AcquireTokenForUserAssignedManagedIdentity, builder.CommonParameters.ApiId);
            }
        }

        [TestMethod]
        public async Task ManagedIdentityCacheTestAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.BuildConcrete();

                CancellationTokenSource cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;

                var appTokenCacheRecoder = mi.AppTokenCacheInternal.RecordAccess((args) =>
                {
                    Assert.AreEqual(Constants.ManagedIdentityDefaultTenant, args.RequestTenantId);
                    Assert.AreEqual(Constants.ManagedIdentityDefaultClientId, args.ClientId);
                    Assert.IsNull(args.Account);
                    Assert.IsTrue(args.IsApplicationCache);
                    Assert.AreEqual(cancellationToken, args.CancellationToken);
                });

                httpManager.AddManagedIdentityMockHandler(
                    AppServiceEndpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.AppService);

                var result = await mi.AcquireTokenForManagedIdentity(Resource).ExecuteAsync(cancellationToken).ConfigureAwait(false);

                appTokenCacheRecoder.AssertAccessCounts(1, 1);
            }
        }

        [DataTestMethod]
        [DataRow(1, false, false)] // Unix timestamp
        [DataRow(2, false, false)] // Unix timestamp
        [DataRow(3, true, false)]  // Unix timestamp
        [DataRow(1, false, true)]  // ISO 8601
        [DataRow(2, false, true)]  // ISO 8601
        [DataRow(3, true, true)]   // ISO 8601
        public async Task ManagedIdentityExpiresOnTestAsync(int expiresInHours, bool refreshOnHasValue, bool useIsoFormat)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    AppServiceEndpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(expiresInHours, useIsoFormat),
                    ManagedIdentitySource.AppService);

                AcquireTokenForManagedIdentityParameterBuilder builder = mi.AcquireTokenForManagedIdentity(Resource);
                AuthenticationResult result = await builder.ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity, builder.CommonParameters.ApiId);
                Assert.AreEqual(refreshOnHasValue, result.AuthenticationResultMetadata.RefreshOn.HasValue);
                Assert.IsTrue(result.ExpiresOn > DateTimeOffset.UtcNow, "The token's ExpiresOn should be in the future.");

            }
        }

        [TestMethod]
        [ExpectedException(typeof(MsalClientException))]
        public async Task ManagedIdentityInvalidRefreshOnThrowsAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                    AppServiceEndpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(0),
                    ManagedIdentitySource.AppService);

                AcquireTokenForManagedIdentityParameterBuilder builder = mi.AcquireTokenForManagedIdentity(Resource);

                AuthenticationResult result = await builder.ExecuteAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task ManagedIdentityIsProActivelyRefreshedAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                Trace.WriteLine("1. Setup an app with a token cache with one AT");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.BuildConcrete();

                httpManager.AddManagedIdentityMockHandler(
                        AppServiceEndpoint,
                        Resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        ManagedIdentitySource.AppService);

                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                var refreshOn = TestCommon.UpdateATWithRefreshOn(mi.AppTokenCacheInternal.Accessor).RefreshOn;
                TokenCacheAccessRecorder cacheAccess = mi.AppTokenCacheInternal.RecordAccess();

                Trace.WriteLine("3. Configure MSI to respond with a valid token");
                httpManager.AddManagedIdentityMockHandler(
                        AppServiceEndpoint,
                        Resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        ManagedIdentitySource.AppService);

                // Act
                Trace.WriteLine("4. ATM - should perform an RT refresh");
                result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                TestCommon.YieldTillSatisfied(() => httpManager.QueueSize == 0);

                Assert.IsNotNull(result);

                Assert.AreEqual(0, httpManager.QueueSize,
                    "MSAL should have refreshed the token because the original AT was marked for refresh");
                
                cacheAccess.WaitTo_AssertAcessCounts(1, 1);

                Assert.AreEqual(CacheRefreshReason.ProactivelyRefreshed, result.AuthenticationResultMetadata.CacheRefreshReason);

                Assert.AreEqual(refreshOn, result.AuthenticationResultMetadata.RefreshOn);

                result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                
                Assert.AreEqual(CacheRefreshReason.NotApplicable, result.AuthenticationResultMetadata.CacheRefreshReason);
            }
        }

        [TestMethod]
        public async Task ProactiveRefresh_CancelsSuccessfully_Async()
        {
            bool wasErrorLogged = false;

            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithLogging(LocalLogCallback)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.BuildConcrete();

                httpManager.AddManagedIdentityMockHandler(
                        AppServiceEndpoint,
                        Resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        ManagedIdentitySource.AppService);

                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                TestCommon.UpdateATWithRefreshOn(mi.AppTokenCacheInternal.Accessor);

                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;
                cts.Cancel();
                cts.Dispose();

                // Act
                result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Assert
                Assert.IsTrue(TestCommon.YieldTillSatisfied(() => wasErrorLogged));

                void LocalLogCallback(LogLevel level, string message, bool containsPii)
                {
                    if (level == LogLevel.Warning &&
                        message.Contains(SilentRequestHelper.ProactiveRefreshCancellationError))
                    {
                        wasErrorLogged = true;
                    }
                }
            }
        }

        [TestMethod]
        public async Task ParallelRequests_CallTokenEndpointOnceAsync()
        {
            int numOfTasks = 10; 
            int identityProviderHits = 0;
            int cacheHits = 0;

            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                Trace.WriteLine("1. Setup an app with a token cache with one AT");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.BuildConcrete();

                httpManager.AddManagedIdentityMockHandler(
                        AppServiceEndpoint,
                        Resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        ManagedIdentitySource.AppService);

                Task[] tasks = new Task[numOfTasks];
                for (int i = 0; i < numOfTasks; i++)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        AuthenticationResult authResult = await mi.AcquireTokenForManagedIdentity(Resource)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                        if (authResult.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider)
                        {
                            // Increment identity hits count
                            Interlocked.Increment(ref identityProviderHits);
                            Assert.IsTrue(identityProviderHits == 1);
                        }
                        else
                        {
                            // Increment cache hits count
                            Interlocked.Increment(ref cacheHits);
                        }
                    });
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                Debug.WriteLine($"Total Identity Hits: {identityProviderHits}");
                Debug.WriteLine($"Total Cache Hits: {cacheHits}");
                Assert.IsTrue(cacheHits == 9);
            }
        }

        [TestMethod]
        // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4472
        // Should throw TaskCanceledException instead of trying to take a semaphore
        public async Task CanceledRequest_ThrowsTaskCanceledExceptionAsync()
        {
            var app = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .BuildConcrete();

            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            await AssertException.TaskThrowsAsync<TaskCanceledException>(
                () => app.AcquireTokenForManagedIdentity(Resource)
                        .WithForceRefresh(true)
                        .ExecuteAsync(tokenSource.Token)).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(ManagedIdentitySource.Imds, ImdsEndpoint)]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, ServiceFabricEndpoint)]
        public async Task InvalidJsonResponseHandling(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(
                     endpoint,
                     "scope",
                     MockHelpers.GetMsiErrorBadJson(),
                     managedIdentitySource);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.AreEqual(managedIdentitySource.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityResponseParseFailure, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityJsonParseFailure, ex.Message);
            }
        }

        [DataTestMethod]
        [DataRow(Resource, "https://graph.microsoft.com", ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [DataRow(Resource, "https://graph.microsoft.com", ManagedIdentitySource.Imds, ImdsEndpoint)]
        [DataRow(Resource, "https://graph.microsoft.com", ManagedIdentitySource.AzureArc, AzureArcEndpoint)]
        [DataRow(Resource, "https://graph.microsoft.com", ManagedIdentitySource.CloudShell, CloudShellEndpoint)]
        [DataRow(Resource, "https://graph.microsoft.com", ManagedIdentitySource.ServiceFabric, ServiceFabricEndpoint)]
        [DataRow(Resource, "https://graph.microsoft.com", ManagedIdentitySource.MachineLearning, MachineLearningEndpoint)]
        public async Task ManagedIdentityRequestTokensForDifferentScopesTestAsync(
            string initialResource, 
            string newResource, 
            ManagedIdentitySource source, 
            string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(source, endpoint);

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Mock handler for the initial resource request
                httpManager.AddManagedIdentityMockHandler(endpoint, initialResource,
                    MockHelpers.GetMsiSuccessfulResponse(), source);

                // Request token for initial resource
                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(initialResource).ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Mock handler for the new resource request
                httpManager.AddManagedIdentityMockHandler(endpoint, newResource,
                    MockHelpers.GetMsiSuccessfulResponse(), source);

                // Request token for new resource
                result = await mi.AcquireTokenForManagedIdentity(newResource).ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Request token again for the same initial resource to check cache usage
                result = await mi.AcquireTokenForManagedIdentity(initialResource).ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                // Request token again for the new resource to check cache usage
                result = await mi.AcquireTokenForManagedIdentity(newResource).ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(ManagedIdentitySource.AppService)]
        [DataRow(ManagedIdentitySource.Imds)]
        [DataRow(ManagedIdentitySource.AzureArc)]
        [DataRow(ManagedIdentitySource.CloudShell)]
        [DataRow(ManagedIdentitySource.ServiceFabric)]
        [DataRow(ManagedIdentitySource.MachineLearning)]
        public async Task UnsupportedManagedIdentitySource_ThrowsExceptionDuringTokenAcquisitionAsync(
            ManagedIdentitySource managedIdentitySource)
        {
            string UnsupportedEndpoint = "unsupported://endpoint";

            using (new EnvVariableContext())
            {
                // Set unsupported environment variable
                SetEnvironmentVariables(managedIdentitySource, UnsupportedEndpoint);

                // Create the Managed Identity Application
                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned);

                // Build the application
                var mi = miBuilder.Build();

                // Attempt to acquire a token and verify an exception is thrown
                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("https://management.azure.com")
                        .ExecuteAsync()
                        .ConfigureAwait(false)).ConfigureAwait(false);

                // Verify the exception details
                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task MixedUserAndSystemAssignedManagedIdentityTestAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, AppServiceEndpoint);

                // User-assigned identity client ID
                string UserAssignedClientId = "d3adb33f-c0de-ed0c-c0de-deadb33fc0d3";
                string SystemAssignedClientId = "system_assigned_managed_identity";

                // Create a builder for user-assigned identity
                var userAssignedBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedClientId(UserAssignedClientId))
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                userAssignedBuilder.Config.AccessorOptions = null;

                var userAssignedMI = userAssignedBuilder.BuildConcrete();

                // Record token cache access for user-assigned identity
                var userAssignedCacheRecorder = userAssignedMI.AppTokenCacheInternal.RecordAccess();

                // Mock handler for user-assigned token
                httpManager.AddManagedIdentityMockHandler(
                    AppServiceEndpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.AppService,
                    userAssignedId: UserAssignedClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                var userAssignedResult = await userAssignedMI.AcquireTokenForManagedIdentity(Resource).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(userAssignedResult);
                Assert.AreEqual(TokenSource.IdentityProvider, userAssignedResult.AuthenticationResultMetadata.TokenSource);

                // Verify user-assigned cache entries
                userAssignedCacheRecorder.AssertAccessCounts(1, 1);

                // Create a builder for system-assigned identity
                var systemAssignedBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                systemAssignedBuilder.Config.AccessorOptions = null;

                var systemAssignedMI = systemAssignedBuilder.BuildConcrete();

                // Record token cache access for system-assigned identity
                var systemAssignedCacheRecorder = systemAssignedMI.AppTokenCacheInternal.RecordAccess();

                // Mock handler for system-assigned token
                httpManager.AddManagedIdentityMockHandler(
                    AppServiceEndpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.AppService);

                var systemAssignedResult = await systemAssignedMI.AcquireTokenForManagedIdentity(Resource).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(systemAssignedResult);
                Assert.AreEqual(TokenSource.IdentityProvider, systemAssignedResult.AuthenticationResultMetadata.TokenSource);

                // Verify system-assigned cache entries
                systemAssignedCacheRecorder.AssertAccessCounts(1, 1);

                // Ensure the cache contains correct entries for both identities
                var userAssignedTokens = userAssignedMI.AppTokenCacheInternal.Accessor.GetAllAccessTokens();
                var systemAssignedTokens = systemAssignedMI.AppTokenCacheInternal.Accessor.GetAllAccessTokens();

                Assert.AreEqual(1, userAssignedTokens.Count, "User-assigned cache entry missing.");
                Assert.AreEqual(1, systemAssignedTokens.Count, "System-assigned cache entry missing.");

                // Verify the ClientId for each cached entry
                Assert.AreEqual(UserAssignedClientId, userAssignedTokens[0].ClientId, "User-assigned ClientId mismatch in cache.");
                Assert.AreEqual(SystemAssignedClientId, systemAssignedTokens[0].ClientId, "System-assigned ClientId mismatch in cache.");
            }
        }

        [DataTestMethod]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint, HttpStatusCode.NotFound)]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint, HttpStatusCode.RequestTimeout)]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint, 429)]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint, HttpStatusCode.InternalServerError)]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint, HttpStatusCode.ServiceUnavailable)]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint, HttpStatusCode.GatewayTimeout)]
        [DataRow(ManagedIdentitySource.AzureArc, AzureArcEndpoint, HttpStatusCode.GatewayTimeout)]
        [DataRow(ManagedIdentitySource.CloudShell, CloudShellEndpoint, HttpStatusCode.GatewayTimeout)]
        [DataRow(ManagedIdentitySource.Imds, ImdsEndpoint, HttpStatusCode.GatewayTimeout)]
        [DataRow(ManagedIdentitySource.MachineLearning, MachineLearningEndpoint, HttpStatusCode.GatewayTimeout)]
        [DataRow(ManagedIdentitySource.ServiceFabric, ServiceFabricEndpoint, HttpStatusCode.GatewayTimeout)]
        public async Task ManagedIdentityRetryPolicyLifeTimeIsPerRequestAsync(
            ManagedIdentitySource managedIdentitySource,
            string endpoint,
            HttpStatusCode statusCode)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disable cache to avoid pollution
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                // Simulate permanent errors (to trigger the maximum number of retries)
                const int NumErrors = ManagedIdentityRequest.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES + 1; // initial request + maximum number of retries (3)
                for (int i = 0; i < NumErrors; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        Resource,
                        "",
                        managedIdentitySource,
                        statusCode: statusCode);
                }
                
                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);
                Assert.IsNotNull(ex);

                // 4 total: request + 3 retries
                Assert.AreEqual(LinearRetryPolicy.numRetries, 1 + ManagedIdentityRequest.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES);
                Assert.AreEqual(httpManager.QueueSize, 0);

                for (int i = 0; i < NumErrors; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        Resource,
                        "",
                        managedIdentitySource,
                        statusCode: HttpStatusCode.InternalServerError);
                }

                ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);
                Assert.IsNotNull(ex);

                // 4 total: request + 3 retries
                // (numRetries would be x2 if retry policy was NOT per request)
                Assert.AreEqual(LinearRetryPolicy.numRetries, 1 + ManagedIdentityRequest.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES);
                Assert.AreEqual(httpManager.QueueSize, 0);

                for (int i = 0; i < NumErrors; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        endpoint,
                        Resource,
                        "",
                        managedIdentitySource,
                        statusCode: HttpStatusCode.InternalServerError);
                }

                ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);
                Assert.IsNotNull(ex);

                // 4 total: request + 3 retries
                // (numRetries would be x3 if retry policy was NOT per request)
                Assert.AreEqual(LinearRetryPolicy.numRetries, 1 + ManagedIdentityRequest.DEFAULT_MANAGED_IDENTITY_MAX_RETRIES);
                Assert.AreEqual(httpManager.QueueSize, 0);
            }
        }
    }
}
