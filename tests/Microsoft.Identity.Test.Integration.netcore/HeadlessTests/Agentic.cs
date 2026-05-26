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

        #region Multi-CCA Clean Pattern Tests

        // These tests demonstrate the "clean multi-CCA" pattern for agent identity:
        //   - Blueprint CCA: owns the real credential (cert), calls AcquireTokenForClient + WithFmiPath for Leg 1
        //   - Agent CCA: client_id = agentAppId, WithClientAssertion callback chains to Leg 1 (cached by blueprint)
        //     - Leg 2: AcquireTokenForClient on agent CCA for instance token
        //     - Leg 3: AcquireTokenByUserFederatedIdentityCredential on agent CCA for user token
        //   - No WithFmiPathForClientAssertion, no WithClientIdOverride, no AcquireTokenForAgent

        /// <summary>
        /// Builds a blueprint CCA and an agent CCA where the agent's assertion callback
        /// chains back to the blueprint CCA's Leg 1. This is the "setup once, reuse across users" pattern.
        /// </summary>
        private static (IConfidentialClientApplication blueprintCca, IConfidentialClientApplication agentCca)
            BuildBlueprintAndAgentCcaPair()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            var agentCca = ConfidentialClientApplicationBuilder
                .Create(AgentIdentity) // agent's client_id
                .WithClientAssertion(async (AssertionRequestOptions _) =>
                {
                    // Leg 1: Blueprint acquires FMI token (cached by blueprintCca)
                    var leg1 = await blueprintCca
                        .AcquireTokenForClient([TokenExchangeUrl])
                        .WithFmiPath(AgentIdentity)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Trace.WriteLine($"Leg 1 (T1) from: {leg1.AuthenticationResultMetadata.TokenSource}");
                    return leg1.AccessToken;
                })
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .Build();

            return (blueprintCca, agentCca);
        }

        /// <summary>
        /// Full 3-leg agent identity flow using only standard shipped APIs.
        /// Blueprint CCA (Leg 1) → Agent CCA (Leg 2) → Agent CCA (Leg 3, UserFIC).
        /// Verifies that all three legs produce tokens from the identity provider,
        /// and that the Leg 3 user token is cached and retrievable via AcquireTokenSilent.
        /// (T1 and T2 are app tokens — their caching is verified in IntermediateTokensCached_Test.)
        /// </summary>
        [TestMethod]
        public async Task MultiCca_CleanPattern_FullFlow_WithSilent_Test()
        {
            var (_, agentCca) = BuildBlueprintAndAgentCcaPair();

            // Leg 2: Agent instance token
            var leg2Result = await agentCca
                .AcquireTokenForClient([TokenExchangeUrl])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(leg2Result.AccessToken, "T2 should not be null");
            Trace.WriteLine($"Leg 2 (T2) from: {leg2Result.AuthenticationResultMetadata.TokenSource}");

            // Leg 3: User-scoped token
            var leg3Result = await ((IByUserFederatedIdentityCredential)agentCca)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, leg2Result.AccessToken)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(leg3Result.AccessToken, "User token should not be null");
            Assert.IsNotNull(leg3Result.Account, "Account should not be null");
            Assert.AreEqual(TokenSource.IdentityProvider, leg3Result.AuthenticationResultMetadata.TokenSource);
            Trace.WriteLine($"Leg 3 (user token) from: {leg3Result.AuthenticationResultMetadata.TokenSource}");

            // Silent: cached user token
            IAccount account = await agentCca.GetAccountAsync(leg3Result.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            Assert.IsNotNull(account, "Account should be retrievable from cache");

            var silentResult = await agentCca
                .AcquireTokenSilent([Scope], account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silentResult.AuthenticationResultMetadata.TokenSource,
                "Token should come from cache");
            Assert.AreEqual(leg3Result.AccessToken, silentResult.AccessToken,
                "Cached token should match the original");
            Trace.WriteLine($"Silent result from: {silentResult.AuthenticationResultMetadata.TokenSource}");
        }

        /// <summary>
        /// Verifies that the Leg 2 (instance token) is cached by the agent CCA and returned
        /// from cache on a repeat AcquireTokenForClient call (the assertion callback does not
        /// fire again because Leg 2 is served from cache).
        /// Leg 1 caching is tested separately in Leg1_DifferentFmiPaths_CachedIndependently_Test.
        /// </summary>
        [TestMethod]
        public async Task MultiCca_CleanPattern_IntermediateTokensCached_Test()
        {
            var (blueprintCca, agentCca) = BuildBlueprintAndAgentCcaPair();

            // Leg 2: triggers assertion callback → Leg 1 (hits identity provider)
            var leg2Result1 = await agentCca
                .AcquireTokenForClient([TokenExchangeUrl])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, leg2Result1.AuthenticationResultMetadata.TokenSource,
                "First Leg 2 call should hit identity provider");

            // Leg 2 repeat: should come from agent CCA's app token cache
            // (The assertion callback won't even fire because Leg 2 is cached)
            var leg2Result2 = await agentCca
                .AcquireTokenForClient([TokenExchangeUrl])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg2Result2.AuthenticationResultMetadata.TokenSource,
                "Second Leg 2 call should come from cache");
            Assert.AreEqual(leg2Result1.AccessToken, leg2Result2.AccessToken,
                "Cached T2 should match original");
        }

        /// <summary>
        /// App-only agent identity: only Legs 1-2, no user involved.
        /// The agent CCA acquires a client_credentials token for a downstream API (e.g. Graph).
        /// </summary>
        [TestMethod]
        public async Task MultiCca_CleanPattern_AppOnly_Test()
        {
            var (_, agentCca) = BuildBlueprintAndAgentCcaPair();

            // App-only: agent acquires a client_credentials token for Graph (no UserFIC)
            var result = await agentCca
                .AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken, "Agent app token should not be null");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Trace.WriteLine($"Agent app token from: {result.AuthenticationResultMetadata.TokenSource}");

            // Cached on repeat
            var cached = await agentCca
                .AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource,
                "Agent app token should be cached");
        }

        /// <summary>
        /// OID-based user identification: the shipped AcquireTokenByUserFederatedIdentityCredential
        /// only accepts a UPN (string username). A Guid overload for OID does not yet exist.
        ///
        /// This test is a STUB documenting the gap and verifying that the UPN path works,
        /// then noting what would be needed for OID support.
        ///
        /// REQUIRED CHANGE: Add a Guid overload to IByUserFederatedIdentityCredential that sends
        /// "user_id" instead of "username" in the POST body. See PR #5883 for the proposed implementation.
        /// </summary>
        [TestMethod]
        public async Task MultiCca_CleanPattern_OidNotYetSupported_Test()
        {
            var (_, agentCca) = BuildBlueprintAndAgentCcaPair();

            // Leg 2
            var leg2Result = await agentCca
                .AcquireTokenForClient([TokenExchangeUrl])
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Leg 3: UPN path works
            var upnResult = await ((IByUserFederatedIdentityCredential)agentCca)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, leg2Result.AccessToken)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(upnResult.Account, "Account should not be null");
            string userOid = upnResult.Account.HomeAccountId.ObjectId;
            Assert.IsNotNull(userOid, "User OID should be discoverable from UPN-based result");
            Trace.WriteLine($"Discovered user OID: {userOid}");

            // OID path: the Guid overload does not exist on the shipped interface.
            // A customer who identifies users by OID would currently need to do something like:
            //
            //   var oidResult = await agentCca
            //       .AcquireTokenForClient(scopes)
            //       .OnBeforeTokenRequest(data =>
            //       {
            //           data.BodyParameters["grant_type"] = "user_fic";
            //           data.BodyParameters["user_id"] = userOid;
            //           data.BodyParameters["user_federated_identity_credential"] = t2Token;
            //           return Task.CompletedTask;
            //       })
            //       .ExecuteAsync();
            //
            // This is the kind of manual body manipulation we want to eliminate.
            // The fix is to add: AcquireTokenByUserFederatedIdentityCredential(scopes, Guid userObjectId, assertion)
        }

        /// <summary>
        /// Blueprint CCA is used for both a standard client_credentials call AND as the
        /// blueprint for agent identity. Verifies the two uses don't interfere.
        /// </summary>
        [TestMethod]
        public async Task MultiCca_CleanPattern_BlueprintUsedForBothAgentAndNonAgent_Test()
        {
            var (blueprintCca, agentCca) = BuildBlueprintAndAgentCcaPair();

            // Blueprint: standard client_credentials for its own purposes (non-agent)
            var blueprintResult = await blueprintCca
                .AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(blueprintResult.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, blueprintResult.AuthenticationResultMetadata.TokenSource);

            // Agent: full 3-leg flow using the same blueprint's Leg 1
            var leg2Result = await agentCca
                .AcquireTokenForClient([TokenExchangeUrl])
                .ExecuteAsync()
                .ConfigureAwait(false);

            var leg3Result = await ((IByUserFederatedIdentityCredential)agentCca)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, leg2Result.AccessToken)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(leg3Result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, leg3Result.AuthenticationResultMetadata.TokenSource);

            // Blueprint's non-agent token should still be cached
            var blueprintCached = await blueprintCca
                .AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, blueprintCached.AuthenticationResultMetadata.TokenSource,
                "Blueprint's non-agent token should still be cached after agent operations");
            Assert.AreEqual(blueprintResult.AccessToken, blueprintCached.AccessToken,
                "Blueprint's cached token should be unchanged");
        }

        /// <summary>
        /// Verifies that calling UFIC twice for the same user hits the network both times.
        /// Then confirms AcquireTokenSilent returns the cached token.
        /// This demonstrates the recommended pattern: UFIC once, then AcquireTokenSilent.
        /// </summary>
        [TestMethod]
        public async Task MultiCca_CleanPattern_UficAlwaysHitsNetwork_SilentWorks_Test()
        {
            var (_, agentCca) = BuildBlueprintAndAgentCcaPair();

            // Leg 2
            var leg2Result = await agentCca
                .AcquireTokenForClient([TokenExchangeUrl])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"Leg 2 from: {leg2Result.AuthenticationResultMetadata.TokenSource}");

            // Leg 3 (first call): hits identity provider
            var ufic1 = await ((IByUserFederatedIdentityCredential)agentCca)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, leg2Result.AccessToken)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, ufic1.AuthenticationResultMetadata.TokenSource,
                "First UFIC call should hit identity provider");
            Trace.WriteLine($"UFIC call 1 from: {ufic1.AuthenticationResultMetadata.TokenSource}");

            // Leg 3 (second call, same user, same scopes): also hits identity provider
            var ufic2 = await ((IByUserFederatedIdentityCredential)agentCca)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, leg2Result.AccessToken)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, ufic2.AuthenticationResultMetadata.TokenSource,
                "Second UFIC call should ALSO hit identity provider (UFIC never checks cache)");
            Trace.WriteLine($"UFIC call 2 from: {ufic2.AuthenticationResultMetadata.TokenSource}");

            // AcquireTokenSilent: returns from cache
            var account = await agentCca.GetAccountAsync(ufic2.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            var silent = await agentCca
                .AcquireTokenSilent([Scope], account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silent.AuthenticationResultMetadata.TokenSource,
                "AcquireTokenSilent should return from cache");
            Trace.WriteLine($"Silent from: {silent.AuthenticationResultMetadata.TokenSource}");
        }

        /// <summary>
        /// Verifies that Leg 1 tokens for different agents are cached independently
        /// on the same blueprint CCA (different fmi_path = different cache entries).
        /// </summary>
        [TestMethod]
        public async Task MultiCca_CleanPattern_Leg1_DifferentFmiPaths_CachedIndependently_Test()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            // Leg 1 for AgentIdentity
            var t1Agent1 = await blueprintCca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, t1Agent1.AuthenticationResultMetadata.TokenSource);
            Trace.WriteLine($"T1 for agent 1 from: {t1Agent1.AuthenticationResultMetadata.TokenSource}");

            // Repeat Leg 1 for same agent — should be cached
            var t1Agent1Cached = await blueprintCca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, t1Agent1Cached.AuthenticationResultMetadata.TokenSource,
                "Repeat Leg 1 for same agent should return from cache");
            Assert.AreEqual(t1Agent1.AccessToken, t1Agent1Cached.AccessToken);

            // Leg 1 for a different fmi_path (using ClientId as a stand-in for a second agent)
            // This should hit the network because fmi_path is part of the cache key
            var t1Agent2 = await blueprintCca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(ClientId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, t1Agent2.AuthenticationResultMetadata.TokenSource,
                "Leg 1 for different fmi_path should hit identity provider");
            Trace.WriteLine($"T1 for agent 2 from: {t1Agent2.AuthenticationResultMetadata.TokenSource}");

            // Original agent's T1 should still be cached and unchanged
            var t1Agent1Still = await blueprintCca
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, t1Agent1Still.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(t1Agent1.AccessToken, t1Agent1Still.AccessToken,
                "Agent 1's T1 should still be cached after Agent 2's Leg 1");
        }

        #endregion
    }
}

