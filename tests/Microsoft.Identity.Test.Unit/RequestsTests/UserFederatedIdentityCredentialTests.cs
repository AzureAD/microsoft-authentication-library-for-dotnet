// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
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

        #region OID-Based UserFIC Tests

        private static readonly Guid FakeUserOid = new Guid("11111111-2222-3333-4444-555555555555");

        /// <summary>
        /// Verifies that when the Guid overload of AcquireTokenByUserFederatedIdentityCredential is used,
        /// the token request sends "user_id" (OID) instead of "username" (UPN) in the POST body.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenByUserFic_WithOid_SendsUserIdParameter_Async()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedUrl = TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.GrantType, OAuth2GrantType.UserFic },
                    { OAuth2Parameter.UserId, FakeUserOid.ToString("D") },
                    { OAuth2Parameter.UserFederatedIdentityCredential, FakeAssertion }
                },
                // Verify that "username" is NOT sent when using the OID overload
                UnExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Username, "" }
                },
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            var app = BuildCCA(httpManager);

            // Act
            var result = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    FakeUserOid,
                    FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        /// <summary>
        /// Verifies that the UPN overload sends "username" and NOT "user_id" in the POST body.
        /// This is the inverse of the OID test and ensures the two paths are mutually exclusive.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenByUserFic_WithUpn_SendsUsernameParameter_Async()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedUrl = TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.GrantType, OAuth2GrantType.UserFic },
                    { OAuth2Parameter.Username, FakeUsername },
                    { OAuth2Parameter.UserFederatedIdentityCredential, FakeAssertion }
                },
                // Verify that "user_id" is NOT sent when using the UPN overload
                UnExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.UserId, "" }
                },
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            var app = BuildCCA(httpManager);

            // Act
            var result = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    FakeUsername,
                    FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        #endregion

        #region Multi-User Cache Tests (Low-Level API)

        /// <summary>
        /// Adds a mock handler for a UserFIC call with a specific username and a token response
        /// that returns a distinct user identity (OID and preferred_username).
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
        /// Verifies that when two different users (by UPN) acquire tokens via UserFIC on the same CCA,
        /// AcquireTokenSilent returns the correct cached token for each user and does not cross-contaminate.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenByUserFic_TwoUpns_SilentReturnsCorrectToken_Async()
        {
            const string User1Upn = "alice@contoso.com";
            const string User1Oid = "oid-alice-1111";
            const string User1Token = "access-token-alice";

            const string User2Upn = "bob@contoso.com";
            const string User2Oid = "oid-bob-2222";
            const string User2Token = "access-token-bob";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var app = BuildCCA(httpManager);

            // Acquire token for User 1 (Alice) via UserFIC
            AddMockHandlerForUserFicWithIdentity(httpManager, User1Upn, User1Oid, User1Token);

            var result1 = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User1Token, result1.AccessToken);
            Assert.AreEqual(User1Upn, result1.Account.Username);

            // Acquire token for User 2 (Bob) via UserFIC
            AddMockHandlerForUserFicWithIdentity(httpManager, User2Upn, User2Oid, User2Token);

            var result2 = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User2Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User2Token, result2.AccessToken);
            Assert.AreEqual(User2Upn, result2.Account.Username);

            // Both accounts should be in the cache
            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(2, accounts.Count(), "Two accounts should be cached");

            // AcquireTokenSilent for User 1 → should return Alice's token, NOT Bob's
            var account1 = accounts.First(a => string.Equals(a.Username, User1Upn, StringComparison.OrdinalIgnoreCase));
            var silent1 = await app.AcquireTokenSilent(TestConstants.s_scope, account1).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silent1.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User1Token, silent1.AccessToken, "Silent call for Alice should return Alice's token");

            // AcquireTokenSilent for User 2 → should return Bob's token, NOT Alice's
            var account2 = accounts.First(a => string.Equals(a.Username, User2Upn, StringComparison.OrdinalIgnoreCase));
            var silent2 = await app.AcquireTokenSilent(TestConstants.s_scope, account2).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silent2.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User2Token, silent2.AccessToken, "Silent call for Bob should return Bob's token");
        }

        /// <summary>
        /// Verifies that when two different users (by OID) acquire tokens via UserFIC on the same CCA,
        /// AcquireTokenSilent resolves the correct account by OID and returns the correct cached token.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenByUserFic_TwoOids_SilentReturnsCorrectToken_Async()
        {
            const string User1Upn = "carol@contoso.com";
            const string User1Oid = "oid-carol-3333";
            const string User1Token = "access-token-carol";

            const string User2Upn = "dave@contoso.com";
            const string User2Oid = "oid-dave-4444";
            const string User2Token = "access-token-dave";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var app = BuildCCA(httpManager);

            // Acquire token for User 1 (Carol) via UserFIC using UPN
            AddMockHandlerForUserFicWithIdentity(httpManager, User1Upn, User1Oid, User1Token);

            var result1 = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User1Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(User1Token, result1.AccessToken);

            // Acquire token for User 2 (Dave) via UserFIC using UPN
            AddMockHandlerForUserFicWithIdentity(httpManager, User2Upn, User2Oid, User2Token);

            var result2 = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(TestConstants.s_scope, User2Upn, FakeAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(User2Token, result2.AccessToken);

            // Now retrieve by OID — find the correct account using HomeAccountId.ObjectId
            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(2, accounts.Count(), "Two accounts should be cached");

            // Lookup by OID for Carol
            var carolAccount = accounts.First(a =>
                string.Equals(a.HomeAccountId.ObjectId, User1Oid, StringComparison.OrdinalIgnoreCase));
            var silentCarol = await app.AcquireTokenSilent(TestConstants.s_scope, carolAccount).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silentCarol.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User1Token, silentCarol.AccessToken, "OID-based lookup for Carol should return Carol's token");

            // Lookup by OID for Dave
            var daveAccount = accounts.First(a =>
                string.Equals(a.HomeAccountId.ObjectId, User2Oid, StringComparison.OrdinalIgnoreCase));
            var silentDave = await app.AcquireTokenSilent(TestConstants.s_scope, daveAccount).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silentDave.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User2Token, silentDave.AccessToken, "OID-based lookup for Dave should return Dave's token");
        }

        #endregion

        #region High-Level AcquireTokenForAgent Multi-User Tests

        /// <summary>
        /// Verifies that two calls to AcquireTokenForAgent for different users produce correct tokens,
        /// and that a subsequent call for the first user returns a cached token (via AcquireTokenSilent
        /// inside AgentTokenRequest) without any additional HTTP calls.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_TwoUpns_CacheReturnsCorrectUserToken_Async()
        {
            // Arrange
            const string AgentAppId = "00000000-0000-0000-0000-000000001234";
            const string User1Upn = "alice@contoso.com";
            const string User1Oid = "oid-alice-1111";
            const string User1Token = "access-token-alice";

            const string User2Upn = "bob@contoso.com";
            const string User2Oid = "oid-bob-2222";
            const string User2Token = "access-token-bob";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithExperimentalFeatures(true)
                .WithHttpManager(httpManager)
                .BuildConcrete();

            // --- User 1 (Alice): 3 HTTP calls ---
            // Leg 1: Blueprint AcquireTokenForClient (FMI credential, consumed by assertion CCA's client assertion callback)
            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(token: "fmi-credential-token")
            });

            // Leg 2: Assertion CCA AcquireTokenForClient (assertion token)
            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(token: "assertion-token")
            });

            // Leg 3: Agent CCA AcquireTokenByUserFIC (user token for Alice)
            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                    User1Oid, User1Upn, TestConstants.s_scope.ToArray(), accessToken: User1Token)
            });

            // Act: AcquireTokenForAgent for User 1
            var agentId1 = AgentIdentity.WithUsername(AgentAppId, User1Upn);
            var result1 = await blueprintCca
                .AcquireTokenForAgent(TestConstants.s_scope, agentId1)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert: User 1 token from IdP
            Assert.AreEqual(User1Token, result1.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

            // --- User 2 (Bob): only 1 HTTP call (FMI cred + assertion token cached) ---
            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                    User2Oid, User2Upn, TestConstants.s_scope.ToArray(), accessToken: User2Token)
            });

            // Act: AcquireTokenForAgent for User 2
            var agentId2 = AgentIdentity.WithUsername(AgentAppId, User2Upn);
            var result2 = await blueprintCca
                .AcquireTokenForAgent(TestConstants.s_scope, agentId2)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert: User 2 token from IdP
            Assert.AreEqual(User2Token, result2.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);

            // --- User 1 again: should come from cache (no HTTP calls) ---
            var result1Again = await blueprintCca
                .AcquireTokenForAgent(TestConstants.s_scope, agentId1)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert: User 1 token from cache
            Assert.AreEqual(TokenSource.Cache, result1Again.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(User1Token, result1Again.AccessToken);
        }

        /// <summary>
        /// Verifies that a pre-cancelled CancellationToken propagates through the multi-leg
        /// agent flow (AgentTokenRequest → inner AcquireTokenForClient → token request pipeline)
        /// and causes an OperationCanceledException without making any token HTTP calls.
        /// The cancellation is detected at TokenClient.SendTokenRequestAsync, which calls
        /// CancellationToken.ThrowIfCancellationRequested() before issuing the HTTP request.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_WithPreCancelledToken_ThrowsOperationCanceledException_Async()
        {
            // Arrange
            const string AgentAppId = "00000000-0000-0000-0000-000000001234";
            const string UserUpn = "alice@contoso.com";

            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithExperimentalFeatures(true)
                .WithHttpManager(httpManager)
                .BuildConcrete();

            var agentId = AgentIdentity.WithUsername(AgentAppId, UserUpn);

            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            // Act & Assert – the cancelled token propagates through AgentTokenRequest.ExecuteAsync
            // into the inner Leg 2 AcquireTokenForClient call, where TokenClient detects the
            // cancellation and throws OperationCanceledException before any HTTP request is made.
            await AssertException.TaskThrowsAsync<OperationCanceledException>(
                () => blueprintCca
                    .AcquireTokenForAgent(TestConstants.s_scope, agentId)
                    .ExecuteAsync(tokenSource.Token))
                .ConfigureAwait(false);
        }

        #endregion
    }
}
