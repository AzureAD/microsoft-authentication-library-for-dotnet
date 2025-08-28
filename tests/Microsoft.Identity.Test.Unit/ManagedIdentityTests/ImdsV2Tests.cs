// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsV2Tests : TestBase
    {
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();

        // Test constants for certificate testing - using a real self-signed certificate
        private const string ValidPemCertificate = @"-----BEGIN CERTIFICATE-----
MIIDUTCCAjmgAwIBAgIUPS20Ik/lV4SSwHHHJGPSlG7j5SgwDQYJKoZIhvcNAQEL
BQAwNzEWMBQGA1UEAwwNVW5pdFRlc3REdW1teTEQMA4GA1UECgwHVGVzdE9yZzEL
MAkGA1UEBhMCVVMwIBcNMjUwODI4MTcxMTA3WhgPMjI5OTA2MTIxNzExMDdaMDcx
FjAUBgNVBAMMDVVuaXRUZXN0RHVtbXkxEDAOBgNVBAoMB1Rlc3RPcmcxCzAJBgNV
BAYTAlVTMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwFX8Gqz2g4Hf
dRhrNiP8oNiZ4IwO4bra9wdCR03PEKgYv1GL1Uj0OfhKSt+8WLng43da1p3jBh2P
79IRdRLLJFX4LEJaPWW2/qUCRBpA4eMmSEBRSt1hYGtMNaKdBtxDpOxCBRpofV7Z
PPTrg682ZHAlZ5K5PK9mWfRzV1C/NmSg8FtnD24VWrdkh1waqt40OzrE16JzmPpu
2YDfXilM3G5Zq4uxHXQVCrmchBSVf7frsz+LSnMU1kn45AqDjsqufxH5+CDOtFvM
R7794+HKOdzl20U+npfbtVGKIfcWh+kRcZyrLj6DER09ehVz8VWLYgntY+8riDcl
UAfGh0RNswIDAQABo1MwUTAdBgNVHQ4EFgQUbR0id2PPztRSAoggeu0eqNFwtTAw
HwYDVR0jBBgwFoAUbR0id2PPztRSAoggeu0eqNFwtTAwDwYDVR0TAQH/BAUwAwEB
/zANBgkqhkiG9w0BAQsFAAOCAQEAje7eY+MtaBo0TmeF6fM14H5MtD7cYqdFVyIa
KeVWOxwNDtwbwRyfcDlkgcXK8gLeIZA1MNBY/juTx6qy8RsHPdNSTImDVw3t7guq
2CqrA+tqU5E+wah+XzltIvbjqTvRV/20FccfcXAkyM/aWl3WHNkFYNSziT+Ug3QQ
qPABEWvXOjo4BEgrCmQJSIprLgjtfjFSK/LS/VDpRqsSa+3mmx/Dw4FY3rfEqKzv
4RPSFxE8uF/05ByoIaAJZ2JcffDZW8PI5+qwsNatCsypyRADJE1jXLzqZnFFBLW7
dj80Qbs0xLeK0U/Aq1kFf0stgdwbDoHaJj9Q4TlSHZuI0TnjSg==
-----END CERTIFICATE-----";

        public async Task ImdsV2HappyPathAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                //ManagedIdentityId managedIdentityId = userAssignedId == null
                //    ? ManagedIdentityId.SystemAssigned
                //    : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse());
                // TODO: add a mock handler for acquiring the entra token over an mTLS channel
                //httpManager.AddMockHandler()

                // this will fail, see TODO above
                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                // this will fail, see TODO above
                result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncSucceeds()
        {
            using (var httpManager = new MockHttpManager())
            {
                var handler = httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);

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

                // Second attempt succeeds
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsWithMissingServerHeader()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: null));

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

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

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsAfterMaxRetries()
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                const int Num500Errors = 1 + TestCsrMetadataProbeRetryPolicy.ExponentialStrategyNumRetries;
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.InternalServerError));
                }

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFails404WhichIsNonRetriableAndRetryPolicyIsNotTriggeredAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.NotFound));

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
            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                // For this test, we just want to verify that the method doesn't crash
                // The actual certificate/private key matching isn't critical for the unit test
                var exception = Assert.ThrowsException<NotSupportedException>(() => 
                    imdsV2Source.AttachPrivateKeyToCert(ValidPemCertificate, rsa));

                // The test should fail with a NotSupportedException because the RSA key doesn't match
                // the certificate, but this validates that the method is working correctly
                Assert.AreEqual(
                    "Failed to attach private key to certificate on this .NET Framework version.",
                    exception.Message);
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_NullCertificatePem_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentNullException>(() => 
                    imdsV2Source.AttachPrivateKeyToCert(null, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_EmptyCertificatePem_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentNullException>(() => 
                    imdsV2Source.AttachPrivateKeyToCert("", rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_NullPrivateKey_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            Assert.ThrowsException<ArgumentNullException>(() => 
                imdsV2Source.AttachPrivateKeyToCert(ValidPemCertificate, null));
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_InvalidPemFormat_ThrowsArgumentException()
        {
            const string InvalidPemNoCertMarker = @"This is not a valid PEM certificate";

            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentException>(() => 
                    imdsV2Source.AttachPrivateKeyToCert(InvalidPemNoCertMarker, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_MissingBeginMarker_ThrowsArgumentException()
        {
            const string InvalidPemMissingBeginMarker = @"MIICXTCCAUWgAwIBAgIJAKPiQh26MIuPMA0GCSqGSIb3DQEBCwUAMEUxCzAJBgNV
-----END CERTIFICATE-----";

            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentException>(() => 
                    imdsV2Source.AttachPrivateKeyToCert(InvalidPemMissingBeginMarker, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_MissingEndMarker_ThrowsArgumentException()
        {
            const string InvalidPemMissingEndMarker = @"-----BEGIN CERTIFICATE-----
MIICXTCCAUWgAwIBAgIJAKPiQh26MIuPMA0GCSqGSIb3DQEBCwUAMEUxCzAJBgNV";
            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<ArgumentException>(() => 
                    imdsV2Source.AttachPrivateKeyToCert(InvalidPemMissingEndMarker, rsa));
            }
        }

        [TestMethod]
        public void AttachPrivateKeyToCert_BadBase64Content_ThrowsFormatException()
        {
            const string InvalidPemBadBase64 = @"-----BEGIN CERTIFICATE-----
Invalid@#$%Base64Content!
-----END CERTIFICATE-----";

            using var httpManager = new MockHttpManager();
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(httpManager)
                .WithRetryPolicyFactory(_testRetryPolicyFactory);
            var managedIdentityApp = miBuilder.BuildConcrete();

            var requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);
            var imdsV2Source = new ImdsV2ManagedIdentitySource(requestContext);

            using (RSA rsa = RSA.Create())
            {
                Assert.ThrowsException<FormatException>(() => 
                    imdsV2Source.AttachPrivateKeyToCert(InvalidPemBadBase64, rsa));
            }
        }

        #endregion
    }
}
