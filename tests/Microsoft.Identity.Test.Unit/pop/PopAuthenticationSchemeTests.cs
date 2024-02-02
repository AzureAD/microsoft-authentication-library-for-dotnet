// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.Pop
{
    [TestClass]
    public class PopAuthenticationSchemeTests : TestBase
    {
        // Key and JWT copied from the JWT spec https://tools.ietf.org/html/rfc7638#section-3
        private const string JWK = "{\"e\":\"AQAB\",\"kty\":\"RSA\",\"n\":\"0vx7agoebGcQSuuPiLJXZptN9nndrQmbXEps2aiAFbWhM78LhWx4cbbfAAtVT86zwu1RK7aPFFxuhDR1L6tSoc_BJECPebWKRXjBZCiFV4n3oknjhMstn64tZ_2W-5JsGY4Hc5n9yBXArwl93lqt7_RN5w6Cf0h4QyQ5v-65YGjQR0_FDW2QvzqY368QQMicAtaSqzs8KJZgnYb9c7d0zgdAZHzu6qMQvRL5hajrn1n91CbOpbISD08qNLyrdkt-bFTWhAI4vMQFh6WeZu0fM4lFd2NcRwr3XPksINHaQ-G_xBniIqbw0Ls1jF44-csFCur-kEgU8awapJzKnqDKgw\"}";
        private const string JWT = "NzbLsXh8uDCcd-6MNwXF4W_7noWXFZAfHkxZsRGC9Xs"; // for the JWK key

        [TestMethod]
        public void NullArgsTest()
        {
            using (var harness = CreateTestHarness())
            {
                Uri uri = new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b");
                PoPAuthenticationConfiguration config = null;

                AssertException.Throws<ArgumentNullException>(() => new PopAuthenticationScheme(config, harness.ServiceBundle));

                config = new PoPAuthenticationConfiguration(uri);
                config.PopCryptoProvider = new InMemoryCryptoProvider();

                AssertException.Throws<ArgumentNullException>(() => new PopAuthenticationScheme(config, null));
                AssertException.Throws<ArgumentNullException>(() => new PoPAuthenticationConfiguration((HttpRequestMessage)null));
                AssertException.Throws<ArgumentNullException>(() => new PoPAuthenticationConfiguration((Uri)null));
            }
        }

        [TestMethod]
        public void ValidatePopRequestAndToken()
        {
            using (var harness = CreateTestHarness())
            {
                // Arrange
                Uri uri = new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b");
                PoPAuthenticationConfiguration popConfig = new PoPAuthenticationConfiguration(uri);
                popConfig.HttpMethod = HttpMethod.Post;

                var popCryptoProvider = Substitute.For<IPoPCryptoProvider>();
                var serviceBundle = Substitute.For<IServiceBundle>();
                popCryptoProvider.CannonicalPublicKeyJwk.Returns(JWK);
                popCryptoProvider.CryptographicAlgorithm.Returns("RS256");
                popConfig.PopCryptoProvider = popCryptoProvider;
                const string AtSecret = "secret";
                MsalAccessTokenCacheItem msalAccessTokenCacheItem = TokenCacheHelper.CreateAccessTokenItem();
                msalAccessTokenCacheItem.Secret = AtSecret;

                // Act
                PopAuthenticationScheme authenticationScheme = new PopAuthenticationScheme(popConfig, harness.ServiceBundle);
                var tokenParams = authenticationScheme.GetTokenRequestParams();
                var popTokenString = authenticationScheme.FormatAccessToken(msalAccessTokenCacheItem);
                JwtSecurityToken decodedPopToken = new JwtSecurityToken(popTokenString);

                // Assert
                Assert.AreEqual("PoP", authenticationScheme.AuthorizationHeaderPrefix);
                Assert.AreEqual(TokenType.Pop, authenticationScheme.TelemetryTokenType);
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
                Assert.IsTrue(JToken.DeepEquals(initialJwk, jwkFromPopAssertion["jwk"]));
            }
        }

        [TestMethod]
        public async Task ValidateKeyExpirationAsync()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                PoPAuthenticationConfiguration popConfig = new PoPAuthenticationConfiguration(new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b"));
                popConfig.HttpMethod = HttpMethod.Get;

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                .WithHttpManager(harness.HttpManager)
                                .WithExperimentalFeatures()
                                .WithClientSecret("some-secret")
                                .BuildConcrete();

                TokenCacheHelper.PopulateCache(app.AppTokenCacheInternal.Accessor);

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    authority: TestConstants.AuthorityCommonTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(token: $"header.{Guid.NewGuid()}.signature", tokenType: "pop"));

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    authority: TestConstants.AuthorityCommonTenant,
                    responseMessage: MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(token: $"header.{Guid.NewGuid()}.signature", tokenType: "pop"));

                Guid correlationId = Guid.NewGuid();
                TestTimeService testClock = new TestTimeService();
                PoPProviderFactory.TimeService = testClock;

                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                var initialToken = result.AccessToken;

                //Advance time 7 hours. Should still be the same key and token
                testClock.MoveToFuture(TimeSpan.FromHours(7));
                PoPProviderFactory.TimeService = testClock;

                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(GetAccessTokenFromPopToken(result.AccessToken), GetAccessTokenFromPopToken(initialToken));
                Assert.AreEqual(GetModulusFromPopToken(result.AccessToken), GetModulusFromPopToken(initialToken));
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);

                //Advance time 2 hours. Should be a different key
                testClock.MoveToFuture(TimeSpan.FromHours(2));
                PoPProviderFactory.TimeService = testClock;

                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithProofOfPossession(popConfig)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreNotEqual(GetModulusFromPopToken(result.AccessToken), GetModulusFromPopToken(initialToken));
                Assert.AreNotEqual(GetAccessTokenFromPopToken(result.AccessToken), GetAccessTokenFromPopToken(initialToken));
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);
            }
        }

        private string GetAccessTokenFromPopToken(string popToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(popToken);
            return jsonToken.Payload["at"].ToString();
        }

        private string GetModulusFromPopToken(string popToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(popToken);

            var jwtDecoded = Base64UrlHelpers.Decode(jsonToken.EncodedPayload);
            var jObj = JObject.Parse(jsonToken.Payload.First().Value.ToString());
            return jObj["jwk"]["n"].ToString();
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
}
