// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{

    [TestClass]
    public class ManagedIdentityTests : TestBase
    {
        internal const string Resource = "https://management.azure.com";
        internal const string ResourceDefaultSuffix = "https://management.azure.com/.default";
        internal const string AppServiceEndpoint = "http://127.0.0.1:41564/msi/token";
        internal const string ImdsEndpoint = "http://169.254.169.254/metadata/identity/oauth2/token";
        internal const string AzureArcEndpoint = "http://localhost:40342/metadata/identity/oauth2/token";
        internal const string CloudShellEndpoint = "http://localhost:40342/metadata/identity/oauth2/token";
        internal const string ServiceFabricEndpoint = "http://localhost:40342/metadata/identity/oauth2/token";

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
        public async Task ManagedIdentityHappyPathAsync(
            string endpoint,
            string scope,
            ManagedIdentitySource managedIdentitySource)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var mi = ManagedIdentityApplicationBuilder.Create(SystemAssignedManagedIdentity.Default())
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

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
        [DataRow(AppServiceEndpoint, ManagedIdentitySource.AppService, TestConstants.ClientId, UserAssignedIdType.ClientId)]
        [DataRow(AppServiceEndpoint, ManagedIdentitySource.AppService, TestConstants.MiResourceId, UserAssignedIdType.ResourceId)]
        [DataRow(ImdsEndpoint, ManagedIdentitySource.Imds, TestConstants.ClientId, UserAssignedIdType.ClientId)]
        [DataRow(ImdsEndpoint, ManagedIdentitySource.Imds, TestConstants.MiResourceId, UserAssignedIdType.ResourceId)]
        [DataRow(ServiceFabricEndpoint, ManagedIdentitySource.ServiceFabric, TestConstants.ClientId, UserAssignedIdType.ClientId)]
        [DataRow(ServiceFabricEndpoint, ManagedIdentitySource.ServiceFabric, TestConstants.MiResourceId, UserAssignedIdType .ResourceId)]
        public async Task ManagedIdentityUserAssignedHappyPathAsync(
            string endpoint,
            ManagedIdentitySource managedIdentitySource,
            string userAssignedId,
            UserAssignedIdType userAssignedIdType)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var mi = ManagedIdentityApplicationBuilder.Create(
                    userAssignedIdType == UserAssignedIdType.ClientId ? 
                        UserAssignedManagedIdentity.FromClientId(userAssignedId) : 
                        UserAssignedManagedIdentity.FromResourceId(userAssignedId))
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    managedIdentitySource,
                    userAssignedClientIdOrResourceId: userAssignedId,
                    userAssignedIdType: userAssignedIdType);

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

                var mi = ManagedIdentityApplicationBuilder.Create(SystemAssignedManagedIdentity.Default())
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

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
        public async Task ManagedIdentityForceRefreshTestAsync(
            string endpoint,
            string scope,
            ManagedIdentitySource managedIdentitySource)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                var mi = ManagedIdentityApplicationBuilder.Create(SystemAssignedManagedIdentity.Default())
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

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
        public async Task ManagedIdentityTestWrongScopeAsync(string resource, ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create(SystemAssignedManagedIdentity.Default())
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager).Build();

                httpManager.AddManagedIdentityMockHandler(endpoint, resource, MockHelpers.GetMsiErrorResponse(),
                    managedIdentitySource, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(endpoint, resource, MockHelpers.GetMsiErrorResponse(),
                    managedIdentitySource, statusCode: HttpStatusCode.InternalServerError);

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(managedIdentitySource, ex.ManagedIdentitySource);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
            }
        }

        [DataTestMethod]
        [DataRow(ManagedIdentitySource.AppService, AppServiceEndpoint)]
        [DataRow(ManagedIdentitySource.Imds, ImdsEndpoint)]
        [DataRow(ManagedIdentitySource.AzureArc, AzureArcEndpoint)]
        [DataRow(ManagedIdentitySource.CloudShell, CloudShellEndpoint)]
        [DataRow(ManagedIdentitySource.ServiceFabric, ServiceFabricEndpoint)]
        public async Task ManagedIdentityErrorResponseNoPayloadTestAsync(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create(SystemAssignedManagedIdentity.Default())
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager).Build();

                httpManager.AddManagedIdentityMockHandler(endpoint, "scope", "",
                    managedIdentitySource, statusCode: HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler(endpoint, "scope", "",
                    managedIdentitySource, statusCode: HttpStatusCode.InternalServerError);

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(managedIdentitySource, ex.ManagedIdentitySource);
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
        public async Task ManagedIdentityNullResponseAsync(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create(SystemAssignedManagedIdentity.Default())
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager).Build();

                httpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    Resource,
                    "",
                    managedIdentitySource,
                    statusCode: HttpStatusCode.OK);

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(managedIdentitySource, ex.ManagedIdentitySource);
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
        public async Task ManagedIdentityUnreachableNetworkAsync(ManagedIdentitySource managedIdentitySource, string endpoint)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(managedIdentitySource, endpoint);

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create(SystemAssignedManagedIdentity.Default())
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager).Build();

                httpManager.AddFailingRequest(new HttpRequestException("A socket operation was attempted to an unreachable network.", 
                    new SocketException(10051)));

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(managedIdentitySource, ex.ManagedIdentitySource);
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

                var mi = ManagedIdentityApplicationBuilder.Create(SystemAssignedManagedIdentity.Default())
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

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

                var mi = ManagedIdentityApplicationBuilder.Create(UserAssignedManagedIdentity.FromClientId(TestConstants.ClientId))
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityMockHandler(
                    AppServiceEndpoint,
                    Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.AppService,
                    userAssignedClientIdOrResourceId: TestConstants.ClientId,
                    userAssignedIdType: UserAssignedIdType.ClientId);

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

                var mi = ManagedIdentityApplicationBuilder.Create(SystemAssignedManagedIdentity.Default())
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .BuildConcrete();
                CancellationTokenSource cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;

                var appTokenCacheRecoder = mi.AppTokenCache.RecordAccess((args) =>
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
    }
}
