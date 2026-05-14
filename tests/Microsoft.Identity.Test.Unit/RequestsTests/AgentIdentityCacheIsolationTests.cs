// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    /// <summary>
    /// Tests that verify cache isolation when using WithClientIdOverride for agent identity scenarios.
    /// These tests use mocked HTTP responses to simulate multiple users and agents,
    /// ensuring no cache collisions occur within a single ConfidentialClientApplication instance.
    /// </summary>
    [TestClass]
    public class AgentIdentityCacheIsolationTests : TestBase
    {
        // Blueprint app (the CCA's configured client ID)
        private const string BlueprintClientId = "urn:microsoft:identity:fmi";

        // Agent app (the override client ID for legs 2 & 3)
        private const string AgentAppId = "agent-app-id-1111";

        // Second agent app (to test multi-agent isolation)
        private const string AgentAppId2 = "agent-app-id-2222";

        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";
        private const string GraphScope = "https://graph.microsoft.com/.default";
        private const string FmiPath = "some/fmi/path";

        // User 1 identifiers
        private const string User1Uid = "user1-uid-aaaa";
        private const string User1Utid = "tenant-utid-1111";
        private const string User1Upn = "user1@contoso.com";

        // User 2 identifiers
        private const string User2Uid = "user2-uid-bbbb";
        private const string User2Utid = "tenant-utid-1111"; // same tenant
        private const string User2Upn = "user2@contoso.com";

        [TestMethod]
        public async Task TwoUsers_SameAgent_NoCacheCollision_Test()
        {
            // This test verifies that tokens for two different users acquired through
            // the same agent identity flow do NOT collide in the cache.
            // Each user should have their own distinct AT, RT, and IDT entries.

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var app = ConfidentialClientApplicationBuilder
                .Create(BlueprintClientId)
                .WithAuthority(ClientApplicationBase.DefaultAuthority, true)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithExperimentalFeatures(true)
                .WithHttpManager(httpManager)
                .BuildConcrete();

            // === User 1 flow ===

            // Leg 1: FMI token (app token, blueprint clientId)
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: "t1-fmi-token-user1");

            var leg1User1 = await app.AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithFmiPath(FmiPath)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, leg1User1.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("t1-fmi-token-user1", leg1User1.AccessToken);

            // Leg 2: Instance token (app token, agent clientId via override)
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: "t2-instance-token-user1");

            var leg2User1 = await app.AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithClientIdOverride(AgentAppId)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = "t1-fmi-token-user1";
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, leg2User1.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("t2-instance-token-user1", leg2User1.AccessToken);

            // Leg 3: User token (user cache, agent clientId)
            httpManager.AddMockHandler(CreateUserFicMockHandler(
                uid: User1Uid, utid: User1Utid, displayName: User1Upn,
                accessToken: "user1-graph-token", refreshToken: "user1-rt",
                scope: GraphScope));

            var leg3User1 = await ((IByUserFederatedIdentityCredential)app)
                .AcquireTokenByUserFederatedIdentityCredential(new[] { GraphScope }, User1Upn, "t2-instance-token-user1")
                .WithClientIdOverride(AgentAppId)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = "t1-fmi-token-user1";
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, leg3User1.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("user1-graph-token", leg3User1.AccessToken);
            Assert.IsNotNull(leg3User1.Account);

            // === User 2 flow ===
            // Leg 1 is shared (same FMI token), so should come from cache
            var leg1User2 = await app.AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithFmiPath(FmiPath)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg1User2.AuthenticationResultMetadata.TokenSource,
                "Leg 1 FMI token should be cached and shared across users");

            // Leg 2 is also shared (same agent instance token, same scope)
            var leg2User2 = await app.AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithClientIdOverride(AgentAppId)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = "t1-fmi-token-user1";
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, leg2User2.AuthenticationResultMetadata.TokenSource,
                "Leg 2 instance token should be cached and shared across users");

            // Leg 3: Different user — must go to IdP (different homeAccountId)
            httpManager.AddMockHandler(CreateUserFicMockHandler(
                uid: User2Uid, utid: User2Utid, displayName: User2Upn,
                accessToken: "user2-graph-token", refreshToken: "user2-rt",
                scope: GraphScope));

            var leg3User2 = await ((IByUserFederatedIdentityCredential)app)
                .AcquireTokenByUserFederatedIdentityCredential(new[] { GraphScope }, User2Upn, "t2-instance-token-user1")
                .WithClientIdOverride(AgentAppId)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = "t1-fmi-token-user1";
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, leg3User2.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("user2-graph-token", leg3User2.AccessToken);
            Assert.IsNotNull(leg3User2.Account);

            // === Verify cache isolation ===

            // App cache should have exactly 2 entries: Leg1 (blueprint) + Leg2 (agent override)
            var appTokens = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens();
            Assert.HasCount(2, appTokens, "Should have 2 app tokens: one for Leg 1 (blueprint), one for Leg 2 (agent)");

            var leg1Token = appTokens.Single(t => t.ClientId == BlueprintClientId);
            var leg2Token = appTokens.Single(t => t.ClientId == AgentAppId);
            Assert.AreNotEqual(leg1Token.CacheKey, leg2Token.CacheKey,
                "Leg 1 and Leg 2 should have different cache keys");

            // User cache should have 2 access tokens (one per user), 2 refresh tokens, 2 id tokens
            var userAccessTokens = app.UserTokenCacheInternal.Accessor.GetAllAccessTokens();
            Assert.HasCount(2, userAccessTokens, "Should have 2 user access tokens (one per user)");

            // Both user tokens should have the agent's clientId
            Assert.IsTrue(userAccessTokens.All(t => t.ClientId == AgentAppId),
                "Both user tokens should be stored with the agent's clientId");

            // They should have different HomeAccountIds (different users)
            var homeAccountIds = userAccessTokens.Select(t => t.HomeAccountId).Distinct().ToList();
            Assert.HasCount(2, homeAccountIds, "User tokens should have distinct HomeAccountIds");

            // Verify refresh tokens are also distinct
            var refreshTokens = app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens();
            Assert.HasCount(2, refreshTokens, "Should have 2 refresh tokens (one per user)");
            Assert.IsTrue(refreshTokens.All(t => t.ClientId == AgentAppId),
                "Both refresh tokens should be stored with the agent's clientId");
            var rtHomeAccountIds = refreshTokens.Select(t => t.HomeAccountId).Distinct().ToList();
            Assert.HasCount(2, rtHomeAccountIds, "Refresh tokens should have distinct HomeAccountIds");

            // === Verify silent calls retrieve correct tokens (no cross-user pollution) ===
            var user1Silent = await app
                .AcquireTokenSilent(new[] { GraphScope }, leg3User1.Account)
                .WithClientIdOverride(AgentAppId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, user1Silent.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("user1-graph-token", user1Silent.AccessToken,
                "Silent for user1 should return user1's token, not user2's");

            var user2Silent = await app
                .AcquireTokenSilent(new[] { GraphScope }, leg3User2.Account)
                .WithClientIdOverride(AgentAppId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, user2Silent.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("user2-graph-token", user2Silent.AccessToken,
                "Silent for user2 should return user2's token, not user1's");
        }

        [TestMethod]
        public async Task TwoAgents_SameUser_NoCacheCollision_Test()
        {
            // This test verifies that tokens for the same user acquired through
            // TWO DIFFERENT agents do NOT collide in the cache.

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var app = ConfidentialClientApplicationBuilder
                .Create(BlueprintClientId)
                .WithAuthority(ClientApplicationBase.DefaultAuthority, true)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithExperimentalFeatures(true)
                .WithHttpManager(httpManager)
                .BuildConcrete();

            // === Agent 1 flow ===

            // Leg 1
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "t1-agent1");

            await app.AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithFmiPath(FmiPath)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Leg 2 for Agent 1
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "t2-agent1");

            var leg2Agent1 = await app.AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithClientIdOverride(AgentAppId)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = "t1-agent1";
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Leg 3 for Agent 1 + User 1
            httpManager.AddMockHandler(CreateUserFicMockHandler(
                uid: User1Uid, utid: User1Utid, displayName: User1Upn,
                accessToken: "user1-via-agent1", refreshToken: "user1-rt-agent1",
                scope: GraphScope));

            var user1Agent1 = await ((IByUserFederatedIdentityCredential)app)
                .AcquireTokenByUserFederatedIdentityCredential(new[] { GraphScope }, User1Upn, leg2Agent1.AccessToken)
                .WithClientIdOverride(AgentAppId)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = "t1-agent1";
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("user1-via-agent1", user1Agent1.AccessToken);

            // === Agent 2 flow (same user, different agent) ===

            // Leg 2 for Agent 2 (Leg 1 is shared)
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "t2-agent2");

            var leg2Agent2 = await app.AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithClientIdOverride(AgentAppId2)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = "t1-agent1";
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Leg 3 for Agent 2 + User 1 (same user, different agent)
            httpManager.AddMockHandler(CreateUserFicMockHandler(
                uid: User1Uid, utid: User1Utid, displayName: User1Upn,
                accessToken: "user1-via-agent2", refreshToken: "user1-rt-agent2",
                scope: GraphScope));

            var user1Agent2 = await ((IByUserFederatedIdentityCredential)app)
                .AcquireTokenByUserFederatedIdentityCredential(new[] { GraphScope }, User1Upn, leg2Agent2.AccessToken)
                .WithClientIdOverride(AgentAppId2)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = "t1-agent1";
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("user1-via-agent2", user1Agent2.AccessToken);

            // === Verify cache isolation ===

            // App cache: Leg1 (blueprint) + Leg2 (agent1) + Leg2 (agent2) = 3 tokens
            var appTokens = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens();
            Assert.HasCount(3, appTokens, "Should have 3 app tokens: Leg1 + Leg2(agent1) + Leg2(agent2)");
            Assert.AreEqual(1, appTokens.Count(t => t.ClientId == BlueprintClientId));
            Assert.AreEqual(1, appTokens.Count(t => t.ClientId == AgentAppId));
            Assert.AreEqual(1, appTokens.Count(t => t.ClientId == AgentAppId2));

            // User cache: 2 access tokens (same user, different agents = different clientIds)
            var userAccessTokens = app.UserTokenCacheInternal.Accessor.GetAllAccessTokens();
            Assert.HasCount(2, userAccessTokens,
                "Should have 2 user access tokens (same user, different agents)");

            var agent1UserToken = userAccessTokens.Single(t => t.ClientId == AgentAppId);
            var agent2UserToken = userAccessTokens.Single(t => t.ClientId == AgentAppId2);

            // Same user (same HomeAccountId) but different clientIds = different cache keys
            Assert.AreEqual(agent1UserToken.HomeAccountId, agent2UserToken.HomeAccountId,
                "Same user should have same HomeAccountId");
            Assert.AreNotEqual(agent1UserToken.CacheKey, agent2UserToken.CacheKey,
                "Different agents should produce different cache keys for same user");

            // Verify refresh tokens are also isolated by agent
            var refreshTokens = app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens();
            Assert.HasCount(2, refreshTokens, "Should have 2 refresh tokens (one per agent)");
            Assert.AreEqual(1, refreshTokens.Count(t => t.ClientId == AgentAppId));
            Assert.AreEqual(1, refreshTokens.Count(t => t.ClientId == AgentAppId2));

            // === Verify silent calls retrieve correct per-agent tokens ===
            var silentAgent1 = await app
                .AcquireTokenSilent(new[] { GraphScope }, user1Agent1.Account)
                .WithClientIdOverride(AgentAppId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silentAgent1.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("user1-via-agent1", silentAgent1.AccessToken,
                "Silent with agent1 override should return agent1's token");

            var silentAgent2 = await app
                .AcquireTokenSilent(new[] { GraphScope }, user1Agent2.Account)
                .WithClientIdOverride(AgentAppId2)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silentAgent2.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("user1-via-agent2", silentAgent2.AccessToken,
                "Silent with agent2 override should return agent2's token");
        }

        [TestMethod]
        public async Task AgentUserToken_DoesNotCollide_WithBlueprintUserToken_Test()
        {
            // This test verifies that a user token acquired through the agent identity flow
            // (with WithClientIdOverride) does NOT collide with a user token acquired directly
            // by the blueprint app (without override) for the same user and scope.

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var app = ConfidentialClientApplicationBuilder
                .Create(BlueprintClientId)
                .WithAuthority(ClientApplicationBase.DefaultAuthority, true)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithExperimentalFeatures(true)
                .WithHttpManager(httpManager)
                .BuildConcrete();

            // First: Acquire a user token through the agent flow (with override)
            // Leg 1
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "t1-fmi");

            await app.AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithFmiPath(FmiPath)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Leg 2
            httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "t2-instance");

            await app.AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithClientIdOverride(AgentAppId)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = "t1-fmi";
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Leg 3: user token via agent
            httpManager.AddMockHandler(CreateUserFicMockHandler(
                uid: User1Uid, utid: User1Utid, displayName: User1Upn,
                accessToken: "user1-via-agent", refreshToken: "user1-rt-agent",
                scope: GraphScope));

            var agentUserResult = await ((IByUserFederatedIdentityCredential)app)
                .AcquireTokenByUserFederatedIdentityCredential(new[] { GraphScope }, User1Upn, "t2-instance")
                .WithClientIdOverride(AgentAppId)
                .OnBeforeTokenRequest(data =>
                {
                    data.BodyParameters["client_assertion"] = "t1-fmi";
                    data.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("user1-via-agent", agentUserResult.AccessToken);

            // Second: Acquire a user token directly as the blueprint app (no override)
            // using the same scope and same user
            httpManager.AddMockHandler(CreateUserFicMockHandler(
                uid: User1Uid, utid: User1Utid, displayName: User1Upn,
                accessToken: "user1-via-blueprint", refreshToken: "user1-rt-blueprint",
                scope: GraphScope));

            var blueprintUserResult = await ((IByUserFederatedIdentityCredential)app)
                .AcquireTokenByUserFederatedIdentityCredential(new[] { GraphScope }, User1Upn, "some-blueprint-assertion")
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("user1-via-blueprint", blueprintUserResult.AccessToken);

            // === Verify no collision ===
            var userAccessTokens = app.UserTokenCacheInternal.Accessor.GetAllAccessTokens();
            Assert.HasCount(2, userAccessTokens,
                "Should have 2 user tokens: one via agent, one via blueprint");

            var agentToken = userAccessTokens.Single(t => t.ClientId == AgentAppId);
            var blueprintToken = userAccessTokens.Single(t => t.ClientId == BlueprintClientId);

            Assert.AreNotEqual(agentToken.CacheKey, blueprintToken.CacheKey,
                "Agent and blueprint tokens for same user+scope should have different cache keys");

            // Silent with override should return agent token
            var silentAgent = await app
                .AcquireTokenSilent(new[] { GraphScope }, agentUserResult.Account)
                .WithClientIdOverride(AgentAppId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("user1-via-agent", silentAgent.AccessToken);

            // Silent without override should return blueprint token
            var silentBlueprint = await app
                .AcquireTokenSilent(new[] { GraphScope }, blueprintUserResult.Account)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("user1-via-blueprint", silentBlueprint.AccessToken);
        }

        /// <summary>
        /// Creates a mock HTTP handler that returns a user FIC token response with the given user identity.
        /// </summary>
        private static MockHttpMessageHandler CreateUserFicMockHandler(
            string uid, string utid, string displayName,
            string accessToken, string refreshToken, string scope)
        {
            string idToken = MockHelpers.CreateIdToken(uid, displayName, utid);
            string clientInfo = MockHelpers.CreateClientInfo(uid, utid);

            string responseBody =
                "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":\"" + scope +
                "\",\"access_token\":\"" + accessToken +
                "\",\"refresh_token\":\"" + refreshToken +
                "\",\"client_info\":\"" + clientInfo +
                "\",\"id_token\":\"" + idToken +
                "\",\"id_token_expires_in\":\"3600\"}";

            return new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(responseBody)
            };
        }
    }
}
