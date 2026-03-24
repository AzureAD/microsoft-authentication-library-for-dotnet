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
    }
}
