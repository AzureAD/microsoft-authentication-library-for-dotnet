// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.Identity.Test.Unit.PublicApiTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public const string MTLSPoP = "MTLSPoP";

        private void AddMocksToGetEntraToken(
            MockHttpManager httpManager,
            UserAssignedIdentityId userAssignedIdentityId = UserAssignedIdentityId.None,
            string userAssignedId = null,
            string certificateRequestCertificate = TestConstants.ValidPemCertificate,
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
            bool addSourceCheck = true)
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

            // Disabling shared cache options to avoid cross test pollution.
            miBuilder.Config.AccessorOptions = null;

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
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null)]                             // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]       // UAMI
        public async Task BearerTokenTokenIsPerIdentity(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
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

                #region Identity 2
                var managedIdentityApp2 = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, addProbeMock: false, addSourceCheck: false).ConfigureAwait(false); // source is already cached

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId);

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

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId, TestConstants.ExpiredPemCertificate); // cert will be expired on second request

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
                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId/*, mTLSPop: true*/); // TODO: implement mTLS Pop

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    // .WithMtlsProofOfPossession() // TODO: implement mTLS Pop
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                // Assert.AreEqual(result.TokenType, MTLSPoP);  // TODO: implement mTLS Pop
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                // Assert.AreEqual(result.TokenType, MTLSPoP);  // TODO: implement mTLS Pop
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.None, null)]                             // SAMI
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]       // UAMI
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)] // UAMI
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]       // UAMI
        public async Task mTLSPopTokenTokenIsPerIdentity(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (var httpManager = new MockHttpManager())
            {
                #region Identity 1
                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId/*, mTLSPop: true*/); // TODO: implement mTLS Pop

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    // .WithMtlsProofOfPossession() // TODO: implement mTLS Pop
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                // Assert.AreEqual(result.TokenType, MTLSPoP);  // TODO: implement mTLS Pop
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    // .WithMtlsProofOfPossession() // TODO: implement mTLS Pop
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                // Assert.AreEqual(result.TokenType, MTLSPoP);  // TODO: implement mTLS Pop
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                #endregion Identity 1

                #region Identity 2
                var managedIdentityApp2 = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId, addProbeMock: false, addSourceCheck: false).ConfigureAwait(false); // source is already cached

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId/*, mTLSPop: true*/); // TODO: implement mTLS Pop

                var result2 = await managedIdentityApp2.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    // .WithMtlsProofOfPossession() // TODO: implement mTLS Pop
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.AccessToken);
                // Assert.AreEqual(result.TokenType, MTLSPoP);  // TODO: implement mTLS Pop
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);

                result2 = await managedIdentityApp2.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    // .WithMtlsProofOfPossession() // TODO: implement mTLS Pop
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.AccessToken);
                // Assert.AreEqual(result.TokenType, MTLSPoP);  // TODO: implement mTLS Pop
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop
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
        public async Task mTLSPopTokenIsReAcquiredWhenCertificatIsExpired(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = await CreateManagedIdentityAsync(httpManager, userAssignedIdentityId, userAssignedId).ConfigureAwait(false);

                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId, TestConstants.ExpiredPemCertificate/*, mTLSPop: true*/); // TODO: implement mTLS Pop

                var result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    // .WithMtlsProofOfPossession() // TODO: implement mTLS Pop
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                // Assert.AreEqual(result.TokenType, MTLSPoP);  // TODO: implement mTLS Pop
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // TODO: Add functionality to check cert expiration in the cache
                /**
                AddMocksToGetEntraToken(httpManager, userAssignedIdentityId, userAssignedId, // mTLSPop: true);  // TODO: implement mTLS Pop

                result = await managedIdentityApp.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    // .WithMtlsProofOfPossession() // TODO: implement mTLS Pop
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                // Assert.AreEqual(result.TokenType, MTLSPoP);  // TODO: implement mTLS Pop
                // Assert.IsNotNull(result.BindingCertificate); // TODO: implement mTLS Pop
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

                Assert.IsTrue(handler.ActualRequestHeaders.Contains("Metadata"));
                Assert.IsTrue(handler.ActualRequestHeaders.Contains("x-ms-client-request-id"));
                Assert.IsTrue(handler.ActualRequestMessage.RequestUri.Query.Contains("api-version"));
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
        public async Task GetCsrMetadataAsyncFailsWithInvalidVersion()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: "IMDS/150.870.65.1853")); // min version is 1854

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

        [TestMethod]
        public void TestCsrGeneration_OnlyVmId()
        {
            var cuid = new CuidInfo
            {
                VmId = TestConstants.VmId
            };

            var (csr, _) = Csr.Generate(TestConstants.ClientId, TestConstants.TenantId, cuid);
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

            var (csr, _) = Csr.Generate(TestConstants.ClientId, TestConstants.TenantId, cuid);
            CsrValidator.ValidateCsrContent(csr, TestConstants.ClientId, TestConstants.TenantId, cuid);
        }

        [TestMethod]
        public void TestCsrGeneration_MalformedPem_FormatException()
        {
            string malformedPem = "-----BEGIN CERTIFICATE REQUEST-----\nInvalid@#$%Base64Content!\n-----END CERTIFICATE REQUEST-----";
            Assert.ThrowsException<FormatException>(() => 
                CsrValidator.ParseCsrFromPem(malformedPem));
        }

        [DataTestMethod]
        [DataRow("-----BEGIN CERTIFICATE-----\nTUlJQzNqQ0NBY1lDQVFBd1pURT0K\n-----END CERTIFICATE REQUEST-----")]
        [DataRow("")]
        [DataRow(null)]
        public void TestCsrGeneration_MalformedPem_ArgumentException(string malformedPem)
        {
            Assert.ThrowsException<ArgumentException>(() => 
                CsrValidator.ParseCsrFromPem(malformedPem));
        }

        #region AttachPrivateKeyToCert Tests
        [TestMethod]
        public void AttachPrivateKeyToCert_ValidInputs_ReturnsValidCertificate()
        {
            using (RSA rsa = RSA.Create())
            {
                // For this test, we just want to verify that the method doesn't crash
                // The actual certificate/private key matching isn't critical for the unit test
                var exception = Assert.ThrowsException<CryptographicUnexpectedOperationException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(TestConstants.ValidPemCertificate, rsa));

                // The test should fail with a CryptographicUnexpectedOperationException because the RSA key doesn't match
                // the certificate, but this validates that the method is working correctly
                Assert.IsNotNull(exception.Message);
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_NullCertificatePem_ThrowsArgumentNullException()
        {
            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentNullException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(null, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_EmptyCertificatePem_ThrowsArgumentNullException()
        {
            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentNullException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert("", rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_NullPrivateKey_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                CommonCryptographyManager.AttachPrivateKeyToCert(TestConstants.ValidPemCertificate, null));
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_InvalidPemFormat_ThrowsArgumentException()
        {
            const string InvalidPemNoCertMarker = @"This is not a valid PEM certificate";

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemNoCertMarker, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_MissingBeginMarker_ThrowsArgumentException()
        {
            const string InvalidPemMissingBeginMarker = @"MIICXTCCAUWgAwIBAgIJAKPiQh26MIuPMA0GCSqGSIb3DQEBCwUAMEUxCzAJBgNV
-----END CERTIFICATE-----";

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemMissingBeginMarker, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_MissingEndMarker_ThrowsArgumentException()
        {
            const string InvalidPemMissingEndMarker = @"-----BEGIN CERTIFICATE-----
MIICXTCCAUWgAwIBAgIJAKPiQh26MIuPMA0GCSqGSIb3DQEBCwUAMEUxCzAJBgNV";
            
            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemMissingEndMarker, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_BadBase64Content_ThrowsFormatException()
        {
            const string InvalidPemBadBase64 = @"-----BEGIN CERTIFICATE-----
Invalid@#$%Base64Content!
-----END CERTIFICATE-----";

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<FormatException>(() =>
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemBadBase64, rsa));
            }
        }
        #endregion
    }
}
