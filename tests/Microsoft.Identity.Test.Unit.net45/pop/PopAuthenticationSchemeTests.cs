// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.PoP;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit
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
            var popCryptoProvider = Substitute.For<IPoPCryptoProvider>();

            AssertException.Throws<ArgumentException>(() => new PoPAuthenticationScheme(null, method, popCryptoProvider));
            AssertException.Throws<ArgumentException>(() => new PoPAuthenticationScheme(uri, null, popCryptoProvider));
            AssertException.Throws<ArgumentException>(() => new PoPAuthenticationScheme(uri, method, null));
        }

        [TestMethod]
        public void ValidatePopRequestAndToken()
        {
            // Arrange
            Uri uri = new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b");
            HttpMethod method = HttpMethod.Post;
            var popCryptoProvider = Substitute.For<IPoPCryptoProvider>();
            popCryptoProvider.CannonicalPublicKeyJwk.Returns(JWK);
            const string AtSecret = "secret";
            MsalAccessTokenCacheItem msalAccessTokenCacheItem = new MsalAccessTokenCacheItem()
            {
                Secret = AtSecret
            };

            // Act
            PoPAuthenticationScheme authenticationScheme = new PoPAuthenticationScheme(uri, method, popCryptoProvider);
            var tokenParams = authenticationScheme.GetTokenRequestParams();
            var popTokenString = authenticationScheme.FormatAccessToken(msalAccessTokenCacheItem);
            JwtSecurityToken decodedPopToken = new JwtSecurityToken(popTokenString);

            // Assert
            Assert.AreEqual("POP", authenticationScheme.AuthorizationHeaderPrefix);
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
