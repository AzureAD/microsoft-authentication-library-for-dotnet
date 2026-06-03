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
    /// Unit tests for <c>WithClaimsFromClient()</c> across all three auth flows:
    ///   1. MSIv1 (IMDS GET — claims as query parameter)
    ///   2. Confidential Client / AcquireTokenForClient (claims merged into ESTS POST body)
    ///   3. Cache-key isolation — different claims values produce separate cache entries
    /// </summary>
    [TestClass]
    public class WithClaimsFromClientTests : TestBase
    {
        // A simple NSP-style claims payload used across tests. MSIv1 only allows the `xms_az_nwperimid` key.
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
            // Arrange
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .Build();

                // Act — should not throw
                var builder = mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaimsFromClient(emptyClaims);

                // Assert — ClientClaims must remain unset (no cache component added)
                Assert.IsNull(builder.CommonParameters.ClientClaims,
                    "Empty/null claims should not set ClientClaims.");
                Assert.IsNull(builder.CommonParameters.CacheKeyComponents,
                    "Empty/null claims should not add cache key components.");
            }
        }

        [TestMethod]
        public void WithClaimsFromClient_SetsClientClaimsOnCommonParameters()
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
                    .WithClaimsFromClient(NspClaims);

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
        public void WithClaimsFromClient_DoesNotSetCommonParametersClaims()
        {
            // WithClaimsFromClient must NOT touch CommonParameters.Claims — doing so would
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
                    .WithClaimsFromClient(NspClaims);

                // Assert — CommonParameters.Claims (the cache-bypass property) must be null
                Assert.IsNull(builder.CommonParameters.Claims,
                    "WithClaimsFromClient must NOT set CommonParameters.Claims — that would bypass the cache.");
            }
        }

        // ---------------------------------------------------------------------------------
        // MSIv1 (IMDS GET) — claims forwarded as a query parameter
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public async Task WithClaimsFromClient_Imds_ForwardsClaimsAsQueryParameterAsync()
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
                string normalizedClaims = NspClaims;
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    extraQueryParameters: new Dictionary<string, string> { { "claims", Uri.EscapeDataString(normalizedClaims) } });

                // Act
                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_Imds_TokenIsCached_SecondCallDoesNotHitNetworkAsync()
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
                string normalizedClaims = NspClaims;
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    extraQueryParameters: new Dictionary<string, string> { { "claims", Uri.EscapeDataString(normalizedClaims) } });

                // Act — first call
                var result1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Act — second call (no new mock handler added)
                var result2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaimsFromClient(NspClaims)
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
        public async Task WithClaimsFromClient_Imds_DifferentClaims_ProduceSeparateCacheEntriesAsync()
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

                string normalizedNsp = NspClaims;
                string normalizedOther = OtherClaims;

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
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaimsFromClient(OtherClaims)
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
        public async Task WithClaimsFromClient_Imds_DoesNotBypassCache_UnlikeWithClaimsAsync()
        {
            // WithClaims() bypasses the cache on every call.
            // WithClaimsFromClient() must NOT bypass the cache — second call should be a cache hit.
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)

                    .Build();

                string normalizedClaims = NspClaims;

                // Only one mock handler — if the second call also hits the network it will throw
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    extraQueryParameters: new Dictionary<string, string> { { "claims", Uri.EscapeDataString(normalizedClaims) } });

                // Act
                await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert — second call must be a cache hit, not a network call
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource,
                    "WithClaimsFromClient must use the cache (unlike WithClaims which always bypasses).");
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_Imds_CombinedWithWithClaims_ForwardsClientClaimsAndBypassesCacheAsync()
        {
            // When both .WithClaims (server-issued challenge) and .WithClaimsFromClient (client claims)
            // are supplied on the same MSI request:
            //   - Only the client claims are forwarded to IMDS as the `claims` query parameter
            //     (server-issued challenges are not a recognised IMDS contract).
            //   - .WithClaims causes the request to bypass the cache on every call, so two back-to-back
            //     calls with identical inputs must each hit the network.
            const string ServerClaims = @"{""access_token"":{""nbf"":{""essential"":true}}}";

            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .Build();

                // Two mocks — both expect ONLY the client claims in the `claims` parameter, never
                // a merged value that includes ServerClaims. If MSAL accidentally merges them or
                // forwards the server-issued claims, neither handler will match and the test fails.
                for (int i = 0; i < 2; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.ImdsEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiSuccessfulResponse(),
                        ManagedIdentitySource.Imds,
                        extraQueryParameters: new Dictionary<string, string>
                        {
                            { "claims", Uri.EscapeDataString(NspClaims) }
                        });
                }

                // Act — first call
                var result1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaims(ServerClaims)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Act — second call with identical inputs; .WithClaims must force a network round-trip
                var result2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaims(ServerClaims)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource,
                    "First call should hit the network.");
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource,
                    "WithClaims must bypass the cache even when WithClaimsFromClient is also set.");
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_Imds_NoClaims_ClaimsParamAbsentFromRequestAsync()
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

                // Act — no WithClaimsFromClient call
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

                // Standard success response — no body parameter expectation
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityUtidTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage());

                // Act — no WithClaimsFromClient
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
        //
        // Note: WithClaimsFromClient intentionally does NOT validate the JSON at builder time.
        // Per reviewer feedback (Bogdan), MSAL stores the raw caller string verbatim and does no
        // parsing on the hot path. Invalid JSON (e.g. "not-valid-json", "null") is forwarded as-is
        // and will surface as an MsalServiceException from the wire when IMDS/ESTS rejects it, or
        // as an MsalClientException from MergeClaimsObjects on cache miss when a server-issued
        // claims challenge is also present. Builder-time fail-fast tests were removed when the
        // NormalizeClaimsJson code path was deleted.
        // ---------------------------------------------------------------------------------

        // ---------------------------------------------------------------------------------
        // Non-IMDS sources — builder behavior
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public void WithClaimsFromClient_NonImdsSource_SetsBuilderParameterButThrowsOnExecution()
        {
            // WithClaimsFromClient() sets the builder parameter for any MI source — the guard that
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
                    .WithClaimsFromClient(NspClaims);

                // Assert — parameter is stored on the builder regardless of source;
                // MsalClientException is thrown later when the request is executed.
                Assert.IsNotNull(builder.CommonParameters.ClientClaims,
                    "ClientClaims must be set on the builder even for non-IMDS sources.");
                Assert.IsTrue(builder.CommonParameters.CacheKeyComponents.ContainsKey("client_claims"),
                    "Cache key component must be registered.");
            }
        }

        [TestMethod]
        [DataRow(ManagedIdentitySource.AppService, "http://127.0.0.1:41564/msi/token")]
        [DataRow(ManagedIdentitySource.AzureArc, "http://127.0.0.1:40342/metadata/identity/oauth2/token")]
        [DataRow(ManagedIdentitySource.CloudShell, "http://localhost:50342/oauth2/token")]
        [DataRow(ManagedIdentitySource.ServiceFabric, "https://127.0.0.1:2377/metadata/identity/oauth2/token")]
        [DataRow(ManagedIdentitySource.MachineLearning, "http://localhost:7071/msi/token")]
        public async Task WithClaimsFromClient_NonImdsSource_ExecuteThrowsMsalClientExceptionAsync(
            ManagedIdentitySource source,
            string endpoint)
        {
            // Only IMDS / IMDSv2 are wired to forward client claims today. Any other source must
            // fail fast with MsalClientException at execute time so callers don't silently lose
            // their claims (and so the cache doesn't pollute with keys the endpoint never saw).
            using (new EnvVariableContext())
            {
                SetEnvironmentVariables(source, endpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures(true)
                    .Build();

                MsalClientException ex = await Assert.ThrowsExactlyAsync<MsalClientException>(
                    () => mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .WithClaimsFromClient(NspClaims)
                            .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.InvalidRequest, ex.ErrorCode);
                Assert.Contains(source.ToString(), ex.Message,
                    "Error message should name the detected source.");
                Assert.Contains("IMDS", ex.Message,
                    "Error message should explain only IMDS sources are supported.");
            }
        }

        // ---------------------------------------------------------------------------------
        // MSIv1 claim allowlist validation — only xms_az_nwperimid is permitted
        // ---------------------------------------------------------------------------------

        private const string ValidNspClaim = @"{""xms_az_nwperimid"":{""values"":[""perimid-1234""]}}";
        private const string UnsupportedClaim = @"{""custom_claim"":{""essential"":true}}";
        private const string MixedClaims = @"{""xms_az_nwperimid"":{""values"":[""perimid-1234""]},""other_claim"":{""essential"":true}}";

        [TestMethod]
        public async Task WithClaimsFromClient_Imds_ValidXmsAzNwperimid_SucceedsAsync()
        {
            // xms_az_nwperimid is the only allowed claim for MSIv1; a request carrying it must succeed.
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .Build();

                string normalizedClaims = ValidNspClaim;
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    extraQueryParameters: new Dictionary<string, string> { { "claims", Uri.EscapeDataString(normalizedClaims) } });

                // Act
                var result = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .WithClaimsFromClient(ValidNspClaim)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_Imds_UnsupportedClaim_ThrowsMsalClientExceptionAsync()
        {
            // Any claim key other than xms_az_nwperimid must be rejected before the network call,
            // so the caller gets a clear error instead of an opaque HTTP 400 from IMDS.
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .Build();

                // Act & Assert — MsalClientException must be thrown before any HTTP request is made
                MsalClientException ex = await Assert.ThrowsExactlyAsync<MsalClientException>(
                    () => mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .WithClaimsFromClient(UnsupportedClaim)
                            .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.InvalidRequest, ex.ErrorCode);
                Assert.Contains("xms_az_nwperimid", ex.Message, "Error message should name the only allowed claim.");
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_Imds_MixedClaims_ThrowsMsalClientExceptionAsync()
        {
            // Even if xms_az_nwperimid is present, any additional claims must be rejected.
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .Build();

                // Act & Assert
                MsalClientException ex = await Assert.ThrowsExactlyAsync<MsalClientException>(
                    () => mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                            .WithClaimsFromClient(MixedClaims)
                            .ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.InvalidRequest, ex.ErrorCode);
            }
        }
    }
}


