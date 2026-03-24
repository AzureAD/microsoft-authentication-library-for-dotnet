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

        #region UserFIC Primitive + App-only Tests (Lower-Level API)

        /// <summary>
        /// Validates the low-level UserFIC primitive: builds separate assertion and main CCAs,
        /// acquires a user_fic assertion via FMI, then exchanges it for a user-scoped Graph token
        /// using the UPN-based AcquireTokenByUserFederatedIdentityCredential overload.
        /// Also verifies that the resulting token is cached and can be retrieved silently.
        /// </summary>
        [TestMethod]
        public async Task AgentUserIdentityGetsTokenForGraphTest()
        {
            await AgentUserIdentityGetsTokenForGraphAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Validates app-only (no user) token acquisition for an agent app.
        /// The agent CCA uses an FMI-based client assertion to get a client credentials token
        /// for Graph, without any user identity involved.
        /// </summary>
        [TestMethod]
        public async Task AgentGetsAppTokenForGraphTest()
        {
            await AgentGetsAppTokenForGraph().ConfigureAwait(false);
        }

        private static async Task AgentGetsAppTokenForGraph()
        {
            var cca = ConfidentialClientApplicationBuilder
                        .Create(AgentIdentity)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithClientAssertion((AssertionRequestOptions _) => GetAppCredentialAsync(AgentIdentity))
                        .Build();

            var result = await cca.AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"FMI app credential from : {result.AuthenticationResultMetadata.TokenSource}");
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

        #endregion

        #region AcquireTokenForAgent Tests (High-Level API)

        /// <summary>
        /// Tests the high-level AcquireTokenForAgent API with a UPN-based AgentIdentity.
        /// This exercises the full 3-leg flow (FMI credential → assertion → UserFIC exchange)
        /// orchestrated internally by AgentTokenRequest, using a blueprint CCA with SN+I certificate.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_WithUpn_Test()
        {
            // Arrange: Blueprint CCA configured with certificate (SN+I) for FMI flows
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            var agentId = Client.AgentIdentity.WithUsername(AgentIdentity, UserUpn);

            // Act: Use the high-level AcquireTokenForAgent API
            var result = await blueprintCca
                .AcquireTokenForAgent([Scope], agentId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.AccessToken, "Access token should not be null");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource, "Token should be from identity provider");

            Trace.WriteLine($"AcquireTokenForAgent (UPN) token source: {result.AuthenticationResultMetadata.TokenSource}");
        }

        /// <summary>
        /// Tests the high-level AcquireTokenForAgent API for app-only (no user) scenarios.
        /// Only Legs 1-2 are performed: the blueprint CCA fetches an FMI credential, then
        /// an internal agent CCA uses it to get a client credentials token for Graph.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_AppOnly_Test()
        {
            // Arrange: Blueprint CCA configured with certificate (SN+I) for FMI flows
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            var agentId = Client.AgentIdentity.AppOnly(AgentIdentity);

            // Act: Use the high-level AcquireTokenForAgent API for app-only scenario
            var result = await blueprintCca
                .AcquireTokenForAgent([Scope], agentId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.AccessToken, "Access token should not be null");

            Trace.WriteLine($"AcquireTokenForAgent (AppOnly) token source: {result.AuthenticationResultMetadata.TokenSource}");
        }

        #endregion

        #region UserFIC Guid Overload Tests

        /// <summary>
        /// Tests the Guid (OID) overload of the low-level AcquireTokenByUserFederatedIdentityCredential primitive.
        /// First discovers the user's OID by calling the UPN-based flow, then acquires a fresh assertion
        /// and calls the Guid overload to verify it sends user_id (OID) instead of username (UPN).
        /// </summary>
        [TestMethod]
        public async Task UserFic_WithGuidObjectId_Test()
        {
            // Arrange: First obtain a token via UPN to get the user's OID
            var assertionApp = ConfidentialClientApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithClientAssertion(async (AssertionRequestOptions a) =>
                {
                    return await GetAppCredentialAsync(a.ClientAssertionFmiPath ?? AgentIdentity).ConfigureAwait(false);
                })
                .Build();

            var cca = ConfidentialClientApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithExperimentalFeatures(true)
                .WithClientAssertion((AssertionRequestOptions _) => GetAppCredentialAsync(AgentIdentity))
                .Build();

            // Step 1: Get assertion via FMI path
            var assertionResult = await assertionApp
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPathForClientAssertion(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            string assertion1 = assertionResult.AccessToken;

            // Step 2: Get user token via UPN to discover the user's OID
            var upnResult = await (cca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, assertion1)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(upnResult.Account, "Account should not be null");

            // Extract the user OID from the account's HomeAccountId (format: oid.tid)
            string oidString = upnResult.Account.HomeAccountId.ObjectId;
            Assert.IsNotNull(oidString, "OID should not be null");
            Guid userOid = Guid.Parse(oidString);

            Trace.WriteLine($"Discovered user OID: {userOid}");

            // Step 3: Now acquire a NEW assertion (since the first one was consumed)
            var assertionResult2 = await assertionApp
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithForceRefresh(true)
                .WithFmiPathForClientAssertion(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            string assertion2 = assertionResult2.AccessToken;

            // Act: Use the Guid overload of AcquireTokenByUserFederatedIdentityCredential
            var result = await (cca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], userOid, assertion2)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.AccessToken, "Access token should not be null");
            Assert.AreEqual(oidString, result.Account.HomeAccountId.ObjectId, "OID should match");

            Trace.WriteLine($"UserFIC Guid overload token source: {result.AuthenticationResultMetadata.TokenSource}");
        }

        /// <summary>
        /// Tests the high-level AcquireTokenForAgent API with a Guid (OID)-based AgentIdentity.
        /// Discovers the user's OID via the UPN path first, then creates an AgentIdentity(agentAppId, userOid)
        /// and verifies the full 3-leg flow succeeds using the OID-based UserFIC exchange.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_WithOid_Test()
        {
            // Arrange: First discover the user's OID by running a UPN-based flow
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            // Get the OID via the UPN path
            var upnResult = await blueprintCca
                .AcquireTokenForAgent([Scope], Client.AgentIdentity.WithUsername(AgentIdentity, UserUpn))
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(upnResult.Account, "Account should not be null after UPN-based flow");
            Guid userOid = Guid.Parse(upnResult.Account.HomeAccountId.ObjectId);
            Trace.WriteLine($"Discovered user OID: {userOid}");

            // Act: Build a new blueprint CCA and use the OID-based AgentIdentity
            var blueprintCca2 = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            var agentId = new Client.AgentIdentity(AgentIdentity, userOid);

            var result = await blueprintCca2
                .AcquireTokenForAgent([Scope], agentId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.AccessToken, "Access token should not be null");
            Assert.AreEqual(
                upnResult.Account.HomeAccountId.ObjectId,
                result.Account.HomeAccountId.ObjectId,
                "OID should match between UPN and OID flows");

            Trace.WriteLine($"AcquireTokenForAgent (OID) token source: {result.AuthenticationResultMetadata.TokenSource}");
        }

        #endregion

        #region Cache Isolation Tests

        /// <summary>
        /// Verifies that two separate blueprint CCA instances maintain independent caches,
        /// and that the internal agent CCAs created by AcquireTokenForAgent are cached and
        /// reused within each blueprint (so subsequent agent calls hit the cache).
        ///
        /// Scenario:
        ///   CCA1 (blueprint1): makes a non-agent client credential call, then a silent call → cache hit.
        ///   CCA2 (blueprint2): makes a non-agent client credential call, then a silent call → cache hit;
        ///     then makes an agent call (UPN) → identity provider; then a second identical agent call → cache hit.
        ///   Finally, verifies CCA1's agent CCA cache is empty (no bleed from CCA2).
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_CacheIsolation_Test()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // === CCA1: Non-agent only ===
            var cca1 = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            // CCA1: First call hits the identity provider
            var cca1Result1 = await cca1
                .AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(cca1Result1.AccessToken, "CCA1 first call should return a token");
            Assert.AreEqual(TokenSource.IdentityProvider, cca1Result1.AuthenticationResultMetadata.TokenSource,
                "CCA1 first call should come from identity provider");

            // CCA1: Second call should come from cache
            var cca1Result2 = await cca1
                .AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, cca1Result2.AuthenticationResultMetadata.TokenSource,
                "CCA1 second call should come from cache");

            // === CCA2: Non-agent + agent ===
            var cca2 = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            // CCA2: Non-agent call - should NOT get CCA1's cached token (separate instance, separate cache)
            var cca2Result1 = await cca2
                .AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(cca2Result1.AccessToken, "CCA2 first call should return a token");
            Assert.AreEqual(TokenSource.IdentityProvider, cca2Result1.AuthenticationResultMetadata.TokenSource,
                "CCA2 first call should come from identity provider (no cache bleed from CCA1)");

            // CCA2: Non-agent silent call - should come from CCA2's own cache
            var cca2Result2 = await cca2
                .AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, cca2Result2.AuthenticationResultMetadata.TokenSource,
                "CCA2 second call should come from its own cache");

            // CCA2: Agent call (first time) - should hit identity provider
            var agentId = Client.AgentIdentity.WithUsername(AgentIdentity, UserUpn);

            var agentResult1 = await cca2
                .AcquireTokenForAgent([Scope], agentId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(agentResult1.AccessToken, "Agent first call should return a token");
            Assert.AreEqual(TokenSource.IdentityProvider, agentResult1.AuthenticationResultMetadata.TokenSource,
                "Agent first call should come from identity provider");

            // CCA2: Agent call (second time, same identity) - should come from the cached internal CCA
            var agentResult2 = await cca2
                .AcquireTokenForAgent([Scope], agentId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, agentResult2.AuthenticationResultMetadata.TokenSource,
                "Agent second call should come from cache (internal CCA reuse)");

            // Verify CCA1 has no agent CCA cache entries (no bleed from CCA2's agent operations)
            var cca1Cache = (ConfidentialClientApplication)cca1;
            Assert.IsTrue(cca1Cache.AgentCcaCache.IsEmpty,
                "CCA1 should have no agent CCA cache entries");

            // Verify CCA2 has agent CCA cache entries (the internal CCAs were cached)
            var cca2Cache = (ConfidentialClientApplication)cca2;
            Assert.IsFalse(cca2Cache.AgentCcaCache.IsEmpty,
                "CCA2 should have cached agent CCA instances");

            Trace.WriteLine($"CCA2 agent CCA cache size: {cca2Cache.AgentCcaCache.Count}");
        }

        /// <summary>
        /// Verifies that WithForceRefresh(true) on AcquireTokenForAgent bypasses the
        /// internal AcquireTokenSilent cache check and always hits the identity provider.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_ForceRefresh_Test()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var cca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            var agentId = Client.AgentIdentity.WithUsername(AgentIdentity, UserUpn);

            // First call: hits identity provider and populates cache
            var result1 = await cca
                .AcquireTokenForAgent([Scope], agentId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource,
                "First call should come from identity provider");

            // Second call without ForceRefresh: should come from cache
            var result2 = await cca
                .AcquireTokenForAgent([Scope], agentId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource,
                "Second call should come from cache");

            // Third call with ForceRefresh: should bypass cache and hit identity provider
            var result3 = await cca
                .AcquireTokenForAgent([Scope], agentId)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result3.AuthenticationResultMetadata.TokenSource,
                "ForceRefresh call should bypass cache and come from identity provider");
        }

        /// <summary>
        /// Verifies that the internal AcquireTokenSilent account-matching logic correctly
        /// resolves cached tokens when switching between UPN-based and OID-based AgentIdentity
        /// for the same user on the same blueprint CCA.
        ///
        /// Scenario:
        ///   1. AcquireTokenForAgent with UPN → hits identity provider, populates cache.
        ///   2. AcquireTokenForAgent with UPN again → cache hit (UPN match).
        ///   3. AcquireTokenForAgent with OID (same user) → cache hit (OID match on the same account).
        ///
        /// This proves that the FindMatchingAccount logic works for both identifier types
        /// and that an OID lookup can find a token originally cached via a UPN-based call.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_UpnThenOid_SharesCache_Test()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var cca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            // Step 1: UPN-based call → identity provider (populates cache)
            var upnIdentity = Client.AgentIdentity.WithUsername(AgentIdentity, UserUpn);

            var upnResult = await cca
                .AcquireTokenForAgent([Scope], upnIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, upnResult.AuthenticationResultMetadata.TokenSource,
                "First UPN call should come from identity provider");
            Assert.IsNotNull(upnResult.Account, "Account should not be null");

            // Extract the OID from the returned account
            string oidString = upnResult.Account.HomeAccountId.ObjectId;
            Assert.IsNotNull(oidString, "OID should not be null in the account");
            Guid userOid = Guid.Parse(oidString);

            // Step 2: UPN-based call again → cache hit (sanity check)
            var upnResult2 = await cca
                .AcquireTokenForAgent([Scope], upnIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, upnResult2.AuthenticationResultMetadata.TokenSource,
                "Second UPN call should come from cache");

            // Step 3: OID-based call for the SAME user → should also be a cache hit
            // because FindMatchingAccount matches by HomeAccountId.ObjectId
            var oidIdentity = new Client.AgentIdentity(AgentIdentity, userOid);

            var oidResult = await cca
                .AcquireTokenForAgent([Scope], oidIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, oidResult.AuthenticationResultMetadata.TokenSource,
                "OID call for the same user should come from cache (OID-based account match)");
            Assert.AreEqual(oidString, oidResult.Account.HomeAccountId.ObjectId,
                "OID should match between UPN-cached and OID-retrieved tokens");

            Trace.WriteLine($"UPN token: {upnResult.AccessToken.Substring(0, 20)}...");
            Trace.WriteLine($"OID token: {oidResult.AccessToken.Substring(0, 20)}...");
        }

        #endregion

        #region Shared Helpers

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

        #endregion
    }
}
