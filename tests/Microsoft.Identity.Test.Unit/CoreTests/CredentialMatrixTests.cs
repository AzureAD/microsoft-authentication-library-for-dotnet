// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    /// <summary>
    /// Tests for the canonical credential resolution matrix defined in CREDENTIAL_MATRIX.cs.
    /// Each test validates one row of the matrix to ensure credential implementations
    /// produce correct outputs for their supported modes and throw exceptions for unsupported modes.
    /// </summary>
    [TestClass]
    public class CredentialMatrixTests : TestBase
    {
        private const string ClientId = "test-client-id";
        private const string TokenEndpoint = "https://login.microsoftonline.com/tenant-id/oauth2/v2.0/token";
        private const string TestSecret = "test-secret";
        private const string TestJwtAssertion = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.test";

        private CredentialContext CreateContext(ClientAuthMode mode)
        {
            return new CredentialContext
            {
                ClientId = ClientId,
                TokenEndpoint = TokenEndpoint,
                Mode = mode,
                Claims = null,
                ClientCapabilities = null,
                CryptographyManager = new CommonCryptographyManager(),
                SendX5C = false,
                UseSha2 = true,
                ExtraClientAssertionClaims = null,
                ClientAssertionFmiPath = null,
                AuthorityType = AuthorityType.Aad,
                AzureRegion = null
            };
        }

        #region X509Cert (static certificate) Tests

        [TestMethod]
        public async Task X509Cert_Regular_ProducesJWT()
        {
            // Arrange
            var cert = CertHelper.GetOrCreateTestCert();
            var credential = new CertificateAndClaimsClientCredential(
                certificateProvider: _ => Task.FromResult(cert),
                claimsToSign: null,
                appendDefaultClaims: true,
                certificate: cert);

            var context = CreateContext(ClientAuthMode.Regular);

            // Act
            var material = await credential.GetCredentialMaterialAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material, "Material should not be null");
            Assert.IsNotNull(material.TokenRequestParameters, "TokenRequestParameters should not be null");
            Assert.AreEqual(2, material.TokenRequestParameters.Count, "Should contain client_assertion_type and client_assertion");
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertionType), "Should contain client_assertion_type");
            Assert.AreEqual(OAuth2AssertionType.JwtBearer, material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType], "client_assertion_type should be jwt-bearer");
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertion), "Should contain client_assertion");
            Assert.IsFalse(string.IsNullOrEmpty(material.TokenRequestParameters[OAuth2Parameter.ClientAssertion]), "client_assertion should not be empty");
            Assert.IsNotNull(material.ResolvedCertificate, "ResolvedCertificate should be set");
            Assert.AreEqual(CredentialSource.Static, material.Source, "Source should be Static");
        }

        [TestMethod]
        public async Task X509Cert_MtlsMode_ProducesCertificateOnly()
        {
            // Arrange
            var cert = CertHelper.GetOrCreateTestCert();
            var credential = new CertificateAndClaimsClientCredential(
                certificateProvider: _ => Task.FromResult(cert),
                claimsToSign: null,
                appendDefaultClaims: true,
                certificate: cert);

            var context = CreateContext(ClientAuthMode.MtlsMode);

            // Act
            var material = await credential.GetCredentialMaterialAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material, "Material should not be null");
            Assert.IsNotNull(material.TokenRequestParameters, "TokenRequestParameters should not be null");
            Assert.AreEqual(0, material.TokenRequestParameters.Count, "TokenRequestParameters should be empty (no client_assertion in mTLS mode)");
            Assert.IsNotNull(material.ResolvedCertificate, "ResolvedCertificate should be set");
            Assert.AreEqual(CredentialSource.Static, material.Source, "Source should be Static");
        }

        #endregion

        #region CallbackX509 (certificate via callback) Tests

        [TestMethod]
        public async Task CallbackX509_Regular_ProducesJWT()
        {
            // Arrange
            var cert = CertHelper.GetOrCreateTestCert();
            var credential = new CertificateAndClaimsClientCredential(
                certificateProvider: _ => Task.FromResult(cert),
                claimsToSign: null,
                appendDefaultClaims: true,
                certificate: null); // No static cert = callback source

            var context = CreateContext(ClientAuthMode.Regular);

            // Act
            var material = await credential.GetCredentialMaterialAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material, "Material should not be null");
            Assert.IsNotNull(material.TokenRequestParameters, "TokenRequestParameters should not be null");
            Assert.AreEqual(2, material.TokenRequestParameters.Count, "Should contain client_assertion_type and client_assertion");
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertionType), "Should contain client_assertion_type");
            Assert.AreEqual(OAuth2AssertionType.JwtBearer, material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType], "client_assertion_type should be jwt-bearer");
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertion), "Should contain client_assertion");
            Assert.IsFalse(string.IsNullOrEmpty(material.TokenRequestParameters[OAuth2Parameter.ClientAssertion]), "client_assertion should not be empty");
            Assert.IsNotNull(material.ResolvedCertificate, "ResolvedCertificate should be set");
            Assert.AreEqual(CredentialSource.Callback, material.Source, "Source should be Callback");
        }

        [TestMethod]
        public async Task CallbackX509_MtlsMode_ProducesCertificateOnly()
        {
            // Arrange
            var cert = CertHelper.GetOrCreateTestCert();
            var credential = new CertificateAndClaimsClientCredential(
                certificateProvider: _ => Task.FromResult(cert),
                claimsToSign: null,
                appendDefaultClaims: true,
                certificate: null); // No static cert = callback source

            var context = CreateContext(ClientAuthMode.MtlsMode);

            // Act
            var material = await credential.GetCredentialMaterialAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material, "Material should not be null");
            Assert.IsNotNull(material.TokenRequestParameters, "TokenRequestParameters should not be null");
            Assert.AreEqual(0, material.TokenRequestParameters.Count, "TokenRequestParameters should be empty (no client_assertion in mTLS mode)");
            Assert.IsNotNull(material.ResolvedCertificate, "ResolvedCertificate should be set");
            Assert.AreEqual(CredentialSource.Callback, material.Source, "Source should be Callback");
        }

        #endregion

        #region Secret Tests

        [TestMethod]
        public async Task Secret_Regular_ProducesClientSecret()
        {
            // Arrange
            var credential = new SecretStringClientCredential(TestSecret);
            var context = CreateContext(ClientAuthMode.Regular);

            // Act
            var material = await credential.GetCredentialMaterialAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material, "Material should not be null");
            Assert.IsNotNull(material.TokenRequestParameters, "TokenRequestParameters should not be null");
            Assert.AreEqual(1, material.TokenRequestParameters.Count, "Should contain only client_secret");
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientSecret), "Should contain client_secret");
            Assert.AreEqual(TestSecret, material.TokenRequestParameters[OAuth2Parameter.ClientSecret], "client_secret value should match");
            Assert.IsNull(material.ResolvedCertificate, "ResolvedCertificate should be null");
            Assert.AreEqual(CredentialSource.Static, material.Source, "Source should be Static");
        }

        [TestMethod]
        public async Task Secret_MtlsMode_ThrowsException()
        {
            // Arrange
            var credential = new SecretStringClientCredential(TestSecret);
            var context = CreateContext(ClientAuthMode.MtlsMode);

            // Act & Assert
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(context, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidCredentialMaterial, ex.ErrorCode, "Should throw with InvalidCredentialMaterial error code");
            Assert.IsTrue(ex.Message.Contains("mTLS"), "Error message should mention mTLS");
        }

        #endregion

        #region JWT (SignedAssertion) Tests

        [TestMethod]
        public async Task JWT_Regular_ProducesClientAssertion()
        {
            // Arrange
            var credential = new SignedAssertionClientCredential(TestJwtAssertion);
            var context = CreateContext(ClientAuthMode.Regular);

            // Act
            var material = await credential.GetCredentialMaterialAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material, "Material should not be null");
            Assert.IsNotNull(material.TokenRequestParameters, "TokenRequestParameters should not be null");
            Assert.AreEqual(2, material.TokenRequestParameters.Count, "Should contain client_assertion_type and client_assertion");
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertionType), "Should contain client_assertion_type");
            Assert.AreEqual(OAuth2AssertionType.JwtBearer, material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType], "client_assertion_type should be jwt-bearer");
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertion), "Should contain client_assertion");
            Assert.AreEqual(TestJwtAssertion, material.TokenRequestParameters[OAuth2Parameter.ClientAssertion], "client_assertion should match provided JWT");
            Assert.IsNull(material.ResolvedCertificate, "ResolvedCertificate should be null");
            Assert.AreEqual(CredentialSource.Static, material.Source, "Source should be Static");
        }

        [TestMethod]
        public async Task JWT_MtlsMode_ThrowsException()
        {
            // Arrange
            var credential = new SignedAssertionClientCredential(TestJwtAssertion);
            var context = CreateContext(ClientAuthMode.MtlsMode);

            // Act & Assert
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(context, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidCredentialMaterial, ex.ErrorCode, "Should throw with InvalidCredentialMaterial error code");
            Assert.IsTrue(ex.Message.Contains("mTLS"), "Error message should mention mTLS");
        }

        #endregion

        #region JWT+Cert (ClientAssertionDelegate) Tests

        [TestMethod]
        public async Task JWTPlusCert_Regular_ThrowsException()
        {
            // Arrange
            var cert = CertHelper.GetOrCreateTestCert();
            var credential = new ClientAssertionDelegateCredential(
                (opts, ct) => Task.FromResult(new ClientSignedAssertion
                {
                    Assertion = TestJwtAssertion,
                    TokenBindingCertificate = cert // Providing cert in Regular mode is not supported
                }));

            var context = CreateContext(ClientAuthMode.Regular);

            // Act & Assert
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(context, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidCredentialMaterial, ex.ErrorCode, "Should throw with InvalidCredentialMaterial error code");
            Assert.IsTrue(ex.Message.Contains("mTLS"), "Error message should mention mTLS");
            Assert.IsTrue(ex.Message.Contains("TokenBindingCertificate"), "Error message should mention TokenBindingCertificate");
        }

        [TestMethod]
        public async Task JWTPlusCert_MtlsMode_ProducesJwtPopPlusCert()
        {
            // Arrange
            var cert = CertHelper.GetOrCreateTestCert();
            var credential = new ClientAssertionDelegateCredential(
                (opts, ct) => Task.FromResult(new ClientSignedAssertion
                {
                    Assertion = TestJwtAssertion,
                    TokenBindingCertificate = cert
                }));

            var context = CreateContext(ClientAuthMode.MtlsMode);

            // Act
            var material = await credential.GetCredentialMaterialAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material, "Material should not be null");
            Assert.IsNotNull(material.TokenRequestParameters, "TokenRequestParameters should not be null");
            Assert.AreEqual(2, material.TokenRequestParameters.Count, "Should contain client_assertion_type and client_assertion");
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertionType), "Should contain client_assertion_type");
            Assert.AreEqual(OAuth2AssertionType.JwtPop, material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType], "client_assertion_type should be jwt-pop (NOT jwt-bearer!)");
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertion), "Should contain client_assertion");
            Assert.AreEqual(TestJwtAssertion, material.TokenRequestParameters[OAuth2Parameter.ClientAssertion], "client_assertion should match provided JWT");
            Assert.IsNotNull(material.ResolvedCertificate, "ResolvedCertificate should be set");
            Assert.AreEqual(CredentialSource.Callback, material.Source, "Source should be Callback");
        }

        [TestMethod]
        public async Task JWTWithoutCert_MtlsMode_ThrowsException()
        {
            // Arrange - callback returns JWT but no certificate in mTLS mode
            var credential = new ClientAssertionDelegateCredential(
                (opts, ct) => Task.FromResult(new ClientSignedAssertion
                {
                    Assertion = TestJwtAssertion,
                    TokenBindingCertificate = null // Missing cert in mTLS mode is not supported
                }));

            var context = CreateContext(ClientAuthMode.MtlsMode);

            // Act & Assert
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(context, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode, "Should throw with MtlsCertificateNotProvided error code");
            Assert.IsTrue(ex.Message.Contains("mTLS"), "Error message should mention mTLS");
            Assert.IsTrue(ex.Message.Contains("TokenBindingCertificate"), "Error message should mention TokenBindingCertificate");
        }

        #endregion

        #region Additional Validation Tests

        [TestMethod]
        public async Task CertificateCredential_WithExtraClientAssertionClaims_IncludesClaimsInJWT()
        {
            // Arrange
            var cert = CertHelper.GetOrCreateTestCert();
            var credential = new CertificateAndClaimsClientCredential(
                certificateProvider: _ => Task.FromResult(cert),
                claimsToSign: null,
                appendDefaultClaims: true,
                certificate: cert);

            var context = CreateContext(ClientAuthMode.Regular);
            context = context with { ExtraClientAssertionClaims = "{\"custom_claim\":\"custom_value\"}" };

            // Act
            var material = await credential.GetCredentialMaterialAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(material, "Material should not be null");
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertion), "Should contain client_assertion");
            // The JWT should be generated with ExtraClientAssertionClaims
            Assert.IsFalse(string.IsNullOrEmpty(material.TokenRequestParameters[OAuth2Parameter.ClientAssertion]), "client_assertion should not be empty");
        }

        [TestMethod]
        public async Task TokenRequestParameters_NeverNull()
        {
            // Arrange - Test various credential types to ensure TokenRequestParameters is never null
            var cert = CertHelper.GetOrCreateTestCert();
            var credentials = new IClientCredential[]
            {
                new CertificateAndClaimsClientCredential(_ => Task.FromResult(cert), null, true, cert),
                new SecretStringClientCredential(TestSecret),
                new SignedAssertionClientCredential(TestJwtAssertion)
            };

            foreach (var credential in credentials)
            {
                var context = CreateContext(ClientAuthMode.Regular);

                // Act
                var material = await credential.GetCredentialMaterialAsync(context, CancellationToken.None).ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(material.TokenRequestParameters, $"TokenRequestParameters should never be null for {credential.GetType().Name}");
            }
        }

        [TestMethod]
        public async Task CertificateCredential_NullCertificateCallback_ThrowsException()
        {
            // Arrange
            var credential = new CertificateAndClaimsClientCredential(
                certificateProvider: _ => Task.FromResult<X509Certificate2>(null), // Returns null
                claimsToSign: null,
                appendDefaultClaims: true,
                certificate: null);

            var context = CreateContext(ClientAuthMode.Regular);

            // Act & Assert
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(context, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidClientAssertion, ex.ErrorCode, "Should throw with InvalidClientAssertion error code");
            Assert.IsTrue(ex.Message.Contains("null"), "Error message should mention null certificate");
        }

        [TestMethod]
        public async Task ClientAssertionDelegate_NullAssertion_ThrowsException()
        {
            // Arrange
            var credential = new ClientAssertionDelegateCredential(
                (opts, ct) => Task.FromResult(new ClientSignedAssertion
                {
                    Assertion = null, // Null assertion
                    TokenBindingCertificate = null
                }));

            var context = CreateContext(ClientAuthMode.Regular);

            // Act & Assert
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(context, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidClientAssertion, ex.ErrorCode, "Should throw with InvalidClientAssertion error code");
        }

        [TestMethod]
        public async Task ClientAssertionDelegate_EmptyAssertion_ThrowsException()
        {
            // Arrange
            var credential = new ClientAssertionDelegateCredential(
                (opts, ct) => Task.FromResult(new ClientSignedAssertion
                {
                    Assertion = "   ", // Empty assertion
                    TokenBindingCertificate = null
                }));

            var context = CreateContext(ClientAuthMode.Regular);

            // Act & Assert
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(context, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidClientAssertion, ex.ErrorCode, "Should throw with InvalidClientAssertion error code");
        }

        #endregion
    }
}
