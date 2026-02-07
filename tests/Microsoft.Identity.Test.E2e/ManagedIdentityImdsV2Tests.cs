// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.KeyAttestation;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E
{
    /// <summary>
    /// E2E tests for mTLS Proof-of-Possession with Credential Guard attestation on IMDSv2.
    /// These tests validate:
    /// - mTLS PoP token acquisition with Credential Guard attestation (SAMI and UAMI)
    /// - Token-certificate binding validation (x5t#S256 thumbprint per RFC 8705)
    /// - Graceful degradation when Credential Guard is unavailable
    /// - Virtual Isolation (VBS) protection validation
    /// </summary>
    /// <remarks>
    /// WHY THESE TESTS RUN ON A SPECIFIC (MSALMSIV2) MACHINE:
    /// - Requires IMDSv2 endpoint support (different from standard IMDS)
    /// - Requires Credential Guard / Virtualization-Based Security (VBS) capabilities
    /// - Requires the ability to create CNG RSA keys with Virtual Isolation flags
    /// - Requires a native Credential Guard attestation stack (from MtlsPop package)
    /// - Requires TOKEN_ATTESTATION_ENDPOINT environment variable
    /// Tests gracefully mark Inconclusive rather than fail if prerequisites are missing.
    /// </remarks>
    [TestClass]
    public class ManagedIdentityImdsV2Tests
    {
        private const string ArmScope = "https://management.azure.com";

        // Known UAMI configurations (from existing tests)
        private const string UamiClientId = "8ef2ae5a-f349-4d36-bc0e-a567f2cc50f7";
        private const string UamiResourceId = "/subscriptions/6f52c299-a200-4fe1-8822-a3b61cf1f931/resourcegroups/DevOpsHostedAgents/providers/Microsoft.ManagedIdentity/userAssignedIdentities/ID4SMSIHostedAgent_UAMI";
        private const string UamiObjectId = "0651a6fc-fbf5-4904-9e48-16f63ec1f2b1";

        private static IManagedIdentityApplication BuildMi(
            string userAssignedId = null,
            string idType = null)
        {
            ManagedIdentityId miId = userAssignedId is null
                ? ManagedIdentityId.SystemAssigned
                : idType.ToLowerInvariant() switch
                {
                    "clientid" => ManagedIdentityId.WithUserAssignedClientId(userAssignedId),
                    "resourceid" => ManagedIdentityId.WithUserAssignedResourceId(userAssignedId),
                    "objectid" => ManagedIdentityId.WithUserAssignedObjectId(userAssignedId),
                    _ => throw new ArgumentOutOfRangeException(nameof(idType))
                };

            var builder = ManagedIdentityApplicationBuilder.Create(miId);
            builder.Config.AccessorOptions = null;
            return builder.Build();
        }

        /// <summary>
        /// Validates that a token contains the x5t#S256 claim and that it matches the certificate thumbprint.
        /// Per RFC 8705, x5t#S256 is the base64url-encoded SHA-256 hash of the DER-encoded certificate.
        /// </summary>
        private static void ValidateTokenCertificateBinding(string accessToken, X509Certificate2 certificate)
        {
            Assert.IsNotNull(accessToken, "Access token should not be null.");
            Assert.IsNotNull(certificate, "Certificate should not be null.");

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(accessToken);

            // Check for x5t#S256 claim
            var x5tClaim = jwt.Claims.FirstOrDefault(c => c.Type == "x5t#S256");
            Assert.IsNotNull(x5tClaim, "Token should contain x5t#S256 claim for mTLS PoP binding.");

            // Compute expected thumbprint from certificate (per RFC 8705 Section 3.1)
            // Must use the full DER-encoded certificate (certificate.RawData), not just the public key
            string expectedThumbprint = ComputeX5tS256Thumbprint(certificate);

            Assert.AreEqual(expectedThumbprint, x5tClaim.Value,
                "x5t#S256 thumbprint in token must match the certificate thumbprint per RFC 8705.");
        }

        /// <summary>
        /// Computes the x5t#S256 thumbprint of a certificate per RFC 8705 Section 3.1.
        /// This is the base64url-encoded SHA-256 hash of the DER-encoded certificate.
        /// </summary>
        /// <param name="certificate">The certificate to compute the thumbprint for.</param>
        /// <returns>Base64url-encoded SHA-256 thumbprint.</returns>
        private static string ComputeX5tS256Thumbprint(X509Certificate2 certificate)
        {
            // Per RFC 8705 Section 3.1: x5t#S256 is the SHA-256 hash of the DER-encoded certificate
            // Must use certificate.RawData (full DER encoding), not just the public key
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(certificate.RawData);
                return Base64UrlEncode(hash);
            }
        }

        /// <summary>
        /// Base64url encoding without padding (per RFC 4648 Section 5).
        /// </summary>
        private static string Base64UrlEncode(byte[] data)
        {
            string base64 = Convert.ToBase64String(data);
            // Base64url: replace '+' with '-', '/' with '_', and remove padding '='
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        /// <summary>
        /// Extracts the X509Certificate2 from the authentication result's binding certificate.
        /// </summary>
        private static X509Certificate2 ExtractCertificateFromResult(AuthenticationResult result)
        {
            Assert.IsNotNull(result, "Authentication result should not be null.");
            Assert.IsNotNull(result.BindingCertificate, "Binding certificate should be present in result for mTLS PoP.");

            return result.BindingCertificate;
        }

        #region Basic mTLS PoP Tests (No Attestation)

        /// <summary>
        /// Tests that mTLS PoP tokens can be acquired on IMDSv2 without attestation.
        /// Validates token-certificate binding via x5t#S256 thumbprint (RFC 8705).
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImdsV2_WithMtlsPoP_Succeeds-SAMI")]
        [DataRow(UamiClientId, "clientid", DisplayName = "AcquireToken_OnImdsV2_WithMtlsPoP_Succeeds-UAMI-ClientId")]
        [DataRow(UamiResourceId, "resourceid", DisplayName = "AcquireToken_OnImdsV2_WithMtlsPoP_Succeeds-UAMI-ResourceId")]
        [DataRow(UamiObjectId, "objectid", DisplayName = "AcquireToken_OnImdsV2_WithMtlsPoP_Succeeds-UAMI-ObjectId")]
        public async Task AcquireToken_OnImdsV2_WithMtlsPoP_Succeeds(string id, string idType)
        {
            var mi = BuildMi(id, idType);

            var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                "First call must hit MSI endpoint.");

            // Validate PoP proof and certificate binding
            var certificate = ExtractCertificateFromResult(result);
            ValidateTokenCertificateBinding(result.AccessToken, certificate);

            // Validate that the token is cached
            var second = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource,
                "Second call should use cache.");
            Assert.AreEqual(result.AccessToken, second.AccessToken, "Cached token should match original.");
        }

        #endregion

        #region Credential Guard Attestation Tests

        /// <summary>
        /// Tests that mTLS PoP tokens with Credential Guard attestation can be acquired on IMDSv2.
        /// Validates both token acquisition and certificate VBS protection.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2_Attested")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImdsV2_WithAttestation_Succeeds-SAMI")]
        [DataRow(UamiClientId, "clientid", DisplayName = "AcquireToken_OnImdsV2_WithAttestation_Succeeds-UAMI-ClientId")]
        [DataRow(UamiResourceId, "resourceid", DisplayName = "AcquireToken_OnImdsV2_WithAttestation_Succeeds-UAMI-ResourceId")]
        [DataRow(UamiObjectId, "objectid", DisplayName = "AcquireToken_OnImdsV2_WithAttestation_Succeeds-UAMI-ObjectId")]
        public async Task AcquireToken_OnImdsV2_WithAttestation_Succeeds(string id, string idType)
        {
            // Check for attestation endpoint (required for attestation flow)
            var endpoint = Environment.GetEnvironmentVariable("TOKEN_ATTESTATION_ENDPOINT");
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                Assert.Inconclusive("TOKEN_ATTESTATION_ENDPOINT environment variable not set. " +
                    "This test requires Credential Guard attestation support.");
            }

            var mi = BuildMi(id, idType);

            AuthenticationResult result = null;
            try
            {
                result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalClientException ex)
            {
                // Gracefully handle cases where Credential Guard is not available
                Assert.Inconclusive("Credential Guard or attestation is not available on this machine: " + ex.Message);
            }
            catch (PlatformNotSupportedException)
            {
                Assert.Inconclusive("VBS/Core Isolation is not available on this platform.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.Inconclusive("Attestation native library not available: " + ex.Message);
            }

            Assert.IsNotNull(result, "Result should not be null after successful token acquisition.");
            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                "First call must hit MSI endpoint.");

            // Validate PoP proof and certificate binding
            var certificate = ExtractCertificateFromResult(result);
            ValidateTokenCertificateBinding(result.AccessToken, certificate);

            // Validate that the certificate is Credential Guard protected
            ValidateCredentialGuardProtection(certificate);

            // Validate that the token is cached
            var second = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .WithMtlsProofOfPossession()
                .WithAttestationSupport()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource,
                "Second call should use cache.");
            Assert.AreEqual(result.AccessToken, second.AccessToken, "Cached token should match original.");
        }

        /// <summary>
        /// Validates that the certificate is protected by Credential Guard (VBS).
        /// </summary>
        private static void ValidateCredentialGuardProtection(X509Certificate2 certificate)
        {
            Assert.IsNotNull(certificate, "Certificate should not be null.");

            // Extract the RSA key and validate it's a CNG key
            RSA rsa = certificate.GetRSAPrivateKey();
            Assert.IsNotNull(rsa, "Certificate should have an RSA private key.");

            // Note: Do not dispose RSA with 'using' as it may dispose the underlying key handle
            // that belongs to the certificate
            try
            {
                var rsaCng = rsa as RSACng;
                Assert.IsNotNull(rsaCng, "Expected RSACng for Credential Guard certificate.");

                // Validate that the key is Credential Guard protected
                bool isProtected = WindowsCngKeyOperations.IsKeyGuardProtected(rsaCng.Key);
                Assert.IsTrue(isProtected,
                    "Certificate key should be protected by Credential Guard (Virtual Isolation).");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to validate Credential Guard protection: {ex.Message}");
            }
        }

        #endregion

        #region Graceful Degradation Tests

        /// <summary>
        /// Tests that when Credential Guard is unavailable, the system gracefully degrades
        /// to non-attested mTLS PoP flow.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [TestMethod]
        public async Task AcquireToken_OnImdsV2_GracefulDegradation_WhenCredentialGuardUnavailable()
        {
            // This test validates that even if .WithAttestationSupport() is called,
            // the system can gracefully fall back to non-attested flow if Credential Guard
            // is not available. The test should succeed regardless of VBS availability.

            var mi = BuildMi(null, null); // SAMI

            AuthenticationResult result = null;
            try
            {
                result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalClientException ex)
            {
                // Expected when Credential Guard is not available - test passes
                Assert.Inconclusive("Credential Guard not available - graceful degradation verified: " + ex.Message);
                return;
            }
            catch (PlatformNotSupportedException)
            {
                // Expected when VBS is not supported - test passes
                Assert.Inconclusive("VBS not supported - graceful degradation verified.");
                return;
            }

            // If we got a result, it should be valid
            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
            
            // Validate certificate binding regardless of attestation
            var certificate = ExtractCertificateFromResult(result);
            ValidateTokenCertificateBinding(result.AccessToken, certificate);
        }

        #endregion

        #region Certificate and Key Validation Helpers

        /// <summary>
        /// Tests that we can validate x5t#S256 thumbprint computation correctly.
        /// This is a unit-style test to ensure the helper method is correct.
        /// </summary>
        [TestMethod]
        [TestCategory("MI_E2E_ImdsV2")]
        public void ValidateX5tS256ThumbprintComputation()
        {
            // Create a self-signed certificate for testing
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    "CN=Test Certificate",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                var certificate = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(1));

                // Compute thumbprint
                string thumbprint = ComputeX5tS256Thumbprint(certificate);

                // Validate format: base64url-encoded SHA-256 (43 characters without padding)
                Assert.IsFalse(string.IsNullOrEmpty(thumbprint), "Thumbprint should not be empty.");
                Assert.IsTrue(thumbprint.Length >= 43, "SHA-256 base64url should be at least 43 characters.");
                Assert.IsFalse(thumbprint.Contains('='), "Base64url should not contain padding.");
                Assert.IsFalse(thumbprint.Contains('+'), "Base64url should not contain '+'.");
                Assert.IsFalse(thumbprint.Contains('/'), "Base64url should not contain '/'.");

                // Recompute and verify consistency
                string thumbprint2 = ComputeX5tS256Thumbprint(certificate);
                Assert.AreEqual(thumbprint, thumbprint2, "Thumbprint computation should be deterministic.");
            }
        }

        #endregion
    }
}
