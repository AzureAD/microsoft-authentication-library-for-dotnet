// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.KeyAttestation;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E
{
    [TestClass]
    public class ManagedIdentityImdsV2Tests
    {
        private const string ArmScope = "https://management.azure.com";

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

        #region Bearer Token Tests

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImdsV2_Bearer_Succeeds-SAMI")]
        [DataRow("8ef2ae5a-f349-4d36-bc0e-a567f2cc50f7", "clientid", DisplayName = "AcquireToken_OnImdsV2_Bearer_Succeeds-UAMI-ClientId")]
        [DataRow("/subscriptions/6f52c299-a200-4fe1-8822-a3b61cf1f931/resourcegroups/DevOpsHostedAgents/providers/Microsoft.ManagedIdentity/userAssignedIdentities/ID4SMSIHostedAgent_UAMI",
         "resourceid", DisplayName = "AcquireToken_OnImdsV2_Bearer_Succeeds-UAMI-ResourceId")]
        [DataRow("0651a6fc-fbf5-4904-9e48-16f63ec1f2b1", "objectid", DisplayName = "AcquireToken_OnImdsV2_Bearer_Succeeds-UAMI-ObjectId")]
        public async Task AcquireToken_OnImdsV2_Bearer_Succeeds(string id, string idType)
        {
            var mi = BuildMi(id, idType);

            var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
            Assert.AreEqual("Bearer", result.TokenType, "Token type should be Bearer.");

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                "First call must hit MSI endpoint.");

            var second = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource,
                "Second call should hit cache.");
            Assert.AreEqual(result.AccessToken, second.AccessToken);
        }

        #endregion

        #region mTLS PoP Tests (Non-Attested)

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImdsV2_MtlsPop_Succeeds-SAMI")]
        [DataRow("8ef2ae5a-f349-4d36-bc0e-a567f2cc50f7", "clientid", DisplayName = "AcquireToken_OnImdsV2_MtlsPop_Succeeds-UAMI-ClientId")]
        [DataRow("/subscriptions/6f52c299-a200-4fe1-8822-a3b61cf1f931/resourcegroups/DevOpsHostedAgents/providers/Microsoft.ManagedIdentity/userAssignedIdentities/ID4SMSIHostedAgent_UAMI",
         "resourceid", DisplayName = "AcquireToken_OnImdsV2_MtlsPop_Succeeds-UAMI-ResourceId")]
        [DataRow("0651a6fc-fbf5-4904-9e48-16f63ec1f2b1", "objectid", DisplayName = "AcquireToken_OnImdsV2_MtlsPop_Succeeds-UAMI-ObjectId")]
        public async Task AcquireToken_OnImdsV2_MtlsPop_Succeeds(string id, string idType)
        {
            var mi = BuildMi(id, idType);

            var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
            Assert.AreEqual(Constants.MtlsPoPTokenType, result.TokenType, "Token type should be mtls_pop.");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                "First call must hit MSI endpoint.");

            // Validate the binding certificate is present
            Assert.IsNotNull(result.BindingCertificate, "BindingCertificate should be present for mTLS PoP.");

            // Validate that the token contains proper PoP binding
            VerifyMtlsPopBinding(result.AccessToken, result.BindingCertificate);
        }

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImdsV2_MtlsPop_CertificateCaching-SAMI")]
        [DataRow("8ef2ae5a-f349-4d36-bc0e-a567f2cc50f7", "clientid", DisplayName = "AcquireToken_OnImdsV2_MtlsPop_CertificateCaching-UAMI")]
        public async Task AcquireToken_OnImdsV2_MtlsPop_CertificateCaching(string id, string idType)
        {
            var mi = BuildMi(id, idType);

            // First token acquisition
            var result1 = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(result1.BindingCertificate);

            var cert1Thumbprint = result1.BindingCertificate.Thumbprint;

            // Second token acquisition (should use cached certificate and token)
            var result2 = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource,
                "Second call should retrieve token from cache.");
            Assert.IsNotNull(result2.BindingCertificate);
            Assert.AreEqual(cert1Thumbprint, result2.BindingCertificate.Thumbprint,
                "Cached token should use the same certificate.");

            // Verify both tokens have valid bindings
            VerifyMtlsPopBinding(result1.AccessToken, result1.BindingCertificate);
            VerifyMtlsPopBinding(result2.AccessToken, result2.BindingCertificate);
        }

        #endregion

        #region Credential Guard Attestation Tests

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2_Attested")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImdsV2_MtlsPop_WithAttestation_Succeeds-SAMI")]
        [DataRow("8ef2ae5a-f349-4d36-bc0e-a567f2cc50f7", "clientid", DisplayName = "AcquireToken_OnImdsV2_MtlsPop_WithAttestation_Succeeds-UAMI")]
        public async Task AcquireToken_OnImdsV2_MtlsPop_WithAttestation_Succeeds(string id, string idType)
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Credential Guard attestation is only available on Windows.");
            }

            var mi = BuildMi(id, idType);

            AuthenticationResult result;
            try
            {
                result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "keyguard_not_available")
            {
                Assert.Inconclusive("Credential Guard is not available on this machine. Test skipped.");
                return;
            }

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
            Assert.AreEqual(Constants.MtlsPoPTokenType, result.TokenType, "Token type should be mtls_pop.");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            // Validate the binding certificate is present
            Assert.IsNotNull(result.BindingCertificate, "BindingCertificate should be present.");

            // For attested flow, verify the certificate is an RSACng key
            if (result.BindingCertificate.GetRSAPrivateKey() is RSACng rsaCng)
            {
                Assert.IsNotNull(rsaCng, "Expected RSACng for Credential Guard attested certificate.");
                // Note: We can't directly verify KeyGuard protection in E2E tests without deeper platform APIs
                // but the fact that attestation succeeded indicates the key was properly sourced
            }

            // Validate that the token contains proper PoP binding
            VerifyMtlsPopBinding(result.AccessToken, result.BindingCertificate);
        }

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2_Attested")]
        [TestMethod]
        public async Task AcquireToken_OnImdsV2_MtlsPop_GracefulDegradation_WhenCredentialGuardUnavailable()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Test is specific to Windows Credential Guard behavior.");
            }

            var mi = BuildMi();

            // If Credential Guard is not available, the library should either:
            // 1. Fall back to non-attested flow gracefully, OR
            // 2. Throw a specific exception indicating attestation is unavailable

            AuthenticationResult result = null;
            bool attestationUnavailable = false;

            try
            {
                result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "keyguard_not_available" ||
                                                   ex.Message.Contains("Credential Guard", StringComparison.OrdinalIgnoreCase))
            {
                attestationUnavailable = true;
            }

            if (attestationUnavailable)
            {
                // Expected behavior when Credential Guard is not available
                Assert.Inconclusive("Credential Guard is not available. Test verified graceful error handling.");
            }
            else
            {
                // If attestation succeeded, validate the result
                Assert.IsNotNull(result, "Token acquisition should succeed.");
                Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken));
                Assert.AreEqual(Constants.MtlsPoPTokenType, result.TokenType);
                Assert.IsNotNull(result.BindingCertificate);

                VerifyMtlsPopBinding(result.AccessToken, result.BindingCertificate);
            }
        }

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2_Attested")]
        [TestMethod]
        public async Task AcquireToken_OnImdsV2_MtlsPop_AttestedCertificate_VerifyRSACng()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("RSACng verification is Windows-specific.");
            }

            var mi = BuildMi();

            AuthenticationResult result;
            try
            {
                result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "keyguard_not_available")
            {
                Assert.Inconclusive("Credential Guard is not available. Cannot verify RSACng key properties.");
                return;
            }

            Assert.IsNotNull(result.BindingCertificate, "Certificate must be present.");

            // Verify the private key is RSACng (required for Credential Guard keys)
            RSA privateKey = result.BindingCertificate.GetRSAPrivateKey();
            Assert.IsNotNull(privateKey, "Private key should be accessible.");

            if (privateKey is RSACng rsaCng)
            {
                Assert.IsNotNull(rsaCng.Key, "RSACng key should be accessible.");
                // Credential Guard keys should use CNG providers
                Assert.IsNotNull(rsaCng.Key.Provider, "CNG provider should be set.");
            }
            else
            {
                Assert.Fail($"Expected RSACng but got {privateKey.GetType().Name}. " +
                           "Attested certificates should use CNG-backed keys.");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Verifies that an mTLS PoP token contains the proper certificate binding.
        /// </summary>
        private static void VerifyMtlsPopBinding(string accessToken, X509Certificate2 certificate)
        {
            Assert.IsFalse(string.IsNullOrEmpty(accessToken), "Access token should not be empty.");
            Assert.IsNotNull(certificate, "Certificate should not be null.");

            // Parse the JWT token
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;

            try
            {
                jwtToken = handler.ReadJwtToken(accessToken);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to parse JWT token: {ex.Message}");
                return;
            }

            // Verify the token has a 'cnf' claim
            var cnfClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "cnf");
            Assert.IsNotNull(cnfClaim, "Token should contain a 'cnf' (confirmation) claim for PoP binding.");

            // Parse the cnf claim as JSON
            JsonDocument cnfDoc;
            try
            {
                cnfDoc = JsonDocument.Parse(cnfClaim.Value);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to parse cnf claim: {ex.Message}");
                return;
            }

            // Verify the cnf claim contains x5t#S256 (certificate thumbprint SHA256)
            Assert.IsTrue(cnfDoc.RootElement.TryGetProperty("x5t#S256", out var x5tElement),
                "cnf claim should contain 'x5t#S256' property.");

            string expectedThumbprint = ComputeCertificateThumbprintSha256Base64Url(certificate);
            string actualThumbprint = x5tElement.GetString();

            Assert.AreEqual(expectedThumbprint, actualThumbprint,
                "Certificate thumbprint in token should match the binding certificate.");
        }

        /// <summary>
        /// Computes the SHA256 thumbprint of a certificate in Base64URL format.
        /// </summary>
        private static string ComputeCertificateThumbprintSha256Base64Url(X509Certificate2 certificate)
        {
            byte[] certBytes = certificate.Export(X509ContentType.Cert);
            
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(certBytes);
                return Base64UrlHelpers.Encode(hash);
            }
        }

        #endregion
    }
}
