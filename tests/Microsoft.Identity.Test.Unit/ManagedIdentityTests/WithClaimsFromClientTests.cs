// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class WithClaimsFromClientTests : TestBase
    {
        private const string Resource = "https://management.azure.com";
        private const string ImdsEndpoint = "http://169.254.169.254/metadata/identity/oauth2/token";

        // OIDC Section 5.5 — NSP claim with essential + value
        private const string NspClaims =
            """{"access_token":{"xms_nsp_id":{"essential":true,"value":"nsp-perimeter-001"}}}""";
        private const string NspClaimsDifferent =
            """{"access_token":{"xms_nsp_id":{"essential":true,"value":"nsp-perimeter-002"}}}""";
        // OIDC Section 5.5.1 — voluntary claim (null value)
        private const string VoluntaryClaim =
            """{"access_token":{"xms_nsp_id":null}}""";

        private IManagedIdentityApplication BuildMi(MockHttpManager httpManager)
        {
            return ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithExperimentalFeatures(true)
                .WithHttpManager(httpManager)
                .Build();
        }

        private void AddImdsHandler(MockHttpManager httpManager, string claimsJson)
        {
            var handler = httpManager.AddManagedIdentityMockHandler(
                ImdsEndpoint, Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.Imds);

            handler.ExpectedQueryParams["claims"] = Uri.EscapeDataString(claimsJson);
        }

        [TestMethod]
        public async Task WithClaimsFromClient_SameClaims_ReturnsCacheHit()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);
                var mi = BuildMi(httpManager);
                AddImdsHandler(httpManager, NspClaims);

                var result1 = await mi.AcquireTokenForManagedIdentity(Resource)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync().ConfigureAwait(false);

                var result2 = await mi.AcquireTokenForManagedIdentity(Resource)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_DifferentClaims_DifferentCacheEntries()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);
                var mi = BuildMi(httpManager);
                AddImdsHandler(httpManager, NspClaims);
                AddImdsHandler(httpManager, NspClaimsDifferent);

                var result1 = await mi.AcquireTokenForManagedIdentity(Resource)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync().ConfigureAwait(false);

                var result2 = await mi.AcquireTokenForManagedIdentity(Resource)
                    .WithClaimsFromClient(NspClaimsDifferent)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_DoesNotBypassCache()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);
                var mi = BuildMi(httpManager);
                AddImdsHandler(httpManager, NspClaims);

                await mi.AcquireTokenForManagedIdentity(Resource)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync().ConfigureAwait(false);

                // No second handler — cache must be used
                var result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public void WithClaimsFromClient_InvalidJson_ThrowsImmediately()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);
                var mi = BuildMi(httpManager);

                var ex = AssertException.Throws<MsalClientException>(() =>
                    mi.AcquireTokenForManagedIdentity(Resource)
                        .WithClaimsFromClient("not-valid-json{{{"));

                Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
            }
        }

        [TestMethod]
        public void WithClaimsFromClient_NullOrEmpty_ThrowsArgumentNull()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);
                var mi = BuildMi(httpManager);

                AssertException.Throws<ArgumentNullException>(() =>
                    mi.AcquireTokenForManagedIdentity(Resource).WithClaimsFromClient(null));

                AssertException.Throws<ArgumentNullException>(() =>
                    mi.AcquireTokenForManagedIdentity(Resource).WithClaimsFromClient(""));

                AssertException.Throws<ArgumentNullException>(() =>
                    mi.AcquireTokenForManagedIdentity(Resource).WithClaimsFromClient("   "));
            }
        }

        [TestMethod]
        public void WithClaimsFromClient_RequiresExperimentalFeature()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);

                var mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .Build();

                var ex = AssertException.Throws<MsalClientException>(() =>
                    mi.AcquireTokenForManagedIdentity(Resource)
                        .WithClaimsFromClient(NspClaims));

                Assert.AreEqual(MsalError.ExperimentalFeature, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_ForwardsClaimsAsQueryParam()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);
                var mi = BuildMi(httpManager);
                var handler = AddImdsHandlerWithReturn(httpManager, NspClaims);

                await mi.AcquireTokenForManagedIdentity(Resource)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(handler.ActualRequestMessage);
                string requestUrl = handler.ActualRequestMessage.RequestUri.ToString();
                StringAssert.Contains(requestUrl, "claims=",
                    "Expected 'claims' query parameter in URL: " + requestUrl);
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_OidcVoluntaryClaim_Cached()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);
                var mi = BuildMi(httpManager);
                AddImdsHandler(httpManager, VoluntaryClaim);

                var result1 = await mi.AcquireTokenForManagedIdentity(Resource)
                    .WithClaimsFromClient(VoluntaryClaim)
                    .ExecuteAsync().ConfigureAwait(false);

                var result2 = await mi.AcquireTokenForManagedIdentity(Resource)
                    .WithClaimsFromClient(VoluntaryClaim)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public void WithClaimsFromClient_JsonArray_ThrowsInvalidFormat()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);
                var mi = BuildMi(httpManager);

                // Valid JSON but not a JSON object — OIDC requires an object
                var ex = AssertException.Throws<MsalClientException>(() =>
                    mi.AcquireTokenForManagedIdentity(Resource)
                        .WithClaimsFromClient("[1,2,3]"));

                Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_CalledTwice_LastWins()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, ImdsEndpoint);
                var mi = BuildMi(httpManager);
                AddImdsHandler(httpManager, NspClaimsDifferent);

                // Call WithClaimsFromClient twice — second call should overwrite
                var result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .WithClaimsFromClient(NspClaims)
                    .WithClaimsFromClient(NspClaimsDifferent)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        private MockHttpMessageHandler AddImdsHandlerWithReturn(MockHttpManager httpManager, string claimsJson)
        {
            var handler = httpManager.AddManagedIdentityMockHandler(
                ImdsEndpoint, Resource,
                MockHelpers.GetMsiSuccessfulResponse(),
                ManagedIdentitySource.Imds);

            handler.ExpectedQueryParams["claims"] = Uri.EscapeDataString(claimsJson);
            return handler;
        }
    }
}
