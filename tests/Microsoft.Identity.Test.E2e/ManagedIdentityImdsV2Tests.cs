// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.KeyAttestation;
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
    /// Comprehensive E2E tests for IMDSv2 Managed Identity with mTLS Proof-of-Possession (PoP) support.
    /// These tests run on the MSALMSIV2 pool which has IMDSv2 endpoint configured.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ManagedIdentityImdsV2Tests
    {
        private const string ArmScope = "https://graph.microsoft.com";

        // UAMI identifiers for MSALMSIV2 pool
        private const string UamiClientId = "6325cd32-9911-41f3-819c-416cdf9104e7";
        private const string UamiResourceId = "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami";
        private const string UamiObjectId = "ecb2ad92-3e30-4505-b79f-ac640d069f24";

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

        #region Credential Guard Attestation Tests

        /// <summary>
        /// Tests mTLS PoP with Credential Guard attestation.
        /// Requires Windows Credential Guard (VBS) to be enabled on the MSALMSIV2 VM.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2_Attested")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImdsV2_MtlsPoP_WithAttestation_Succeeds-SAMI")]
        [DataRow(UamiClientId, "clientid", DisplayName = "AcquireToken_OnImdsV2_MtlsPoP_WithAttestation_Succeeds-UAMI-ClientId")]
        public async Task AcquireToken_OnImdsV2_MtlsPoP_WithAttestation_Succeeds(string id, string idType)
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Credential Guard attestation is only available on Windows.");
            }

            // Check if TOKEN_ATTESTATION_ENDPOINT is configured (required for attestation)
            var attestationEndpoint = Environment.GetEnvironmentVariable("TOKEN_ATTESTATION_ENDPOINT");
            if (string.IsNullOrWhiteSpace(attestationEndpoint))
            {
                Assert.Inconclusive("TOKEN_ATTESTATION_ENDPOINT is not configured. Attestation tests require this environment variable.");
            }

            var mi = BuildMi(id, idType);

            try
            {
                var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
                Assert.AreEqual("mtls_pop", result.TokenType, "Token type should be 'mtls_pop' for mTLS PoP flow.");
                Assert.IsNotNull(result.BindingCertificate, "BindingCertificate should not be null for PoP token.");

                // Validate the certificate is backed by Credential Guard (RSACng with proper properties)
                ValidateCredentialGuardCertificate(result.BindingCertificate);

                // Validate the token-certificate binding
                ValidateMtlsPopBinding(result.AccessToken, result.BindingCertificate);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                    "First call must hit MSI endpoint.");
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "credential_guard_not_available")
            {
                Assert.Inconclusive("Credential Guard is not available on this machine. Ensure VBS and Credential Guard are enabled.");
            }
            catch (CryptographicException ex)
            {
                Assert.Inconclusive($"Cryptographic operation failed. Credential Guard may not be properly configured: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests graceful degradation when Credential Guard is not available.
        /// Should fall back to non-attested mTLS PoP flow.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [TestMethod]
        public async Task AcquireToken_OnImdsV2_MtlsPoP_GracefulDegradation_WhenCredentialGuardUnavailable()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("This test is only applicable on Windows.");
            }

            var mi = BuildMi();

            // When attestation endpoint is not configured, should fall back to non-attested flow
            var originalEndpoint = Environment.GetEnvironmentVariable("TOKEN_ATTESTATION_ENDPOINT");
            try
            {
                // Temporarily clear the endpoint to simulate unavailability
                Environment.SetEnvironmentVariable("TOKEN_ATTESTATION_ENDPOINT", null);

                var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
                Assert.AreEqual("mtls_pop", result.TokenType, "Token type should be 'mtls_pop' for mTLS PoP flow.");
                Assert.IsNotNull(result.BindingCertificate, "BindingCertificate should not be null.");

                // Should succeed with non-attested certificate
                ValidateMtlsPopBinding(result.AccessToken, result.BindingCertificate);
            }
            finally
            {
                // Restore original endpoint
                Environment.SetEnvironmentVariable("TOKEN_ATTESTATION_ENDPOINT", originalEndpoint);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validates that the access token properly binds to the certificate via the cnf claim.
        /// </summary>
        private static void ValidateMtlsPopBinding(string accessToken, X509Certificate2 bindingCertificate)
        {
            Assert.IsNotNull(accessToken, "Access token should not be null.");
            Assert.IsNotNull(bindingCertificate, "Binding certificate should not be null.");

            // Parse the JWT
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(accessToken);

            // Extract the cnf (confirmation) claim
            var cnfClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "cnf");
            Assert.IsNotNull(cnfClaim, "Access token should contain 'cnf' claim for certificate binding.");

            // Parse the cnf claim value (should be JSON with x5t#S256 thumbprint)
            var cnfJson = System.Text.Json.JsonDocument.Parse(cnfClaim.Value);
            Assert.IsTrue(cnfJson.RootElement.TryGetProperty("x5t#S256", out var x5tElement),
                "cnf claim should contain 'x5t#S256' property.");

            var tokenThumbprint = x5tElement.GetString();
            Assert.IsFalse(string.IsNullOrEmpty(tokenThumbprint), "x5t#S256 thumbprint should not be empty.");

            // Compute the expected thumbprint from the certificate
            var expectedThumbprint = ComputeX5tS256Thumbprint(bindingCertificate);

            Assert.AreEqual(expectedThumbprint, tokenThumbprint,
                "Token's x5t#S256 thumbprint should match the computed certificate thumbprint.");
        }

        /// <summary>
        /// Computes the x5t#S256 thumbprint of a certificate per RFC 8705.
        /// This is the base64url-encoded SHA-256 hash of the DER-encoded X.509 certificate.
        /// </summary>
        private static string ComputeX5tS256Thumbprint(X509Certificate2 certificate)
        {
            // Per RFC 8705 Section 3.1, compute SHA-256 hash of the DER-encoded certificate (RawData)
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(certificate.RawData);
                return Base64UrlEncode(hash);
            }
        }

        /// <summary>
        /// Encodes a byte array to base64url format (URL-safe base64 without padding).
        /// </summary>
        private static string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            // Convert to base64url by replacing characters and removing padding
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        /// <summary>
        /// Validates that a certificate is backed by Credential Guard (RSACng with VBS protection).
        /// </summary>
        private static void ValidateCredentialGuardCertificate(X509Certificate2 certificate)
        {
            Assert.IsNotNull(certificate, "Certificate should not be null.");

            // Get the private key as RSA
            // Note: GetRSAPrivateKey() returns an RSA instance that should not be disposed
            // as it may dispose the underlying key handle that belongs to the certificate
            var rsa = certificate.GetRSAPrivateKey();
            Assert.IsNotNull(rsa, "Certificate should have an RSA private key.");

            // Check if it's an RSACng (required for Credential Guard)
            if (rsa is RSACng rsaCng)
            {
                // Verify the key is protected by Virtual Isolation (Credential Guard)
                var key = rsaCng.Key;
                Assert.IsNotNull(key, "CNG key should not be null.");

                try
                {
                    // Check for Virtual Iso property (indicates Credential Guard protection)
                    var virtualIsoProp = key.GetProperty("Virtual Iso", CngPropertyOptions.None);
                    var bytes = virtualIsoProp.GetValue();
                    
                    // Check if the property indicates Virtual Isolation is enabled
                    // The property is typically a DWORD (4 bytes), non-zero means VBS-protected
                    bool isVirtualIso = false;
                    if (bytes != null && bytes.Length >= 4)
                    {
                        // Use ReadOnlySpan for safer byte reading on supported platforms
#if NET8_0_OR_GREATER
                        isVirtualIso = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes) != 0;
#else
                        // On older platforms, use BitConverter with explicit little-endian handling
                        // Create a copy to avoid mutating the original bytes array
                        if (!BitConverter.IsLittleEndian)
                        {
                            var reversedBytes = (byte[])bytes.Clone();
                            Array.Reverse(reversedBytes, 0, 4);
                            isVirtualIso = BitConverter.ToInt32(reversedBytes, 0) != 0;
                        }
                        else
                        {
                            isVirtualIso = BitConverter.ToInt32(bytes, 0) != 0;
                        }
#endif
                    }

                    if (!isVirtualIso)
                    {
                        // If not Virtual Iso, test is inconclusive rather than failing
                        Assert.Inconclusive("Certificate key is not protected by Credential Guard. VBS may not be enabled.");
                    }
                }
                catch (CryptographicException)
                {
                    // Property not available - Credential Guard not active
                    Assert.Inconclusive("Virtual Iso property not available. Credential Guard may not be active.");
                }
            }
            else
            {
                // Not an RSACng - could be software-backed fallback
                Assert.Inconclusive($"Certificate uses {rsa.GetType().Name} instead of RSACng. May be using fallback provider.");
            }
        }

        #endregion
    }
}
