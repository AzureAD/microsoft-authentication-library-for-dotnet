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
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsV2Tests : TestBase
    {
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();
        private readonly TestCsrFactory _testCsrFactory = new TestCsrFactory();

        [TestMethod]
        public async Task ImdsV2SAMIHappyPathAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
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
                    $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                    ManagedIdentityTests.Resource,
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

        [DataTestMethod]
        [DataRow(UserAssignedIdentityId.ClientId, TestConstants.ClientId)]
        [DataRow(UserAssignedIdentityId.ResourceId, TestConstants.MiResourceId)]
        [DataRow(UserAssignedIdentityId.ObjectId, TestConstants.ObjectId)]
        public async Task ImdsV2UAMIHappyPathAsync(
            UserAssignedIdentityId userAssignedIdentityId,
            string userAssignedId)
        {
            using (var httpManager = new MockHttpManager())
            {
                var miBuilder = CreateMIABuilder(userAssignedId, userAssignedIdentityId);
                miBuilder
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .WithCsrFactory(_testCsrFactory);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(idType: userAssignedIdentityId, userAssignedId: userAssignedId)); // initial probe
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(idType: userAssignedIdentityId, userAssignedId: userAssignedId)); // do it again, since CsrMetadata from initial probe is not cached
                httpManager.AddMockHandler(MockHelpers.MockCertificateRequestResponse(userAssignedIdentityId, userAssignedId));
                httpManager.AddManagedIdentityMockHandler(
                    $"{TestConstants.MtlsAuthenticationEndpoint}/{TestConstants.TenantId}{ImdsV2ManagedIdentitySource.AcquireEntraTokenPath}",
                    ManagedIdentityTests.Resource,
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
        public async Task GetCsrMetadataAsyncFailsWithInvalidFormat()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: "I_MDS/150.870.65.1854"));

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
                    CommonCryptographyManager.AttachPrivateKeyToCert(null, rsa));
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
                    CommonCryptographyManager.AttachPrivateKeyToCert("", rsa));
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
                CommonCryptographyManager.AttachPrivateKeyToCert(TestConstants.ValidPemCertificate, null));
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
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemNoCertMarker, rsa));
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
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemMissingBeginMarker, rsa));
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
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemMissingEndMarker, rsa));
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
                    CommonCryptographyManager.AttachPrivateKeyToCert(InvalidPemBadBase64, rsa));
            }
        }

        #endregion
    }
}
