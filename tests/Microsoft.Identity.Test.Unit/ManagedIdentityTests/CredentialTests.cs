// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;
using Microsoft.Identity.Client.Internal;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Unit tests for the Credential component in the Managed Identity scenario.
    /// </summary>
    [TestClass]
    public class CredentialTests : TestBase
    {
        internal const string CredentialEndpoint = "http://169.254.169.254/metadata/identity/credential";
        internal const string MtlsEndpoint = "https://centraluseuap.mtlsauth.microsoft.com/" +
            "72f988bf-86f1-41af-91ab-2d7cd011db47/oauth2/v2.0/token";
        internal const string Resource = "https://management.azure.com";

        /// <summary>
        /// Tests the happy path for acquiring credentials with various CryptoKeyTypes.
        /// </summary>
        /// <param name="cryptoKeyType">The type of cryptographic key used.</param>
        [DataTestMethod]
        [DataRow(CryptoKeyType.Machine)]
        [DataRow(CryptoKeyType.User)]
        [DataRow(CryptoKeyType.InMemory)]
        [DataRow(CryptoKeyType.Ephemeral)]
        [DataRow(CryptoKeyType.KeyGuard)]
        public async Task CredentialHappyPathAsync(int keyType)
        {
            CryptoKeyType cryptoKeyType = (CryptoKeyType)keyType;

            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), cryptoKeyType);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                // Act
                // We should get the auth result from the token provider
                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(miBuilder.Config.ClientId, "system_assigned_managed_identity");
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Act
                // We should get the auth result from the cache
                result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        /// <summary>
        /// Tests the happy path for acquiring credentials with various CryptoKeyTypes.
        /// </summary>
        /// <param name="cryptoKeyType">The type of cryptographic key used.</param>
        [DataTestMethod]
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow(TestConstants.MiResourceId, UserAssignedIdentityId.ResourceId)]
        [DataRow(TestConstants.ObjectId, UserAssignedIdentityId.ObjectId)]
        public async Task CredentialUserAssignedHappyPathAsync(string userAssignedId,
            UserAssignedIdentityId userAssignedIdentityId)
        {
            CryptoKeyType cryptoKeyType = CryptoKeyType.Machine;

            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(userAssignedId, userAssignedIdentityId);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), cryptoKeyType);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                miBuilder.WithHttpManager(httpManager);

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse(),
                    userAssignedId: userAssignedId,
                    userAssignedIdentityId: userAssignedIdentityId);

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                // Act
                // We should get the auth result from the token provider
                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(miBuilder.Config.ClientId, userAssignedId);
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Act
                // We should get the auth result from the cache
                result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        /// <summary>
        /// Tests the Force Refresh on MI.
        /// </summary>
        [TestMethod]
        public async Task CredentialForceRefreshAsync()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Machine);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                // Act
                // We should get the auth result from the token provider
                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Act
                // We should get the auth result from the cache
                result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                // We should get the auth result from the token provider when force refreshed
                result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithForceRefresh(true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        /// <summary>
        /// Tests the Claims on MI.
        /// </summary>
        [TestMethod]
        public async Task CredentialWithClaimsAsync()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures(true)
                    .WithClientCapabilities(new[] { "CP1" })
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.User);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                // Act
                // We should get the auth result from the token provider
                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Act
                // We should get the auth result from the cache
                result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                // Arrange
                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                // Act
                // We should get the auth result from the token provider when claims are passed
                var builder = mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaims(TestConstants.Claims);

                result = await builder.ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        /// <summary>
        /// Tests the Invalid Credential endpoint.
        /// </summary>
        [TestMethod]
        public async Task InvalidCredentialEndpointAsync()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                //Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Arrange
                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                var correlationId = Guid.NewGuid();

                httpManager.AddTokenErrorResponse("invalid_grant", System.Net.HttpStatusCode.BadRequest);

                MsalUiRequiredException ex = await Assert.ThrowsExceptionAsync<MsalUiRequiredException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                //Act
                Assert.IsNotNull(ex);
            }
        }

        /// <summary>
        /// Tests the Failed response from mtls endpoint.
        /// </summary>
        [TestMethod]
        public async Task FailedResponseAsync()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                //Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                var correlationId = Guid.NewGuid();
                var tokenRequest = httpManager.AddFailureTokenEndpointResponse(
                    "invalid_grant",
                    TestConstants.AuthorityUtidTenant,
                    correlationId.ToString());

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                //Act
                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.Credential, ex.ManagedIdentitySource);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.CredentialEndpointNoResponseReceived), ex.Message);
            }
        }

        [DataTestMethod]
        [DataRow("https://graph.microsoft.com")]
        public async Task ManagedIdentityDifferentScopesTestAsync(string anotherScope)
      
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                //Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Arrange
                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // Acquire token for same scope
                result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                // Arrange
                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    anotherScope,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    anotherScope,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                // Acquire token for another scope
                result = await mi.AcquireTokenForManagedIdentity(anotherScope).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Common.TestData.GetMtlsInvalidResourceErrorData), typeof(Common.TestData), DynamicDataSourceType.Method)]
        public async Task ManagedIdentityTestWrongScopeAsync(
            Func<string> errorFactory, // Use Func<string> for dynamic error data
            string resource,
            string expectedErrorMessage)
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                //Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Arrange
                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    resource,
                    isCredentialEndpoint: false,
                    errorFactory.Invoke(), statusCode: HttpStatusCode.BadRequest);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ex.Message, expectedErrorMessage);
                //Assert.AreEqual(ManagedIdentitySource.Credential, ex.ManagedIdentitySource);
                //Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
            }
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(null)]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ManagedIdentityTestNullOrEmptyScopeAsync(string resource)
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                //Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                await mi.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task ManagedIdentityErrorResponseNoPayloadTestAsync()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                //Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Arrange
                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    "",
                    statusCode: HttpStatusCode.InternalServerError);

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.Credential, ex.ManagedIdentitySource);
                Assert.AreEqual(MsalError.CredentialRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.CredentialEndpointNoResponseReceived, ex.Message);
            }
        }

        [TestMethod]
        public async Task ManagedIdentityNullResponseAsync()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                //Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Arrange
                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    "",
                    statusCode: HttpStatusCode.OK);

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.Credential, ex.ManagedIdentitySource);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityInvalidResponse, ex.Message);
            }
        }

        [TestMethod]
        public async Task ManagedIdentityUnreachableNetworkAsync()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                //Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Arrange
                httpManager.AddFailingRequest(new HttpRequestException("A socket operation was attempted to an unreachable network.",
                    new SocketException(10051)));

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.Credential, ex.ManagedIdentitySource);
                Assert.AreEqual(MsalError.ManagedIdentityUnreachableNetwork, ex.ErrorCode);
                Assert.AreEqual("A socket operation was attempted to an unreachable network.", ex.Message);
            }
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.RequestTimeout)]
        [DataRow(HttpStatusCode.InternalServerError)]
        [DataRow(HttpStatusCode.ServiceUnavailable)]
        [DataRow(HttpStatusCode.GatewayTimeout)]
        [DataRow(HttpStatusCode.NotFound)]
        public async Task ManagedIdentityTestRetryAsync(HttpStatusCode statusCode)
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                //Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Arrange
                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    "",
                    statusCode: statusCode);

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    "",
                    statusCode: statusCode);

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.CredentialRequestFailed, ex.ErrorCode);
                //Assert.IsTrue(ex.IsRetryable);
            }
        }

        [TestMethod]
        public async Task SystemAssignedManagedIdentityApiIdTestAsync()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                //Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Arrange
                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

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
            CryptoKeyType cryptoKeyType = CryptoKeyType.Machine;

            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(TestConstants.ClientId, UserAssignedIdentityId.ClientId);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), cryptoKeyType);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                miBuilder.WithHttpManager(httpManager);

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse(),
                    userAssignedId: TestConstants.ClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                // Act
                // We should get the auth result from the token provider
                var builder = mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource);
                var result = await builder.ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(miBuilder.Config.ClientId, TestConstants.ClientId);
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                Assert.AreEqual(ApiEvent.ApiIds.AcquireTokenForUserAssignedManagedIdentity, builder.CommonParameters.ApiId);
            }
        }

        [TestMethod]
        public async Task ManagedIdentityCacheTestAsync()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(TestConstants.ClientId, UserAssignedIdentityId.ClientId);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Machine);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                miBuilder.WithHttpManager(httpManager);

                ManagedIdentityApplication mi = miBuilder.BuildConcrete();

                CancellationTokenSource cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;

                var appTokenCacheRecoder = mi.AppTokenCacheInternal.RecordAccess((args) =>
                {
                    Assert.AreEqual(Constants.ManagedIdentityDefaultTenant, args.RequestTenantId);
                    Assert.AreEqual(Constants.CredentialIdentityDefaultClientId, args.ClientId);
                    Assert.IsNull(args.Account);
                    Assert.IsTrue(args.IsApplicationCache);
                    Assert.AreEqual(cancellationToken, args.CancellationToken);
                });

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse(), 
                    userAssignedId: TestConstants.ClientId, 
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                var result = await mi.AcquireTokenForManagedIdentity(Resource).ExecuteAsync(cancellationToken).ConfigureAwait(false);

                appTokenCacheRecoder.AssertAccessCounts(1, 1);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MsalManagedIdentityException))]
        public async Task ManagedIdentityNoClientIdInCredentialResponseThrowsAsync()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(TestConstants.ClientId, UserAssignedIdentityId.ClientId);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Machine);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                miBuilder.WithHttpManager(httpManager);

                ManagedIdentityApplication mi = miBuilder.BuildConcrete();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse(client_id: null),
                    userAssignedId: TestConstants.ClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                AcquireTokenForManagedIdentityParameterBuilder builder = mi.AcquireTokenForManagedIdentity(Resource);

                AuthenticationResult result = await builder.ExecuteAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MsalManagedIdentityException))]
        public async Task ManagedIdentityNoTokenUrlThrowsAsync()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(TestConstants.ClientId, UserAssignedIdentityId.ClientId);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Machine);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                miBuilder.WithHttpManager(httpManager);

                ManagedIdentityApplication mi = miBuilder.BuildConcrete();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse(regional_token_url: null),
                    userAssignedId: TestConstants.ClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                AcquireTokenForManagedIdentityParameterBuilder builder = mi.AcquireTokenForManagedIdentity(Resource);

                AuthenticationResult result = await builder.ExecuteAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MsalManagedIdentityException))]
        public async Task ManagedIdentityNoTenantIdThrowsAsync()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(TestConstants.ClientId, UserAssignedIdentityId.ClientId);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Machine);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                miBuilder.WithHttpManager(httpManager);

                ManagedIdentityApplication mi = miBuilder.BuildConcrete();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse(tenant_id: null),
                    userAssignedId: TestConstants.ClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                AcquireTokenForManagedIdentityParameterBuilder builder = mi.AcquireTokenForManagedIdentity(Resource);

                AuthenticationResult result = await builder.ExecuteAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MsalManagedIdentityException))]
        public async Task ManagedIdentityNoCredentialInResponseThrowsAsync()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(TestConstants.ClientId, UserAssignedIdentityId.ClientId);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Machine);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                miBuilder.WithHttpManager(httpManager);

                ManagedIdentityApplication mi = miBuilder.BuildConcrete();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse(credential: null),
                    userAssignedId: TestConstants.ClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                AcquireTokenForManagedIdentityParameterBuilder builder = mi.AcquireTokenForManagedIdentity(Resource);

                AuthenticationResult result = await builder.ExecuteAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task ManagedIdentityIsProactivelyRefreshedAsync()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = CreateMIABuilder(TestConstants.ClientId, UserAssignedIdentityId.ClientId);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Machine);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                miBuilder.WithHttpManager(httpManager);

                ManagedIdentityApplication mi = miBuilder.BuildConcrete();

                Trace.WriteLine("1. Setup an app with a token cache with one AT");

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse(),
                    userAssignedId: TestConstants.ClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Trace.WriteLine("2. Configure AT so that it shows it needs to be refreshed");
                var refreshOn = Common.TestCommon.UpdateATWithRefreshOn(mi.AppTokenCacheInternal.Accessor).RefreshOn;
                TokenCacheAccessRecorder cacheAccess = mi.AppTokenCacheInternal.RecordAccess();

                Trace.WriteLine("3. Configure MSI to respond with a valid token");

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse(),
                    userAssignedId: TestConstants.ClientId,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId);

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                // Act
                Trace.WriteLine("4. ATM - should perform an RT refresh");
                result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Common.TestCommon.YieldTillSatisfied(() => httpManager.QueueSize == 0);

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
        public async Task ParallelRequests_CallTokenEndpointOnceAsync()
        {
            int numOfTasks = 10;
            int identityProviderHits = 0;
            int cacheHits = 0;

            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                // Arrange
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.Ephemeral);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Arrange
                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    ManagedIdentityTests.Resource,
                    isCredentialEndpoint: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                Task[] tasks = new Task[numOfTasks];
                for (int i = 0; i < numOfTasks; i++)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        AuthenticationResult authResult = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
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
    }
}
