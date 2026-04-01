// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Lab.Api.Core.Mocks;
using Microsoft.Identity.Lab.Api;
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
    }
}
