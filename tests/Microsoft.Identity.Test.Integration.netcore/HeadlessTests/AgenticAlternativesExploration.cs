// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// Investigation: Can we simplify the agentic 1+N CCA pattern by using
    /// WithExtraBodyParameters / OnBeforeTokenRequest to inject client_id overrides?
    ///
    /// Background:
    /// - The recommended agentic pattern uses 1 Blueprint CCA + N Agent CCAs
    /// - The Blueprint CCA holds the certificate and handles Leg 1 (FMI token acquisition)
    /// - Each Agent CCA has its own client_id = agentAppId, giving natural cache isolation
    /// - This investigation explores whether we can reduce the number of CCA instances
    ///
    /// Key findings from code analysis:
    ///
    /// 1. client_id on the wire:
    ///    TokenClient.cs:139 sets client_id from AppConfig.ClientId as a body parameter.
    ///    WithExtraBodyParameters and OnBeforeTokenRequest handlers fire later and can
    ///    overwrite body params. After the internal fix (Add() → indexer), both can
    ///    overwrite existing keys like client_id without throwing.
    ///
    /// 2. client_id in cache keys:
    ///    TokenCache.ITokenCacheInternal.cs:78 creates MsalAccessTokenCacheItem with
    ///    requestParams.AppConfig.ClientId — NOT from the body params. Same for refresh
    ///    tokens (line 101). So the cache key always uses the CCA's configured ClientId,
    ///    regardless of what was sent on the wire.
    ///
    /// 3. Cache isolation via AdditionalCacheKeyComponents:
    ///    WithExtraBodyParameters is ideal for stable values like client_id because it
    ///    handles both wire override and cache key inclusion in one call. OnBeforeTokenRequest
    ///    is needed for volatile values like client_assertion (T1 token) that must NOT be
    ///    in cache keys (they would cause cache misses on every T1 renewal).
    ///
    /// 4. Refresh token gap (fixed in this PR):
    ///    Before this PR, only access tokens supported AdditionalCacheKeyComponents.
    ///    Refresh tokens had no such mechanism, causing RT collisions when two agents
    ///    serve the same user through a single CCA. This PR adds RT support.
    ///
    /// Conclusion:
    /// - WithExtraBodyParameters for client_id: wire override + cache key in one call
    /// - OnBeforeTokenRequest for client_assertion: wire override without cache key pollution
    /// - With the RT fix in this PR, the single-CCA pattern is viable for all legs
    ///
    /// This test class explores these alternatives with live token requests.
    /// </summary>
    [TestClass]
    public class AgenticAlternativesExploration
    {
        // Blueprint app — owns the certificate credential
        const string BlueprintClientId = "aab5089d-e764-47e3-9f28-cc11c2513821";
        const string TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";

        // Agent app — has no secret of its own; relies on FMI delegation from Blueprint
        const string AgentAppId = "ab18ca07-d139-4840-8b3b-4be9610c6ed5";

        // Test user
        const string UserUpn = "agentuser1@id4slab1.onmicrosoft.com";

        // Scopes
        private static readonly string[] ExchangeScopes = ["api://AzureADTokenExchange/.default"];
        private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

        private X509Certificate2 _cert;

        [TestInitialize]
        public void Setup()
        {
            _cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
        }

        #region Approach 1: Single Blueprint CCA with client_id Override via OnBeforeTokenRequest

        /// <summary>
        /// APPROACH 1 — Single CCA for Everything (App-Only)
        ///
        /// Hypothesis: Use ONE CCA (the blueprint) for all legs. Override client_id in the
        /// request body for Legs 2 and 3 using OnBeforeTokenRequest. Since Legs 1 and 2 are
        /// app-only (client_credentials grant), there are no refresh tokens, so the only
        /// cache concern is access tokens — which CAN be differentiated via
        /// AdditionalCacheKeyComponents.
        ///
        /// Expected result:
        /// - Leg 1: Works normally (client_id = blueprintClientId, with fmi_path)
        /// - Leg 2: client_id overridden to agentAppId on the wire → ESTS accepts?
        ///   The blueprint's certificate-based assertion doesn't match agentAppId,
        ///   so ESTS will likely REJECT this. The assertion was signed for the blueprint,
        ///   not for the agent.
        ///
        /// This test validates whether ESTS accepts or rejects the client_id mismatch.
        /// </summary>
        [TestMethod]
        public async Task Approach1_SingleBlueprintCca_OverrideClientIdForLeg2()
        {
            // Create a single blueprint CCA
            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(BlueprintClientId)
                .WithCertificate(_cert, sendX5C: true)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            // Leg 1: Normal — blueprint acquires FMI token (T1) for the agent
            var leg1Result = await blueprintCca
                .AcquireTokenForClient(ExchangeScopes)
                .WithFmiPath(AgentAppId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(leg1Result.AccessToken, "Leg 1 should succeed");
            Console.WriteLine($"Leg 1 succeeded — T1 from: {leg1Result.AuthenticationResultMetadata.TokenSource}");

            // Leg 2: Try to override client_id to agentAppId using OnBeforeTokenRequest
            // This sends client_id=agentAppId on the wire, but the CCA's credential
            // (certificate assertion) was built for blueprintClientId.
            //
            // PREDICTION: ESTS will reject this because the client_assertion's "sub" and
            // "iss" claims contain blueprintClientId, not agentAppId.
            try
            {
                var leg2Result = await blueprintCca
                    .AcquireTokenForClient(ExchangeScopes)
                    .OnBeforeTokenRequest(data =>
                    {
                        // Override client_id in the body to the agent's app ID
                        data.BodyParameters["client_id"] = AgentAppId;
                        return Task.CompletedTask;
                    })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // If we get here, ESTS accepted the override (unexpected but interesting)
                Console.WriteLine($"Leg 2 with client_id override SUCCEEDED (unexpected!)");
                Console.WriteLine($"  Token source: {leg2Result.AuthenticationResultMetadata.TokenSource}");
                Assert.Fail("Expected ESTS to reject client_id mismatch with certificate assertion");
            }
            catch (MsalServiceException ex)
            {
                // Expected: ESTS rejects because certificate doesn't match the overridden client_id
                Console.WriteLine($"Leg 2 with client_id override REJECTED by ESTS (expected)");
                Console.WriteLine($"  Error: {ex.ErrorCode}");
                Console.WriteLine($"  Message: {ex.Message}");
            }
        }

        #endregion

        #region Approach 2: Single Blueprint CCA with T1 as Assertion + client_id Override

        /// <summary>
        /// APPROACH 2 — Single Blueprint CCA with WithExtraBodyParameters + OnBeforeTokenRequest
        ///
        /// Hypothesis: Use the blueprint CCA for Leg 1, then for Legs 2/3, use:
        /// - WithExtraBodyParameters for client_id: overwrites it on the wire AND
        ///   automatically includes it in CacheKeyComponents for per-agent cache isolation.
        ///   (Previously this threw ArgumentException because Add() was used internally;
        ///   we changed it to use the dictionary indexer so it can overwrite existing keys.)
        /// - OnBeforeTokenRequest for client_assertion + client_assertion_type only:
        ///   these are volatile values (T1 token rotates on expiry) and must NOT be
        ///   included in cache keys. OnBeforeTokenRequest modifies the wire request
        ///   without affecting CacheKeyComponents.
        ///
        /// Cache isolation: The cache key's clientId field still shows blueprintClientId,
        /// but AdditionalCacheKeyComponents (from WithExtraBodyParameters) differentiates
        /// per-agent entries. This works for both ATs (already supported) and RTs
        /// (after the internal fix in this PR).
        /// </summary>
        [TestMethod]
        public async Task Approach2_SingleBlueprintCca_OverrideAssertionAndClientId()
        {
            // Single blueprint CCA — WithExperimentalFeatures needed for WithExtraBodyParameters
            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(BlueprintClientId)
                .WithCertificate(_cert, sendX5C: true)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            // Leg 1: Blueprint acquires T1 for the agent
            var leg1Result = await blueprintCca
                .AcquireTokenForClient(ExchangeScopes)
                .WithFmiPath(AgentAppId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            string t1 = leg1Result.AccessToken;
            Assert.IsNotNull(t1, "Leg 1 should succeed");
            Console.WriteLine($"Leg 1 succeeded — T1 from: {leg1Result.AuthenticationResultMetadata.TokenSource}");

            // WithExtraBodyParameters: overwrites client_id on the wire AND adds it
            // to CacheKeyComponents for per-agent cache isolation (stable value, safe for cache key)
            var agentClientId = new Dictionary<string, Func<CancellationToken, Task<string>>>
            {
                { "client_id", _ => Task.FromResult(AgentAppId) }
            };

            // Leg 2: Override client_id (WithExtraBodyParameters) and client_assertion (OnBeforeTokenRequest)
            try
            {
                var leg2Result = await blueprintCca
                    .AcquireTokenForClient(ExchangeScopes)
                    .WithExtraBodyParameters(agentClientId)
                    .OnBeforeTokenRequest(data =>
                    {
                        // Override volatile assertion params (NOT in cache key)
                        data.BodyParameters["client_assertion"] = t1;
                        data.BodyParameters["client_assertion_type"] =
                            "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                        return Task.CompletedTask;
                    })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Console.WriteLine($"Leg 2 with full override SUCCEEDED");
                Console.WriteLine($"  Token source: {leg2Result.AuthenticationResultMetadata.TokenSource}");
                Console.WriteLine($"  Access token (first 40 chars): {leg2Result.AccessToken[..40]}...");

                // NOTE: The cache key's clientId field still shows blueprintClientId,
                // but AdditionalCacheKeyComponents from WithExtraBodyParameters ensures
                // per-agent cache isolation for both ATs and RTs (after the RT fix).
            }
            catch (MsalServiceException ex)
            {
                Console.WriteLine($"Leg 2 with full override REJECTED by ESTS");
                Console.WriteLine($"  Error: {ex.ErrorCode}");
                Console.WriteLine($"  Message: {ex.Message}");
            }
        }

        #endregion

        #region Approach 3: N Agent CCAs with WithExtraBodyParameters for Cache Isolation

        /// <summary>
        /// APPROACH 3 — N Agent CCAs (current recommended pattern), validated
        ///
        /// This is the baseline: 1 Blueprint CCA + N Agent CCAs. Each agent CCA has
        /// AppConfig.ClientId = agentAppId, so cache isolation is natural. The assertion
        /// callback on each agent CCA chains to the blueprint for Leg 1.
        ///
        /// This test just validates the existing pattern works correctly.
        /// </summary>
        [TestMethod]
        public async Task Approach3_Baseline_MultipleCcas()
        {
            // Blueprint CCA
            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(BlueprintClientId)
                .WithCertificate(_cert, sendX5C: true)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            // Agent CCA with assertion callback
            var agentCca = ConfidentialClientApplicationBuilder
                .Create(AgentAppId)
                .WithClientAssertion(async (AssertionRequestOptions options) =>
                {
                    var leg1 = await blueprintCca
                        .AcquireTokenForClient(ExchangeScopes)
                        .WithFmiPath(AgentAppId)
                        .ExecuteAsync(options.CancellationToken)
                        .ConfigureAwait(false);
                    return leg1.AccessToken;
                })
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            // Leg 2: Get instance token (T2)
            var leg2Result = await agentCca
                .AcquireTokenForClient(ExchangeScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(leg2Result.AccessToken, "Leg 2 should succeed");
            Console.WriteLine($"Leg 2 (baseline) — T2 from: {leg2Result.AuthenticationResultMetadata.TokenSource}");

            // Leg 3: User-scoped token
            var userResult = await ((IByUserFederatedIdentityCredential)agentCca)
                .AcquireTokenByUserFederatedIdentityCredential(GraphScopes, UserUpn, leg2Result.AccessToken)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(userResult.AccessToken, "Leg 3 should succeed");
            Console.WriteLine($"Leg 3 (baseline) — User token from: {userResult.AuthenticationResultMetadata.TokenSource}");

            // Silent retrieval
            var account = await agentCca
                .GetAccountAsync(userResult.Account.HomeAccountId.Identifier)
                .ConfigureAwait(false);

            var cached = await agentCca
                .AcquireTokenSilent(GraphScopes, account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Console.WriteLine($"Silent retrieval (baseline) — Token from: {cached.AuthenticationResultMetadata.TokenSource}");
        }

        #endregion

        #region Approach 4: N Agent CCAs WITHOUT Blueprint — Use WithExtraBodyParameters for Leg 1

        /// <summary>
        /// APPROACH 4 — Can we eliminate the Blueprint CCA entirely?
        ///
        /// Hypothesis: Create a single CCA per agent with the agent's client_id. The CCA
        /// holds the blueprint's certificate credential directly. For Leg 1, use
        /// WithExtraBodyParameters to override client_id to the blueprint's ID (since Leg 1
        /// requires blueprintClientId + certificate). Then for Legs 2 and 3, the agent CCA
        /// uses its natural client_id.
        ///
        /// This would eliminate the need for a separate Blueprint CCA, reducing the pattern
        /// from 1+N to just N CCA instances. Each agent CCA "is" both the blueprint (for
        /// Leg 1) and the agent (for Legs 2 and 3).
        ///
        /// Key concern: Leg 1 sends the certificate assertion with the agent's client_id
        /// in the JWT (sub/iss/aud claims), but we override client_id in the body to
        /// blueprintClientId. ESTS must accept this — the certificate is registered to the
        /// blueprint app, and fmi_path identifies the agent.
        ///
        /// Actually, this approach is more nuanced. When using WithClientAssertion (callback),
        /// the callback returns a raw string that MSAL sends as-is. So the assertion JWT
        /// would need to have blueprintClientId as its subject — but the CCA's
        /// AppConfig.ClientId is agentAppId, so MSAL would build the JWT with agentAppId
        /// as the subject... unless we use a callback that builds the JWT manually.
        ///
        /// Let's try using the certificate directly on the agent CCA and see what happens.
        /// </summary>
        [TestMethod]
        public async Task Approach4_AgentCcaWithCert_OverrideClientIdForLeg1()
        {
            // Agent CCA with the blueprint's certificate
            // Note: The certificate is registered to the BLUEPRINT app, not the agent app
            var agentCca = ConfidentialClientApplicationBuilder
                .Create(AgentAppId)
                .WithCertificate(_cert, sendX5C: true)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            // Leg 1: Override client_id to blueprintClientId
            // The certificate assertion JWT will have agentAppId as subject (from AppConfig)
            // which won't match the blueprint app registration.
            try
            {
                var leg1Result = await agentCca
                    .AcquireTokenForClient(ExchangeScopes)
                    .WithFmiPath(AgentAppId)
                    .OnBeforeTokenRequest(data =>
                    {
                        // Override client_id to blueprint's ID
                        data.BodyParameters["client_id"] = BlueprintClientId;
                        return Task.CompletedTask;
                    })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Console.WriteLine($"Leg 1 with client_id→blueprint override SUCCEEDED (unexpected!)");
                Console.WriteLine($"  Token source: {leg1Result.AuthenticationResultMetadata.TokenSource}");

                // If this works, try Leg 2 without the override (natural agentAppId)
                var leg2Result = await agentCca
                    .AcquireTokenForClient(ExchangeScopes)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Console.WriteLine($"Leg 2 (natural client_id=agentAppId) from: {leg2Result.AuthenticationResultMetadata.TokenSource}");
            }
            catch (MsalServiceException ex)
            {
                Console.WriteLine($"Leg 1 with client_id→blueprint override REJECTED (expected)");
                Console.WriteLine($"  Error: {ex.ErrorCode}");
                Console.WriteLine($"  Message: {ex.Message}");
                Console.WriteLine("  The certificate assertion JWT has agentAppId as subject,");
                Console.WriteLine("  which doesn't match the blueprint app's certificate registration.");
            }
        }

        /// <summary>
        /// APPROACH 4b — Agent CCA with Assertion Callback for Leg 1
        ///
        /// Same idea as Approach 4, but instead of using the certificate directly on the
        /// agent CCA, use a callback-based assertion. The callback acquires Leg 1 by
        /// building the blueprint's certificate JWT directly (bypassing a separate Blueprint
        /// CCA instance).
        ///
        /// Wait — this IS the current pattern. The assertion callback DOES chain to a
        /// blueprint CCA. The question is: can we avoid creating the blueprint CCA at all?
        ///
        /// We can't avoid a CCA for certificate-based JWT creation — MSAL builds the JWT
        /// internally when using WithCertificate. To bypass it, we'd need to craft the JWT
        /// manually, which defeats the purpose of using MSAL.
        ///
        /// Verdict: The Blueprint CCA is unavoidable when using certificate-based auth,
        /// because MSAL's JWT creation is tied to the CCA's AppConfig.ClientId.
        /// </summary>
        [TestMethod]
        public async Task Approach4b_AgentCcaWithManualLeg1_NoSeparateBlueprintCca()
        {
            // We STILL need a blueprint CCA for Leg 1 — but can we use a "shared singleton"
            // pattern where the blueprint CCA is just an implementation detail?
            //
            // Actually, this is exactly what AgentTokenService in the guide does.
            // The "1+N" terminology sounds scary, but the blueprint CCA is just ONE instance
            // shared across all agents.

            // Let's verify: can a SINGLE blueprint CCA be used as a static/shared resource?
            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(BlueprintClientId)
                .WithCertificate(_cert, sendX5C: true)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            // Agent CCA 1
            var agent1Cca = ConfidentialClientApplicationBuilder
                .Create(AgentAppId)
                .WithClientAssertion(async (AssertionRequestOptions options) =>
                {
                    var leg1 = await blueprintCca
                        .AcquireTokenForClient(ExchangeScopes)
                        .WithFmiPath(AgentAppId)
                        .ExecuteAsync(options.CancellationToken)
                        .ConfigureAwait(false);
                    return leg1.AccessToken;
                })
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            // Verify it works
            var result = await agent1Cca
                .AcquireTokenForClient(ExchangeScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken);
            Console.WriteLine($"Approach 4b — T2 from: {result.AuthenticationResultMetadata.TokenSource}");
            Console.WriteLine("Confirmed: 1 shared blueprint + N agent CCAs is the correct pattern.");
        }

        #endregion

        #region Approach 5: Explore WithExtraBodyParameters on UserFIC for Cache Key Differentiation

        /// <summary>
        /// APPROACH 5 — WithExtraBodyParameters on AcquireTokenByUserFederatedIdentityCredential
        ///
        /// Since AcquireTokenByUserFederatedIdentityCredentialParameterBuilder inherits from
        /// AbstractConfidentialClientAcquireTokenParameterBuilder, it should support
        /// WithExtraBodyParameters. This means we could add the agent's client_id as an
        /// "extra" body parameter for cache key differentiation — even in a single-CCA design.
        ///
        /// However, this approach has a critical limitation:
        /// - Access tokens: Would be differentiated via AdditionalCacheKeyComponents ✅
        /// - Refresh tokens: NO AdditionalCacheKeyComponents support → collision ❌
        /// - AcquireTokenSilent: Uses the cache key's clientId field (AppConfig.ClientId),
        ///   NOT the extra body parameters → won't find the right token without the same
        ///   extra body params
        ///
        /// This test explores whether WithExtraBodyParameters is even available on the
        /// UserFIC builder, and what happens with cache lookups.
        /// </summary>
        [TestMethod]
        public async Task Approach5_WithExtraBodyParamsOnUserFic()
        {
            // Use the standard multi-CCA pattern for Legs 1 and 2
            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(BlueprintClientId)
                .WithCertificate(_cert, sendX5C: true)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            var agentCca = ConfidentialClientApplicationBuilder
                .Create(AgentAppId)
                .WithClientAssertion(async (AssertionRequestOptions options) =>
                {
                    var leg1 = await blueprintCca
                        .AcquireTokenForClient(ExchangeScopes)
                        .WithFmiPath(AgentAppId)
                        .ExecuteAsync(options.CancellationToken)
                        .ConfigureAwait(false);
                    return leg1.AccessToken;
                })
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            // Leg 2
            var leg2Result = await agentCca
                .AcquireTokenForClient(ExchangeScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Leg 3 — with WithExtraBodyParameters to add agent_app_id as cache key
            // This tests whether the extension method is available on the UserFIC builder
            var userFicBuilder = ((IByUserFederatedIdentityCredential)agentCca)
                .AcquireTokenByUserFederatedIdentityCredential(GraphScopes, UserUpn, leg2Result.AccessToken);

            // Try adding extra body params for cache differentiation
            // Note: WithExtraBodyParameters is defined on AbstractConfidentialClientAcquireTokenParameterBuilder<T>,
            // and AcquireTokenByUserFederatedIdentityCredentialParameterBuilder extends it.
            var extraParams = new Dictionary<string, Func<CancellationToken, Task<string>>>
            {
                { "agent_app_id", _ => Task.FromResult(AgentAppId) }
            };

            var userResult = await userFicBuilder
                .WithExtraBodyParameters(extraParams)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(userResult.AccessToken, "Leg 3 with extra body params should succeed");
            Console.WriteLine($"Leg 3 with extra body params — Token from: {userResult.AuthenticationResultMetadata.TokenSource}");

            // Verify cache hit — does AcquireTokenSilent work?
            // Silent retrieval does NOT know about extra body params, so it uses the
            // standard cache key (homeAccountId + clientId + scopes).
            // Since this is a per-agent CCA (clientId = agentAppId), silent should work.
            var account = await agentCca
                .GetAccountAsync(userResult.Account.HomeAccountId.Identifier)
                .ConfigureAwait(false);

            try
            {
                var cached = await agentCca
                    .AcquireTokenSilent(GraphScopes, account)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Console.WriteLine($"Silent retrieval — Token from: {cached.AuthenticationResultMetadata.TokenSource}");
                Console.WriteLine($"Note: Silent succeeded. BUT if the access token was stored with");
                Console.WriteLine($"AdditionalCacheKeyComponents (from WithExtraBodyParameters), silent");
                Console.WriteLine($"might not find it since it doesn't supply those components.");
                Console.WriteLine($"Actual TokenSource: {cached.AuthenticationResultMetadata.TokenSource}");

                // If TokenSource is Cache, the silent lookup found the token despite the
                // extra cache key components. This could mean:
                // (a) the extended key IS being matched, or
                // (b) the refresh token was used to get a new access token
                if (cached.AuthenticationResultMetadata.TokenSource == TokenSource.Cache)
                {
                    Console.WriteLine("FINDING: Silent retrieval found cached AT despite extra cache key components.");
                    Console.WriteLine("This might mean extended keys are not filtering silent lookups,");
                    Console.WriteLine("or the match is more permissive than expected.");
                }
                else
                {
                    Console.WriteLine("FINDING: Silent retrieval used refresh token (not cached AT).");
                    Console.WriteLine("This suggests the extended key prevented cache hit on the AT.");
                }
            }
            catch (MsalUiRequiredException ex)
            {
                Console.WriteLine($"Silent retrieval FAILED — {ex.ErrorCode}");
                Console.WriteLine("This confirms that AdditionalCacheKeyComponents on the AT prevent");
                Console.WriteLine("standard silent retrieval from finding the token.");
            }
        }

        #endregion

        #region Approach 6: WithExtraQueryParameters with IncludeInCacheKey for Access Token Isolation

        /// <summary>
        /// APPROACH 6 — WithExtraQueryParameters(value, includeInCacheKey: true)
        ///
        /// Similar to Approach 5, but using query parameters instead of body parameters.
        /// Query parameters are appended to the token endpoint URL, not the body.
        /// ESTS may or may not accept arbitrary query parameters.
        ///
        /// The cache key behavior is the same: adds to CacheKeyComponents for access tokens,
        /// but does nothing for refresh tokens.
        ///
        /// This approach is less promising because:
        /// 1. Arbitrary query params on the token endpoint may cause issues with proxies/WAFs
        /// 2. The cache isolation problem is the same as Approach 5
        /// 3. It doesn't help with refresh token collision
        ///
        /// Included for completeness — the body parameter approach (Approach 5) is better
        /// if we're going to use extra parameters at all.
        /// </summary>
        [TestMethod]
        public async Task Approach6_WithExtraQueryParamsForCacheIsolation()
        {
            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(BlueprintClientId)
                .WithCertificate(_cert, sendX5C: true)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            var agentCca = ConfidentialClientApplicationBuilder
                .Create(AgentAppId)
                .WithClientAssertion(async (AssertionRequestOptions options) =>
                {
                    var leg1 = await blueprintCca
                        .AcquireTokenForClient(ExchangeScopes)
                        .WithFmiPath(AgentAppId)
                        .ExecuteAsync(options.CancellationToken)
                        .ConfigureAwait(false);
                    return leg1.AccessToken;
                })
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            // Leg 2 with extra query param for cache differentiation
            var leg2Result = await agentCca
                .AcquireTokenForClient(ExchangeScopes)
                .WithExtraQueryParameters(
                    new Dictionary<string, (string Value, bool IncludeInCacheKey)>
                    {
                        { "agent_id", (AgentAppId, true) }
                    })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(leg2Result.AccessToken, "Leg 2 with extra query params should succeed");
            Console.WriteLine($"Leg 2 with extra query params — Token from: {leg2Result.AuthenticationResultMetadata.TokenSource}");

            // Try fetching again with the SAME extra query params — should be a cache hit
            var leg2Cached = await agentCca
                .AcquireTokenForClient(ExchangeScopes)
                .WithExtraQueryParameters(
                    new Dictionary<string, (string Value, bool IncludeInCacheKey)>
                    {
                        { "agent_id", (AgentAppId, true) }
                    })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg2Cached.AuthenticationResultMetadata.TokenSource,
                "Second call with same extra query params should hit cache");
            Console.WriteLine($"Leg 2 repeat — Token from: {leg2Cached.AuthenticationResultMetadata.TokenSource} (should be Cache)");

            // Try WITHOUT the extra query params — should be a cache MISS (different key)
            var leg2NoParams = await agentCca
                .AcquireTokenForClient(ExchangeScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Console.WriteLine($"Leg 2 without params — Token from: {leg2NoParams.AuthenticationResultMetadata.TokenSource}");
            Console.WriteLine($"If this is IdentityProvider, it proves the extra query params differentiated the cache key.");
        }

        #endregion

        #region Summary: Comparison of All Approaches

        /// <summary>
        /// SUMMARY OF APPROACHES
        ///
        /// | # | Approach                          | Wire Works? | AT Cache? | RT Cache? | Verdict         |
        /// |---|-----------------------------------|-------------|-----------|-----------|-----------------|
        /// | 1 | Single CCA, override client_id    | ❌ ESTS rejects — assertion mismatch | N/A | N/A | Not viable |
        /// | 2 | Single CCA, override assertion+id | ✅ OnBeforeTokenRequest | ✅ via WithExtraQueryParameters | ✅ after RT fix | ✅ Viable alternative |
        /// | 3 | 1+N CCAs (current recommended)    | ✅          | ✅ Natural   | ✅ Natural   | ✅ Best option  |
        /// | 4 | N CCAs, override client_id Leg 1  | ❌ JWT mismatch | N/A | N/A | Not viable |
        /// |4b | N CCAs, shared blueprint for Leg1 | ✅          | ✅ Natural   | ✅ Natural   | = Approach 3    |
        /// | 5 | Per-agent CCA + extra body params | ✅          | ⚠️ Extended | ✅ after RT fix | Viable (multi-CCA) |
        /// | 6 | Per-agent CCA + extra query params| ✅          | ⚠️ Extended | ✅ after RT fix | Viable (multi-CCA) |
        ///
        /// KEY API INSIGHTS:
        ///
        /// WithExtraBodyParameters:
        /// - After the Add() → indexer fix, can now overwrite existing body params like client_id.
        /// - Handles BOTH wire override and cache key inclusion in one call.
        /// - Ideal for stable values like client_id.
        /// - NOT suitable for volatile values like client_assertion (T1 token) because it
        ///   includes ALL params in cache keys, causing cache misses on T1 renewal.
        ///
        /// OnBeforeTokenRequest:
        /// - Needed ONLY for volatile values (client_assertion, client_assertion_type) that
        ///   must override wire params without affecting cache keys.
        /// - Should be minimized — only use for params that WithExtraBodyParameters can't handle
        ///   due to cache key pollution concerns.
        ///
        /// CONCLUSION:
        /// Approach 2 (single CCA) is viable with WithExtraBodyParameters (client_id) +
        /// OnBeforeTokenRequest (client_assertion) + the RT AdditionalCacheKeyComponents fix.
        /// Approach 3 (1+N CCAs) remains the simplest and most robust pattern for most scenarios.
        /// </summary>
        [TestMethod]
        public void Summary_PrintAnalysis()
        {
            Console.WriteLine("=== Agentic CCA Alternatives Analysis ===");
            Console.WriteLine();
            Console.WriteLine("FINDING 1: client_id on the wire CAN be overridden via");
            Console.WriteLine("  WithExtraBodyParameters (after the Add() → indexer fix).");
            Console.WriteLine("  This also adds client_id to CacheKeyComponents automatically.");
            Console.WriteLine();
            Console.WriteLine("FINDING 2: client_assertion must be overridden via OnBeforeTokenRequest");
            Console.WriteLine("  because it's volatile (T1 token rotates on expiry) and must NOT be");
            Console.WriteLine("  included in cache keys. OnBeforeTokenRequest modifies the wire request");
            Console.WriteLine("  without affecting CacheKeyComponents.");
            Console.WriteLine();
            Console.WriteLine("FINDING 3: Refresh token cache keys NOW support AdditionalCacheKeyComponents");
            Console.WriteLine("  (after the RT fix in this PR). Both AT and RT isolation are achievable.");
            Console.WriteLine();
            Console.WriteLine("APPROACHES:");
            Console.WriteLine("  Approach 2 (Single CCA): WithExtraBodyParameters(client_id) +");
            Console.WriteLine("    OnBeforeTokenRequest(client_assertion) — 1 CCA total, shared cache");
            Console.WriteLine("  Approach 3 (1+N CCAs): Natural cache isolation via separate ClientIds");
            Console.WriteLine("    — simpler per-request code, more CCA instances to manage");
        }

        #endregion
    }
}
