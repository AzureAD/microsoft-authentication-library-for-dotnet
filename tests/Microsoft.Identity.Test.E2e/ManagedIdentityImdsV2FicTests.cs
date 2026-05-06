// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
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
    /// The Leg 2 ConfApp configuration (client ID, authority) is read from Key Vault using
    /// the VM's managed identity — no lab certificate required on the MSALMSIV2 pool.
    /// The ConfApp must be registered with a FIC that trusts this MSI.
    ///
    /// These tests run on the MSALMSIV2 pool (IMDSv2 + Credential Guard).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ManagedIdentityImdsV2FicTests
    {
        private const string TokenExchangeResource = "api://AzureADTokenExchange";
        private const string GraphScope = "https://graph.microsoft.com/.default";
        private const string MsalTeamKeyVaultUri = "https://id4skeyvault.vault.azure.net/";
        private const string AppS2SSecretName = "App-S2S-Config";

        // UAMI identifiers (same pool as ManagedIdentityImdsV2Tests)
        private const string UamiClientId = "6325cd32-9911-41f3-819c-416cdf9104e7";

        private static IManagedIdentityApplication BuildMsi(string userAssignedClientId = null)
        {
            ManagedIdentityId miId = userAssignedClientId is null
                ? ManagedIdentityId.SystemAssigned
                : ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId);

            var builder = ManagedIdentityApplicationBuilder.Create(miId);
            builder.Config.AccessorOptions = null;
            return builder.Build();
        }

        private static IConfidentialClientApplication BuildConfApp(string appId, string authority, AuthenticationResult leg1Result)
        {
            // For bearer Leg 2: pass the binding certificate to prove possession of the
            // Leg 1 mTLS PoP token, but do not call .WithMtlsProofOfPossession() on the
            // AcquireTokenForClient request — that keeps the final token as Bearer.
            return ConfidentialClientApplicationBuilder
                .Create(appId)
                .WithAuthority(authority)
                .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                .WithClientAssertion((_, ct) => Task.FromResult(new ClientSignedAssertion
                {
                    Assertion = leg1Result.AccessToken,
                    TokenBindingCertificate = leg1Result.BindingCertificate
                }))
                .Build();
        }

        /// <summary>
        /// Reads the App-S2S-Config secret from the MSAL team Key Vault using the VM's managed identity.
        /// This avoids the lab certificate requirement that LabResponseHelper uses.
        /// Prerequisite: the MSALMSIV2 pool MSI must have read access to id4skeyvault.vault.azure.net.
        /// </summary>
        private static async Task<(string AppId, string Authority)> GetConfAppConfigViaMsiAsync()
        {
            // Use SAMI to get a KV token — no lab cert needed on the MSIV2 VM
            var msiApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned).Build();
            var tokenResult = await msiApp
                .AcquireTokenForManagedIdentity("https://vault.azure.net")
                .ExecuteAsync()
                .ConfigureAwait(false);

            var credential = new MsalMsiTokenCredential(tokenResult.AccessToken, tokenResult.ExpiresOn);
            var secretClient = new SecretClient(new Uri(MsalTeamKeyVaultUri), credential);
            var secret = await secretClient.GetSecretAsync(AppS2SSecretName).ConfigureAwait(false);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonObject = JsonNode.Parse(secret.Value.Value).AsObject();
            var appToken = jsonObject
                .FirstOrDefault(kvp => string.Equals(kvp.Key, "app", StringComparison.OrdinalIgnoreCase))
                .Value;

            if (appToken == null)
            {
                throw new InvalidOperationException($"Key Vault secret '{AppS2SSecretName}' does not contain an 'app' property.");
            }

            var appConfig = appToken.Deserialize<AppConfigDto>(options)
                ?? throw new InvalidOperationException($"Failed to deserialize 'app' from Key Vault secret '{AppS2SSecretName}'.");

            return (appConfig.AppId, appConfig.Authority);
        }

        /// <summary>
        /// MSI Leg 1 → ConfApp Leg 2 bearer token.
        /// Verifies the full FIC two-leg exchange produces a valid bearer token.
        /// </summary>
        [RunOnAzureDevOps]
        [TestCategory("MI_E2E_ImdsV2")]
        [TestMethod]
        [DataRow(null, DisplayName = "FicTwoLeg_Bearer_SAMI")]
        [DataRow(UamiClientId, DisplayName = "FicTwoLeg_Bearer_UAMI-ClientId")]
        public async Task AcquireToken_OnImdsV2_FicTwoLeg_BearerToken_Succeeds(string uamiClientId)
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Credential Guard attestation is only available on Windows.");
            }

            // Read ConfApp config from KV using the VM's MSI (no lab cert required)
            var (confAppId, confAppAuthority) = await GetConfAppConfigViaMsiAsync().ConfigureAwait(false);

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
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                Assert.Inconclusive($"Cryptographic operation failed. Credential Guard may not be properly configured: {ex.Message}");
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

            var confApp = BuildConfApp(confAppId, confAppAuthority, leg1Result);

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
        /// Wraps a MSAL access token as an Azure.Core.TokenCredential for use with the Azure SDK.
        /// </summary>
        private sealed class MsalMsiTokenCredential : TokenCredential
        {
            private readonly AccessToken _token;

            internal MsalMsiTokenCredential(string accessToken, DateTimeOffset expiresOn)
            {
                _token = new AccessToken(accessToken, expiresOn);
            }

            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
                => _token;

            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
                => ValueTask.FromResult(_token);
        }

        /// <summary>Minimal projection of the 'app' JSON block inside the App-S2S-Config KV secret.</summary>
        private sealed class AppConfigDto
        {
            public string AppId { get; set; }
            public string Authority { get; set; }
        }
    }
}
