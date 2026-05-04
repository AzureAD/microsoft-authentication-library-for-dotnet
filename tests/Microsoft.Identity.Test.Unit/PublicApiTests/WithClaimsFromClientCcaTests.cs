// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class WithClaimsFromClientCcaTests : TestBase
    {
        private const string ClientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
        private readonly string[] _scope = new[] { "api://scope/.default" };

        private const string NspClaims =
            """{"access_token":{"xms_nsp_id":{"essential":true,"value":"nsp-001"}}}""";
        private const string NspClaimsDifferent =
            """{"access_token":{"xms_nsp_id":{"essential":true,"value":"nsp-002"}}}""";

        [TestMethod]
        public async Task WithClaimsFromClient_CCA_CachedAndPartitioned_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();

                // First request — hits endpoint
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_nsp1");

                var result1 = await app.AcquireTokenForClient(_scope)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("token_nsp1", result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Same claims — cache hit
                var result2 = await app.AcquireTokenForClient(_scope)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("token_nsp1", result2.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);

                // Different claims — cache miss, new token
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_nsp2");

                var result3 = await app.AcquireTokenForClient(_scope)
                    .WithClaimsFromClient(NspClaimsDifferent)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("token_nsp2", result3.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result3.AuthenticationResultMetadata.TokenSource);

                // Two distinct tokens in cache
                Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            }
        }

        [TestMethod]
        public async Task WithClaimsFromClient_CCA_DoesNotBypassCache_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "cached_token");

                await app.AcquireTokenForClient(_scope)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // No second mock handler — proves cache is used
                var result = await app.AcquireTokenForClient(_scope)
                    .WithClaimsFromClient(NspClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("cached_token", result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public void WithClaimsFromClient_CCA_InvalidJson_Throws()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                var ex = AssertException.Throws<MsalClientException>(() =>
                    app.AcquireTokenForClient(_scope)
                        .WithClaimsFromClient("not-json"));

                Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
            }
        }

        [TestMethod]
        public void WithClaimsFromClient_CCA_JsonArray_Throws()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures(true)
                    .BuildConcrete();

                var ex = AssertException.Throws<MsalClientException>(() =>
                    app.AcquireTokenForClient(_scope)
                        .WithClaimsFromClient("[1,2,3]"));

                Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
            }
        }

        [TestMethod]
        public void WithClaimsFromClient_CCA_RequiresExperimental()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                var ex = AssertException.Throws<MsalClientException>(() =>
                    app.AcquireTokenForClient(_scope)
                        .WithClaimsFromClient(NspClaims));

                Assert.AreEqual(MsalError.ExperimentalFeature, ex.ErrorCode);
            }
        }
    }
}
