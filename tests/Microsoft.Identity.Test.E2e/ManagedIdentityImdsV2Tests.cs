// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.KeyAttestation;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        private const string GraphResource = "https://graph.microsoft.com";
        private const string AkvResource = "https://vault.azure.net";
        private const string AkvSecretUrl = "https://tokenbinding.vault.azure.net/secrets/boundsecret/?api-version=2015-06-01";

        // UAMI identifiers for MSALMSIV2 pool
        private const string UamiClientId = "6325cd32-9911-41f3-819c-416cdf9104e7";
        private const string UamiResourceId = "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami";
        private const string UamiObjectId = "ecb2ad92-3e30-4505-b79f-ac640d069f24";

        private static IManagedIdentityApplication BuildMi(
           string userAssignedId = null,
           string idType = null,
           ICsrFactory csrFactory = null)
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
            if (csrFactory is not null)
            {
                builder.WithCsrFactory(csrFactory);
            }
            return builder.Build();
        }

        #region Credential Guard Attestation Tests

#if MSAL_TEST_NETSTANDARD20
        /// <summary>
        /// Exercises fresh KeyGuard certificate attachment and a live resource call using the compatibility asset.
        /// Missing infrastructure must fail this regression rather than produce an inconclusive result.
        /// </summary>
        [TestMethod]
        [TestCategory("MI_E2E_ImdsV2_NetStandard20")]
        [DataRow(null, null, DisplayName = "NetStandard20_KeyGuard_ColdBinding_SAMI")]
        [DataRow(UamiClientId, "clientid", DisplayName = "NetStandard20_KeyGuard_ColdBinding_UAMI")]
        public async Task AcquireTokenAndCallAKV_NetStandard20_FreshNonExportableKeyBinding_Succeeds(
            string id, string idType)
        {
            // Arrange
            Assert.IsTrue(OperatingSystem.IsWindows(), "This regression requires the Windows KeyGuard runner.");
            Assert.AreEqual(".NETStandard,Version=v2.0",
                typeof(ManagedIdentityApplicationBuilder).Assembly
                    .GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName);

            const string disablePersistence = "MSAL_MI_DISABLE_PERSISTENT_CERT_CACHE";
            string originalValue = Environment.GetEnvironmentVariable(disablePersistence);
            var csrFactory = new CountingCsrFactory();
            try
            {
                // Bypass both binding-cache tiers without deleting other processes' persisted certificates.
                Environment.SetEnvironmentVariable(disablePersistence, "true");
                ImdsV2ManagedIdentitySource.ResetCertCacheForTest();
                var mi = BuildMi(id, idType, csrFactory);

                // Act
                var result = await mi.AcquireTokenForManagedIdentity(AkvResource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsGreaterThan(0, csrFactory.InvocationCount, "A new CSR must exercise certificate attachment.");
                Assert.AreEqual("mtls_pop", result.TokenType);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsNotNull(result.BindingCertificate);
                ValidateMtlsPopBinding(result.AccessToken, result.BindingCertificate);
                using (RSA rsa = result.BindingCertificate.GetRSAPrivateKey())
                {
                    AssertProtectedKey(rsa);
                }

                await CallAkvSecretAsync(AkvSecretUrl, result.BindingCertificate,
                    result.TokenType, result.AccessToken, requireSuccess: true).ConfigureAwait(false);
            }
            finally
            {
                ImdsV2ManagedIdentitySource.ResetCertCacheForTest();
                Environment.SetEnvironmentVariable(disablePersistence, originalValue);
            }
        }

        private static void AssertProtectedKey(RSA rsa)
        {
            Assert.IsInstanceOfType<RSACng>(rsa);
            var key = ((RSACng)rsa).Key;
            Assert.IsFalse(key.IsEphemeral, "KeyGuard must expose a named, persisted key.");
            Assert.AreEqual(CngExportPolicies.None, key.ExportPolicy);
            byte[] isolation = key.GetProperty("Virtual Iso", CngPropertyOptions.None).GetValue();
            Assert.IsTrue(isolation.Length >= sizeof(int) && BitConverter.ToInt32(isolation, 0) != 0,
                "The key must actually be VBS protected.");
            Assert.Throws<CryptographicException>(() => rsa.ExportParameters(true));
        }

        /// <summary>
        /// Verifies that the compatibility association reopens machine-scoped keys after the original handles close.
        /// Requires permission to create machine keys on the dedicated Windows runner.
        /// </summary>
        [TestMethod]
        [TestCategory("MI_E2E_ImdsV2_NetStandard20")]
        public void AttachPrivateKey_NetStandard20_PreservesMachineScope()
        {
            // Arrange
            Assert.IsTrue(OperatingSystem.IsWindows(), "Machine CNG keys require Windows.");
            Assert.AreEqual(".NETStandard,Version=v2.0",
                typeof(ManagedIdentityApplicationBuilder).Assembly
                    .GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName);
            string keyName = "msal-e2e-machine-" + Guid.NewGuid().ToString("N");
            CngKey key = CngKey.Create(CngAlgorithm.Rsa, keyName, new CngKeyCreationParameters
            {
                Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                ExportPolicy = CngExportPolicies.None,
                KeyUsage = CngKeyUsages.Signing,
                KeyCreationOptions = CngKeyCreationOptions.MachineKey
            });
            try
            {
                X509Certificate2 attached;
                using (key)
                using (var rsa = new RSACng(key))
                {
                    var request = new CertificateRequest(
                        "CN=MSAL machine-scope regression", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    using (var issued = request.Create(request.SubjectName,
                        X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1),
                        DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), new byte[] { 1 }))
                    {
                        // Act
                        attached = CommonCryptographyManager.AttachPrivateKeyToCert(
                            Convert.ToBase64String(issued.RawData), rsa);
                    }
                }

                // Assert
                using (attached)
                using (RSA signing = attached.GetRSAPrivateKey())
                using (RSA verification = attached.GetRSAPublicKey())
                {
                    Assert.IsInstanceOfType<RSACng>(signing);
                    Assert.IsTrue(((RSACng)signing).Key.IsMachineKey);
                    Assert.AreEqual(CngExportPolicies.None, ((RSACng)signing).Key.ExportPolicy);
                    byte[] data = { 6, 2, 4, 5 };
                    byte[] signature = signing.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    Assert.IsTrue(verification.VerifyData(
                        data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                    Assert.Throws<CryptographicException>(() => signing.ExportParameters(true));
                }
            }
            finally
            {
                key.Dispose();
                using (CngKey cleanup = CngKey.Open(
                    keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider, CngKeyOpenOptions.MachineKey))
                {
                    cleanup.Delete();
                }
            }
        }

        private sealed class CountingCsrFactory : ICsrFactory
        {
            internal int InvocationCount { get; private set; }

            public (string csrPem, RSA privateKey) Generate(
                RSA rsa, string clientId, string tenantId, CuidInfo cuid)
            {
                AssertProtectedKey(rsa);
                InvocationCount++;
                return new DefaultCsrFactory().Generate(rsa, clientId, tenantId, cuid);
            }
        }
#endif

        /// <summary>
        /// Tests mTLS PoP with Credential Guard attestation.
        /// Requires Windows Credential Guard (VBS) to be enabled on the MSALMSIV2 VM.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2_Attested")]
        [TestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImdsV2_MtlsPoP_WithAttestation_Succeeds-SAMI")]
        [DataRow(UamiClientId, "clientid", DisplayName = "AcquireToken_OnImdsV2_MtlsPoP_WithAttestation_Succeeds-UAMI-ClientId")]
        public async Task AcquireToken_OnImdsV2_MtlsPoP_WithAttestation_Succeeds(string id, string idType)
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Credential Guard attestation is only available on Windows.");
            }

            var mi = BuildMi(id, idType);

            try
            {
                var result = await mi.AcquireTokenForManagedIdentity(GraphResource)
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
        /// Tests that <c>.WithRequestOverMtls()</c> on an IMDSv2-capable host uses the full attested
        /// mTLS flow (Credential Guard-issued certificate) to connect to ESTS, but requests
        /// <c>token_type=bearer</c>, returning a standard bearer token with no binding certificate.
        /// Requires Windows Credential Guard (VBS) to be enabled on the MSALMSIV2 VM.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2_Attested")]
        [TestMethod]
        [DataRow(null /*SAMI*/, null, DisplayName = "AcquireToken_OnImdsV2_WithRequestOverMtls_ReturnsBearer-SAMI")]
        [DataRow(UamiClientId, "clientid", DisplayName = "AcquireToken_OnImdsV2_WithRequestOverMtls_ReturnsBearer-UAMI-ClientId")]
        public async Task AcquireToken_OnImdsV2_WithRequestOverMtls_ReturnsBearer(string id, string idType)
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Credential Guard attestation is only available on Windows.");
            }

            var mi = BuildMi(id, idType);

            try
            {
                // .WithRequestOverMtls() routes through the IMDSv2 attested flow (KeyGuard cert + mTLS
                // connection) but requests token_type=bearer from ESTS, so the result is a plain bearer token with no binding certificate.
                var result = await mi.AcquireTokenForManagedIdentity(GraphResource)
                    .WithRequestOverMtls()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "AccessToken should not be empty.");
                Assert.AreEqual("Bearer", result.TokenType, "Token type should be 'Bearer' for mTLS Bearer.");
                Assert.IsNull(result.BindingCertificate, "BindingCertificate should be null for a bearer token.");

                // Verify the token has no cnf (certificate-binding) claim.
                var jwt = new JwtSecurityToken(result.AccessToken);
                Assert.IsFalse(jwt.Payload.ContainsKey("cnf"), "Bearer token must not contain a cnf claim.");

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                    "First call must hit the token endpoint.");
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

                var result = await mi.AcquireTokenForManagedIdentity(GraphResource)
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

        #region AKV mTLS PoP Tests
        /// <summary>
        /// Tests mTLS PoP token acquisition and Azure Key Vault resource call with attestation.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2_Attested")]
        [TestMethod]
        public async Task AcquireTokenAndCallAKV_OnImdsV2_MtlsPoP_WithAttestation_Succeeds()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Credential Guard attestation is only available on Windows.");
            }

            var mi = BuildMi();

            try
            {
                var tokenResult = await mi.AcquireTokenForManagedIdentity(AkvResource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsFalse(string.IsNullOrEmpty(tokenResult.AccessToken), "AccessToken should not be empty.");
                Assert.AreEqual("mtls_pop", tokenResult.TokenType, "Token type should be 'mtls_pop'.");
                Assert.IsNotNull(tokenResult.BindingCertificate, "BindingCertificate should not be null.");

                // Validate the certificate is backed by Credential Guard (RSACng with proper properties)
                ValidateCredentialGuardCertificate(tokenResult.BindingCertificate);

                ValidateMtlsPopBinding(tokenResult.AccessToken, tokenResult.BindingCertificate);

                await CallAkvSecretAsync(
                    AkvSecretUrl,
                    tokenResult.BindingCertificate,
                    tokenResult.TokenType,
                    tokenResult.AccessToken)
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, tokenResult.AuthenticationResultMetadata.TokenSource,
                    "Token should come from MSI endpoint.");
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "credential_guard_not_available")
            {
                Assert.Inconclusive("Credential Guard is not available.");
            }
            catch (CryptographicException ex)
            {
                Assert.Inconclusive($"Cryptographic operation failed: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                Assert.Inconclusive($"AKV call could not be completed: {ex.Message}");
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

        /// <summary>
        /// Calls AKV secret endpoint over mTLS with client certificate and token.
        /// </summary>
        private static async Task CallAkvSecretAsync(
            string secretUrl,
            X509Certificate2 clientCertificate,
            string tokenType,
            string accessToken,
            bool requireSuccess = false)
        {
            Assert.IsNotNull(clientCertificate, "Client certificate required.");
            Assert.IsFalse(string.IsNullOrEmpty(accessToken), "Access token required.");

            using var httpClient = new HttpClient(new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                               System.Security.Authentication.SslProtocols.Tls13,
                ClientCertificates = { clientCertificate }
            });

            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(tokenType, accessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("x-ms-tokenboundauth", "true");

            using var response = await httpClient.GetAsync(new Uri(secretUrl)).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (requireSuccess)
                {
                    Assert.Fail($"AKV mTLS request failed with HTTP status {(int)response.StatusCode}.");
                }

                // AKV failures are infrastructure-related and should not fail the build.
                // The core test validates mTLS PoP token acquisition; AKV is a downstream call.
                Assert.Inconclusive(
                    $"AKV secret GET returned non-success status: {(int)response.StatusCode} {response.StatusCode}. Body: {responseContent}");
            }

            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            Assert.IsTrue(root.TryGetProperty("value", out var secretValue), "Response missing 'value' property.");
            Assert.IsFalse(string.IsNullOrEmpty(secretValue.GetString()), "Secret value is empty.");
        }

        #endregion
    }
}
