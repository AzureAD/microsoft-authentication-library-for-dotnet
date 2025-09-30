// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.MtlsPop;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
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

        // Fake attestation provider used by mTLS PoP tests so we never hit the real service
        private static readonly Func<AttestationTokenInput, CancellationToken, Task<AttestationTokenResponse>>
            s_fakeAttestationProvider =
                (input, ct) => Task.FromResult(new AttestationTokenResponse
                {
                    AttestationToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.fake.attestation.sig"
                });

        public const string Bearer = "Bearer";
        public const string MTLSPoP = "mtls_pop";

        private const string altUserAssignedId = "some_other_id";

        private void AddMocksToGetEntraToken(
            MockHttpManager httpManager,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null,
            string certificateRequestCertificate = TestConstants.ValidRawCertificate,
            bool mTLSPop = false)
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

            httpManager.AddMockHandler(MockHelpers.MockImdsV2EntraTokenRequestResponse(_identityLoggerAdapter, mTLSPop));
        }

        private async Task<IManagedIdentityApplication> CreateManagedIdentityAsync(
            MockHttpManager httpManager,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null,
            bool addProbeMock = true,
            bool addSourceCheck = true,
            ManagedIdentityKeyType managedIdentityKeyType = ManagedIdentityKeyType.InMemory)
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
                .WithRetryPolicyFactory(_testRetryPolicyFactory)
                .WithCsrFactory(_testCsrFactory);

            var managedIdentityApp = miBuilder.Build();

            if (addProbeMock)
            {
                if (uami)
                {
                    httpManager.AddMockHandler(MockHelpers.MockCsrResponse(userAssignedIdentityId: userAssignedIdentityId, userAssignedId: userAssignedId));
                }
                else
                {
                    httpManager.AddMockHandler(MockHelpers.MockCsrResponse());
                }
            }

            if (addSourceCheck)
            {
                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);
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
                // Default for bearer tests: no attestation.
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
        #region Bearer Token Tests
        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null)]                             // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]       // UAMI
        public async Task BearerTokenHappyPath(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, managedIdentityKeyType: ManagedIdentityKeyType.InMemory).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId);

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, Bearer);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, Bearer);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null, null)]                                        // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId, altUserAssignedId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId, altUserAssignedId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId, altUserAssignedId)]       // UAMI
        public async Task BearerTokenTokenIsPerIdentity(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId,
            string userAssignedId2)
        {
            using (var httpManager = new MockHttpManager())
            {
                #region Identity 1
                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId);

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, Bearer);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, Bearer);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                #endregion Identity 1

                ApplicationBase.ResetStateForTest();

                #region Identity 2
                UserAssignedIdentityId identity2Type = userAssignedIdentityId; // keep the same type, that's the most common scenario
                var managedIdentityApp2 = await CreateManagedIdentityAsync(httpManager, identity2Type, userAssignedId2).ConfigureAwait(false); // source is already cached

                AddMocksToGetEntraToken(httpManager, identity2Type, userAssignedId2);

                var result2 = await managedIdentityApp2.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(result.TokenType, Bearer);
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);

                result2 = await managedIdentityApp2.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(result.TokenType, Bearer);
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
        public async Task BearerTokenIsReAcquiredWhenCertificatIsExpired(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId, TestConstants.ExpiredRawCertificate); // cert will be expired on second request

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, Bearer);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // TODO: Add functionality to check cert expiration in the cache
                /**
                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId);

                result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, Bearer);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                Assert.AreEqual(CertificateCache.Count, 1); // expired cert was removed from the cache
                */
            }
        }
        #endregion Bearer Token Tests

        #region mTLS PoP Token Tests
        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null)]                             // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]       // UAMI
        public async Task mTLSPopTokenHappyPath(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId, mTLSPop: true);

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(s_fakeAttestationProvider)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop BindingCertificate
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // TODO: broken until Gladwin's PR is merged in
                /*result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(s_fakeAttestationProvider)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop BindingCertificate
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);*/
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null, null)]                                          // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId, altUserAssignedId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId, altUserAssignedId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId, altUserAssignedId)]       // UAMI
        public async Task mTLSPopTokenTokenIsPerIdentity(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId,
            string userAssignedId2)
        {
            using (var httpManager = new MockHttpManager())
            {
                #region Identity 1
                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId, mTLSPop: true);

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(s_fakeAttestationProvider)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop BindingCertificate
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // TODO: broken until Gladwin's PR is merged in
                /*result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop BindingCertificate
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);*/
                #endregion Identity 1

                ApplicationBase.ResetStateForTest();

                #region Identity 2
                UserAssignedIdentityId identity2Type = userAssignedIdentityId; // keep the same type, that's the most common scenario
                var managedIdentityApp2 = await CreateManagedIdentityAsync(
                    httpManager,
                    identity2Type,
                    userAssignedId2,
                    managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false); // source is already cached

                AddMocksToGetEntraToken(httpManager, identity2Type, userAssignedId2, mTLSPop: true);

                var result2 = await managedIdentityApp2.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(s_fakeAttestationProvider)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop BindingCertificate
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);

                // TODO: broken until Gladwin's PR is merged in
                /*result2 = await managedIdentityApp2.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(s_fakeAttestationProvider)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop BindingCertificate
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);*/
                #endregion Identity 2

                // TODO: Assert.AreEqual(CertificateCache.Count, 2);
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null)]                             // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]       // UAMI
        public async Task mTLSPopTokenIsReAcquiredWhenCertificatIsExpired(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId, TestConstants.ExpiredRawCertificate, mTLSPop: true);

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(s_fakeAttestationProvider)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop BindingCertificate
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // TODO: Add functionality to check cert expiration in the cache
                /**
                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId, mTLSPop: true);

                result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationProviderForTests(s_fakeAttestationProvider)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.TokenType, MTLSPoP);
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop BindingCertificate
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                Assert.AreEqual(CertificateCache.Count, 1); // expired cert was removed from the cache
                */
            }
        }
        #endregion mTLS Pop Token Tests
        #endregion Acceptance Tests

        [TestMethod]
        public async Task GetCsrMetadataAsyncSucceeds()
        {
            using (var httpManager = new MockHttpManager())
            {
                var handler = httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                await CreateManagedIdentityAsync(httpManager, addProbeMock: false).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncSucceedsAfterRetry()
        {
            using (var httpManager = new MockHttpManager())
            {
                // First attempt fails with INTERNAL_SERVER_ERROR (500)
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.InternalServerError));

                // Second attempt succeeds (defined inside of CreateSAMIAsync)
                await CreateManagedIdentityAsync(httpManager).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsWithMissingServerHeader()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: null));

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, addProbeMock: false, addSourceCheck: false).ConfigureAwait(false);

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsWithInvalidFormat()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: "I_MDS/150.870.65.1854"));

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, addProbeMock: false, addSourceCheck: false).ConfigureAwait(false);

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsAfterMaxRetries()
        {
            using (var httpManager = new MockHttpManager())
            {
                const int Num500Errors = 1 + TestCsrMetadataProbeRetryPolicy.ExponentialStrategyNumRetries;
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.InternalServerError));
                }

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, addProbeMock: false, addSourceCheck: false).ConfigureAwait(false);

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFails404WhichIsNonRetriableAndRetryPolicyIsNotTriggeredAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.NotFound));

                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, addProbeMock: false, addSourceCheck: false).ConfigureAwait(false);

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        #region Cuid Tests
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
        #endregion

        [DataTestMethod]
        [DataRow("Invalid@#$%Certificate!")]
        [DataRow("")]
        [DataRow(null)]
        public void TestCsrGeneration_BadCert_ThrowsMsalServiceException(string badCert)
        {
            Assert.ThrowsException<MsalServiceException>(() =>
                CsrValidator.ParseRawCsr(badCert));
        }

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
        public async Task MtlsPop_AttestationProviderMissing_ThrowsClientException()
        {
            using (var httpManager = new MockHttpManager())
            {
                var mi = await CreateManagedIdentityAsync(httpManager, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                // CreateManagedIdentityAsync does a probe; Add one more CSR response for the actual acquire.
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .WithMtlsProofOfPossession()
                        // Intentionally DO NOT call .WithAttestationProviderForTests(...)
                        .ExecuteAsync().ConfigureAwait(false)
                ).ConfigureAwait(false);

                Assert.AreEqual("attestation_failure", ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task MtlsPop_AttestationProviderReturnsNull_ThrowsClientException()
        {
            using (var httpManager = new MockHttpManager())
            {
                var mi = await CreateManagedIdentityAsync(httpManager,  managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                // CreateManagedIdentityAsync does a probe; Add one more CSR response for the actual acquire.
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                var nullProvider = new Func<AttestationTokenInput, CancellationToken, Task<AttestationTokenResponse>>(
                    (input, ct) => Task.FromResult<AttestationTokenResponse>(null));

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .WithMtlsProofOfPossession()
                        .WithAttestationProviderForTests(nullProvider)
                        .ExecuteAsync().ConfigureAwait(false)
                ).ConfigureAwait(false);

                Assert.AreEqual("attestation_failed", ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task MtlsPop_AttestationProviderReturnsEmptyToken_ThrowsClientException()
        {
            using (var httpManager = new MockHttpManager())
            {
                var mi = await CreateManagedIdentityAsync(httpManager, managedIdentityKeyType: ManagedIdentityKeyType.KeyGuard).ConfigureAwait(false);

                // CreateManagedIdentityAsync does a probe; Add one more CSR response for the actual acquire.
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                var emptyProvider = new Func<AttestationTokenInput, CancellationToken, Task<AttestationTokenResponse>>(
                    (input, ct) => Task.FromResult(new AttestationTokenResponse { AttestationToken = "   " }));

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                        .WithMtlsProofOfPossession()
                        .WithAttestationProviderForTests(emptyProvider)
                        .ExecuteAsync().ConfigureAwait(false)
                ).ConfigureAwait(false);

                Assert.AreEqual("attestation_failed", ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task mTLSPop_RequestedWithoutKeyGuard_ThrowsClientException()
        {
            using (var httpManager = new MockHttpManager())
            {
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
        #endregion
    }
}
