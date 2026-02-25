// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !ANDROID && !iOS
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
        private const string FakeAssertion = "fake_federated_assertion_token";
        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";

        [TestMethod]
        public async Task UserFic_HappyPath_ReturnsTokenAsync()
        {
            using var harness = CreateTestHarness();

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(new Uri(TestConstants.AuthorityCommonTenant), true)
                .WithHttpManager(harness.HttpManager)
                .WithCertificate(CertHelper.GetOrCreateTestCert())
                .Build();

            harness.HttpManager.AddInstanceDiscoveryMockHandler();
            harness.HttpManager.AddMockHandler(CreateUserFicTokenResponseHandler());

            int callbackInvocations = 0;
            var result = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    TestConstants.Username,
                    async () =>
                    {
                        callbackInvocations++;
                        return await Task.FromResult(FakeAssertion).ConfigureAwait(false);
                    })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(1, callbackInvocations, "Callback should be invoked exactly once for a fresh token.");
        }

        [TestMethod]
        public async Task UserFic_CacheHit_DoesNotInvokeCallbackAsync()
        {
            using var harness = CreateTestHarness();

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(new Uri(TestConstants.AuthorityCommonTenant), true)
                .WithHttpManager(harness.HttpManager)
                .WithCertificate(CertHelper.GetOrCreateTestCert())
                .Build();

            harness.HttpManager.AddInstanceDiscoveryMockHandler();
            harness.HttpManager.AddMockHandler(CreateUserFicTokenResponseHandler());

            int callbackInvocations = 0;
            Func<Task<string>> callback = async () =>
            {
                callbackInvocations++;
                return await Task.FromResult(FakeAssertion).ConfigureAwait(false);
            };

            // First call: hits the network and invokes callback
            var firstResult = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    TestConstants.Username,
                    callback)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(1, callbackInvocations);
            Assert.AreEqual(TokenSource.IdentityProvider, firstResult.AuthenticationResultMetadata.TokenSource);

            // Second call: should hit the cache
            int callbackInvocationsBeforeSilent = callbackInvocations;
            IAccount account = await app.GetAccountAsync(firstResult.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            Assert.IsNotNull(account, "Account should be cached after the first UserFIC call.");

            var silentResult = await app
                .AcquireTokenSilent(TestConstants.s_scope, account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silentResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(callbackInvocationsBeforeSilent, callbackInvocations, "Callback must NOT be invoked for a cache hit.");
        }

        [TestMethod]
        public async Task UserFic_WithForceRefresh_InvokesCallbackAgainAsync()
        {
            using var harness = CreateTestHarness();

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(new Uri(TestConstants.AuthorityCommonTenant), true)
                .WithHttpManager(harness.HttpManager)
                .WithCertificate(CertHelper.GetOrCreateTestCert())
                .Build();

            harness.HttpManager.AddInstanceDiscoveryMockHandler();
            harness.HttpManager.AddMockHandler(CreateUserFicTokenResponseHandler()); // first call
            harness.HttpManager.AddMockHandler(CreateUserFicTokenResponseHandler()); // force-refresh call

            int callbackInvocations = 0;
            Func<Task<string>> callback = async () =>
            {
                callbackInvocations++;
                return await Task.FromResult(FakeAssertion).ConfigureAwait(false);
            };

            // First call: hits the network
            await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    TestConstants.Username,
                    callback)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(1, callbackInvocations);

            // Second call: with ForceRefresh should bypass cache and invoke callback again
            var result = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    TestConstants.Username,
                    callback)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(2, callbackInvocations, "Callback should be invoked again on ForceRefresh.");
        }

        [TestMethod]
        public void UserFic_NullUsername_ThrowsAsync()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(CertHelper.GetOrCreateTestCert())
                .Build();

            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                (app as IByUserFederatedIdentityCredential)
                    .AcquireTokenByUserFederatedIdentityCredential(
                        TestConstants.s_scope,
                        null,
                        () => Task.FromResult(FakeAssertion)));

            Assert.AreEqual("username", ex.ParamName);
        }

        [TestMethod]
        public void UserFic_NullCallback_Throws()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(CertHelper.GetOrCreateTestCert())
                .Build();

            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                (app as IByUserFederatedIdentityCredential)
                    .AcquireTokenByUserFederatedIdentityCredential(
                        TestConstants.s_scope,
                        TestConstants.Username,
                        null));

            Assert.AreEqual("assertionCallback", ex.ParamName);
        }

        [TestMethod]
        public async Task UserFic_RequestContainsCorrectGrantTypeAndParams_Async()
        {
            using var harness = CreateTestHarness();

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(new Uri(TestConstants.AuthorityCommonTenant), true)
                .WithHttpManager(harness.HttpManager)
                .WithCertificate(CertHelper.GetOrCreateTestCert())
                .Build();

            harness.HttpManager.AddInstanceDiscoveryMockHandler();

            string capturedGrantType = null;
            string capturedUsername = null;
            string capturedAssertion = null;

            var handler = new MockHttpMessageHandler
            {
                ExpectedUrl = TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                AdditionalRequestValidation = request =>
                {
                    var body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var parameters = body.Split('&')
                        .Select(p => p.Split('='))
                        .ToDictionary(kv => Uri.UnescapeDataString(kv[0]), kv => kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "");

                    parameters.TryGetValue("grant_type", out capturedGrantType);
                    parameters.TryGetValue("username", out capturedUsername);
                    parameters.TryGetValue("user_federated_identity_credential", out capturedAssertion);
                }
            };
            harness.HttpManager.AddMockHandler(handler);

            await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    TestConstants.s_scope,
                    TestConstants.Username,
                    () => Task.FromResult(FakeAssertion))
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(OAuth2GrantType.UserFic, capturedGrantType, "grant_type must be user_fic");
            Assert.AreEqual(TestConstants.Username, capturedUsername, "username must be set");
            Assert.AreEqual(FakeAssertion, capturedAssertion, "user_federated_identity_credential must contain assertion");
        }

        private static MockHttpMessageHandler CreateUserFicTokenResponseHandler()
        {
            return new MockHttpMessageHandler
            {
                ExpectedUrl = TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            };
        }
    }
}
#endif
