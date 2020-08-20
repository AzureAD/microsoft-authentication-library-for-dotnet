// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Client;
using System.Threading;
using Microsoft.Identity.Client.PlatformsCommon;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Unit.PoP
{
    [TestClass]
    public class PopAuthenticationSchemeTests
    {
        // Key and JWT copied from the JWT spec https://tools.ietf.org/html/rfc7638#section-3
        private const string JWK = "{\"e\":\"AQAB\",\"kty\":\"RSA\",\"n\":\"0vx7agoebGcQSuuPiLJXZptN9nndrQmbXEps2aiAFbWhM78LhWx4cbbfAAtVT86zwu1RK7aPFFxuhDR1L6tSoc_BJECPebWKRXjBZCiFV4n3oknjhMstn64tZ_2W-5JsGY4Hc5n9yBXArwl93lqt7_RN5w6Cf0h4QyQ5v-65YGjQR0_FDW2QvzqY368QQMicAtaSqzs8KJZgnYb9c7d0zgdAZHzu6qMQvRL5hajrn1n91CbOpbISD08qNLyrdkt-bFTWhAI4vMQFh6WeZu0fM4lFd2NcRwr3XPksINHaQ-G_xBniIqbw0Ls1jF44-csFCur-kEgU8awapJzKnqDKgw\"}";
        private const string JWT = "NzbLsXh8uDCcd-6MNwXF4W_7noWXFZAfHkxZsRGC9Xs"; // for the JWK key

        [TestMethod]
        public void NullArgsTest()
        {
            Uri uri = new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b");
            HttpMethod method = HttpMethod.Post;
            HttpRequestMessage httpRequest = new HttpRequestMessage(method, uri);
            var popCryptoProvider = Substitute.For<IPoPCryptoProvider>();

            AssertException.Throws<ArgumentNullException>(() => new PoPAuthenticationScheme(null, popCryptoProvider));
            AssertException.Throws<ArgumentNullException>(() => new PoPAuthenticationScheme(httpRequest, null));
        }

        [TestMethod]
        public void ValidatePopRequestAndToken()
        {
            // Arrange
            Uri uri = new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b");
            HttpMethod method = HttpMethod.Post;
            HttpRequestMessage httpRequest = new HttpRequestMessage(method, uri);

            var popCryptoProvider = Substitute.For<IPoPCryptoProvider>();
            popCryptoProvider.CannonicalPublicKeyJwk.Returns(JWK);
            const string AtSecret = "secret";
            MsalAccessTokenCacheItem msalAccessTokenCacheItem = TokenCacheHelper.CreateAccessTokenItem();
            msalAccessTokenCacheItem.Secret = AtSecret;

            // Act
            PoPAuthenticationScheme authenticationScheme = new PoPAuthenticationScheme(httpRequest, popCryptoProvider);
            var tokenParams = authenticationScheme.GetTokenRequestParams();
            var popTokenString = authenticationScheme.FormatAccessToken(msalAccessTokenCacheItem);
            JwtSecurityToken decodedPopToken = new JwtSecurityToken(popTokenString);

            // Assert
            Assert.AreEqual("PoP", authenticationScheme.AuthorizationHeaderPrefix);
            Assert.AreEqual(JWT, authenticationScheme.KeyId);
            Assert.AreEqual(2, tokenParams.Count);
            Assert.AreEqual("pop", tokenParams["token_type"]);

            // This is the base64 URL encoding of the JWK containing only the KeyId
            Assert.AreEqual("eyJraWQiOiJOemJMc1hoOHVEQ2NkLTZNTndYRjRXXzdub1dYRlpBZkhreFpzUkdDOVhzIn0", tokenParams["req_cnf"]);
            Assert.AreEqual("RS256", decodedPopToken.Header.Alg);
            Assert.AreEqual(JWT, decodedPopToken.Header.Kid);
            Assert.AreEqual("pop", decodedPopToken.Header.Typ);
            Assert.AreEqual("RS256", decodedPopToken.SignatureAlgorithm);

            AssertSimpleClaim(decodedPopToken, "at", AtSecret);
            AssertSimpleClaim(decodedPopToken, "m", HttpMethod.Post.ToString());
            AssertSimpleClaim(decodedPopToken, "u", "www.contoso.com");
            AssertSimpleClaim(decodedPopToken, "p", "/path1/path2");

            string nonce = AssertSimpleClaim(decodedPopToken, "nonce");
            Assert.IsFalse(string.IsNullOrEmpty(nonce));
            string jwk = AssertSimpleClaim(decodedPopToken, "cnf");
            var jwkFromPopAssertion = JToken.Parse(jwk);

            var initialJwk = JToken.Parse(JWK);
            Assert.IsTrue(jwkFromPopAssertion["jwk"].DeepEquals(initialJwk));
        }

        [TestMethod]
        public void ValidateKeyExpiration()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                HttpRequestMessage request1 = new HttpRequestMessage(HttpMethod.Get, new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b"));

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                Guid correlationId = Guid.NewGuid();
                TestClock testClock = new TestClock();
                testClock.TestTime = DateTime.UtcNow;
                var provider = new NetSharedPoPCryptoProvider(testClock);

                app.AcquireTokenInteractive(TestConstants.s_scope)
                    .WithProofOfPosession(request1, provider)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                var JWK = provider.CannonicalPublicKeyJwk;
                //Advance time 7 hours. Should still be the same key
                testClock.TestTime = testClock.TestTime + TimeSpan.FromSeconds(60 * 60 * 7);

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);
                app.AcquireTokenInteractive(TestConstants.s_scope)
                    .WithProofOfPosession(request1, provider)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(JWK == provider.CannonicalPublicKeyJwk);
                //Advance time 1 hour. Should be a different key
                testClock.TestTime = testClock.TestTime + TimeSpan.FromSeconds(60 * 60 * 1);

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);
                app.AcquireTokenInteractive(TestConstants.s_scope)
                    .WithProofOfPosession(request1, provider)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(JWK != provider.CannonicalPublicKeyJwk);
            }
        }

        internal MockHttpAndServiceBundle CreateTestHarness(
                                            TelemetryCallback telemetryCallback = null,
                                            LogCallback logCallback = null,
                                            bool isExtendedTokenLifetimeEnabled = false)
        {
            return new MockHttpAndServiceBundle(
                telemetryCallback,
                logCallback,
                isExtendedTokenLifetimeEnabled,
                testContext: null);
        }

        private static string AssertSimpleClaim(JwtSecurityToken jwt, string expectedKey, string optionalExpectedValue = null)
        {
            string value = jwt.Claims.Single(c => c.Type.Equals(expectedKey, StringComparison.InvariantCultureIgnoreCase)).Value;
            if (optionalExpectedValue != null)
            {
                Assert.AreEqual(optionalExpectedValue, value);
            }
            return value;
        }
    }

    class TestClock : ITimeService
    {
        public DateTime TestTime { get; set; }

        public DateTime GetUtcNow()
        {
            return TestTime;
        }
    }
}
