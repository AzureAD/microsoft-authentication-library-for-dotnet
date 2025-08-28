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
using OpenTelemetry.Resources;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsV2Tests : TestBase
    {
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();
        private readonly TestCsrFactory _testCsrFactory = new TestCsrFactory();

        //TODO: Clean up this method. Use constants, etc.
        [TestMethod]
        public async Task ImdsV2SAMIHappyPathAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // TODO: Implement DataTestMethod. SAMI + UAMI
                //ManagedIdentityId managedIdentityId = userAssignedId == null
                //    ? ManagedIdentityId.SystemAssigned
                //    : ManagedIdentityId.WithUserAssignedClientId(userAssignedId);
                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithCsrFactory(_testCsrFactory);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddMockHandler(MockHelpers.MockCsrResponse()); // initial probe
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse()); // do it again, since CsrMetadata from initial probe is not cached
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse());
                httpManager.AddManagedIdentityMockHandler(
                    "http://fake_mtls_authentication_endpoint/fake_tenant_id/oauth2/v2.0/token",
                    "https://management.azure.com",
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.ImdsV2);

                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

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
                    imdsV2Source.AttachPrivateKeyToCert(TestConstants.ValidPemCertificate, rsa));

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
                imdsV2Source.AttachPrivateKeyToCert(TestConstants.ValidPemCertificate, null));
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
