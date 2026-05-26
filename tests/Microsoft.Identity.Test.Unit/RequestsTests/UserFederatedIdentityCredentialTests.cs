// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class UserFederatedIdentityCredentialTests : TestBase
    {
        private const string FakeAssertion = "fake.assertion.jwt";
        private const string FakeUsername = "user@contoso.com";

        private MockHttpMessageHandler AddMockHandlerForUserFic(
            MockHttpManager httpManager,
            string authority = TestConstants.AuthorityCommonTenant)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.GrantType, OAuth2GrantType.UserFic },
                    { OAuth2Parameter.Username, FakeUsername },
                    { OAuth2Parameter.UserFederatedIdentityCredential, FakeAssertion }
                },
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            };

            httpManager.AddMockHandler(handler);
            return handler;
        }

        private ConfidentialClientApplication BuildCCA(MockHttpManager httpManager)
        {
            return ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithHttpManager(httpManager)
                .BuildConcrete();
        }

        [TestMethod]
        public async Task AcquireTokenByUserFic_SendsCorrectOAuth2Parameters_Async()
        {
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            AddMockHandlerForUserFic(httpManager);

            var app = BuildCCA(httpManager);

            var result = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    FakeUsername,
                    FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        public async Task AcquireTokenByUserFic_TokenIsStoredInUserCache_Async()
        {
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            AddMockHandlerForUserFic(httpManager);

            var app = BuildCCA(httpManager);

            var result = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    FakeUsername,
                    FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account, "Account should be returned from the user cache.");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            // Token should be stored in user token cache
            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts, "Accounts should not be null after token is stored in user cache.");
        }

        [TestMethod]
        public async Task AcquireTokenByUserFic_WithForceRefresh_CallsIdentityProvider_Async()
        {
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            // Two mock handlers are added; MockHttpManager verifies both are consumed,
            // confirming that two separate calls to the identity provider were made.
            AddMockHandlerForUserFic(httpManager);
            var app = BuildCCA(httpManager);

            var firstResult = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    FakeUsername,
                    FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, firstResult.AuthenticationResultMetadata.TokenSource);

            // Second call with ForceRefresh - should call the identity provider again
            AddMockHandlerForUserFic(httpManager);

            var secondResult = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    FakeUsername,
                    FakeAssertion)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, secondResult.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        public void AcquireTokenByUserFic_NullUsername_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var app = BuildCCA(httpManager);

            AssertException.Throws<ArgumentNullException>(() =>
                (app as IByUserFederatedIdentityCredential)
                    .AcquireTokenByUserFederatedIdentityCredential(
                        TestConstants.s_scope,
                        username: null,
                        assertion: FakeAssertion));
        }

        [TestMethod]
        public void AcquireTokenByUserFic_NullAssertion_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var app = BuildCCA(httpManager);

            AssertException.Throws<ArgumentNullException>(() =>
                (app as IByUserFederatedIdentityCredential)
                    .AcquireTokenByUserFederatedIdentityCredential(
                        TestConstants.s_scope,
                        username: FakeUsername,
                        assertion: null));
        }

        [TestMethod]
        public void AcquireTokenByUserFic_EmptyAssertion_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var app = BuildCCA(httpManager);

            AssertException.Throws<ArgumentNullException>(() =>
                (app as IByUserFederatedIdentityCredential)
                    .AcquireTokenByUserFederatedIdentityCredential(
                        TestConstants.s_scope,
                        username: FakeUsername,
                        assertion: string.Empty));
        }

        #region Multi-CCA Agent Identity Pattern Tests

        // These tests validate the "clean multi-CCA" pattern for agent identity scenarios,
        // using only standard shipped APIs: AcquireTokenForClient + WithFmiPath (Leg 1),
        // AcquireTokenForClient on an agent CCA with WithClientAssertion (Leg 2),
        // and AcquireTokenByUserFederatedIdentityCredential (Leg 3).
        //
        // No WithFmiPathForClientAssertion, no WithClientIdOverride, no AcquireTokenForAgent.

        private const string AgentAppId = "00000000-0000-0000-0000-000000001234";
        private const string AgentAppId2 = "00000000-0000-0000-0000-000000005678";

        /// <summary>
        /// Builds a "blueprint" CCA (the parent app that owns the real credential).
        /// </summary>
        private ConfidentialClientApplication BuildBlueprintCCA(MockHttpManager httpManager)
        {
            return ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithHttpManager(httpManager)
                .BuildConcrete();
        }

        /// <summary>
        /// Builds an "agent" CCA (client_id = agentAppId, credential = T1 via assertion callback).
        /// The assertion callback simulates Leg 1 by returning a fixed T1 token.
        /// </summary>
        private ConfidentialClientApplication BuildAgentCCA(MockHttpManager httpManager, string agentAppId, string t1Token)
        {
            return ConfidentialClientApplicationBuilder
                .Create(agentAppId)
                .WithClientAssertion((AssertionRequestOptions _) =>
                    Task.FromResult(t1Token))
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithHttpManager(httpManager)
                .BuildConcrete();
        }

        /// <summary>
        /// Adds a mock handler for AcquireTokenForClient (Leg 1 or Leg 2).
        /// Returns a client_credentials response with the specified access token.
        /// </summary>
        private static void AddMockHandlerForClientCredentials(
            MockHttpManager httpManager,
            string accessToken)
        {
            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(token: accessToken)
            });
        }

        /// <summary>
        /// Adds a mock handler for a UserFIC call with a specific user identity in the response.
        /// </summary>
        private static void AddMockHandlerForUserFicWithIdentity(
            MockHttpManager httpManager,
            string username,
            string userOid,
            string accessToken,
            string authority = TestConstants.AuthorityCommonTenant)
        {
            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.GrantType, OAuth2GrantType.UserFic },
                    { OAuth2Parameter.Username, username },
                    { OAuth2Parameter.UserFederatedIdentityCredential, FakeAssertion }
                },
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                    userOid,
                    username,
                    TestConstants.s_scope.ToArray(),
                    accessToken: accessToken)
            });
        }

        /// <summary>
        /// Full multi-CCA agent identity flow: blueprint CCA (Leg 1) → agent CCA (Leg 2 + Leg 3).
        /// Verifies that all three legs produce tokens from the identity provider,
        /// and that the Leg 3 user token is cached and retrievable via AcquireTokenSilent.
        /// (T1 and T2 are app tokens — their caching is verified in separate tests.)
        /// </summary>
        [TestMethod]
        public async Task MultiCca_AgentIdentity_FullFlow_WithSilentCacheHit_Async()
        {
            // Arrange
            const string User1Upn = "alice@contoso.com";
            const string User1Oid = "oid-alice-1111";
            const string User1Token = "access-token-alice";
            const string T1Token = "fmi-credential-token-leg1";
            const string T2Token = "instance-token-leg2";

            // Use a single shared HttpManager — instance discovery is cached globally,
            // so a second HttpManager's instance discovery handler would never be consumed.
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var blueprintCca = BuildBlueprintCCA(httpManager);

            // Leg 1: Blueprint acquires FMI token (T1)
            AddMockHandlerForClientCredentials(httpManager, T1Token);

            var leg1Result = await blueprintCca
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithFmiPath(AgentAppId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(T1Token, leg1Result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, leg1Result.AuthenticationResultMetadata.TokenSource);

            // Agent CCA with T1 as assertion (shares the same HttpManager)
            var agentCca = BuildAgentCCA(httpManager, AgentAppId, T1Token);

            // Leg 2: Agent acquires instance token (T2)
            AddMockHandlerForClientCredentials(httpManager, T2Token);

            var leg2Result = await agentCca
                .AcquireTokenForClient(new HashSet<string> { "api://AzureADTokenExchange/.default" })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(T2Token, leg2Result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, leg2Result.AuthenticationResultMetadata.TokenSource);

            // Leg 3: Agent acquires user-scoped token
            AddMockHandlerForUserFicWithIdentity(httpManager, User1Upn, User1Oid, User1Token);

            var leg3Result = await (agentCca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(User1Token, leg3Result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, leg3Result.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(leg3Result.Account, "Account should not be null");
            Assert.AreEqual(User1Upn, leg3Result.Account.Username);

            // Silent: AcquireTokenSilent on agent CCA should return cached user token
            var accounts = await agentCca.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, accounts.Count(), "One account should be cached");

            var silent = await agentCca
                .AcquireTokenSilent(TestConstants.s_scope, accounts.First())
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silent.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User1Token, silent.AccessToken, "Silent call should return cached token");
        }

        /// <summary>
        /// Two different users acquire tokens via the same agent CCA.
        /// Verifies that:
        ///   - Each user gets a distinct token
        ///   - AcquireTokenSilent returns the correct token per user
        ///   - No cross-contamination between users in the agent CCA's cache
        /// </summary>
        [TestMethod]
        public async Task MultiCca_AgentIdentity_TwoUsers_NoCacheCollision_Async()
        {
            // Arrange
            const string User1Upn = "alice@contoso.com";
            const string User1Oid = "oid-alice-1111";
            const string User1Token = "access-token-alice";

            const string User2Upn = "bob@contoso.com";
            const string User2Oid = "oid-bob-2222";
            const string User2Token = "access-token-bob";

            const string T1Token = "fmi-token";

            using var agentHttpManager = new MockHttpManager();
            agentHttpManager.AddInstanceDiscoveryMockHandler();

            var agentCca = BuildAgentCCA(agentHttpManager, AgentAppId, T1Token);

            // User 1: Leg 3 (Leg 2 assumed to have been done — T2 reused via FakeAssertion)
            AddMockHandlerForUserFicWithIdentity(agentHttpManager, User1Upn, User1Oid, User1Token);

            var result1 = await (agentCca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(User1Token, result1.AccessToken);
            Assert.AreEqual(User1Upn, result1.Account.Username);

            // User 2: Leg 3
            AddMockHandlerForUserFicWithIdentity(agentHttpManager, User2Upn, User2Oid, User2Token);

            var result2 = await (agentCca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User2Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(User2Token, result2.AccessToken);
            Assert.AreEqual(User2Upn, result2.Account.Username);

            // Both accounts should be in the agent CCA's cache
            var accounts = await agentCca.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(2, accounts.Count(), "Two user accounts should be cached");

            // Silent for User 1 → returns Alice's token
            var account1 = accounts.First(a => string.Equals(a.Username, User1Upn, StringComparison.OrdinalIgnoreCase));
            var silent1 = await agentCca.AcquireTokenSilent(TestConstants.s_scope, account1).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silent1.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User1Token, silent1.AccessToken, "Silent for Alice should return Alice's token");

            // Silent for User 2 → returns Bob's token
            var account2 = accounts.First(a => string.Equals(a.Username, User2Upn, StringComparison.OrdinalIgnoreCase));
            var silent2 = await agentCca.AcquireTokenSilent(TestConstants.s_scope, account2).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silent2.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User2Token, silent2.AccessToken, "Silent for Bob should return Bob's token");
        }

        /// <summary>
        /// Verifies that using a CCA for both agent identity (UserFIC) and standard client_credentials
        /// does not cause cache collisions. The agent CCA's app token (Leg 2) and its user tokens
        /// (Leg 3) should coexist with a standard client_credentials token on a separate blueprint CCA.
        /// </summary>
        [TestMethod]
        public async Task MultiCca_AgentAndNonAgent_NoCacheInterference_Async()
        {
            // Arrange
            const string User1Upn = "alice@contoso.com";
            const string User1Oid = "oid-alice-1111";
            const string UserToken = "user-scoped-agent-token";
            const string T1Token = "fmi-token";
            const string T2Token = "instance-token";
            const string BlueprintAppToken = "blueprint-app-token";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var blueprintCca = BuildBlueprintCCA(httpManager);
            var agentCca = BuildAgentCCA(httpManager, AgentAppId, T1Token);

            // Blueprint: standard client_credentials call (non-agent)
            AddMockHandlerForClientCredentials(httpManager, BlueprintAppToken);

            var blueprintResult = await blueprintCca
                .AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(BlueprintAppToken, blueprintResult.AccessToken);

            // Agent: Leg 2 (app token on agent CCA)
            AddMockHandlerForClientCredentials(httpManager, T2Token);

            var leg2Result = await agentCca
                .AcquireTokenForClient(new HashSet<string> { "api://AzureADTokenExchange/.default" })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(T2Token, leg2Result.AccessToken);

            // Agent: Leg 3 (user token on agent CCA)
            AddMockHandlerForUserFicWithIdentity(httpManager, User1Upn, User1Oid, UserToken);

            var userResult = await (agentCca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(UserToken, userResult.AccessToken);

            // Verify blueprint's app token is still cached and unchanged
            var blueprintCached = await blueprintCca
                .AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, blueprintCached.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(BlueprintAppToken, blueprintCached.AccessToken,
                "Blueprint app token should be unaffected by agent CCA operations");

            // Verify agent's Leg 2 app token is still cached
            var leg2Cached = await agentCca
                .AcquireTokenForClient(new HashSet<string> { "api://AzureADTokenExchange/.default" })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg2Cached.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(T2Token, leg2Cached.AccessToken,
                "Agent instance token (Leg 2) should be cached");

            // Verify agent's user token is still cached via AcquireTokenSilent
            var silent = await agentCca
                .AcquireTokenSilent(TestConstants.s_scope, userResult.Account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silent.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(UserToken, silent.AccessToken,
                "Agent user token should be cached on agent CCA");
        }

        /// <summary>
        /// Two separate agent CCAs (different agent app IDs) operate independently.
        /// Verifies that tokens acquired on one agent CCA do not appear on the other.
        /// </summary>
        [TestMethod]
        public async Task MultiCca_TwoAgents_IndependentCaches_Async()
        {
            // Arrange
            const string User1Upn = "alice@contoso.com";
            const string User1Oid = "oid-alice-1111";
            const string Agent1Token = "user-token-agent1";
            const string Agent2Token = "user-token-agent2";
            const string T1Token = "fmi-token";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var agentCca1 = BuildAgentCCA(httpManager, AgentAppId, T1Token);
            var agentCca2 = BuildAgentCCA(httpManager, AgentAppId2, T1Token);

            // Agent 1: acquire token for Alice
            AddMockHandlerForUserFicWithIdentity(httpManager, User1Upn, User1Oid, Agent1Token);

            var result1 = await (agentCca1 as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(Agent1Token, result1.AccessToken);

            // Agent 2: acquire token for the same user (Alice) — different agent app
            AddMockHandlerForUserFicWithIdentity(httpManager, User1Upn, User1Oid, Agent2Token);

            var result2 = await (agentCca2 as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(Agent2Token, result2.AccessToken);

            // Silent on agent1 → returns agent1's token, not agent2's
            var silent1 = await agentCca1
                .AcquireTokenSilent(TestConstants.s_scope, result1.Account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silent1.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(Agent1Token, silent1.AccessToken,
                "Agent 1 silent should return agent 1's token");

            // Silent on agent2 → returns agent2's token, not agent1's
            var silent2 = await agentCca2
                .AcquireTokenSilent(TestConstants.s_scope, result2.Account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silent2.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(Agent2Token, silent2.AccessToken,
                "Agent 2 silent should return agent 2's token");
        }

        /// <summary>
        /// OID-based user identification: the current shipped API only supports UPN (string username).
        /// A Guid overload for OID does not yet exist on IByUserFederatedIdentityCredential.
        ///
        /// This test is a STUB documenting the gap. When the Guid overload is added
        /// (as proposed in PR #5883), uncomment the Act/Assert sections.
        ///
        /// Without the Guid overload, a customer would need to use OnBeforeTokenRequest to inject
        /// "user_id" instead of "username" — which is the kind of hack we want to avoid.
        /// </summary>
        [TestMethod]
        public void MultiCca_AgentIdentity_OidOverload_NotYetAvailable()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = BuildCCA(httpManager);

            // The Guid overload does not exist on the shipped IByUserFederatedIdentityCredential interface.
            // Attempting to call it would be a compile error:
            //
            //   Guid userOid = new Guid("11111111-2222-3333-4444-555555555555");
            //   var result = await (app as IByUserFederatedIdentityCredential)
            //       .AcquireTokenByUserFederatedIdentityCredential(
            //           TestConstants.s_scope,
            //           userOid,          // <-- compile error: no Guid overload
            //           FakeAssertion)
            //       .ExecuteAsync();
            //
            // REQUIRED CHANGE: Add a Guid overload to IByUserFederatedIdentityCredential that sends
            // "user_id" (OID formatted as Guid "D") instead of "username" (UPN) in the POST body.
            //
            // See PR #5883 for the proposed implementation.

            // This test is intentionally a stub — no assertion needed.
            // The purpose is to document that the Guid/OID overload is missing from the shipped API.
        }

        /// <summary>
        /// App-only agent identity: Legs 1-2 only (no user, no Leg 3).
        /// The blueprint CCA acquires T1 (FMI token), then the agent CCA acquires an app token
        /// for a downstream API using T1 as its credential.
        ///
        /// This pattern does NOT use AcquireTokenByUserFederatedIdentityCredential at all —
        /// it's pure client_credentials on both CCAs.
        /// </summary>
        [TestMethod]
        public async Task MultiCca_AgentIdentity_AppOnly_NoUserFic_Async()
        {
            // Arrange
            const string T1Token = "fmi-token";
            const string AgentAppToken = "agent-app-token-for-graph";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var blueprintCca = BuildBlueprintCCA(httpManager);

            // Leg 1: Blueprint acquires FMI token
            AddMockHandlerForClientCredentials(httpManager, T1Token);

            var leg1Result = await blueprintCca
                .AcquireTokenForClient(new HashSet<string> { "api://AzureADTokenExchange/.default" })
                .WithFmiPath(AgentAppId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(T1Token, leg1Result.AccessToken);

            // Agent CCA: uses T1 as its credential, acquires app token for Graph
            var agentCca = BuildAgentCCA(httpManager, AgentAppId, T1Token);

            AddMockHandlerForClientCredentials(httpManager, AgentAppToken);

            var agentResult = await agentCca
                .AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(AgentAppToken, agentResult.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, agentResult.AuthenticationResultMetadata.TokenSource);

            // Verify agent app token is cached
            var cached = await agentCca
                .AcquireTokenForClient(TestConstants.s_scope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(AgentAppToken, cached.AccessToken,
                "Agent app token should be cached");
        }

        /// <summary>
        /// Verifies that calling AcquireTokenByUserFederatedIdentityCredential twice for the same
        /// user (without ForceRefresh) hits the network both times — UFIC never checks cache.
        ///
        /// This is an inherent design choice: UFIC always sends a token request to the identity
        /// provider, similar to AcquireTokenInteractive. The result IS cached, but only
        /// AcquireTokenSilent will retrieve it from cache.
        ///
        /// NOTE: This is NOT a bug — it matches the pattern of other assertion-based flows where
        /// the assertion itself may change between calls. The recommended pattern is:
        ///   1. Call UFIC once to acquire and cache the token
        ///   2. Use AcquireTokenSilent for subsequent requests
        /// </summary>
        [TestMethod]
        public async Task MultiCca_Ufic_AlwaysHitsNetwork_EvenWithoutForceRefresh_Async()
        {
            const string User1Upn = "alice@contoso.com";
            const string User1Oid = "oid-alice-1111";
            const string UserToken1 = "user-token-first-call";
            const string UserToken2 = "user-token-second-call";
            const string T1Token = "fmi-token";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var agentCca = BuildAgentCCA(httpManager, AgentAppId, T1Token);

            // First UFIC call → hits network
            AddMockHandlerForUserFicWithIdentity(httpManager, User1Upn, User1Oid, UserToken1);

            var result1 = await (agentCca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(UserToken1, result1.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource,
                "First UFIC call should hit identity provider");

            // Second UFIC call (same user, same scopes, NO ForceRefresh) → also hits network
            // We MUST add another mock handler because UFIC doesn't check cache
            AddMockHandlerForUserFicWithIdentity(httpManager, User1Upn, User1Oid, UserToken2);

            var result2 = await (agentCca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(UserToken2, result2.AccessToken,
                "Second UFIC call should return the NEW token from identity provider, not cached");
            Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource,
                "Second UFIC call should hit identity provider (UFIC never checks cache)");

            // But AcquireTokenSilent DOES return from cache (the most recently cached token)
            var accounts = await agentCca.GetAccountsAsync().ConfigureAwait(false);
            var silent = await agentCca
                .AcquireTokenSilent(TestConstants.s_scope, accounts.First())
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silent.AuthenticationResultMetadata.TokenSource,
                "AcquireTokenSilent should return from cache");
        }

        /// <summary>
        /// Verifies that Leg 1 tokens for different agents (different fmi_path values) are
        /// cached independently on the same blueprint CCA. Each fmi_path produces a distinct
        /// cache entry via AdditionalCacheKeyComponents.
        /// </summary>
        [TestMethod]
        public async Task MultiCca_Leg1_DifferentFmiPaths_CachedIndependently_Async()
        {
            const string T1Agent1 = "fmi-token-agent1";
            const string T1Agent2 = "fmi-token-agent2";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var blueprintCca = BuildBlueprintCCA(httpManager);

            // Leg 1 for Agent 1
            AddMockHandlerForClientCredentials(httpManager, T1Agent1);

            var result1 = await blueprintCca
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithFmiPath(AgentAppId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(T1Agent1, result1.AccessToken);

            // Leg 1 for Agent 2 — different fmi_path, should hit network
            AddMockHandlerForClientCredentials(httpManager, T1Agent2);

            var result2 = await blueprintCca
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithFmiPath(AgentAppId2)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(T1Agent2, result2.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource,
                "Different fmi_path should produce a cache miss");

            // Repeat Leg 1 for Agent 1 — should return from cache (not Agent 2's token)
            var cached1 = await blueprintCca
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithFmiPath(AgentAppId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(T1Agent1, cached1.AccessToken,
                "Cached T1 for Agent 1 should be returned, not Agent 2's");
            Assert.AreEqual(TokenSource.Cache, cached1.AuthenticationResultMetadata.TokenSource);

            // Repeat Leg 1 for Agent 2 — should also return from cache
            var cached2 = await blueprintCca
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithFmiPath(AgentAppId2)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(T1Agent2, cached2.AccessToken,
                "Cached T1 for Agent 2 should be returned, not Agent 1's");
            Assert.AreEqual(TokenSource.Cache, cached2.AuthenticationResultMetadata.TokenSource);
        }

        /// <summary>
        /// Verifies that AcquireTokenSilent with ForceRefresh on the agent CCA triggers
        /// a refresh token flow. The agent CCA's assertion callback (which returns T1)
        /// fires as the client credential for the grant_type=refresh_token request.
        ///
        /// This is the expected path when a cached user token expires: AcquireTokenSilent
        /// finds no valid AT, locates the RT, and sends grant_type=refresh_token with
        /// client_assertion=T1 (from the callback).
        /// </summary>
        [TestMethod]
        public async Task MultiCca_ForceRefresh_TriggersRefreshTokenFlow_Async()
        {
            const string User1Upn = "alice@contoso.com";
            const string User1Oid = "oid-alice-1111";
            const string OriginalUserToken = "original-user-token";
            const string RefreshedUserToken = "refreshed-user-token";
            const string T1Token = "fmi-token";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var agentCca = BuildAgentCCA(httpManager, AgentAppId, T1Token);

            // Step 1: Acquire initial user token via UFIC (caches AT + RT)
            AddMockHandlerForUserFicWithIdentity(httpManager, User1Upn, User1Oid, OriginalUserToken);

            var initialResult = await (agentCca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(OriginalUserToken, initialResult.AccessToken);
            Assert.IsNotNull(initialResult.Account);

            // Step 2: AcquireTokenSilent with ForceRefresh → skips AT cache, uses RT
            // This triggers a grant_type=refresh_token request with client_assertion=T1
            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken }
                },
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                    User1Oid, User1Upn, TestConstants.s_scope.ToArray(),
                    accessToken: RefreshedUserToken)
            });

            var refreshedResult = await agentCca
                .AcquireTokenSilent(TestConstants.s_scope, initialResult.Account)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(RefreshedUserToken, refreshedResult.AccessToken,
                "ForceRefresh should return a new token from the identity provider");
            Assert.AreEqual(TokenSource.IdentityProvider, refreshedResult.AuthenticationResultMetadata.TokenSource,
                "Token should come from identity provider (RT refresh), not cache");

            // Step 3: Subsequent silent call (no ForceRefresh) → returns refreshed token from cache
            var cachedResult = await agentCca
                .AcquireTokenSilent(TestConstants.s_scope, refreshedResult.Account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(RefreshedUserToken, cachedResult.AccessToken,
                "Cache should now contain the refreshed token");
            Assert.AreEqual(TokenSource.Cache, cachedResult.AuthenticationResultMetadata.TokenSource);
        }

        /// <summary>
        /// Verifies that RemoveAsync on the agent CCA removes only the specified user's
        /// tokens (AT, RT, ID token, account) and does NOT affect:
        ///   - Other users' tokens on the same agent CCA
        ///   - The agent CCA's app tokens (Leg 2, acquired via AcquireTokenForClient)
        ///
        /// RemoveAsync operates on the UserTokenCache. App tokens live in the AppTokenCache
        /// and are not touched.
        /// </summary>
        [TestMethod]
        public async Task MultiCca_RemoveAsync_RemovesOnlyTargetUser_Async()
        {
            const string User1Upn = "alice@contoso.com";
            const string User1Oid = "oid-alice-1111";
            const string User1Token = "access-token-alice";

            const string User2Upn = "bob@contoso.com";
            const string User2Oid = "oid-bob-2222";
            const string User2Token = "access-token-bob";

            const string T1Token = "fmi-token";
            const string T2Token = "instance-token-leg2";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var agentCca = BuildAgentCCA(httpManager, AgentAppId, T1Token);

            // Acquire Leg 2 app token
            AddMockHandlerForClientCredentials(httpManager, T2Token);
            var leg2Result = await agentCca
                .AcquireTokenForClient(new HashSet<string> { "api://AzureADTokenExchange/.default" })
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual(T2Token, leg2Result.AccessToken);

            // Acquire user tokens for Alice and Bob
            AddMockHandlerForUserFicWithIdentity(httpManager, User1Upn, User1Oid, User1Token);
            var result1 = await (agentCca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            AddMockHandlerForUserFicWithIdentity(httpManager, User2Upn, User2Oid, User2Token);
            var result2 = await (agentCca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User2Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Verify both accounts are cached
            var accounts = await agentCca.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(2, accounts.Count(), "Both users should be cached");

            // Remove Alice
            await agentCca.RemoveAsync(result1.Account).ConfigureAwait(false);

            // Verify Alice is gone
            accounts = await agentCca.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, accounts.Count(), "Only Bob should remain after removing Alice");
            Assert.AreEqual(User2Upn, accounts.First().Username, "Remaining account should be Bob");

            // Silent for Alice should fail (no account)
            var aliceAccount = await agentCca.GetAccountAsync(result1.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            Assert.IsNull(aliceAccount, "Alice's account should not be retrievable after removal");

            // Silent for Bob should still work
            var silentBob = await agentCca
                .AcquireTokenSilent(TestConstants.s_scope, result2.Account)
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, silentBob.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User2Token, silentBob.AccessToken, "Bob's token should be unaffected");

            // Agent's app token (Leg 2) should still be cached — RemoveAsync only affects UserTokenCache
            var leg2Cached = await agentCca
                .AcquireTokenForClient(new HashSet<string> { "api://AzureADTokenExchange/.default" })
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, leg2Cached.AuthenticationResultMetadata.TokenSource,
                "Leg 2 app token should be unaffected by RemoveAsync");
            Assert.AreEqual(T2Token, leg2Cached.AccessToken);
        }

        #endregion
    }
}

