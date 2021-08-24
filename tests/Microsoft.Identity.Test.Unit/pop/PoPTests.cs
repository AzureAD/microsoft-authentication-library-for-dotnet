// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.pop
{

    [TestClass]
    public class PoPTests
    {
        private const string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";
        private const string ProtectedUrlWithPort = "https://www.contoso.com:5555/path1/path2?queryParam1=a&queryParam2=b";
        private const string CustomNonce = "my_nonce";

        [TestMethod]
        public async Task POP_ShrValidation_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));
                var popConfig = new PoPAuthenticationConfiguration(request);

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // access token parsing can be done with MSAL's id token parsing logic
                var claims = IdToken.Parse(result.AccessToken).ClaimsPrincipal;

                Assert.IsTrue(!string.IsNullOrEmpty(claims.FindAll("nonce").Single().Value));
                AssertSingedHttpRequestClaims(popConfig, claims);

            }
        }

        [TestMethod]
        public async Task POP_NoHttpRequest_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();

                // no HTTP method binding, but custom nonce
                var popConfig = new PoPAuthenticationConfiguration() { Nonce = CustomNonce };

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // access token parsing can be done with MSAL's id token parsing logic
                var claims = IdToken.Parse(result.AccessToken).ClaimsPrincipal;

                Assert.AreEqual(CustomNonce, claims.FindAll("nonce").Single().Value);
                AssertTsAndJwkClaims(popConfig, claims);

                Assert.IsFalse(claims.FindAll("m").Any());
                Assert.IsFalse(claims.FindAll("u").Any());
                Assert.IsFalse(claims.FindAll("p").Any());
            }
        }

        [TestMethod]
        public async Task POP_WithCustomNonce_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));
                var popConfig = new PoPAuthenticationConfiguration(request) { Nonce = CustomNonce };

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // access token parsing can be done with MSAL's id token parsing logic
                var claims = IdToken.Parse(result.AccessToken).ClaimsPrincipal;

                Assert.AreEqual(CustomNonce, claims.FindAll("nonce").Single().Value);
                AssertSingedHttpRequestClaims(popConfig, claims);
            }
        }

        private static void AssertSingedHttpRequestClaims(PoPAuthenticationConfiguration popConfig, System.Security.Claims.ClaimsPrincipal claims)
        {
            Assert.AreEqual("GET", claims.FindAll("m").Single().Value);
            Assert.AreEqual("www.contoso.com", claims.FindAll("u").Single().Value);
            Assert.AreEqual("/path1/path2", claims.FindAll("p").Single().Value);

            AssertTsAndJwkClaims(popConfig, claims);
        }

        private static void AssertTsAndJwkClaims(PoPAuthenticationConfiguration popConfig, System.Security.Claims.ClaimsPrincipal claims)
        {
            long ts = long.Parse(claims.FindAll("ts").Single().Value);
            CoreAssert.AreEqual(DateTimeOffset.UtcNow, CoreHelpers.UnixTimestampToDateTime(ts), TimeSpan.FromSeconds(5));

            string jwkClaim = claims.FindAll("cnf").Single().Value;
            JToken publicKey = JToken.Parse(popConfig.PopCryptoProvider.CannonicalPublicKeyJwk);
            JObject jwkInConfig = new JObject(new JProperty(PoPClaimTypes.JWK, publicKey));
            var jwkInToken = JObject.Parse(jwkClaim);

            Assert.IsTrue(JObject.DeepEquals(jwkInConfig, jwkInToken));
        }

        [TestMethod]
        public void PopConfig()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));
            var popConfig = new PoPAuthenticationConfiguration(request);

            Assert.AreEqual(HttpMethod.Get, popConfig.HttpMethod);
            Assert.AreEqual("www.contoso.com", popConfig.HttpHost);
            Assert.AreEqual("/path1/path2", popConfig.HttpPath);


            request = new HttpRequestMessage(HttpMethod.Post, new Uri(ProtectedUrlWithPort));
            popConfig = new PoPAuthenticationConfiguration(request);

            Assert.AreEqual(HttpMethod.Post, popConfig.HttpMethod);
            Assert.AreEqual("www.contoso.com:5555", popConfig.HttpHost);
            Assert.AreEqual("/path1/path2", popConfig.HttpPath);

            popConfig = new PoPAuthenticationConfiguration(); // no config

        }
    }

 
}
