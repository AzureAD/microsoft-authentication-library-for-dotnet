// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// Integration tests for cross-cloud FIC (Federated Identity Credentials) combined with
    /// Proof-of-Possession (PoP) token binding.
    ///
    /// These tests validate the two-leg cross-cloud FIC token acquisition pattern:
    ///   Leg 1: Authenticate to Cloud B (e.g., Fairfax/US Gov) with a client credential (secret/cert)
    ///          and request scope api://AzureADTokenExchange/.default to obtain a FIC assertion token.
    ///   Leg 2: Use the assertion token as a client_assertion to authenticate to Cloud A (e.g., Public)
    ///          and acquire an access token for the target resource, optionally with PoP binding.
    ///
    /// Test resources (from ID Web E2E test suite, MSID Lab 4):
    ///   - Public cloud app: 5e71875b-ae52-4a3c-8b82-f6fdc8e1dbe1 (trusts tokens from f6b698c0)
    ///   - Public cloud tenant: msidlab4.onmicrosoft.com
    ///   - Fairfax app: c0555d2d-02f2-4838-802e-3463422e571d
    ///   - Fairfax tenant: 45ff0c17-f8b5-489b-b7fd-2fedebbec0c4
    ///   - Fairfax secret: ARLMSIDLAB1-IDLASBS-App-CC-Secret (in msidlabs Key Vault)
    ///   - FIC audience: api://AzureADTokenExchange (default)
    /// </summary>
    [TestClass]
    public class CrossCloudFicPopTests
    {
        // Fairfax (US Gov) app — source identity for leg 1
        private const string FairfaxClientId = "c0555d2d-02f2-4838-802e-3463422e571d";
        private const string FairfaxTenantId = "45ff0c17-f8b5-489b-b7fd-2fedebbec0c4";
        private const string FairfaxAuthority = "https://login.microsoftonline.us/";
        private const string FairfaxSecretName = "ARLMSIDLAB1-IDLASBS-App-CC-Secret";

        // Public cloud app — FIC target for leg 2
        private const string PublicClientId = "5e71875b-ae52-4a3c-8b82-f6fdc8e1dbe1";
        private const string PublicTenantId = "msidlab4.onmicrosoft.com";
        private const string PublicAuthority = "https://login.microsoftonline.com/";

        // Token exchange scope (FIC audience + /.default)
        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";

        // Target resource scope for the final token
        private const string GraphScope = "https://graph.microsoft.com/.default";

        // PoP configuration
        private const string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        /// <summary>
        /// Cross-cloud FIC: Fairfax → Public, bearer token (no PoP).
        /// Validates the basic two-leg cross-cloud FIC flow works at the MSAL level.
        /// </summary>
        [TestMethod]
        public async Task CrossCloud_FIC_Bearer_TestAsync()
        {
            // Act — Leg 2: exchange assertion for bearer token from public cloud
            var publicCca = ConfidentialClientApplicationBuilder
                .Create(PublicClientId)
                .WithAuthority(PublicAuthority, PublicTenantId)
                .WithClientAssertion(async (AssertionRequestOptions _) =>
                {
                    return await AcquireFicAssertionFromFairfaxAsync().ConfigureAwait(false);
                })
                .WithExperimentalFeatures(true)
                .WithTestLogging()
                .Build();

            var result = await publicCca
                .AcquireTokenForClient(new[] { GraphScope })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("Bearer", result.TokenType);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            Trace.WriteLine($"Cross-cloud FIC bearer token acquired. Source: {result.AuthenticationResultMetadata.TokenSource}");

            // Verify the token was issued by the public cloud
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.AccessToken);
            bool isPublicCloud = jwt.Issuer.Contains("sts.windows.net") || jwt.Issuer.Contains("login.microsoftonline.com");
            Assert.IsTrue(isPublicCloud,
                $"Token issuer should be public cloud, was: {jwt.Issuer}");
        }

        /// <summary>
        /// Cross-cloud FIC + PoP: Fairfax → Public, with Signed HTTP Request PoP binding.
        /// Validates that the FIC assertion can be combined with PoP token binding.
        /// This is the scenario the customer (O365 Suggestion Chips) is attempting.
        /// </summary>
        [TestMethod]
        public async Task CrossCloud_FIC_WithPoP_TestAsync()
        {
            // Arrange — Leg 1: acquire FIC assertion from Fairfax
            string assertion = await AcquireFicAssertionFromFairfaxAsync().ConfigureAwait(false);

            // Arrange — PoP configuration
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.HttpMethod = HttpMethod.Get;

            // Act — Leg 2: exchange assertion for PoP-bound token from public cloud
            var publicCca = ConfidentialClientApplicationBuilder
                .Create(PublicClientId)
                .WithAuthority(PublicAuthority, PublicTenantId)
                .WithClientAssertion((AssertionRequestOptions _) => Task.FromResult(assertion))
                .WithExperimentalFeatures(true)
                .WithTestLogging()
                .Build();

            var result = await publicCca
                .AcquireTokenForClient(new[] { GraphScope })
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual("pop", result.TokenType);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            Trace.WriteLine($"Cross-cloud FIC PoP token acquired. Source: {result.AuthenticationResultMetadata.TokenSource}");

            // Validate PoP token structure
            PoPValidator.VerifyPoPToken(
                PublicClientId,
                ProtectedUrl,
                HttpMethod.Get,
                result);
        }

        /// <summary>
        /// Cross-cloud FIC + PoP with dynamic assertion callback.
        /// Uses WithClientAssertion callback (async delegate) instead of a static assertion string,
        /// which is the recommended pattern for production because it handles assertion token refresh.
        /// </summary>
        [TestMethod]
        public async Task CrossCloud_FIC_WithPoP_DynamicAssertion_TestAsync()
        {
            // Arrange — PoP configuration
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.HttpMethod = HttpMethod.Get;

            // Act — Leg 2 with dynamic assertion provider (leg 1 runs inside the callback)
            var publicCca = ConfidentialClientApplicationBuilder
                .Create(PublicClientId)
                .WithAuthority(PublicAuthority, PublicTenantId)
                .WithClientAssertion(async (AssertionRequestOptions _) =>
                {
                    return await AcquireFicAssertionFromFairfaxAsync().ConfigureAwait(false);
                })
                .WithExperimentalFeatures(true)
                .WithTestLogging()
                .Build();

            var result = await publicCca
                .AcquireTokenForClient(new[] { GraphScope })
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("pop", result.TokenType);

            PoPValidator.VerifyPoPToken(
                PublicClientId,
                ProtectedUrl,
                HttpMethod.Get,
                result);

            // Verify cache hit on second call (assertion callback should NOT be invoked)
            var result2 = await publicCca
                .AcquireTokenForClient(new[] { GraphScope })
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
        }

        /// <summary>
        /// Validates that the Fairfax leg correctly targets login.microsoftonline.us
        /// and uses the default FIC audience (api://AzureADTokenExchange).
        /// </summary>
        [TestMethod]
        public async Task CrossCloud_FIC_Leg1_ValidatesEndpoint_TestAsync()
        {
            // Arrange & Act
            string fairfaxSecret = LabResponseHelper.FetchSecretString(
                FairfaxSecretName,
                LabResponseHelper.KeyVaultSecretsProviderMsid);

            var fairfaxCca = ConfidentialClientApplicationBuilder
                .Create(FairfaxClientId)
                .WithAuthority(FairfaxAuthority, FairfaxTenantId)
                .WithClientSecret(fairfaxSecret)
                .WithExperimentalFeatures(true)
                .WithTestLogging()
                .Build();

            var result = await fairfaxCca
                .AcquireTokenForClient(new[] { TokenExchangeScope })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            // Verify the token endpoint was the Fairfax endpoint
            Assert.Contains(
                "login.microsoftonline.us",
                result.AuthenticationResultMetadata.TokenEndpoint,
                "Token endpoint should be Fairfax");

            // Verify the assertion token has an audience (the resolved app ID for api://AzureADTokenExchange).
            // Note: ESTS resolves api://AzureADTokenExchange to its underlying app ID GUID in the aud claim,
            // so we just verify the audience is present and non-empty.
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.AccessToken);
            var audience = jwt.Payload.Aud;
            Assert.IsNotEmpty(audience, "Assertion token should have an audience claim");

            Trace.WriteLine($"Leg 1 assertion acquired from {result.AuthenticationResultMetadata.TokenEndpoint}");
        }

        /// <summary>
        /// Acquires a FIC assertion token from the Fairfax cloud (leg 1).
        /// This assertion can then be used as a client_assertion in leg 2.
        /// </summary>
        private static async Task<string> AcquireFicAssertionFromFairfaxAsync(
            string tokenExchangeScope = TokenExchangeScope)
        {
            string fairfaxSecret = LabResponseHelper.FetchSecretString(
                FairfaxSecretName,
                LabResponseHelper.KeyVaultSecretsProviderMsid);

            var fairfaxCca = ConfidentialClientApplicationBuilder
                .Create(FairfaxClientId)
                .WithAuthority(FairfaxAuthority, FairfaxTenantId)
                .WithClientSecret(fairfaxSecret)
                .WithExperimentalFeatures(true)
                .WithTestLogging()
                .Build();

            var assertionResult = await fairfaxCca
                .AcquireTokenForClient(new[] { tokenExchangeScope })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Trace.WriteLine($"FIC assertion acquired from Fairfax. " +
                $"Endpoint: {assertionResult.AuthenticationResultMetadata.TokenEndpoint}, " +
                $"Source: {assertionResult.AuthenticationResultMetadata.TokenSource}");

            Assert.IsNotNull(assertionResult.AccessToken, "Leg 1 (Fairfax) should return an assertion token");

            return assertionResult.AccessToken;
        }

        #region Negative tests — wrong token exchange audience

        /// <summary>
        /// Validates that using a cloud-specific audience (USGov) in leg 1 succeeds silently,
        /// but leg 2 fails with AADSTS70022204 because the public cloud app's FIC entry
        /// was registered with the default audience (api://AzureADTokenExchange), not the
        /// USGov-specific one.
        ///
        /// This demonstrates the "silent misconfiguration" problem described in the
        /// FIC token exchange auto-resolution design: the wrong audience only surfaces
        /// as a failure during leg 2 validation, making it hard to diagnose.
        /// </summary>
        [TestMethod]
        public async Task WrongAudience_USGov_Leg1Succeeds_Leg2Fails_TestAsync()
        {
            // Arrange — Leg 1 with the WRONG audience (USGov instead of default)
            const string wrongAudience = "api://AzureADTokenExchangeUSGov/.default";

            // Act — Leg 1: Fairfax ESTS will happily issue a token with this audience.
            // The audience mismatch is only caught by the target app's FIC entry in leg 2.
            string wrongAssertion = await AcquireFicAssertionFromFairfaxAsync(wrongAudience)
                .ConfigureAwait(false);

            Assert.IsNotNull(wrongAssertion,
                "Leg 1 should succeed — ESTS issues tokens for any requested audience without validating FIC");

            Trace.WriteLine("Leg 1 succeeded with USGov audience. Now attempting leg 2...");

            // Act — Leg 2: use the wrong-audience assertion against the public cloud app
            var publicCca = ConfidentialClientApplicationBuilder
                .Create(PublicClientId)
                .WithAuthority(PublicAuthority, PublicTenantId)
                .WithClientAssertion((AssertionRequestOptions _) => Task.FromResult(wrongAssertion))
                .WithExperimentalFeatures(true)
                .WithTestLogging()
                .Build();

            // Assert — Leg 2 should fail because the FIC entry expects api://AzureADTokenExchange
            var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                () => publicCca
                    .AcquireTokenForClient(new[] { GraphScope })
                    .ExecuteAsync(CancellationToken.None),
                allowDerived: true).ConfigureAwait(false);

            Trace.WriteLine($"Leg 2 failed as expected: {ex.ErrorCode} — {ex.Message}");

            // ESTS returns AADSTS700222 or similar for audience mismatch
            Assert.Contains("audience", ex.Message, StringComparison.OrdinalIgnoreCase,
                "Error should mention assertion audience mismatch");
        }

        /// <summary>
        /// Validates that a completely bogus/unknown audience fails at leg 1 itself.
        /// Unlike cloud-specific audiences (which resolve to real app IDs), a bogus
        /// audience has no backing app registration and ESTS rejects it immediately.
        /// </summary>
        [TestMethod]
        public async Task BogusAudience_Leg1Fails_TestAsync()
        {
            // Arrange
            const string bogusAudience = "api://NotARealTokenExchange/.default";

            // Act & Assert — Leg 1 should fail immediately
            var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                () => AcquireFicAssertionFromFairfaxAsync(bogusAudience),
                allowDerived: true).ConfigureAwait(false);

            Trace.WriteLine($"Leg 1 failed as expected: {ex.ErrorCode} — {ex.Message}");

            Assert.IsFalse(string.IsNullOrEmpty(ex.ErrorCode),
                "ESTS should return an error code for unknown audience");
        }

        #endregion
    }
}
