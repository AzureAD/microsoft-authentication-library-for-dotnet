// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.KeyAttestation;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.E2E
{
    /// <summary>
    /// E2E tests for FIC (Federated Identity Credential) two-leg token exchange using MSIv2.
    ///
    /// Flow:
    ///   Leg 1 — MSI acquires an mTLS PoP token for api://AzureADTokenExchange
    ///   Leg 2 — ConfApp uses the Leg 1 token as a ClientSignedAssertion to obtain either a bearer token
    ///           or an mTLS PoP token, toggled by .WithMtlsProofOfPossession() on the Leg 2 request
    ///
    /// These tests run on the MSALMSIV2 pool (IMDSv2 + Credential Guard).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ManagedIdentityImdsV2FicTests
    {
        private const string TokenExchangeResource = "api://AzureADTokenExchange";
        private const string GraphScope = "https://graph.microsoft.com/.default";

        // UAMI identifiers (same pool as ManagedIdentityImdsV2Tests)
        private const string UamiClientId = "6325cd32-9911-41f3-819c-416cdf9104e7";

        // ConfApp registered in the MSI team tenant with FIC trusting the MSALMSIV2 pool MSI
        private const string FicConfAppClientId = "f62c5ae3-bf3a-4af5-afa8-a68b800396e9";
        private const string FicConfAppAuthority = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47";

        private static IManagedIdentityApplication BuildMsi(string userAssignedClientId = null)
        {
            ManagedIdentityId miId = userAssignedClientId is null
                ? ManagedIdentityId.SystemAssigned
                : ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId);

            var builder = ManagedIdentityApplicationBuilder.Create(miId);
            builder.Config.AccessorOptions = null;
            return builder.Build();
        }

        private static IConfidentialClientApplication BuildConfApp(AuthenticationResult leg1Result)
        {
            // Pass the Leg 1 binding certificate on the assertion so Leg 2 can prove possession of the
            // Leg 1 mTLS PoP token. The Leg 2 token type is chosen on the request, not here:
            //   - without .WithMtlsProofOfPossession() on AcquireTokenForClient  -> Bearer
            //   - with    .WithMtlsProofOfPossession() on AcquireTokenForClient  -> mtls_pop
            return ConfidentialClientApplicationBuilder
                .Create(FicConfAppClientId)
                .WithAuthority(FicConfAppAuthority)
                .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                .WithClientAssertion((_, ct) => Task.FromResult(new ClientSignedAssertion
                {
                    Assertion = leg1Result.AccessToken,
                    TokenBindingCertificate = leg1Result.BindingCertificate
                }))
                .Build();
        }

        /// <summary>
        /// Leg 1 — MSI acquires an mTLS PoP token for api://AzureADTokenExchange using Credential Guard attestation.
        /// Shared by the Bearer and mTLS PoP Leg 2 tests. The returned BindingCertificate is what Leg 2 uses to
        /// prove possession. Marks the test inconclusive when Credential Guard is unavailable.
        /// </summary>
        private static async Task<AuthenticationResult> AcquireLeg1MtlsPopTokenAsync(string uamiClientId)
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Credential Guard attestation is only available on Windows.");
            }

            var msiApp = BuildMsi(uamiClientId);

            AuthenticationResult leg1Result;
            try
            {
                leg1Result = await msiApp
                    .AcquireTokenForManagedIdentity(TokenExchangeResource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "credential_guard_not_available")
            {
                Assert.Inconclusive("Credential Guard is not available on this machine.");
                throw; // unreachable: Assert.Inconclusive always throws
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                Assert.Inconclusive($"Cryptographic operation failed. Credential Guard may not be properly configured: {ex.Message}");
                throw; // unreachable: Assert.Inconclusive always throws
            }

            Assert.IsFalse(string.IsNullOrEmpty(leg1Result.AccessToken),
                "Leg 1: AccessToken should not be empty.");
            Assert.AreEqual("mtls_pop", leg1Result.TokenType,
                "Leg 1: TokenType must be 'mtls_pop'.");
            Assert.IsNotNull(leg1Result.BindingCertificate,
                "Leg 1: BindingCertificate must not be null — required for FIC Leg 2.");
            Assert.AreEqual(TokenSource.IdentityProvider, leg1Result.AuthenticationResultMetadata.TokenSource,
                "Leg 1: First call must hit the MSI endpoint.");

            return leg1Result;
        }

        /// <summary>
        /// MSI Leg 1 → ConfApp Leg 2 bearer token.
        /// Verifies the full FIC two-leg exchange produces a valid bearer token.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [TestMethod]
        [DataRow(null, DisplayName = "FicTwoLeg_Bearer_SAMI")] //SAMI Object ID ("11a5d2ba-f08b-4e99-9361-2a07b4bf7af9")
        [DataRow(UamiClientId, DisplayName = "FicTwoLeg_Bearer_UAMI-ClientId")]
        public async Task AcquireToken_OnImdsV2_FicTwoLeg_BearerToken_Succeeds(string uamiClientId)
        {
            // --- Leg 1: MSI acquires an mTLS PoP token for api://AzureADTokenExchange ---

            AuthenticationResult leg1Result = await AcquireLeg1MtlsPopTokenAsync(uamiClientId).ConfigureAwait(false);

            // --- Leg 2: ConfApp exchanges Leg 1 token for a bearer token ---

            var confApp = BuildConfApp(leg1Result);

            var leg2Result = await confApp
                .AcquireTokenForClient(new[] { GraphScope })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(leg2Result.AccessToken),
                "Leg 2: AccessToken should not be empty.");
            Assert.IsTrue(
                string.Equals(leg2Result.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase),
                $"Leg 2: Expected Bearer token type, got '{leg2Result.TokenType}'.");

            // --- Cache hit verification ---

            var leg2Cached = await confApp
                .AcquireTokenForClient(new[] { GraphScope })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg2Cached.AuthenticationResultMetadata.TokenSource,
                "Leg 2: Second call should be served from cache.");
            Assert.AreEqual(leg2Result.AccessToken, leg2Cached.AccessToken,
                "Leg 2: Cached token should match original.");
        }

        /// <summary>
        /// MSI Leg 1 → ConfApp Leg 2 mTLS PoP token.
        /// Same two-leg exchange as the bearer test, but Leg 2 calls .WithMtlsProofOfPossession(),
        /// so the final token is an mTLS PoP token bound to a certificate instead of a bearer token.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [TestMethod]
        [DataRow(null, DisplayName = "FicTwoLeg_MtlsPop_SAMI")] //SAMI Object ID ("11a5d2ba-f08b-4e99-9361-2a07b4bf7af9")
        [DataRow(UamiClientId, DisplayName = "FicTwoLeg_MtlsPop_UAMI-ClientId")]
        public async Task AcquireToken_OnImdsV2_FicTwoLeg_MtlsPopToken_Succeeds(string uamiClientId)
        {
            // --- Leg 1: MSI acquires an mTLS PoP token for api://AzureADTokenExchange ---

            AuthenticationResult leg1Result = await AcquireLeg1MtlsPopTokenAsync(uamiClientId).ConfigureAwait(false);

            // --- Leg 2: ConfApp exchanges the Leg 1 token for an mTLS PoP token ---
            // The only difference from the bearer flow is .WithMtlsProofOfPossession() on the Leg 2 request.

            var confApp = BuildConfApp(leg1Result);

            var leg2Result = await confApp
                .AcquireTokenForClient(new[] { GraphScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(leg2Result.AccessToken),
                "Leg 2: AccessToken should not be empty.");
            Assert.AreEqual("mtls_pop", leg2Result.TokenType,
                "Leg 2: TokenType must be 'mtls_pop' when .WithMtlsProofOfPossession() is used.");
            Assert.IsNotNull(leg2Result.BindingCertificate,
                "Leg 2: BindingCertificate must not be null for an mTLS PoP token.");

            // --- Cache hit verification (same request shape hits the same cache entry) ---

            var leg2Cached = await confApp
                .AcquireTokenForClient(new[] { GraphScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg2Cached.AuthenticationResultMetadata.TokenSource,
                "Leg 2: Second call should be served from cache.");
            Assert.AreEqual(leg2Result.AccessToken, leg2Cached.AccessToken,
                "Leg 2: Cached token should match original.");
        }
    }
}
