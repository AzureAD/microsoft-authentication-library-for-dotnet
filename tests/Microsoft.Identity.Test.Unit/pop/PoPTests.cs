// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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

namespace Microsoft.Identity.Test.Unit.Pop
{

    [TestClass]
    public class PopTests : TestBase
    {
        private const string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";
        private const string ProtectedUrlWithPort = "https://www.contoso.com:5555/path1/path2?queryParam1=a&queryParam2=b";
        private const string CustomNonce = "my_nonce";

        [TestCleanup]
        public override void TestCleanup()
        {
            PoPProviderFactory.Reset();
            base.TestCleanup();
        }

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
                var provider = PoPProviderFactory.GetOrCreateProvider();

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
                AssertSingedHttpRequestClaims(provider, claims);
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
                var provider = PoPProviderFactory.GetOrCreateProvider();

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
                AssertTsAndJwkClaims(provider, claims);

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
                var provider = PoPProviderFactory.GetOrCreateProvider();

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
                AssertSingedHttpRequestClaims(provider, claims);
            }
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
        }

        [TestMethod]
        public async Task CacheKey_Includes_POPKid_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();
                var testTimeService = new TestTimeService();
                PoPProviderFactory.TimeService = testTimeService;

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));
                var popConfig = new PoPAuthenticationConfiguration(request);
                var cacheAccess = app.AppTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                // Act
                Trace.WriteLine("1. AcquireTokenForClient ");
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                string expectedKid = GetKidFromJwk(PoPProviderFactory.GetOrCreateProvider().CannonicalPublicKeyJwk);
                string actualCacheKey = cacheAccess.LastBeforeAccessNotificationArgs.SuggestedCacheKey;
                Assert.AreEqual(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}{1}_{2}_AppTokenCache",
                        expectedKid,
                        TestConstants.ClientId,
                        TestConstants.Utid),
                    actualCacheKey);

                // Arrange - force a new key by moving to the future
                (PoPProviderFactory.TimeService as TestTimeService).MoveToFuture(
                    PoPProviderFactory.KeyRotationInterval.Add(TimeSpan.FromMinutes(10)));

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "pop");

                // Act
                Trace.WriteLine("1. AcquireTokenForClient again, after time passes - expect POP key rotation");
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                   .WithAuthority(TestConstants.AuthorityUtidTenant)
                   .WithProofOfPossession(popConfig)
                   .ExecuteAsync()
                   .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                string expectedKid2 = GetKidFromJwk(PoPProviderFactory.GetOrCreateProvider().CannonicalPublicKeyJwk);
                string actualCacheKey2 = cacheAccess.LastBeforeAccessNotificationArgs.SuggestedCacheKey;
                Assert.AreEqual(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}{1}_{2}_AppTokenCache",
                        expectedKid2,
                        TestConstants.ClientId,
                        TestConstants.Utid),
                    actualCacheKey2);

                Assert.AreNotEqual(actualCacheKey, actualCacheKey2);
            }
        }

        /// <summary>
        /// A key ID that uniquely describes a public / private key pair. While KeyID is not normally
        /// strict, AAD support for PoP requires that we use the base64 encoded JWK thumbprint, as described by 
        /// https://tools.ietf.org/html/rfc7638
        /// </summary>
        private string GetKidFromJwk(string jwk)
        {

            using (SHA256 hash = SHA256.Create())
            {
                byte[] hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(jwk));
                return Base64UrlHelpers.Encode(hashBytes);
            }
        }

        private static void AssertSingedHttpRequestClaims(IPoPCryptoProvider popCryptoProvider, System.Security.Claims.ClaimsPrincipal claims)
        {
            Assert.AreEqual("GET", claims.FindAll("m").Single().Value);
            Assert.AreEqual("www.contoso.com", claims.FindAll("u").Single().Value);
            Assert.AreEqual("/path1/path2", claims.FindAll("p").Single().Value);

            AssertTsAndJwkClaims(popCryptoProvider, claims);
        }

        private static void AssertTsAndJwkClaims(IPoPCryptoProvider popCryptoProvider, System.Security.Claims.ClaimsPrincipal claims)
        {
            long ts = long.Parse(claims.FindAll("ts").Single().Value);
            CoreAssert.IsWithinRange(DateTimeOffset.UtcNow, DateTimeHelpers.UnixTimestampToDateTime(ts), TimeSpan.FromSeconds(5));

            string jwkClaim = claims.FindAll("cnf").Single().Value;
            JToken publicKey = JToken.Parse(popCryptoProvider.CannonicalPublicKeyJwk);
            JObject jwkInConfig = new JObject(new JProperty(PoPClaimTypes.JWK, publicKey));
            var jwkInToken = JObject.Parse(jwkClaim);

            Assert.IsTrue(JObject.DeepEquals(jwkInConfig, jwkInToken));
        }
    }
}
