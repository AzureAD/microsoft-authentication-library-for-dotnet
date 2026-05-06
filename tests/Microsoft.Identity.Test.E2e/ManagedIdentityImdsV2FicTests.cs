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
    ///   Leg 2 — ConfApp uses the Leg 1 token as a ClientSignedAssertion to obtain a bearer token
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

        // TODO: Replace with the client ID of the ConfApp configured with a Federated Identity
        //       Credential (FIC) that trusts the MSI on the MSALMSIV2 pool.
        private const string FicConfAppClientId = "TODO_FIC_CONF_APP_CLIENT_ID";

        // TODO: Replace with the AAD tenant ID for the FIC ConfApp.
        private const string TenantId = "TODO_TENANT_ID";

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
            // For bearer Leg 2: pass the binding certificate to prove possession of the
            // Leg 1 mTLS PoP token, but do not call .WithMtlsProofOfPossession() on the
            // AcquireTokenForClient request — that keeps the final token as Bearer.
            return ConfidentialClientApplicationBuilder
                .Create(FicConfAppClientId)
                .WithTenantId(TenantId)
                .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                .WithClientAssertion((_, ct) => Task.FromResult(new ClientSignedAssertion
                {
                    Assertion = leg1Result.AccessToken,
                    TokenBindingCertificate = leg1Result.BindingCertificate
                }))
                .Build();
        }

        /// <summary>
        /// MSI Leg 1 → ConfApp Leg 2 bearer token.
        /// Verifies the full FIC two-leg exchange produces a valid bearer token.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2_Fic")]
        [TestMethod]
        [DataRow(null, DisplayName = "FicTwoLeg_Bearer_SAMI")]
        [DataRow(UamiClientId, DisplayName = "FicTwoLeg_Bearer_UAMI-ClientId")]
        public async Task AcquireToken_OnImdsV2_FicTwoLeg_BearerToken_Succeeds(string uamiClientId)
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Credential Guard attestation is only available on Windows.");
            }

            // --- Leg 1: MSI acquires mTLS PoP token for api://AzureADTokenExchange ---

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
                return;
            }

            Assert.IsFalse(string.IsNullOrEmpty(leg1Result.AccessToken),
                "Leg 1: AccessToken should not be empty.");
            Assert.AreEqual("mtls_pop", leg1Result.TokenType,
                "Leg 1: TokenType must be 'mtls_pop'.");
            Assert.IsNotNull(leg1Result.BindingCertificate,
                "Leg 1: BindingCertificate must not be null — required for FIC Leg 2.");
            Assert.AreEqual(TokenSource.IdentityProvider, leg1Result.AuthenticationResultMetadata.TokenSource,
                "Leg 1: First call must hit the MSI endpoint.");

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
    }
}
