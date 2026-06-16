// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// Demonstrates the recommended agent identity pattern using the multi-CCA approach:
    /// 1 Blueprint CCA (owns cert, handles Leg 1) + 1 Agent CCA (assertion callback chains to Blueprint).
    /// Covers UPN-based and OID-based UserFIC scenarios.
    /// </summary>
    [TestClass]
    public class AgentIdentitySample
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

        private IConfidentialClientApplication _blueprintCca;
        private IConfidentialClientApplication _agentCca;

        [TestInitialize]
        public void Setup()
        {
            // Step 1: Create the Blueprint CCA (owns the real cert)
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            _blueprintCca = ConfidentialClientApplicationBuilder
                .Create(BlueprintClientId)
                .WithCertificate(cert, sendX5C: true) // SN+I required for FMI
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();

            // Step 2: Create the Agent CCA (assertion callback chains to Blueprint for Leg 1)
            _agentCca = ConfidentialClientApplicationBuilder
                .Create(AgentAppId)
                .WithClientAssertion(async (AssertionRequestOptions options) =>
                {
                    // Leg 1: Blueprint acquires FMI token (T1) for this agent.
                    // AcquireTokenForClient caches T1 automatically.
                    AuthenticationResult leg1 = await _blueprintCca
                        .AcquireTokenForClient(ExchangeScopes)
                        .WithFmiPath(AgentAppId)
                        .ExecuteAsync(options.CancellationToken)
                        .ConfigureAwait(false);

                    return leg1.AccessToken;
                })
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .Build();
        }

        /// <summary>
        /// Scenario 1: App-only token — agent gets a Graph token with no user context.
        /// </summary>
        [TestMethod]
        public async Task Scenario1_AppOnlyToken()
        {
            // Step 3: AcquireTokenForClient — assertion callback handles Leg 1 transparently
            var result = await _agentCca
                .AcquireTokenForClient(GraphScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result.AccessToken);
            Console.WriteLine($"Scenario 1 PASSED — App token from: {result.AuthenticationResultMetadata.TokenSource}");
        }

        /// <summary>
        /// Scenario 2: User-scoped token via UPN — full 3-leg flow.
        /// </summary>
        [TestMethod]
        public async Task Scenario2_UserTokenByUpn()
        {
            // Step 4 (UPN): Leg 2 — get instance token (T2) via agent CCA
            var leg2 = await _agentCca
                .AcquireTokenForClient(ExchangeScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Leg 3 — exchange T2 + UPN for user-scoped token
            var userResult = await ((IByUserFederatedIdentityCredential)_agentCca)
                .AcquireTokenByUserFederatedIdentityCredential(GraphScopes, UserUpn, leg2.AccessToken)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(userResult.AccessToken);
            Console.WriteLine($"Scenario 2 PASSED — User token (UPN) from: {userResult.AuthenticationResultMetadata.TokenSource}");

            // Step 5: Silent retrieval from cache
            IAccount account = await _agentCca
                .GetAccountAsync(userResult.Account.HomeAccountId.Identifier)
                .ConfigureAwait(false);

            var cached = await _agentCca
                .AcquireTokenSilent(GraphScopes, account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Console.WriteLine($"Scenario 2 silent PASSED — Token from: {cached.AuthenticationResultMetadata.TokenSource}");
        }

        /// <summary>
        /// Scenario 3: User-scoped token via OID — same 3-leg flow but identifies user by Object ID.
        /// </summary>
        [TestMethod]
        public async Task Scenario3_UserTokenByOid()
        {
            // First, get the user's OID via a UPN-based call (in production you'd already have this)
            var leg2 = await _agentCca
                .AcquireTokenForClient(ExchangeScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            var upnResult = await ((IByUserFederatedIdentityCredential)_agentCca)
                .AcquireTokenByUserFederatedIdentityCredential(GraphScopes, UserUpn, leg2.AccessToken)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Extract OID from the account (HomeAccountId format: "oid.tid")
            string oidString = upnResult.Account.HomeAccountId.ObjectId;
            Guid userObjectId = Guid.Parse(oidString);
            Console.WriteLine($"User OID: {userObjectId}");

            // Clear the cache so we can demonstrate OID-based acquisition from scratch
            var account = await _agentCca
                .GetAccountAsync(upnResult.Account.HomeAccountId.Identifier)
                .ConfigureAwait(false);
            await _agentCca.RemoveAsync(account).ConfigureAwait(false);

            // Step 4 (OID): Leg 2 — get a fresh instance token
            var leg2Again = await _agentCca
                .AcquireTokenForClient(ExchangeScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Leg 3 — exchange T2 + OID (Guid) for user-scoped token
            var oidResult = await ((IByUserFederatedIdentityCredential)_agentCca)
                .AcquireTokenByUserFederatedIdentityCredential(GraphScopes, userObjectId, leg2Again.AccessToken)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(oidResult.AccessToken);
            Console.WriteLine($"Scenario 3 PASSED — User token (OID) from: {oidResult.AuthenticationResultMetadata.TokenSource}");

            // Verify silent retrieval works for OID-acquired tokens too
            var oidAccount = await _agentCca
                .GetAccountAsync(oidResult.Account.HomeAccountId.Identifier)
                .ConfigureAwait(false);

            var cachedOid = await _agentCca
                .AcquireTokenSilent(GraphScopes, oidAccount)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, cachedOid.AuthenticationResultMetadata.TokenSource);
            Console.WriteLine($"Scenario 3 silent PASSED — Token from: {cachedOid.AuthenticationResultMetadata.TokenSource}");
        }
    }
}
