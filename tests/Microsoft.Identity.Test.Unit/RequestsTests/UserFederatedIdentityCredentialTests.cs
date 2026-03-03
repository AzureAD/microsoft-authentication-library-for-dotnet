// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Client.AppConfig;
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

            bool assertionProviderCalled = false;
            var result = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    FakeUsername,
                    async (options) =>
                    {
                        assertionProviderCalled = true;
                        await Task.Yield();
                        return FakeAssertion;
                    })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(assertionProviderCalled, "AssertionProvider delegate should have been invoked.");
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
                    (options) => Task.FromResult(FakeAssertion))
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
        public async Task AcquireTokenByUserFic_WithForceRefresh_InvokesAssertionProvider_Async()
        {
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();

            // First call
            AddMockHandlerForUserFic(httpManager);
            var app = BuildCCA(httpManager);

            int assertionCallCount = 0;
            Task<string> assertionProvider(AssertionRequestOptions options)
            {
                assertionCallCount++;
                return Task.FromResult(FakeAssertion);
            }

            var firstResult = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    FakeUsername,
                    assertionProvider)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(1, assertionCallCount);
            Assert.AreEqual(TokenSource.IdentityProvider, firstResult.AuthenticationResultMetadata.TokenSource);

            // Second call with ForceRefresh - should re-invoke the assertion provider
            AddMockHandlerForUserFic(httpManager);

            var secondResult = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    FakeUsername,
                    assertionProvider)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(2, assertionCallCount, "AssertionProvider should be called again with ForceRefresh.");
            Assert.AreEqual(TokenSource.IdentityProvider, secondResult.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        public async Task AcquireTokenByUserFic_PassesCancellationTokenToAssertionProvider_Async()
        {
            using var httpManager = new MockHttpManager();
            httpManager.AddInstanceDiscoveryMockHandler();
            AddMockHandlerForUserFic(httpManager);

            var app = BuildCCA(httpManager);

            CancellationToken capturedToken = default;
            using var cts = new CancellationTokenSource();

            var result = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    FakeUsername,
                    (options) =>
                    {
                        capturedToken = options.CancellationToken;
                        return Task.FromResult(FakeAssertion);
                    })
                .ExecuteAsync(cts.Token)
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            // CancellationToken should be propagated to the assertion options
            Assert.AreEqual(cts.Token, capturedToken);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AcquireTokenByUserFic_NullUsername_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var app = BuildCCA(httpManager);

            _ = (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    username: null,
                    assertionProvider: (options) => Task.FromResult(FakeAssertion));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AcquireTokenByUserFic_NullAssertionProvider_ThrowsArgumentNullException()
        {
            using var httpManager = new MockHttpManager();
            var app = BuildCCA(httpManager);

            _ = (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    username: FakeUsername,
                    assertionProvider: null);
        }

        [TestMethod]
        public void FederatedCredentialProvider_FromConfidentialClient_NullCca_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => FederatedCredentialProvider.FromConfidentialClient(null));
        }

        [TestMethod]
        public void FederatedCredentialProvider_FromManagedIdentity_NullId_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => FederatedCredentialProvider.FromManagedIdentity(null));
        }

        [TestMethod]
        public void FederatedCredentialProvider_FromConfidentialClient_ReturnsDelegate()
        {
            using var httpManager = new MockHttpManager();
            var cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithHttpManager(httpManager)
                .Build();

            var provider = FederatedCredentialProvider.FromConfidentialClient(cca);

            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void FederatedCredentialProvider_FromManagedIdentity_ReturnsDelegate()
        {
            var provider = FederatedCredentialProvider.FromManagedIdentity(ManagedIdentityId.SystemAssigned);

            Assert.IsNotNull(provider);
        }
    }
}
