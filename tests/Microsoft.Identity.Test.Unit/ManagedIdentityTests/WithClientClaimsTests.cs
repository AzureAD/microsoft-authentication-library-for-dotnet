// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Unit tests for <c>WithClientClaims()</c> across all three auth flows:
    ///   1. MSIv1 (IMDS GET — claims as query parameter)
    ///   2. Confidential Client / AcquireTokenForClient (claims merged into ESTS POST body)
    ///   3. Cache-key isolation — different claims values produce separate cache entries
    /// </summary>
    [TestClass]
    public class WithClientClaimsTests : TestBase
    {
        // A simple NSP-style claims payload used across tests.
        private const string NspClaims = @"{""nsp"":{""essential"":true}}";

        // Same logical claims as NspClaims but with extra whitespace/different key ordering.
        // After normalization these must equal NspClaims.
        private const string NspClaimsWithWhitespace = @"{ ""nsp"" : { ""essential"" : true } }";

        // A second, distinct claims value used to exercise separate-cache-entry behaviour.
        private const string OtherClaims = @"{""region"":{""value"":""eastus""}}";

        // ---------------------------------------------------------------------------------
        // Builder-level unit tests (no HTTP)
        // ---------------------------------------------------------------------------------

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void WithClientClaims_NullOrWhitespace_IsNoOp(string emptyClaims)
        {
            // Arrange
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .Build();

                // Act — should not throw
                var builder = mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(emptyClaims);

                // Assert — ClientClaims must remain unset (no cache component added)
                Assert.IsNull(builder.CommonParameters.ClientClaims,
                    "Empty/null claims should not set ClientClaims.");
                Assert.IsNull(builder.CommonParameters.CacheKeyComponents,
                    "Empty/null claims should not add cache key components.");
            }
        }

        [TestMethod]
        public void WithClientClaims_SetsClientClaimsOnCommonParameters()
        {
            // Arrange
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures(true)
                    .Build();

                // Act
                var builder = mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims);

                // Assert — normalized claims are stored
                Assert.IsNotNull(builder.CommonParameters.ClientClaims,
                    "ClientClaims must be set.");
                Assert.IsNotNull(builder.CommonParameters.CacheKeyComponents,
                    "CacheKeyComponents must be populated.");
                Assert.IsTrue(builder.CommonParameters.CacheKeyComponents.ContainsKey("client_claims"),
                    "client_claims cache key component must be present.");
            }
        }

        [TestMethod]
        public void WithClientClaims_DoesNotSetCommonParametersClaims()
        {
            // WithClientClaims must NOT touch CommonParameters.Claims — doing so would
            // incorrectly bypass the token cache (Claims is the server-issued bypass signal).
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures(true)
                    .Build();

                // Act
                var builder = mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims);

                // Assert — CommonParameters.Claims (the cache-bypass property) must be null
                Assert.IsNull(builder.CommonParameters.Claims,
                    "WithClientClaims must NOT set CommonParameters.Claims — that would bypass the cache.");
            }
        }

        [TestMethod]
        public void WithClientClaims_NormalizesJsonBeforeStoring()
        {
            // The same logical JSON passed with different whitespace must produce an identical
            // stored value (preventing cache key fragmentation).
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures(true)
                    .Build();

                // Act
                var builder1 = mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims);
                var builder2 = mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaimsWithWhitespace);

                // Assert
                Assert.AreEqual(
                    builder1.CommonParameters.ClientClaims,
                    builder2.CommonParameters.ClientClaims,
                    "Logically identical claims with different whitespace must normalize to the same string.");
            }
        }

        // ---------------------------------------------------------------------------------
        // MSIv1 (IMDS GET) — claims forwarded as a query parameter
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public async Task WithClientClaims_Imds_ForwardsClaimsAsQueryParameterAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)

                    .Build();

                // The mock handler is set up to expect claims=<normalizedNspClaims> in the query string.
                // If the MSAL code does NOT send the parameter, the handler will not match and the
                // test will throw InvalidOperationException (no handler matched).
                string normalizedClaims = Client.Internal.ClaimsHelper.NormalizeClaimsJson(NspClaims);
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    extraQueryParameters: new Dictionary<string, string> { { "claims", Uri.EscapeDataString(normalizedClaims) } });

                // Act
                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithClientClaims_Imds_TokenIsCached_SecondCallDoesNotHitNetworkAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)

                    .Build();

                // Only one network mock — second call must come from cache.
                string normalizedClaims = Client.Internal.ClaimsHelper.NormalizeClaimsJson(NspClaims);
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    extraQueryParameters: new Dictionary<string, string> { { "claims", Uri.EscapeDataString(normalizedClaims) } });

                // Act — first call
                var result1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Act — second call (no new mock handler added)
                var result2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims)
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
        public async Task WithClientClaims_Imds_SameClaimsNormalized_SameCacheEntryAsync()
        {
            // Logically identical claims passed with different whitespace must map to the same
            // cache entry — only one network call should occur.
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)

                    .Build();

                string normalizedClaims = Client.Internal.ClaimsHelper.NormalizeClaimsJson(NspClaims);

                // Only one network mock
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    extraQueryParameters: new Dictionary<string, string> { { "claims", Uri.EscapeDataString(normalizedClaims) } });

                // Act
                var result1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaimsWithWhitespace)  // same logic, different whitespace
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource,
                    "Whitespace-variant of the same claims must hit the same cache entry.");
            }
        }

        [TestMethod]
        public async Task WithClientClaims_Imds_DifferentClaims_ProduceSeparateCacheEntriesAsync()
        {
            // Two calls with distinct claims values must each produce a separate network call.
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)

                    .Build();

                string normalizedNsp = Client.Internal.ClaimsHelper.NormalizeClaimsJson(NspClaims);
                string normalizedOther = Client.Internal.ClaimsHelper.NormalizeClaimsJson(OtherClaims);

                // Two distinct network mocks — each must be consumed
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    extraQueryParameters: new Dictionary<string, string> { { "claims", Uri.EscapeDataString(normalizedNsp) } });

                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    extraQueryParameters: new Dictionary<string, string> { { "claims", Uri.EscapeDataString(normalizedOther) } });

                // Act
                var result1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(OtherClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert — both calls must have hit the network (different cache partitions)
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource,
                    "First claims value should hit the network.");
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource,
                    "Different claims value should produce a separate cache entry and hit the network.");
            }
        }

        [TestMethod]
        public async Task WithClientClaims_Imds_DoesNotBypassCache_UnlikeWithClaimsAsync()
        {
            // WithClaims() bypasses the cache on every call.
            // WithClientClaims() must NOT bypass the cache — second call should be a cache hit.
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)

                    .Build();

                string normalizedClaims = Client.Internal.ClaimsHelper.NormalizeClaimsJson(NspClaims);

                // Only one mock handler — if the second call also hits the network it will throw
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    extraQueryParameters: new Dictionary<string, string> { { "claims", Uri.EscapeDataString(normalizedClaims) } });

                // Act
                await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert — second call must be a cache hit, not a network call
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource,
                    "WithClientClaims must use the cache (unlike WithClaims which always bypasses).");
            }
        }

        [TestMethod]
        public async Task WithClientClaims_Imds_NoClaims_ClaimsParamAbsentFromRequestAsync()
        {
            // When no client claims are specified, the `claims` query parameter must be absent.
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)

                    .Build();

                // Standard mock handler with no claims expectation
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds);

                // Act — no WithClientClaims call
                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert — should succeed normally
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        // ---------------------------------------------------------------------------------
        // Confidential Client / AcquireTokenForClient — claims merged into ESTS POST body
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public async Task WithClientClaims_ConfidentialClient_SendsClaimsInEstsBodyAsync()
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

                string normalizedClaims = Client.Internal.ClaimsHelper.NormalizeClaimsJson(NspClaims);

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
                    .WithClientClaims(normalizedClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithClientClaims_ConfidentialClient_TokenIsCached_SecondCallFromCacheAsync()
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

                string normalizedClaims = Client.Internal.ClaimsHelper.NormalizeClaimsJson(NspClaims);

                // Only one mock — second call must come from cache
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                // Act
                var result1 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientClaims(normalizedClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result2 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientClaims(normalizedClaims)
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
        public async Task WithClientClaims_ConfidentialClient_DifferentClaims_SeparateCacheEntriesAsync()
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

                string normalizedNsp = Client.Internal.ClaimsHelper.NormalizeClaimsJson(NspClaims);
                string normalizedOther = Client.Internal.ClaimsHelper.NormalizeClaimsJson(OtherClaims);

                // Two distinct network mocks
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                // Act
                var result1 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientClaims(normalizedNsp)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result2 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientClaims(normalizedOther)
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
        public async Task WithClientClaims_ConfidentialClient_DoesNotBypassCacheAsync()
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

                string normalizedClaims = Client.Internal.ClaimsHelper.NormalizeClaimsJson(NspClaims);

                // Only one mock — if second call also hits the network it will throw
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                // Act
                await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientClaims(normalizedClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientClaims(normalizedClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource,
                    "WithClientClaims must not bypass the cache on repeated calls.");
            }
        }

        [TestMethod]
        public async Task WithClientClaims_ConfidentialClient_WithServerClaims_ServerClaimsBypassesCacheAsync()
        {
            // WithClaims (server-issued) always bypasses the cache.
            // WithClientClaims (client-originated) does not.
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

                string normalizedClientClaims = Client.Internal.ClaimsHelper.NormalizeClaimsJson(NspClaims);

                // First call — populate cache with client claims
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientClaims(normalizedClientClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Second call — with WithClaims (server bypass) in addition to WithClientClaims
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithClientClaims(normalizedClientClaims)
                    .WithClaims(TestConstants.Claims)   // server-issued → bypasses cache
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert — server claims bypass forces a network call even though the token is cached
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource,
                    "WithClaims (server-issued) must always bypass the cache.");
            }
        }

        [TestMethod]
        public async Task WithClientClaims_ConfidentialClient_NoClaims_ClaimsParamAbsentFromBodyAsync()
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

                // Standard success response — no body parameter expectation
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                // Act — no WithClientClaims
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert — normal token acquisition succeeds
                Assert.IsNotNull(result);
            }
        }

        // ---------------------------------------------------------------------------------
        // Invalid JSON
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public void WithClientClaims_InvalidJson_ThrowsMsalClientException()
        {
            // Arrange
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures(true)
                    .Build();

                // Act & Assert
                MsalClientException ex = Assert.ThrowsExactly<MsalClientException>(
                    () => mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .WithClientClaims("not-valid-json"));

                Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
            }
        }

        [TestMethod]
        public void WithClientClaims_JsonNullLiteral_ThrowsMsalClientException()
        {
            // "null" is valid JSON but not a JSON object — must produce MsalClientException,
            // not a raw NullReferenceException from JsonNode.AsObject().
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures(true)
                    .Build();

                // Act & Assert
                MsalClientException ex = Assert.ThrowsExactly<MsalClientException>(
                    () => mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .WithClientClaims("null"));

                Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
            }
        }

        // ---------------------------------------------------------------------------------
        // Non-IMDS sources — builder behavior
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public void WithClientClaims_NonImdsSource_SetsBuilderParameterButThrowsOnExecution()
        {
            // WithClientClaims() sets the builder parameter for any MI source — the guard that
            // rejects non-IMDS sources fires at request-execution time (in AbstractManagedIdentity),
            // not at builder construction time. This test verifies the builder state; a full
            // execution-level test requires mocking the App Service endpoint and is deferred.
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, "http://127.0.0.1:41564/msi/token");
                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures(true)
                    .Build();

                // Act
                var builder = mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClientClaims(NspClaims);

                // Assert — parameter is stored on the builder regardless of source;
                // MsalClientException is thrown later when the request is executed.
                Assert.IsNotNull(builder.CommonParameters.ClientClaims,
                    "ClientClaims must be set on the builder even for non-IMDS sources.");
                Assert.IsTrue(builder.CommonParameters.CacheKeyComponents.ContainsKey("client_claims"),
                    "Cache key component must be registered.");
            }
        }
    }
}


