// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E
{
    /// <summary>
    /// End-to-end tests for IMDSv2 Managed Identity with mTLS Proof-of-Possession.
    /// 
    /// Test Infrastructure Requirements:
    /// - Azure VM or agent with IMDSv2 endpoint enabled
    /// - System-assigned managed identity (SAMI)
    /// - User-assigned managed identity (UAMI) with appropriate client ID, resource ID, and object ID
    /// - For attested tests: Windows Credential Guard (formerly KeyGuard) enabled
    /// 
    /// Test Categories:
    /// - MI_E2E_ImdsV2: Basic IMDSv2 tests (bearer and mTLS PoP)
    /// - MI_E2E_ImdsV2_Attested: Tests requiring Credential Guard attestation
    /// </summary>
    [TestClass]
    public class ManagedIdentityImdsV2Tests
    {
        private const string ArmScope = "https://management.azure.com";
        private const string GraphScope = "https://graph.microsoft.com";

        // UAMI test identities - update these to match the test environment (copied from ImdsV1 tests)
        private const string TestUamiClientId = "8ef2ae5a-f349-4d36-bc0e-a567f2cc50f7";
        private const string TestUamiResourceId = "/subscriptions/6f52c299-a200-4fe1-8822-a3b61cf1f931/resourcegroups/DevOpsHostedAgents/providers/Microsoft.ManagedIdentity/userAssignedIdentities/ID4SMSIHostedAgent_UAMI";
        private const string TestUamiObjectId = "0651a6fc-fbf5-4904-9e48-16f63ec1f2b1";

        /// <summary>
        /// Specifies the type of user-assigned managed identity identifier.
        /// </summary>
        private enum UamiIdType
        {
            ClientId,
            ResourceId,
            ObjectId
        }

        private static IManagedIdentityApplication BuildMi(
            string userAssignedId = null,
            UamiIdType? idType = null)
        {
            ManagedIdentityId miId = userAssignedId is null
                ? ManagedIdentityId.SystemAssigned
                : idType switch
                {
                    UamiIdType.ClientId => ManagedIdentityId.WithUserAssignedClientId(userAssignedId),
                    UamiIdType.ResourceId => ManagedIdentityId.WithUserAssignedResourceId(userAssignedId),
                    UamiIdType.ObjectId => ManagedIdentityId.WithUserAssignedObjectId(userAssignedId),
                    _ => throw new ArgumentOutOfRangeException(nameof(idType))
                };

            var builder = ManagedIdentityApplicationBuilder.Create(miId);
            builder.Config.AccessorOptions = null;
            return builder.Build();
        }

        #region Bearer Token Tests (Non-PoP)

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "IMDSv2_Bearer_SAMI")]
        [DataRow(TestUamiClientId, UamiIdType.ClientId, DisplayName = "IMDSv2_Bearer_UAMI_ClientId")]
        [DataRow(TestUamiResourceId, UamiIdType.ResourceId, DisplayName = "IMDSv2_Bearer_UAMI_ResourceId")]
        [DataRow(TestUamiObjectId, UamiIdType.ObjectId, DisplayName = "IMDSv2_Bearer_UAMI_ObjectId")]
        public async Task AcquireToken_ImdsV2_Bearer_Succeeds(string id, UamiIdType? idType)
        {
            var mi = BuildMi(id, idType);

            // First call (should hit IDP)
            var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                "First call must hit MSI endpoint.");
            Assert.IsNull(result.BindingCertificate, "Bearer token should not have binding certificate.");

            // Second call (should use cache)
            var cachedResult = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, cachedResult.AuthenticationResultMetadata.TokenSource,
                "Second call should use cache.");
            Assert.AreEqual(result.AccessToken, cachedResult.AccessToken,
                "Cached token should match original token.");
        }

        #endregion

        #region mTLS PoP Tests (Non-Attested)

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [DataTestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "IMDSv2_MtlsPoP_NonAttested_SAMI")]
        [DataRow(TestUamiClientId, UamiIdType.ClientId, DisplayName = "IMDSv2_MtlsPoP_NonAttested_UAMI")]
        public async Task AcquireToken_ImdsV2_MtlsPoP_NonAttested_Succeeds(string id, UamiIdType? idType)
        {
            var mi = BuildMi(id, idType);

            var result = await mi.AcquireTokenForManagedIdentity(GraphScope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Token basics
            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                "First call must hit MSI endpoint.");

            // mTLS PoP specifics
            Assert.AreEqual("mtls_pop", result.TokenType, "Token type should be mtls_pop.");
            Assert.IsNotNull(result.BindingCertificate, "PoP token must have binding certificate.");
            Assert.IsTrue(result.BindingCertificate.HasPrivateKey, "Binding certificate must have private key.");

            // Binding verification (cnf claim matches certificate)
            VerifyMtlsPopBinding(result);
        }

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [TestMethod]
        public async Task ImdsV2_MtlsPoP_Certificate_IsCached()
        {
            var mi = BuildMi(null, null); // SAMI

            // First call (creates certificate)
            var result1 = await mi.AcquireTokenForManagedIdentity(GraphScope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            string cert1Thumbprint = result1.BindingCertificate.Thumbprint;

            // Second call (should reuse certificate)
            var result2 = await mi.AcquireTokenForManagedIdentity(GraphScope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            string cert2Thumbprint = result2.BindingCertificate.Thumbprint;

            //  Same certificate is reused
            Assert.AreEqual(cert1Thumbprint, cert2Thumbprint,
                "Certificate should be cached and reused between calls.");

            // Force refresh (may create new certificate depending on cert lifetime)
            var result3 = await mi.AcquireTokenForManagedIdentity(GraphScope)
                .WithMtlsProofOfPossession()
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Force refresh still works
            Assert.IsNotNull(result3.BindingCertificate, "Force refresh should still provide certificate.");
            Assert.AreEqual(TokenSource.IdentityProvider, result3.AuthenticationResultMetadata.TokenSource,
                "Force refresh should hit IDP.");
        }

        #endregion

        #region mTLS PoP with Attestation Tests (Credential Guard)

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2_Attested")]
        [TestMethod]
        public async Task AcquireToken_ImdsV2_MtlsPoP_Attested_WithCredentialGuard_Succeeds()
        {
            // This test requires Windows Credential Guard (VBS) to be enabled
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("This test requires Windows.");
            }

            var mi = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.SystemAssigned)
                .Build();

            var result = await mi.AcquireTokenForManagedIdentity(GraphScope)
                .WithMtlsProofOfPossession()
                .WithAttestationSupport()
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Token basics
            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
            Assert.AreEqual("mtls_pop", result.TokenType, "Token type should be mtls_pop.");
            Assert.IsNotNull(result.BindingCertificate, "PoP token must have binding certificate.");

            // Certificate is Credential Guard-backed
            using var rsa = result.BindingCertificate.GetRSAPrivateKey();
            Assert.IsNotNull(rsa, "Certificate must have RSA private key.");
            Assert.IsInstanceOfType(rsa, typeof(RSACng), "Credential Guard requires RSACng.");

            var rsaCng = (RSACng)rsa;
            // Note: This is a simple check. Full Credential Guard verification would require
            // checking the key's storage flags and VBS protection status.
            Assert.IsNotNull(rsaCng.Key, "RSACng key handle should be available.");

            // Binding verification
            VerifyMtlsPopBinding(result);
        }

        #endregion

        #region Non-Attested Flow (Graceful Degradation)

        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [TestMethod]
        public async Task ImdsV2_MtlsPoP_WithoutCredentialGuard_FallsBackToNonAttested()
        {
            // This test verifies that when Credential Guard is not available,
            // the flow gracefully falls back to non-attested mTLS PoP

            var mi = BuildMi(null, null); // SAMI

            // Request attestation support (but may not be available)
            var result = await mi.AcquireTokenForManagedIdentity(GraphScope)
                .WithMtlsProofOfPossession()
                .WithAttestationSupport()
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Token is still acquired (non-attested or attested depending on environment)
            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
            Assert.AreEqual("mtls_pop", result.TokenType, "Token type should be mtls_pop.");
            Assert.IsNotNull(result.BindingCertificate, "PoP token must have binding certificate.");

            // Binding works regardless of attestation
            VerifyMtlsPopBinding(result);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Verifies that the mTLS PoP token's cnf claim matches the binding certificate's hash.
        /// This ensures the token is properly bound to the certificate.
        /// </summary>
        private static void VerifyMtlsPopBinding(AuthenticationResult result)
        {
            Assert.IsNotNull(result.BindingCertificate, "Binding certificate must be present.");
            Assert.AreEqual("mtls_pop", result.TokenType, "Token type must be mtls_pop.");

            // Extract cnf.x5t#S256 from token
            string cnfFromToken = ExtractCnfX5tS256FromToken(result.AccessToken);
            Assert.IsNotNull(cnfFromToken, "Token must contain cnf.x5t#S256 claim.");

            // Compute x5t#S256 from certificate
            string certHash = ComputeCertX5tS256(result.BindingCertificate);

            // Verify they match
            Assert.AreEqual(certHash, cnfFromToken,
                "Token cnf.x5t#S256 must match certificate SHA-256 hash (Base64Url-encoded).");
        }

        /// <summary>
        /// Extracts the cnf.x5t#S256 claim from a JWT access token.
        /// </summary>
        private static string ExtractCnfX5tS256FromToken(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2)
                    return null;

                // Decode payload (Base64Url)
                var payload = parts[1];
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var jsonBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                var json = Encoding.UTF8.GetString(jsonBytes);

                // Parse JSON and extract cnf.x5t#S256
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("cnf", out var cnf) &&
                    cnf.TryGetProperty("x5t#S256", out var x5t))
                {
                    return x5t.GetString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Computes the x5t#S256 (certificate SHA-256 hash, Base64Url-encoded) for a certificate.
        /// </summary>
        private static string ComputeCertX5tS256(X509Certificate2 cert)
        {
            var hash = System.Security.Cryptography.SHA256.HashData(cert.RawData);
            return ToBase64Url(hash);
        }

        /// <summary>
        /// Converts byte array to Base64Url encoding (no padding, uses - and _).
        /// </summary>
        private static string ToBase64Url(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        #endregion
    }
}
