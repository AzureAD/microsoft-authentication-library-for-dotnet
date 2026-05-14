// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class Agentic
    {
        const string ClientId = "aab5089d-e764-47e3-9f28-cc11c2513821"; // agent app
        const string TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
        const string AgentIdentity = "ab18ca07-d139-4840-8b3b-4be9610c6ed5";
        const string UserUpn = "agentuser1@id4slab1.onmicrosoft.com";
        private const string TokenExchangeUrl = "api://AzureADTokenExchange/.default";
        private const string Scope = "https://graph.microsoft.com/.default";

        [TestMethod]
        public async Task AgentUserIdentityGetsTokenForGraphTest()
        {
            await AgentUserIdentityGetsTokenForGraphAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AgentGetsAppTokenForGraphTest()
        {
            await AgentGetsAppTokenForGraph().ConfigureAwait(false);
        }

        private static async Task AgentGetsAppTokenForGraph()
        {
            Guid expectedCorrelationId = Guid.NewGuid();
            Guid capturedCorrelationId = Guid.Empty;

            var cca = ConfidentialClientApplicationBuilder
                        .Create(AgentIdentity)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithClientAssertion((AssertionRequestOptions options) =>
                        {
                            capturedCorrelationId = options.CorrelationId;
                            return GetAppCredentialAsync(AgentIdentity);
                        })
                        .Build();

            var result = await cca.AcquireTokenForClient([Scope])
                .WithCorrelationId(expectedCorrelationId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"FMI app credential from : {result.AuthenticationResultMetadata.TokenSource}");

            // Verify CorrelationId flowed to the assertion callback (Issue #5924)
            Assert.AreEqual(expectedCorrelationId, capturedCorrelationId,
                "CorrelationId from WithCorrelationId() must flow to the assertion callback.");
        }

        private static async Task AgentUserIdentityGetsTokenForGraphAsync()
        {
            // Assertion app: acquires the user_fic assertion via FMI path
            var assertionApp = ConfidentialClientApplicationBuilder
                        .Create(AgentIdentity)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithExperimentalFeatures(true)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithClientAssertion(async (AssertionRequestOptions a) =>
                        {
                            Assert.AreEqual(AgentIdentity, a.ClientAssertionFmiPath);
                            var cred = await GetAppCredentialAsync(a.ClientAssertionFmiPath).ConfigureAwait(false);
                            return cred;
                        })
                        .Build();

            // Main app: acquires the final user token via user_fic grant
            var cca = ConfidentialClientApplicationBuilder
                        .Create(AgentIdentity)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)> { { "slice", ("first", false) } })
                        .WithClientAssertion((AssertionRequestOptions _) => GetAppCredentialAsync(AgentIdentity))
                        .Build();

            // Assertion provider using the assertion app with FMI path
            var assertionResult = await assertionApp
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPathForClientAssertion(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"User FIC credential from : {assertionResult.AuthenticationResultMetadata.TokenSource}");
            string assertion = assertionResult.AccessToken;

            var result = await (cca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, assertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            IAccount account = await cca.GetAccountAsync(result.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            var result2 = await cca.AcquireTokenSilent([Scope], account).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource, "Token should be from cache");
        }

        private static async Task<string> GetAppCredentialAsync(string fmiPath)
        {
            Assert.IsNotNull(fmiPath, "fmiPath cannot be null");
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var cca1 = ConfidentialClientApplicationBuilder
                        .Create(ClientId)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithCertificate(cert, sendX5C: true) //sendX5c enables SN+I auth which is required for FMI flows                        
                        .Build();

            var result = await cca1.AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(fmiPath)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"FMI app credential from : {result.AuthenticationResultMetadata.TokenSource}");

            return result.AccessToken;
        }

        #region Single-CCA Agent Identity Tests (WithClientIdOverride)

        [TestMethod]
        public async Task SingleCca_AgentUserIdentity_WithCacheHit_Test()
        {
            // This test demonstrates the agent identity flow using a SINGLE CCA instance
            // and the new WithClientIdOverride parameter, instead of requiring multiple CCAs.
            //
            // Flow:
            //   Leg 1: Blueprint CCA acquires FMI token (T1) targeting the agent app
            //   Leg 2: Same CCA acquires instance token (T2) using T1 as credential, with client_id override
            //   Leg 3: Same CCA acquires user token using T2, with client_id override
            //   Silent: AcquireTokenSilent returns the cached user token

            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Single CCA — configured as the blueprint app
            var cca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            // Leg 1: Acquire FMI token (T1) — uses blueprint's own client_id
            var leg1Result = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(leg1Result.AccessToken, "T1 should not be null");
            Assert.AreEqual(TokenSource.IdentityProvider, leg1Result.AuthenticationResultMetadata.TokenSource);
            Trace.WriteLine($"Leg 1 (T1) acquired from: {leg1Result.AuthenticationResultMetadata.TokenSource}");

            // Leg 2: Acquire instance token (T2) — override client_id to agentAppId, use T1 as assertion
            string t1Token = leg1Result.AccessToken;
            var leg2Result = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithClientIdOverride(AgentIdentity)
                .OnBeforeTokenRequest(data =>
                {
                    // Override the client_assertion to use T1 instead of the certificate
                    data.BodyParameters["client_assertion"] = t1Token;
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(leg2Result.AccessToken, "T2 should not be null");
            Assert.AreEqual(TokenSource.IdentityProvider, leg2Result.AuthenticationResultMetadata.TokenSource);
            Trace.WriteLine($"Leg 2 (T2) acquired from: {leg2Result.AuthenticationResultMetadata.TokenSource}");

            // Leg 3: Acquire user token — override client_id to agentAppId
            var leg3Result = await ((IByUserFederatedIdentityCredential)cca)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, leg2Result.AccessToken)
                .WithClientIdOverride(AgentIdentity)
                .OnBeforeTokenRequest(data =>
                {
                    // Override the client_assertion to use T1 instead of the certificate
                    data.BodyParameters["client_assertion"] = t1Token;
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(leg3Result.AccessToken, "User token should not be null");
            Assert.IsNotNull(leg3Result.Account, "Account should not be null");
            Assert.AreEqual(TokenSource.IdentityProvider, leg3Result.AuthenticationResultMetadata.TokenSource);
            Trace.WriteLine($"Leg 3 (user token) acquired from: {leg3Result.AuthenticationResultMetadata.TokenSource}");

            // Verify cache hits: repeat all 3 legs, all should come from cache
            var leg1Cached = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, leg1Cached.AuthenticationResultMetadata.TokenSource, "Leg 1 should be cached");

            var leg2Cached = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithClientIdOverride(AgentIdentity)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = t1Token;
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, leg2Cached.AuthenticationResultMetadata.TokenSource, "Leg 2 should be cached");

            // Verify no cache collision between Leg 1 and Leg 2 (same scope, different client IDs)
            Assert.AreNotEqual(leg1Result.AccessToken, leg2Result.AccessToken,
                "T1 and T2 should be distinct tokens despite same scope");

            // Silent: Use the account from Leg 3 directly with WithClientIdOverride
            var silentResult = await cca
                .AcquireTokenSilent([Scope], leg3Result.Account)
                .WithClientIdOverride(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silentResult.AuthenticationResultMetadata.TokenSource, "Token should come from cache");
            Assert.AreEqual(leg3Result.AccessToken, silentResult.AccessToken, "Cached token should match original");
            Trace.WriteLine($"Silent result from: {silentResult.AuthenticationResultMetadata.TokenSource}");

            // Silent with ForceRefresh: verify the refresh token path also works with the override
            var silentRefreshed = await cca
                .AcquireTokenSilent([Scope], leg3Result.Account)
                .WithClientIdOverride(AgentIdentity)
                .WithForceRefresh(true)
                .OnBeforeTokenRequest(data =>
                {
                    // Refresh token request also needs the agent's client_assertion
                    data.BodyParameters["client_assertion"] = t1Token;
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, silentRefreshed.AuthenticationResultMetadata.TokenSource, "Force refresh should hit identity provider");
            Assert.IsNotNull(silentRefreshed.AccessToken, "Refreshed token should not be null");
            Trace.WriteLine($"Force-refreshed result from: {silentRefreshed.AuthenticationResultMetadata.TokenSource}");
        }

        [TestMethod]
        public async Task SingleCca_AgentUserIdentity_TwoUsers_NoCacheCollision_Test()
        {
            // This test verifies that two different users' tokens don't collide in the cache
            // when using the same single CCA + WithClientIdOverride pattern.
            //
            // We acquire tokens for user1, then user1 again (should be cache hit),
            // verifying the basic cache isolation by homeAccountId.

            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var cca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            // --- First user flow ---
            var leg1Result = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            string t1Token = leg1Result.AccessToken;

            var leg2Result = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithClientIdOverride(AgentIdentity)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = t1Token;
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            var user1Result = await ((IByUserFederatedIdentityCredential)cca)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, leg2Result.AccessToken)
                .WithClientIdOverride(AgentIdentity)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = t1Token;
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(user1Result.AccessToken);
            Trace.WriteLine($"User 1 token acquired from: {user1Result.AuthenticationResultMetadata.TokenSource}");

            // --- Verify Leg 1 and Leg 2 are cached (reuse for second call) ---
            var leg1Cached = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg1Cached.AuthenticationResultMetadata.TokenSource,
                "Leg 1 token should be served from cache on second call");

            var leg2Cached = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithClientIdOverride(AgentIdentity)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = t1Token;
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg2Cached.AuthenticationResultMetadata.TokenSource,
                "Leg 2 token should be served from cache on second call");

            // --- User 1 silent with WithClientIdOverride ---
            var user1Silent = await cca
                .AcquireTokenSilent([Scope], user1Result.Account)
                .WithClientIdOverride(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, user1Silent.AuthenticationResultMetadata.TokenSource,
                "User 1 silent should hit cache");
            Assert.AreEqual(user1Result.AccessToken, user1Silent.AccessToken);

            // --- Verify no collision: Leg 1 token (blueprint clientId) vs Leg 2 token (agent clientId) ---
            // Both target the same scope (api://AzureADTokenExchange/.default) but should be distinct cache entries
            Assert.AreNotEqual(leg1Result.AccessToken, leg2Result.AccessToken,
                "T1 and T2 should be different tokens despite same scope");
        }

        [TestMethod]
        public async Task SingleCca_Leg1AndLeg2_DifferentCachePartitions_Test()
        {
            // This test verifies that Leg 1 and Leg 2 tokens don't collide in the app cache
            // even though they both target api://AzureADTokenExchange/.default.
            // They should be in different cache partitions because:
            //   Leg 1: clientId = blueprintClientId (no override)
            //   Leg 2: clientId = agentAppId (via WithClientIdOverride)

            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var cca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            // Leg 1
            var leg1 = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Leg 2
            string t1Token = leg1.AccessToken;
            var leg2 = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithClientIdOverride(AgentIdentity)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = t1Token;
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Repeat Leg 1 — should come from cache (proving Leg 2 didn't overwrite it)
            var leg1Again = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg1Again.AuthenticationResultMetadata.TokenSource,
                "Leg 1 token should still be cached after Leg 2 was stored");
            Assert.AreEqual(leg1.AccessToken, leg1Again.AccessToken,
                "Leg 1 cached token should be the same as the original");

            // Repeat Leg 2 — should come from cache (proving Leg 1 repeat didn't overwrite it)
            var leg2Again = await cca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithClientIdOverride(AgentIdentity)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = t1Token;
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg2Again.AuthenticationResultMetadata.TokenSource,
                "Leg 2 token should still be cached after Leg 1 was re-read");
            Assert.AreEqual(leg2.AccessToken, leg2Again.AccessToken,
                "Leg 2 cached token should be the same as the original");
        }

        #endregion
    }
}
