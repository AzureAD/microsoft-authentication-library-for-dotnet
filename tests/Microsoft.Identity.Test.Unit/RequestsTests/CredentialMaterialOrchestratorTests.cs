// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class CredentialMaterialOrchestratorTests
    {
        private ILoggerAdapter _logger;
        private ICryptographyManager _cryptoManager;

        [TestInitialize]
        public void TestInitialize()
        {
            _logger = Substitute.For<ILoggerAdapter>();
            _cryptoManager = Substitute.For<ICryptographyManager>();
        }

        #region Basic Credential Material Tests

        [TestMethod]
        public async Task SecretStringClientCredential_ReturnsCorrectMaterial_Async()
        {
            // Arrange
            const string testSecret = "test-secret-123";
            var credential = new SecretStringClientCredential(testSecret);
            var requestContext = CreateRequestContext(mtlsRequired: false);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.TokenRequestParameters);
            Assert.AreEqual(testSecret, material.TokenRequestParameters["client_secret"]);
            Assert.IsNull(material.MtlsCertificate);
            Assert.IsNotNull(material.Metadata);
            Assert.AreEqual(AssertionType.Secret, material.Metadata.CredentialType);
            Assert.AreEqual("static", material.Metadata.CredentialSource);
            Assert.IsFalse(material.Metadata.MtlsCertificateRequested);
            Assert.AreEqual(0, material.Metadata.ResolutionTimeMs);
        }

        [TestMethod]
        public async Task SignedAssertionClientCredential_ReturnsCorrectMaterial_Async()
        {
            // Arrange
            const string testAssertion = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.test.signature";
            var credential = new SignedAssertionClientCredential(testAssertion);
            var requestContext = CreateRequestContext(mtlsRequired: false);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.TokenRequestParameters);
            Assert.AreEqual("urn:ietf:params:oauth:client-assertion-type:jwt-bearer", material.TokenRequestParameters["client_assertion_type"]);
            Assert.AreEqual(testAssertion, material.TokenRequestParameters["client_assertion"]);
            Assert.IsNull(material.MtlsCertificate);
            Assert.IsNotNull(material.Metadata);
            Assert.AreEqual(AssertionType.ClientAssertion, material.Metadata.CredentialType);
            Assert.AreEqual("static", material.Metadata.CredentialSource);
            Assert.IsFalse(material.Metadata.MtlsCertificateRequested);
            Assert.AreEqual(0, material.Metadata.ResolutionTimeMs);
        }

        [TestMethod]
        public async Task CertificateClientCredential_ReturnsCorrectMaterial_WithoutMtls_Async()
        {
            // Arrange
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(1));
            var credential = new CertificateClientCredential(cert);
            var requestContext = CreateRequestContext(mtlsRequired: false);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.TokenRequestParameters);
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey("client_assertion_type"));
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey("client_assertion"));
            Assert.IsNull(material.MtlsCertificate);
            Assert.IsNotNull(material.Metadata);
            Assert.AreEqual(AssertionType.CertificateWithoutSni, material.Metadata.CredentialType);
            Assert.AreEqual("static", material.Metadata.CredentialSource);
            Assert.IsFalse(material.Metadata.MtlsCertificateRequested);
        }

        [TestMethod]
        public async Task CertificateClientCredential_ReturnsCorrectMaterial_WithMtls_Async()
        {
            // Arrange
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(1));
            var credential = new CertificateClientCredential(cert);
            var requestContext = CreateRequestContext(mtlsRequired: true);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.TokenRequestParameters);
            Assert.AreEqual(0, material.TokenRequestParameters.Count); // No JWT params when mTLS
            Assert.IsNotNull(material.MtlsCertificate);
            Assert.AreEqual(cert.Thumbprint, material.MtlsCertificate.Thumbprint);
            Assert.IsNotNull(material.Metadata);
            Assert.AreEqual(AssertionType.CertificateWithoutSni, material.Metadata.CredentialType);
            Assert.AreEqual("static", material.Metadata.CredentialSource);
            Assert.IsTrue(material.Metadata.MtlsCertificateRequested);
            Assert.IsNotNull(material.Metadata.MtlsCertificateIdHashPrefix);
        }

        #endregion

        #region Orchestrator Validation Tests

        [TestMethod]
        public async Task Orchestrator_ValidatesMissingCert_WhenMtlsRequired_Async()
        {
            // Arrange
            var credential = new SecretStringClientCredential("test-secret");
            var orchestrator = new CredentialMaterialOrchestrator(credential, _logger);
            var requestContext = CreateRequestContext(mtlsRequired: true);
            var mtlsContext = new MtlsValidationContext
            {
                AuthorityType = AuthorityType.Aad,
                AzureRegion = "westus2"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                () => orchestrator.GetValidatedMaterialAsync(requestContext, mtlsContext, CancellationToken.None)
            ).ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);
            Assert.IsTrue(ex.Message.Contains("mTLS Proof of Possession"));
        }

        [TestMethod]
        public async Task Orchestrator_ValidatesMissingRegion_WhenAadAndMtls_Async()
        {
            // Arrange
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(1));
            var credential = new CertificateClientCredential(cert);
            var orchestrator = new CredentialMaterialOrchestrator(credential, _logger);
            var requestContext = CreateRequestContext(mtlsRequired: true);
            var mtlsContext = new MtlsValidationContext
            {
                AuthorityType = AuthorityType.Aad,
                AzureRegion = null // Missing region
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                () => orchestrator.GetValidatedMaterialAsync(requestContext, mtlsContext, CancellationToken.None)
            ).ConfigureAwait(false);

            Assert.AreEqual(MsalError.RegionRequiredForMtlsPop, ex.ErrorCode);
            Assert.IsTrue(ex.Message.Contains("Azure region"));
        }

        [TestMethod]
        public async Task Orchestrator_AllowsNullCert_WhenMtlsNotRequired_Async()
        {
            // Arrange
            var credential = new SecretStringClientCredential("test-secret");
            var orchestrator = new CredentialMaterialOrchestrator(credential, _logger);
            var requestContext = CreateRequestContext(mtlsRequired: false);
            var mtlsContext = new MtlsValidationContext
            {
                AuthorityType = AuthorityType.Aad,
                AzureRegion = "westus2"
            };

            // Act
            var material = await orchestrator.GetValidatedMaterialAsync(requestContext, mtlsContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material);
            Assert.IsNull(material.MtlsCertificate);
        }

        [TestMethod]
        public async Task Orchestrator_AllowsNonAadAuthority_WithoutRegion_Async()
        {
            // Arrange
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(1));
            var credential = new CertificateClientCredential(cert);
            var orchestrator = new CredentialMaterialOrchestrator(credential, _logger);
            var requestContext = CreateRequestContext(mtlsRequired: true);
            var mtlsContext = new MtlsValidationContext
            {
                AuthorityType = AuthorityType.Adfs,
                AzureRegion = null // No region needed for non-AAD
            };

            // Act
            var material = await orchestrator.GetValidatedMaterialAsync(requestContext, mtlsContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.MtlsCertificate);
        }

        [TestMethod]
        public async Task Orchestrator_ThrowsException_WhenCredentialReturnsNull_Async()
        {
            // Arrange
            var mockCredential = Substitute.For<IClientCredential>();
            mockCredential.GetCredentialMaterialAsync(Arg.Any<CredentialRequestContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<CredentialMaterial>(null));

            var orchestrator = new CredentialMaterialOrchestrator(mockCredential, _logger);
            var requestContext = CreateRequestContext(mtlsRequired: false);
            var mtlsContext = new MtlsValidationContext
            {
                AuthorityType = AuthorityType.Aad,
                AzureRegion = "westus2"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                () => orchestrator.GetValidatedMaterialAsync(requestContext, mtlsContext, CancellationToken.None)
            ).ConfigureAwait(false);

            Assert.AreEqual(MsalError.InternalError, ex.ErrorCode);
            Assert.IsTrue(ex.Message.Contains("null material"));
        }

        #endregion

        #region Metadata Tests

        [TestMethod]
        public async Task Metadata_ContainsCorrectCredentialType_ForSecret_Async()
        {
            // Arrange
            var credential = new SecretStringClientCredential("test-secret");
            var requestContext = CreateRequestContext(mtlsRequired: false);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(AssertionType.Secret, material.Metadata.CredentialType);
        }

        [TestMethod]
        public async Task Metadata_ContainsCorrectCredentialType_ForSignedAssertion_Async()
        {
            // Arrange
            var credential = new SignedAssertionClientCredential("test-assertion");
            var requestContext = CreateRequestContext(mtlsRequired: false);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(AssertionType.ClientAssertion, material.Metadata.CredentialType);
        }

        [TestMethod]
        public async Task Metadata_ContainsCorrectCredentialType_ForCertificate_Async()
        {
            // Arrange
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(1));
            var credential = new CertificateClientCredential(cert);
            var requestContext = CreateRequestContext(mtlsRequired: false);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(AssertionType.CertificateWithoutSni, material.Metadata.CredentialType);
        }

        [TestMethod]
        public async Task Metadata_ContainsCredentialSource_Static_Async()
        {
            // Arrange
            var credential = new SecretStringClientCredential("test-secret");
            var requestContext = CreateRequestContext(mtlsRequired: false);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual("static", material.Metadata.CredentialSource);
        }

        [TestMethod]
        public async Task Metadata_TracksResolutionTimeMs_ForSecret_Async()
        {
            // Arrange
            var credential = new SecretStringClientCredential("test-secret");
            var requestContext = CreateRequestContext(mtlsRequired: false);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, material.Metadata.ResolutionTimeMs); // Secret is instant
        }

        [TestMethod]
        public async Task Metadata_TracksResolutionTimeMs_ForCertificate_Async()
        {
            // Arrange
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(1));
            var credential = new CertificateClientCredential(cert);
            var requestContext = CreateRequestContext(mtlsRequired: false);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(material.Metadata.ResolutionTimeMs >= 0);
        }

        [TestMethod]
        public async Task Metadata_TracksMtlsCertificateRequested_WhenMtlsRequired_Async()
        {
            // Arrange
            var credential = new SecretStringClientCredential("test-secret");
            var requestContext = CreateRequestContext(mtlsRequired: true);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(material.Metadata.MtlsCertificateRequested);
        }

        [TestMethod]
        public async Task Metadata_TracksMtlsCertificateRequested_WhenMtlsNotRequired_Async()
        {
            // Arrange
            var credential = new SecretStringClientCredential("test-secret");
            var requestContext = CreateRequestContext(mtlsRequired: false);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsFalse(material.Metadata.MtlsCertificateRequested);
        }

        [TestMethod]
        public async Task Metadata_ContainsMtlsCertificateIdHashPrefix_WhenMtlsCertProvided_Async()
        {
            // Arrange
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(1));
            var credential = new CertificateClientCredential(cert);
            var requestContext = CreateRequestContext(mtlsRequired: true);

            // Act
            var material = await credential.GetCredentialMaterialAsync(requestContext, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material.Metadata.MtlsCertificateIdHashPrefix);
            Assert.IsTrue(material.Metadata.MtlsCertificateIdHashPrefix.Length <= 16);
        }

        #endregion

        #region Helper Methods

        private CredentialRequestContext CreateRequestContext(bool mtlsRequired)
        {
            return new CredentialRequestContext
            {
                ClientId = "test-client-id",
                TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                Claims = null,
                ClientCapabilities = null,
                MtlsRequired = mtlsRequired,
                CancellationToken = CancellationToken.None,
                CryptographyManager = _cryptoManager,
                UseSha2 = true,
                SendX5C = false,
                TenantId = "common"
            };
        }

        private static X509Certificate2 CreateSelfSignedCert(TimeSpan lifetime, string subjectCn = "CN=TestCert")
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                new X500DistinguishedName(subjectCn),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
            var notAfter = notBefore.Add(lifetime);
            return req.CreateSelfSigned(notBefore, notAfter);
        }

        #endregion
    }
}
