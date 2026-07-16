// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    /// <summary>
    /// Unit tests for <c>WithClaimsFromClient()</c> on confidential client
    /// (<see cref="ConfidentialClientApplication"/> / AcquireTokenForClient).
    /// Client-originated claims are merged into the ESTS POST body and participate in cache
    /// keying (unlike server-issued <c>WithClaims</c>, which bypasses the cache).
    /// </summary>
    [TestClass]
    public class WithClaimsFromClientTests : TestBase
    {
        // A simple NSP-style claims payload used across tests.
        private const string NspClaims = @"{""xms_az_nwperimid"":{""essential"":true}}";

        // A second, distinct claims value used to exercise separate-cache-entry behaviour.
        private const string OtherClaims = @"{""xms_az_nwperimid"":{""values"":[""eastus""]}}";

        // ---------------------------------------------------------------------------------
        // Builder-level unit tests (no HTTP)
        // ---------------------------------------------------------------------------------

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void WithClaimsFromClient_NullOrWhitespace_IsNoOp(string emptyClaims)
        {
            // Arrange — experimental features intentionally NOT enabled: the null/whitespace
            // guard must return before the experimental-feature gate is evaluated.
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            // Act — should not throw
            var builder = app.AcquireTokenForClient(TestConstants.s_scope)
                .WithClaimsFromClient(emptyClaims);

            // Assert — ClientClaims must remain unset (no cache component added)
            Assert.IsNull(builder.CommonParameters.ClientClaims,
                "Empty/null claims should not set ClientClaims.");
            Assert.IsNull(builder.CommonParameters.CacheKeyComponents,
                "Empty/null claims should not add cache key components.");
        }

        [TestMethod]
        public void WithClaimsFromClient_SetsClientClaimsOnCommonParameters()
        {
            // Arrange
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithExperimentalFeatures(true)
                .BuildConcrete();

            // Act
            var builder = app.AcquireTokenForClient(TestConstants.s_scope)
                .WithClaimsFromClient(NspClaims);

            // Assert — client claims are stored and keyed into the cache
            Assert.IsNotNull(builder.CommonParameters.ClientClaims,
                "ClientClaims must be set.");
            Assert.IsNotNull(builder.CommonParameters.CacheKeyComponents,
                "CacheKeyComponents must be populated.");
            Assert.IsTrue(builder.CommonParameters.CacheKeyComponents.ContainsKey("client_claims"),
                "client_claims cache key component must be present.");
        }

        [TestMethod]
        public void WithClaimsFromClient_DoesNotSetCommonParametersClaims()
        {
            // WithClaimsFromClient must NOT touch CommonParameters.Claims — doing so would
            // incorrectly bypass the token cache (Claims is the server-issued bypass signal).
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithExperimentalFeatures(true)
                .BuildConcrete();

            // Act
            var builder = app.AcquireTokenForClient(TestConstants.s_scope)
                .WithClaimsFromClient(NspClaims);

            // Assert — CommonParameters.Claims (the cache-bypass property) must be null
            Assert.IsNull(builder.CommonParameters.Claims,
                "WithClaimsFromClient must NOT set CommonParameters.Claims — that would bypass the cache.");
        }

        // ---------------------------------------------------------------------------------
        // Confidential Client / AcquireTokenForClient — claims merged into ESTS POST body
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public async Task WithClaimsFromClient_ConfidentialClient_SendsClaimsInEstsBodyAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                string normalizedClaims = NspClaims;

                // The POST body must contain claims=<normalizedClaims>
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    bodyParameters: new Dictionary<string, string>
                    {
                        { OAuth2Parameter.Claims, normalizedClaims }
                    },
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaimsFromClient(normalizedClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_ConfidentialClient_TokenIsCached_SecondCallFromCacheAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                string normalizedClaims = NspClaims;

                // Only one mock — second call must come from cache
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                // Act
                var result1 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaimsFromClient(normalizedClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result2 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaimsFromClient(normalizedClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource,
                    "First call should hit the network.");
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource,
                    "Second call with identical claims must be served from cache.");
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_ConfidentialClient_DifferentClaims_SeparateCacheEntriesAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                string normalizedNsp = NspClaims;
                string normalizedOther = OtherClaims;

                // Two distinct network mocks
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                // Act
                var result1 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaimsFromClient(normalizedNsp)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result2 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaimsFromClient(normalizedOther)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource,
                    "First claims value should hit the network.");
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource,
                    "Different claims value should produce a separate cache entry and hit the network.");
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_ConfidentialClient_DoesNotBypassCacheAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                string normalizedClaims = NspClaims;

                // Only one mock — if second call also hits the network it will throw
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                // Act
                await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaimsFromClient(normalizedClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaimsFromClient(normalizedClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource,
                    "WithClaimsFromClient must not bypass the cache on repeated calls.");
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_ConfidentialClient_WithServerClaims_ServerClaimsBypassesCacheAsync()
        {
            // WithClaims (server-issued) always bypasses the cache.
            // WithClaimsFromClient (client-originated) does not.
            // When both are used together, the server claim should still bypass the cache.
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                string normalizedClientClaims = NspClaims;

                // First call — populate cache with client claims
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaimsFromClient(normalizedClientClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Second call — with WithClaims (server bypass) in addition to WithClaimsFromClient
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClaimsFromClient(normalizedClientClaims)
                    .WithClaims(TestConstants.Claims)   // server-issued → bypasses cache
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert — server claims bypass forces a network call even though the token is cached
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                    "WithClaims (server-issued) must always bypass the cache.");
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_ConfidentialClient_NoClaims_ClaimsParamAbsentFromBodyAsync()
        {
            // When no client claims are specified, the `claims` body parameter must not appear.
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, TestConstants.Utid)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(harness.HttpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                // Standard success response — assert the `claims` body parameter is absent
                var handler = harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                handler.UnExpectedPostData = new Dictionary<string, string>
                {
                    { OAuth2Parameter.Claims, null }
                };

                // Act — no WithClaimsFromClient
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert — normal token acquisition succeeds
                Assert.IsNotNull(result);
            }
        }
    }
}
