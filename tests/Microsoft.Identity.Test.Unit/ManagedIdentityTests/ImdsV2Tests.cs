// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.KeyAttestation;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.Identity.Test.Unit.PublicApiTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsV2Tests : TestBase
    {
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();
        private readonly TestCsrFactory _testCsrFactory = new TestCsrFactory();
        private readonly IdentityLoggerAdapter _identityLoggerAdapter = new IdentityLoggerAdapter(
            new TestIdentityLogger(),
            Guid.Empty,
            "TestClient",
            "1.0.0",
            enablePiiLogging: false
        );

        public const string Bearer = "Bearer";
        public const string MTLSPoP = "mtls_pop";

        [TestInitialize]
        public void ImdsV2Tests_Init()
        {
            // Clean persisted store so prior DataRows/runs don't leak into this test
            if (ImdsV2TestStoreCleaner.IsWindows)
            {
                // A broad sweep is simplest and safe for our fake endpoints/certs
                ImdsV2TestStoreCleaner.RemoveAllTestArtifacts();
            }
        }

        [TestCleanup]
        public void ImdsV2Tests_Cleanup()
        {
            // Cleanup handled automatically with delegate-based approach
        }

        private void AddMocksToGetEntraToken(
            MockHttpManager httpManager,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null,
            string certificateRequestCertificate = TestConstants.ValidRawCertificate)
        {
            if (userAssignedIdentityId != UserAssignedIdentityId.None && userAssignedId != null)
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(userAssignedIdentityId: userAssignedIdentityId, userAssignedId: userAssignedId));
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse(userAssignedIdentityId, userAssignedId, certificateRequestCertificate));
            }
            else
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse(certificate: certificateRequestCertificate));
            }

            httpManager.AddMockHandler(MockHelpers.MockImdsV2EntraTokenRequestResponse(_identityLoggerAdapter));
        }

        private async Task<IManagedIdentityApplication> CreateManagedIdentityAsync(
            MockHttpManager httpManager,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null,
            bool addProbeMock = true,
            bool addSourceCheck = true,
            ManagedIdentityKeyType managedIdentityKeyType = ManagedIdentityKeyType.InMemory,
            ImdsVersion imdsVersion = ImdsVersion.V2)
        {
            ManagedIdentityApplicationBuilder miBuilder = null;

            var uami = userAssignedIdentityId != UserAssignedIdentityId.None && userAssignedId != null;
            if (uami)
            {
                miBuilder = CreateMIABuilder(userAssignedId, userAssignedIdentityId);
            }
            else
            {
                miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned);
            }

            miBuilder
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);

            if (imdsVersion == ImdsVersion.V2)
            {
                miBuilder.WithCsrFactory(_testCsrFactory);
            }

            var managedIdentityApp = miBuilder.Build();

            if (imdsVersion == ImdsVersion.V1)
            {
                if (addProbeMock)
                {
                    httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2, userAssignedIdentityId, userAssignedId));
                    httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V1, userAssignedIdentityId, userAssignedId));
                }

                return managedIdentityApp;
            }

            if (addProbeMock)
            {
                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V2, userAssignedIdentityId, userAssignedId));
            }

            if (addSourceCheck)
            {
                var miSourceResult = await (managedIdentityApp as ManagedIdentityApplication)
                    .GetManagedIdentitySourceAsync(probe: true, cancellationToken: ManagedIdentityTests.ImdsProbesCancellationToken)
                    .ConfigureAwait(false);

                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSourceResult.Source);
            }

            // Choose deterministic key source for tests.
            IManagedIdentityKeyProvider managedIdentityKeyProvider = null;
            if (managedIdentityKeyType == ManagedIdentityKeyType.KeyGuard)
            {
                // Force KeyGuard keys to deterministically exercise the attestation path.
                managedIdentityKeyProvider = new TestKeyGuardManagedIdentityKeyProvider();
            }
            else if (managedIdentityKeyType == ManagedIdentityKeyType.InMemory)
            {
                // Otherwise, no attestation.
                managedIdentityKeyProvider = new InMemoryManagedIdentityKeyProvider();
            }

            // Inject a test platform proxy that provides the chosen key provider
            if (managedIdentityKeyProvider != null)
            {
                var platformProxy = Substitute.For<IPlatformProxy>();
                platformProxy.ManagedIdentityKeyProvider.Returns(managedIdentityKeyProvider);

                (managedIdentityApp as ManagedIdentityApplication)
                    .ServiceBundle.SetPlatformProxyForTest(platformProxy);
            }

            return managedIdentityApp;
        }

        #region Acceptance Tests
        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null)]                             // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]       // UAMI
        public async Task mTLSPopTokenHappyPath(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId);

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                Assert.IsNotNull(result.BindingCertificate);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                Assert.IsNotNull(result.BindingCertificate);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId, $"{TestConstants.ClientId}-2")]
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId, $"{TestConstants.MiResourceId}-2")]
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId, $"{TestConstants.ObjectId}-2")]
        public async Task mTLSPopTokenIsPerIdentity(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId,
            string userAssignedId2)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                #region Identity 1
                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId);

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                Assert.IsNotNull(result.BindingCertificate);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                Assert.IsNotNull(result.BindingCertificate);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                #endregion Identity 1

                #region Identity 2
                UserAssignedIdentityId userAssignedIdentityId2 = userAssignedIdentityId; // keep the same type, that's the most common scenario
                var managedIdentityApp2 = await CreateManagedIdentityAsync(
                    httpManager,
                    userAssignedIdentityId2,
                    userAssignedId2,
                    addProbeMock: false,
                    addSourceCheck: false,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false); // source is already cached

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId2, userAssignedId2);

                var result2 = await managedIdentityApp2.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(result2.TokenType, MTLSPoP);
                Assert.IsNotNull(result2.BindingCertificate);
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);

                result2 = await managedIdentityApp2.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(result2.TokenType, MTLSPoP);
                Assert.IsNotNull(result2.BindingCertificate);
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
                #endregion Identity 2

                // TODO: Assert.AreEqual(CertificateCache.Count, 2);
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null)]                             // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]       // UAMI
        public async Task mTLSPopTokenIsReAcquiredWhenCertificateIsExpired(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId, TestConstants.ExpiredRawCertificate);

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                Assert.IsNotNull(result.BindingCertificate);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                /**
                // TODO: Add functionality to check cert expiration in the cache
                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId);

                result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                Assert.IsNotNull(result.BindingCertificate);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                */
            }
        }
        #endregion Acceptance Tests

        #region Failure Tests
        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null)]                             // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]       // UAMI
        public async Task ImdsV2EndpointsAreNotAvailableButMtlsPopTokenWasRequested(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ManagedIdentityClient.ResetSourceForTest();

                SetEnvironmentVariables(ManagedIdentitySource.Imds, TestConstants.ImdsEndpoint);

                var managedIdentityApp = await CreateManagedIdentityAsync(
                    httpManager,
                    userAssignedIdentityId,
                    userAssignedId,
                    addProbeMock: false,
                    addSourceCheck: false,
                    imdsVersion: ImdsVersion.V1)
                    .ConfigureAwait(false);

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false)
                ).ConfigureAwait(false);

                Assert.AreEqual(MsalError.ManagedIdentityAllSourcesUnavailable, ex.ErrorCode);
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null)]                             // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]       // UAMI
        public async Task ApplicationsCannotSwitchBetweenImdsVersionsForPreview(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                // IMDSv1 request mock
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    userAssignedId: userAssignedId,
                    userAssignedIdentityId: userAssignedIdentityId);

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    //.WithMtlsProofOfPossession() - excluding this will cause fallback to ImdsV1
                    //.WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(result.TokenType, Bearer);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // even though the app fell back to ImdsV1, the source should still be ImdsV2
                var miSourceResult = await (managedIdentityApp as ManagedIdentityApplication)
                    .GetManagedIdentitySourceAsync(probe: true, cancellationToken: ManagedIdentityTests.ImdsProbesCancellationToken)
                    .ConfigureAwait(false);

                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSourceResult.Source);

                // none of the mocks from AddMocksToGetEntraToken are needed since checking the cache occurs before the network requests
                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession() // this will cause an error to be thrown since the app already fell back to ImdsV1
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false)
                ).ConfigureAwait(false);

                Assert.AreEqual(MsalError.CannotSwitchBetweenImdsVersionsForPreview, ex.ErrorCode);
            }
        }
        #endregion Failure Tests

        #region Probe Tests
        [TestMethod]
        public async Task ProbeImdsEndpointAsyncSucceeds()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V2));

                await CreateManagedIdentityAsync(httpManager, addProbeMock: false).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task ProbeImdsEndpointAsyncSucceedsAfterRetry()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // `retry: true` indicates a retriable status code will be returned
                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2, retry: true));

                // Second attempt succeeds (defined inside of CreateManagedIdentityAsync)
                await CreateManagedIdentityAsync(httpManager).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task ProbeImdsEndpointAsyncFails404WhichIsNonRetriableAndRetryPolicyIsNotTriggeredAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // `retry: false` indicates a retriable status code will be returned
                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2, retry: false));
                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V1));

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, addProbeMock: false, addSourceCheck: false).ConfigureAwait(false);

                var miSourceResult = await (managedIdentityApp as ManagedIdentityApplication)
                    .GetManagedIdentitySourceAsync(probe: true, cancellationToken: ManagedIdentityTests.ImdsProbesCancellationToken)
                    .ConfigureAwait(false);

                Assert.AreEqual(ManagedIdentitySource.Imds, miSourceResult.Source);
            }
        }

        [TestMethod]
        public async Task ImdsProbeEndpointAsync_TimeOutThrowsOperationCanceledException()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned);

                miBuilder
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                var managedIdentityApp = miBuilder.Build();

                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V2));

                var cts = new CancellationTokenSource();
                cts.Cancel();
                var imdsProbesCancellationToken = cts.Token;

                await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
                    await (managedIdentityApp as ManagedIdentityApplication)
                    .GetManagedIdentitySourceAsync(probe: true, cancellationToken: imdsProbesCancellationToken)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            }
        }
        #endregion Probe Tests

        #region Fallback Behavior Tests
        // Verifies non-mTLS request after IMDSv2 detection falls back per-request to IMDSv1 (Bearer),
        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null)]
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]
        public async Task NonMtlsRequest_FallsBackToImdsV1(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ManagedIdentityClient.ResetSourceForTest();
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                // IMDSv1 request mock
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    userAssignedIdentityId: userAssignedIdentityId,
                    userAssignedId: userAssignedId);

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    //.WithMtlsProofOfPossession() - excluding this will cause fallback to ImdsV1
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(Bearer, result.TokenType);
                Assert.IsNull(result.BindingCertificate, "Bearer token should not have binding certificate.");
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // indicates ImdsV2 is still available
                var miSourceResult = await (managedIdentityApp as ManagedIdentityApplication)
                    .GetManagedIdentitySourceAsync(probe: true, cancellationToken: ManagedIdentityTests.ImdsProbesCancellationToken)
                    .ConfigureAwait(false);

                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSourceResult.Source);
            }
        }

        [TestMethod]
        public async Task ImdsV2ProbeFailsMaxRetries_FallsBackToImdsV1()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                const int Num500Errors = 1 + TestImdsProbeRetryPolicy.ExponentialStrategyNumRetries;
                for (int i = 0; i < Num500Errors; i++)
                {
                    // `retry: true` indicates a retriable status code will be returned
                    httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2, retry: true));
                }

                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V1));

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, addProbeMock: false, addSourceCheck: false)
                    .ConfigureAwait(false);

                var miSourceResult = await (managedIdentityApp as ManagedIdentityApplication)
                    .GetManagedIdentitySourceAsync(probe: true, cancellationToken: ManagedIdentityTests.ImdsProbesCancellationToken)
                    .ConfigureAwait(false);

                Assert.AreEqual(ManagedIdentitySource.Imds, miSourceResult.Source);
            }
        }

        // New test: PoP first-call on VM succeeds without any explicit pre-probe
        [TestMethod]
        public async Task MtlsPop_FirstCall_NoExplicitSourceCheck_ProbesAndSucceeds()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ManagedIdentityClient.ResetSourceForTest();

                // Mimic VM: no non-IMDS env-based source.
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // IMPORTANT: do NOT pre-warm by calling GetManagedIdentitySourceAsync(probe:true)
                var mi = await CreateManagedIdentityAsync(
                    httpManager,
                    addProbeMock: true,
                    addSourceCheck: false,                  // <- key difference
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard,
                    imdsVersion: ImdsVersion.V2)
                    .ConfigureAwait(false);

                // After the probe, token flow should proceed normally
                AddMocksToGetEntraToken(httpManager);

                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(MTLSPoP, result.TokenType);
                Assert.IsNotNull(result.BindingCertificate);
            }
        }

        // New test: PoP first-call on VM fails when both IMDS probes fail
        [TestMethod]
        public async Task MtlsPop_FirstCall_BothImdsProbesFail_ThrowsAllSourcesUnavailable()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ManagedIdentityClient.ResetSourceForTest();
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var mi = await CreateManagedIdentityAsync(
                    httpManager,
                    addProbeMock: false,
                    addSourceCheck: false,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard,
                    imdsVersion: ImdsVersion.V2)
                    .ConfigureAwait(false);

                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2));
                httpManager.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V1));

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .WithMtlsProofOfPossession()
                        .ExecuteAsync()
                        .ConfigureAwait(false))
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.ManagedIdentityAllSourcesUnavailable, ex.ErrorCode);
            }
        }

        #endregion

        #region CSR Metadata Tests
        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsWithMissingServerHeader()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager).ConfigureAwait(false);

                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: null));

                var ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync().ConfigureAwait(false)
                ).ConfigureAwait(false);

                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsWithInvalidFormat()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager).ConfigureAwait(false);

                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: "I_MDS/150.870.65.1854"));

                var ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync().ConfigureAwait(false)
                ).ConfigureAwait(false);

                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
            }
        }
        #endregion CSR Metadata Tests

        #region CSR Generation Tests
        [TestMethod]
        public void TestCsrGeneration_OnlyVmId()
        {
            var cuid = new CuidInfo
            {
                VmId = TestConstants.VmId
            };

            var rsa = InMemoryManagedIdentityKeyProvider.CreateRsaKeyPair();
            var (csr, _) = Csr.Generate(rsa, TestConstants.ClientId, TestConstants.TenantId, cuid);
            CsrValidator.ValidateCsrContent(csr, TestConstants.ClientId, TestConstants.TenantId, cuid);
        }

        [TestMethod]
        public void TestCsrGeneration_VmIdAndVmssId()
        {
            var cuid = new CuidInfo
            {
                VmId = TestConstants.VmId,
                VmssId = TestConstants.VmssId
            };

            var rsa = InMemoryManagedIdentityKeyProvider.CreateRsaKeyPair();
            var (csr, _) = Csr.Generate(rsa, TestConstants.ClientId, TestConstants.TenantId, cuid);
            CsrValidator.ValidateCsrContent(csr, TestConstants.ClientId, TestConstants.TenantId, cuid);
        }

        [DataTestMethod]
        [DataRow("Invalid@#$%Certificate!")]
        [DataRow("")]
        [DataRow(null)]
        public void TestCsrGeneration_BadCert_ThrowsMsalServiceException(string badCert)
        {
            Assert.ThrowsException<MsalServiceException>(() =>
                CsrValidator.ParseRawCsr(badCert));
        }
        #endregion CSR Generation Tests

        #region AttachPrivateKeyToCert Tests
        [TestMethod]
        public void AttachPrivateKeyToCert_ValidInputs_ReturnsValidCertificate()
        {
            using (RSA rsa = RSA.Create())
            {
                X509Certificate2 certificate = CommonCryptographyManager.AttachPrivateKeyToCert(TestConstants.ValidRawCertificate, TestCsrFactory.CreateMockRsa());
                Assert.IsNotNull(certificate);
            }
        }

        [DataTestMethod]
        [DataRow("Invalid@#$%Certificate!")]
        [DataRow("")]
        [DataRow(null)]
        public void AttachPrivateKeyToCert_BadContent_ThrowsMsalServiceException(string badCert)
        {
            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<MsalServiceException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(badCert, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_NullPrivateKey_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                CommonCryptographyManager.AttachPrivateKeyToCert(TestConstants.ValidRawCertificate, null));
        }
        #endregion

        #region Attestation Tests
        [TestMethod]
        public async Task MtlsPop_NoAttestationProvider_UsesNonAttestedFlow()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var mi = await CreateManagedIdentityAsync(httpManager, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                // Add mocks for successful non-attested flow (CSR + issuecredential + token)
                // Note: No attestation token in the certificate request
                AddMocksToGetEntraToken(httpManager);

                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    // Intentionally DO NOT call .WithAttestationProviderForTests(...)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(MTLSPoP, result.TokenType, "Should get mTLS PoP token without attestation provider");
                Assert.IsNotNull(result.BindingCertificate, "Should have binding certificate even without attestation");
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task MtlsPop_AttestationProviderReturnsNull_UsesNonAttestedFlow()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var mi = await CreateManagedIdentityAsync(httpManager, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                // Add mocks for successful non-attested flow
                AddMocksToGetEntraToken(httpManager);

                // Test with null-returning attestation provider - should gracefully use non-attested flow
                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(TestAttestationProviders.CreateNullProvider())
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(MTLSPoP, result.TokenType, "Should get mTLS PoP token even with null attestation provider");
                Assert.IsNotNull(result.BindingCertificate);
            }
        }

        [TestMethod]
        public async Task MtlsPop_AttestationProviderReturnsEmptyToken_UsesNonAttestedFlow()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var mi = await CreateManagedIdentityAsync(httpManager, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                // Add mocks for successful non-attested flow
                AddMocksToGetEntraToken(httpManager);

                // Test with empty-string-returning attestation provider - should gracefully use non-attested flow
                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(TestAttestationProviders.CreateEmptyProvider())
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(MTLSPoP, result.TokenType, "Should get mTLS PoP token even with empty attestation provider");
                Assert.IsNotNull(result.BindingCertificate);
            }
        }

        [TestMethod]
        public async Task mTLSPop_RequestedWithoutKeyGuard_ThrowsClientException()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // Force in-memory keys (i.e., not KeyGuard)
                var managedIdentityApp = await CreateManagedIdentityAsync(
                    httpManager,
                    managedIdentityKeyType: ManagedIdentityKeyType.InMemory
                ).ConfigureAwait(false);

                // CreateManagedIdentityAsync does a probe; Add one more CSR response for the actual acquire.
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .WithMtlsProofOfPossession() // request PoP on a non-KeyGuard env
                        .ExecuteAsync().ConfigureAwait(false)
                ).ConfigureAwait(false);

                Assert.AreEqual("mtls_pop_requires_keyguard", ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task mTLSPop_AttestationSupport_IsRequired_ToReusePersistedCert()
        {
            if (!ImdsV2TestStoreCleaner.IsWindows)
            {
                Assert.Inconclusive("Windows-only: relies on CurrentUser/My persisted cert cache semantics.");
            }

            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // Force KeyGuard provider so attestation support is relevant.
                var mi = await CreateManagedIdentityAsync(
                    httpManager,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard
                ).ConfigureAwait(false);

                // (1) First acquire: WITH attestation support (mint cert + token)
                AddMocksToGetEntraToken(httpManager);

                var first = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(first);
                Assert.AreEqual(MTLSPoP, first.TokenType);
                Assert.IsNotNull(first.BindingCertificate);
                Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);

                // (2) Second acquire: NO attestation support + ForceRefresh
                MockHelpers.AddMocks_AttestedCertMustNotBeReused_ExpectIssueCredential400(httpManager);

                var ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .WithForceRefresh(true)
                        .WithMtlsProofOfPossession()
                        // NOTE: intentionally NOT calling .WithAttestationSupport()
                        // Earlier cert was attested, so this call should fail.
                        .ExecuteAsync()
                        .ConfigureAwait(false)
                ).ConfigureAwait(false);

                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                StringAssert.Contains(ex.Message, "Attestation Token is missing");
            }
        }

        [TestMethod]
        public async Task mTLSPop_AttestationSupport_AffectsTokenCacheKey_NonAttestedRequestDoesNotReuseAttestedTokenFromCache()
        {
            if (!ImdsV2TestStoreCleaner.IsWindows)
            {
                Assert.Inconclusive("Windows-only: relies on CurrentUser/My persisted cert cache semantics.");
            }

            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // Force KeyGuard provider so attestation support is relevant.
                var mi = await CreateManagedIdentityAsync(
                    httpManager,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard
                ).ConfigureAwait(false);

                // (1) First acquire: WITH attestation support (mint cert + token)
                AddMocksToGetEntraToken(httpManager);

                var first = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(first);
                Assert.AreEqual(MTLSPoP, first.TokenType);
                Assert.IsNotNull(first.BindingCertificate);
                Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);

                // (2) Second acquire: NO attestation support + ForceRefresh
                MockHelpers.AddMocks_AttestedCertMustNotBeReused_ExpectIssueCredential400(httpManager);

                var ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .WithMtlsProofOfPossession()
                        // NOTE: intentionally NOT calling .WithAttestationSupport()
                        // Earlier cert was attested, so this call should fail.
                        .ExecuteAsync()
                        .ConfigureAwait(false)
                ).ConfigureAwait(false);

                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                StringAssert.Contains(ex.Message, "Attestation Token is missing");
            }
        }

        #endregion

        #region Cached certificate tests
        [TestMethod]
        public async Task mTLSPop_ForceRefresh_UsesCachedCert_NoIssueCredential_PostsCanonicalClientId_AndSkipsAttestation()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                // Start clean across tests
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                var mi = await CreateManagedIdentityAsync(httpManager, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                // First acquire: full flow (CSR + issuecredential + token)
                AddMocksToGetEntraToken(httpManager);

                // Use counting provider for this test
                var countingProvider = TestAttestationProviders.CreateCountingProvider();

                var result1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(countingProvider.GetDelegate())
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(ImdsV2Tests.MTLSPoP, result1.TokenType);
                Assert.IsNotNull(result1.BindingCertificate);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, countingProvider.CallCount, "Attestation must be called exactly once on first mint.");

                // Second acquire: FORCE REFRESH to bypass token cache.
                // Expect: 1x getplatformmetadata + token request. NO /issuecredential. Attestation NOT called again.
                MockHelpers.AddMocksToGetEntraTokenUsingCachedCert(
                    httpManager,
                    _identityLoggerAdapter,
                    mTLSPop: true,
                    assertClientId: true,                 // assert canonical client_id is posted
                    expectedClientId: TestConstants.ClientId);

                var result2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithForceRefresh(true)                // if your API is parameterless, use .WithForceRefresh()
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(countingProvider.GetDelegate())
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(ImdsV2Tests.MTLSPoP, result2.TokenType);
                Assert.IsNotNull(result2.BindingCertificate);
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, countingProvider.CallCount, "Attestation must NOT be invoked on refresh when cert is cached.");
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId, TestConstants.ClientId + "-2")]
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId, TestConstants.MiResourceId + "-2")]
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId, TestConstants.ObjectId + "-2")]
        public async Task mTLSPop_CachedCertIsPerIdentity_OnRefresh_Identity1UsesCache_Identity2Mints(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId1,
            string userAssignedId2)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // Identity 1 – first acquire (mint)
                var mi1 = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId1, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);
                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId1);

                var result1 = await mi1.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Identity 1 – force refresh (should use cached cert ? NO /issuecredential)
                MockHelpers.AddMocksToGetEntraTokenUsingCachedCert(
                    httpManager,
                    _identityLoggerAdapter,
                    mTLSPop: true,
                    assertClientId: true,
                    expectedClientId: TestConstants.ClientId,
                    userAssignedIdentityId: userAssignedIdentityId,
                    userAssignedId: userAssignedId1
                );

                var result1Refresh = await mi1.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithForceRefresh(true)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result1Refresh.AuthenticationResultMetadata.TokenSource);

                // Identity 2 – new identity (should MINT again ? requires /issuecredential)
                var mi2 = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId2, addProbeMock: false, addSourceCheck: false, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);
                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId2);

                var result2 = await mi2.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
            }
        }
        #endregion

        #region Cert cache tests

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null,                    /*isUami*/ false)] // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId,  /*isUami*/ true)]  // UAMI by client_id
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId, /*isUami*/ true)] // UAMI by resource_id
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId,  /*isUami*/ true)]  // UAMI by object_id
        public async Task mTLSPopTokenHappyPath_LongLivedCert_IdentityMapping(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId,
            bool isUami)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // Force KeyGuard so the PoP path is taken
                var managedIdentityApp = await CreateManagedIdentityAsync(
                    httpManager,
                    userAssignedIdentityId,
                    userAssignedId,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard
                ).ConfigureAwait(false);

                // --- First acquire: MINT (CSR + issuecredential + token) with a long-lived cert ---
                // Use the known-good cert that matches TestCsrFactory's RSA and already has a far NotAfter (>= 20 years)
                AddMocksToGetEntraToken(
                    httpManager,
                    userAssignedIdentityId,
                    userAssignedId,
                    certificateRequestCertificate: TestConstants.ValidRawCertificate);

                var first = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(first);
                Assert.AreEqual(MTLSPoP, first.TokenType, "Token type must be mtls_pop");
                Assert.IsNotNull(first.BindingCertificate, "Binding certificate should be present on mTLS PoP tokens");
                Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);

                Assert.IsTrue(first.BindingCertificate.NotAfter.ToUniversalTime() >= DateTime.UtcNow.AddYears(20).AddDays(-1),
                    $"Binding cert NotAfter {first.BindingCertificate.NotAfter:u} should be >= ~20 years from now.");

                var second = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(second);
                Assert.AreEqual(MTLSPoP, second.TokenType);
                Assert.IsNotNull(second.BindingCertificate, "Binding certificate should be present on cached mTLS PoP tokens");
                Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);

                // Optional: Same thumbprint between the two (same cached binding cert)
                Assert.AreEqual(first.BindingCertificate.Thumbprint, second.BindingCertificate.Thumbprint,
                    "Cached mTLS flow should reuse the same binding certificate.");

                // Your existing CN assertion against the baked-in TestConstants.ValidRawCertificate
                AssertCertCN(first.BindingCertificate, "Test");
                AssertCertCN(second.BindingCertificate, "Test");
            }
        }

        /// <summary>
        /// Create TWO long-lived (20y) raw DER (base64) certs with the CSR key:
        ///   - One for SAMI (CN=SAMI-20Y)
        ///   - One for UAMI (CN=UAMI-20Y)
        /// Then run mint + cached flows and assert thumbprints.
        /// </summary>
        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null,                      /*aliasLabel*/ "SAMI")] // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId,    /*aliasLabel*/ "UAMI-ClientId")]
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId,/*aliasLabel*/ "UAMI-ResourceId")]
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId,    /*aliasLabel*/ "UAMI-ObjectId")]
        public async Task mTLSPop_LongLivedCerts_SamiVsUami_DistinctAndCached(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId,
            string aliasLabel)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // Create the two test certs (20-year) from the SAME RSA as CSR (XmlPrivateKey)
                string rawCertSami = CreateRawCertFromXml("CN=SAMI-20Y", notAfterUtc: DateTimeOffset.UtcNow.AddYears(20));
                string rawCertUami = CreateRawCertFromXml("CN=UAMI-20Y", notAfterUtc: DateTimeOffset.UtcNow.AddYears(20));

                // Build an MI app for the row's identity kind (force KeyGuard so mTLS path is used)
                var mi = await CreateManagedIdentityAsync(
                    httpManager,
                    userAssignedIdentityId,
                    userAssignedId,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                // --- First acquire (MINT): return the identity-specific cert we want ---
                // SAMI ? use rawCertSami ; UAMI (any alias) ? use rawCertUami
                string selectedCert = (userAssignedIdentityId == UserAssignedIdentityId.None) ? rawCertSami : rawCertUami;

                AddMocksToGetEntraToken(
                    httpManager,
                    userAssignedIdentityId,
                    userAssignedId,
                    certificateRequestCertificate: selectedCert);

                var first = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(first);
                Assert.AreEqual(MTLSPoP, first.TokenType, $"[{aliasLabel}] token type must be mtls_pop");
                Assert.IsNotNull(first.BindingCertificate, $"[{aliasLabel}] binding cert missing");
                Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource, $"[{aliasLabel}] first acquire must mint from IDP");
                Assert.IsTrue(first.BindingCertificate.NotAfter.ToUniversalTime() >= DateTime.UtcNow.AddYears(20).AddDays(-1),
                    $"[{aliasLabel}] NotAfter {first.BindingCertificate.NotAfter:u} should be ~20y+");

                var thumb1 = first.BindingCertificate.Thumbprint;

                // --- Second acquire: cached; cert should be the SAME (cached binding cert) ---
                var second = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(second);
                Assert.AreEqual(MTLSPoP, second.TokenType, $"[{aliasLabel}] cached token type");
                Assert.IsNotNull(second.BindingCertificate, $"[{aliasLabel}] cached binding cert missing");
                Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource, $"[{aliasLabel}] second acquire should be from cache");
                Assert.AreEqual(thumb1, second.BindingCertificate.Thumbprint, $"[{aliasLabel}] cached must reuse same binding cert");

                var expectedCn = (userAssignedIdentityId == UserAssignedIdentityId.None) ? "SAMI-20Y" : "UAMI-20Y";
                AssertCertCN(first.BindingCertificate, expectedCn);
                AssertCertCN(second.BindingCertificate, expectedCn);
            }
        }

        /// <summary>
        /// End-to-end: mint SAMI & UAMI in one test and prove their binding certs differ,
        /// while each identity reuses its own binding cert from cache.
        /// </summary>
        [TestMethod]
        public async Task mTLSPop_LongLivedCerts_SamiAndUami_ThumbprintsDiffer_AndEachCaches()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // Make two long-lived certs **from the CSR key** so AttachPrivateKey succeeds
                string rawCertSami = CreateRawCertForCsrKey("CN=SAMI-20Y", DateTimeOffset.UtcNow.AddYears(20));
                string rawCertUami = CreateRawCertForCsrKey("CN=UAMI-20Y", DateTimeOffset.UtcNow.AddYears(20));

                // ---------- SAMI ----------
                var sami = await CreateManagedIdentityAsync(
                    httpManager,
                    userAssignedIdentityId: UserAssignedIdentityId.None,
                    userAssignedId: null,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard
                ).ConfigureAwait(false);

                AddMocksToGetEntraToken(
                    httpManager,
                    userAssignedIdentityId: UserAssignedIdentityId.None,
                    userAssignedId: null,
                    certificateRequestCertificate: rawCertSami);

                var s1 = await sami.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(s1.BindingCertificate);
                AssertCertCN(s1.BindingCertificate, "SAMI-20Y");

                var samiThumb = s1.BindingCertificate.Thumbprint;

                // cached
                var s2 = await sami.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, s2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(samiThumb, s2.BindingCertificate.Thumbprint, "SAMI must reuse cached binding cert");

                // ---------- UAMI (client_id) ----------
                var uami = await CreateManagedIdentityAsync(
                    httpManager,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId,
                    userAssignedId: TestConstants.ClientId,
                    addProbeMock: false,
                    addSourceCheck: false,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard
                ).ConfigureAwait(false);

                AddMocksToGetEntraToken(
                    httpManager,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId,
                    userAssignedId: TestConstants.ClientId,
                    certificateRequestCertificate: rawCertUami);

                var u1 = await uami.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(u1.BindingCertificate);
                AssertCertCN(u1.BindingCertificate, "UAMI-20Y");

                var uamiThumb = u1.BindingCertificate.Thumbprint;

                var u2 = await uami.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, u2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(uamiThumb, u2.BindingCertificate.Thumbprint, "UAMI must reuse cached binding cert");

                // Cross-identity certs must differ
                Assert.AreNotEqual(samiThumb, uamiThumb, "SAMI and UAMI must use different binding certs");
            }
        }

        /// <summary>
        /// Subject mapping test that mirrors prod: CN=canonical client_id, DC=tenant id.
        /// - SAMI ? CN = Constants.ManagedIdentityDefaultClientId
        /// - UAMI (client_id|object_id|resource_id) ? CN = TestConstants.ClientId (canonical)
        /// Both assert DC = TestConstants.TenantId and cert cache reuse.
        /// </summary>
        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null,                    /*label*/ "SAMI", /*isUami*/ false)]
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId,  /*label*/ "UAMI-ClientId", /*isUami*/ true)]
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId,  /*label*/ "UAMI-ObjectId", /*isUami*/ true)]
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId,/*label*/"UAMI-ResourceId",/*isUami*/ true)]
        public async Task mTLSPop_SubjectCnDc_MatchesMetadata_AndCaches(
            UserAssignedIdentityId idKind,
            string idValue,
            string label,
            bool isUami)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // Expected mapping (mirrors your live logs)
                string expectedCn = isUami ? TestConstants.ClientId : Constants.ManagedIdentityDefaultClientId;
                string expectedDc = TestConstants.TenantId;

                // Mint a 20-year cert with Subject "CN=<expectedCn>, DC=<expectedDc>" using the CSR key
                string rawCert = CreateRawCertForCsrKeyWithCnDc(expectedCn, expectedDc, DateTimeOffset.UtcNow.AddYears(20));

                var mi = await CreateManagedIdentityAsync(httpManager, idKind, idValue, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard)
                    .ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, idKind, idValue, certificateRequestCertificate: rawCert);

                var first = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(MTLSPoP, first.TokenType, $"[{label}]");
                AssertCertSubjectCnDc(first.BindingCertificate, expectedCn, expectedDc, label);

                var second = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource, $"[{label}] cache");
                Assert.AreEqual(first.BindingCertificate.Thumbprint, second.BindingCertificate.Thumbprint, $"[{label}] thumbprint must be stable");
                AssertCertSubjectCnDc(second.BindingCertificate, expectedCn, expectedDc, label);
            }
        }

        [TestMethod]
        public async Task mTLSPoP_Uami_ClientIdThenObjectId_MintsThenCaches_SubjectCNIsClientId()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                string expectedCn = TestConstants.ClientId;
                string expectedDc = TestConstants.TenantId;
                string rawCert = CreateRawCertForCsrKeyWithCnDc(expectedCn, expectedDc, DateTimeOffset.UtcNow.AddYears(20));

                // (1) client_id ? MINT (CSR + issuecredential + token)
                var miClientId = await CreateManagedIdentityAsync(
                    httpManager,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId,
                    userAssignedId: TestConstants.ClientId,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                AddMocksToGetEntraToken(
                    httpManager,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId,
                    userAssignedId: TestConstants.ClientId,
                    certificateRequestCertificate: rawCert);

                var c1 = await miClientId.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(MTLSPoP, c1.TokenType);
                Assert.AreEqual(TokenSource.IdentityProvider, c1.AuthenticationResultMetadata.TokenSource);
                AssertCertSubjectCnDc(c1.BindingCertificate, expectedCn, expectedDc, "[client_id]");

                // (2) object_id ? MINT (new alias ? its own cache key)
                var miObjectId = await CreateManagedIdentityAsync(
                    httpManager,
                    userAssignedIdentityId: UserAssignedIdentityId.ObjectId,
                    userAssignedId: TestConstants.ObjectId,
                    addProbeMock: false,
                    addSourceCheck: false,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                AddMocksToGetEntraToken(
                    httpManager,
                    userAssignedIdentityId: UserAssignedIdentityId.ObjectId,
                    userAssignedId: TestConstants.ObjectId,
                    certificateRequestCertificate: rawCert);

                var o1 = await miObjectId.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(MTLSPoP, o1.TokenType);
                Assert.AreEqual(TokenSource.IdentityProvider, o1.AuthenticationResultMetadata.TokenSource);
                AssertCertSubjectCnDc(o1.BindingCertificate, expectedCn, expectedDc, "[object_id first]");
                var objectIdThumb = o1.BindingCertificate.Thumbprint;

                // (3) object_id again ? CACHED
                var o2 = await miObjectId.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, o2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(objectIdThumb, o2.BindingCertificate.Thumbprint);
                AssertCertSubjectCnDc(o2.BindingCertificate, expectedCn, expectedDc, "[object_id second]");
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId, "object_id")]
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId, "resource_id")]
        public async Task mTLSPoP_Uami_ClientIdThenAlias_MintsThenCaches_SubjectCNIsClientId(
            UserAssignedIdentityId aliasKind,
            string aliasValue,
            string label)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                string expectedCn = TestConstants.ClientId;
                string expectedDc = TestConstants.TenantId;
                string rawCert = CreateRawCertForCsrKeyWithCnDc(expectedCn, expectedDc, DateTimeOffset.UtcNow.AddYears(20));

                // (1) client_id ? MINT (CSR + issuecredential + token)
                var miClientId = await CreateManagedIdentityAsync(
                    httpManager,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId,
                    userAssignedId: TestConstants.ClientId,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                AddMocksToGetEntraToken(
                    httpManager,
                    userAssignedIdentityId: UserAssignedIdentityId.ClientId,
                    userAssignedId: TestConstants.ClientId,
                    certificateRequestCertificate: rawCert);

                var c1 = await miClientId.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(MTLSPoP, c1.TokenType, "[client_id]");
                Assert.AreEqual(TokenSource.IdentityProvider, c1.AuthenticationResultMetadata.TokenSource, "[client_id] should mint");
                AssertCertSubjectCnDc(c1.BindingCertificate, expectedCn, expectedDc, "[client_id]");

                // (2) alias (object_id/resource_id) ? MINT (new alias ? new cache key)
                var miAlias = await CreateManagedIdentityAsync(
                    httpManager,
                    userAssignedIdentityId: aliasKind,
                    userAssignedId: aliasValue,
                    addProbeMock: false,
                    addSourceCheck: false,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                AddMocksToGetEntraToken(
                    httpManager,
                    userAssignedIdentityId: aliasKind,
                    userAssignedId: aliasValue,
                    certificateRequestCertificate: rawCert);

                var a1 = await miAlias.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(MTLSPoP, a1.TokenType, $"[{label} first]");
                Assert.AreEqual(TokenSource.IdentityProvider, a1.AuthenticationResultMetadata.TokenSource, $"[{label} first] should mint");
                AssertCertSubjectCnDc(a1.BindingCertificate, expectedCn, expectedDc, $"[{label} first]");
                var aliasThumb = a1.BindingCertificate.Thumbprint;

                // (3) alias again ? CACHED (no /issuecredential; no extra mocks needed)
                var a2 = await miAlias.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, a2.AuthenticationResultMetadata.TokenSource, $"[{label} second] should be cached");
                Assert.AreEqual(aliasThumb, a2.BindingCertificate.Thumbprint, $"[{label}] cached binding cert must match");
                AssertCertSubjectCnDc(a2.BindingCertificate, expectedCn, expectedDc, $"[{label} second]");
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId, "UAMI-ClientId")]
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId, "UAMI-ObjectId")]
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId, "UAMI-ResourceId")]
        [DataRow(UserAssignedIdentityId.None, null, "SAMI")]
        public async Task mTLSPop_ShortLivedCert_LessThan24h_NotCached_ReMints(
            UserAssignedIdentityId idKind,
            string idValue,
            string label)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // short-lived cert #1: < 24h => must NOT be cached
                var rawShort1 = CreateRawCertForCsrKeyWithCnDc(
                    cn: (idKind == UserAssignedIdentityId.None ? Constants.ManagedIdentityDefaultClientId : TestConstants.ClientId),
                    dc: TestConstants.TenantId,
                    notAfterUtc: DateTimeOffset.UtcNow.AddHours(23));

                // short-lived cert #2: also < 24h (ensures new thumbprint on re-mint)
                var rawShort2 = CreateRawCertForCsrKeyWithCnDc(
                    cn: (idKind == UserAssignedIdentityId.None ? Constants.ManagedIdentityDefaultClientId : TestConstants.ClientId),
                    dc: TestConstants.TenantId,
                    notAfterUtc: DateTimeOffset.UtcNow.AddHours(23).AddMinutes(5));

                var mi = await CreateManagedIdentityAsync(httpManager, idKind, idValue, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard)
                    .ConfigureAwait(false);

                // FIRST acquire -> MINT with short-lived cert #1
                AddMocksToGetEntraToken(httpManager, idKind, idValue, certificateRequestCertificate: rawShort1);

                var first = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource, $"[{label}] first must mint.");

                // SECOND acquire -> FORCE REFRESH to bypass AT cache; since cert #1 wasn't cached, we must mint again.
                AddMocksToGetEntraToken(httpManager, idKind, idValue, certificateRequestCertificate: rawShort2);

                var second = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithForceRefresh(true) // <-- key change
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, second.AuthenticationResultMetadata.TokenSource, $"[{label}] second must mint (no cert cache for <24h).");
                Assert.AreNotEqual(first.BindingCertificate.Thumbprint, second.BindingCertificate.Thumbprint, $"[{label}] re-mint should produce a new binding cert.");
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId, "UAMI-ClientId")]
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId, "UAMI-ObjectId")]
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId, "UAMI-ResourceId")]
        [DataRow(UserAssignedIdentityId.None, null, "SAMI")]
        public async Task mTLSPop_CertAtLeast24h_IsCached_ReusedOnSecondAcquire(
            UserAssignedIdentityId idKind,
            string idValue,
            string label)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ManagedIdentityClient.ResetSourceForTest();
                SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, TestConstants.ImdsEndpoint);

                // NotAfter >= 24h + 1min ? should be cached and reused
                var rawLong = CreateRawCertForCsrKeyWithCnDc(
                    cn: (idKind == UserAssignedIdentityId.None ? Constants.ManagedIdentityDefaultClientId : TestConstants.ClientId),
                    dc: TestConstants.TenantId,
                    notAfterUtc: DateTimeOffset.UtcNow.AddHours(24).AddMinutes(1));

                var mi = await CreateManagedIdentityAsync(httpManager, idKind, idValue, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard)
                    .ConfigureAwait(false);

                // First acquire ? MINT
                AddMocksToGetEntraToken(httpManager, idKind, idValue, certificateRequestCertificate: rawLong);

                var first = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource, $"[{label}] first must mint long-lived cert.");

                // Second acquire ? CACHED (no /issuecredential mocks needed)
                var second = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource, $"[{label}] second should be cache.");
                Assert.AreEqual(first.BindingCertificate.Thumbprint, second.BindingCertificate.Thumbprint, $"[{label}] cached cert must be reused.");
            }
        }
        #endregion

        #region Cert cache test helpers

        // Build a base64 DER cert (public part only) whose public key == the CSR key used by tests
        private static string CreateRawCertForCsrKey(string subjectCN, DateTimeOffset notAfter)
        {
            using var rsa = TestCsrFactory.CreateMockRsa(); // same key the CSR factory uses
            return CreateRawCertFromKey(rsa, subjectCN, notAfter);
        }

        // Build a base64 DER cert (public part only) with Subject "CN=<cn>, DC=<dc>" and CSR key
        private static string CreateRawCertForCsrKeyWithCnDc(string cn, string dc, DateTimeOffset notAfterUtc)
        {
            using var rsa = TestCsrFactory.CreateMockRsa();
            var subject = $"CN={cn}, DC={dc}";
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                new X500DistinguishedName(subject),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
            using var cert = req.CreateSelfSigned(notBefore, notAfterUtc);
            return Convert.ToBase64String(cert.Export(X509ContentType.Cert));
        }

        private static string CreateRawCertFromKey(RSA key, string subjectCN, DateTimeOffset notAfter)
        {
            var now = DateTimeOffset.UtcNow.AddMinutes(-2);

            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                new X500DistinguishedName(subjectCN),
                key,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            using var cert = req.CreateSelfSigned(now, notAfter);
            // Return public portion only; the product code attaches the private key
            return Convert.ToBase64String(cert.Export(X509ContentType.Cert));
        }

        private static RSA RsaFromXml(string xml)
        {
            var rsa = RSA.Create();

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            var doc = new XmlDocument { XmlResolver = null };
            using (var sr = new StringReader(xml))
            using (var xr = XmlReader.Create(sr, settings))
            {
                doc.Load(xr);
            }

            byte[] B64(string s) => Convert.FromBase64String(s);

            var p = new RSAParameters
            {
                Modulus = B64(doc.DocumentElement["Modulus"].InnerText),
                Exponent = B64(doc.DocumentElement["Exponent"].InnerText),
                P = B64(doc.DocumentElement["P"].InnerText),
                Q = B64(doc.DocumentElement["Q"].InnerText),
                DP = B64(doc.DocumentElement["DP"].InnerText),
                DQ = B64(doc.DocumentElement["DQ"].InnerText),
                InverseQ = B64(doc.DocumentElement["InverseQ"].InnerText),
                D = B64(doc.DocumentElement["D"].InnerText),
            };

            rsa.ImportParameters(p);
            return rsa;
        }

        private static string CreateRawCertFromXml(string subjectCN, DateTimeOffset notAfterUtc)
        {
            using var rsa = RsaFromXml(TestConstants.XmlPrivateKey); // same RSA as CSR/keyguard in tests

            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                new X500DistinguishedName(subjectCN),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
            using var cert = req.CreateSelfSigned(notBefore, notAfterUtc);

            // IMPORTANT: return **public part only** – product code attaches the private key
            return Convert.ToBase64String(cert.Export(X509ContentType.Cert));
        }

        private static void AssertCertCN(X509Certificate2 cert, string expectedCn)
        {
            // SimpleName returns the CN without the "CN=" prefix
            var cn = cert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            // Defensive fallback in case SimpleName is empty on some runtimes
            if (string.IsNullOrEmpty(cn) && !string.IsNullOrEmpty(cert.Subject))
            {
                var subject = cert.Subject; // e.g. "CN=SAMI-20Y"
                const string cnPrefix = "CN=";
                var idx = subject.IndexOf(cnPrefix, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var end = subject.IndexOf(',', idx);
                    cn = (end > idx ? subject.Substring(idx + cnPrefix.Length, end - (idx + cnPrefix.Length))
                                    : subject.Substring(idx + cnPrefix.Length)).Trim();
                }
            }

            Assert.AreEqual(expectedCn, cn, $"Expected CN={expectedCn}, got Subject='{cert.Subject}'.");
        }

        // Parse a specific RDN (e.g., "CN" or "DC") out of the subject
        private static string GetRdn(X509Certificate2 cert, string rdn)
        {
            var dn = cert?.SubjectName?.Name ?? string.Empty;
            foreach (var part in dn.Split(','))
            {
                var kv = part.Trim().Split('=');
                if (kv.Length == 2 && kv[0].Trim().Equals(rdn, StringComparison.OrdinalIgnoreCase))
                    return kv[1].Trim();
            }
            return null;
        }

        private static void AssertCertSubjectCnDc(X509Certificate2 cert, string expectedCn, string expectedDc, string label)
        {
            Assert.IsNotNull(cert);
            var cn = GetRdn(cert, "CN");
            var dc = GetRdn(cert, "DC");

            Assert.AreEqual(expectedCn, cn, $"[{label}] CN mismatch. Subject='{cert.Subject}'");
            Assert.AreEqual(expectedDc, dc, $"[{label}] DC mismatch. Subject='{cert.Subject}'");
        }

        #endregion
    }
}
