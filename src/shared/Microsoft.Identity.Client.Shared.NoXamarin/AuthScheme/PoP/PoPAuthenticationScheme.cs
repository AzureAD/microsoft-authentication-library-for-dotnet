// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    internal class PoPAuthenticationScheme : IAuthenticationScheme
    {
        private static readonly DateTime s_jwtBaselineTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly HttpRequestMessage _httpRequestMessage;
        private readonly IPoPCryptoProvider _popCryptoProvider;

        /// <summary>
        /// Creates POP tokens, i.e. tokens that are bound to an HTTP request and are digitally signed.
        /// </summary>
        /// <remarks>
        /// Currently the signing credential algorithm is hard-coded to RSA with SHA256. Extensibility should be done
        /// by integrating Wilson's SigningCredentials
        /// </remarks>
        public PoPAuthenticationScheme(HttpRequestMessage httpRequestMessage, IPoPCryptoProvider popCryptoProvider)
        {
            _httpRequestMessage = httpRequestMessage ?? throw new ArgumentNullException(nameof(httpRequestMessage));
            _popCryptoProvider = popCryptoProvider ?? throw new ArgumentNullException(nameof(popCryptoProvider));

            var keyThumbprint = ComputeRsaThumbprint(_popCryptoProvider.CannonicalPublicKeyJwk);
            KeyId = Base64UrlHelpers.Encode(keyThumbprint);
        }

        public string AuthorizationHeaderPrefix => PoPRequestParameters.PoPAuthHeaderPrefix;

        public string AccessTokenType => PoPRequestParameters.PoPTokenType;


        /// <summary>
        /// For PoP, we chose to use the base64(jwk_thumbprint)
        /// </summary>
        public string KeyId { get; }

        public IDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>() {
                { OAuth2Parameter.TokenType, PoPRequestParameters.PoPTokenType},
                { PoPRequestParameters.RequestConfirmation, ComputeReqCnf()}
            };
        }

        public string FormatAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            var header = new JObject
            {
                { JsonWebTokenConstants.ReservedHeaderParameters.Algorithm, JsonWebTokenConstants.Algorithms.RsaSha256 },
                { JsonWebTokenConstants.ReservedHeaderParameters.KeyId, KeyId },
                { JsonWebTokenConstants.ReservedHeaderParameters.Type, PoPRequestParameters.PoPTokenType}
            };

            JToken popAssertion = JToken.Parse(_popCryptoProvider.CannonicalPublicKeyJwk);

            var payload = new JObject(
                new JProperty(PoPClaimTypes.At, msalAccessTokenCacheItem.Secret),
                new JProperty(PoPClaimTypes.Ts, (long)(DateTime.UtcNow - s_jwtBaselineTime).TotalSeconds ),
                new JProperty(PoPClaimTypes.HttpMethod, _httpRequestMessage.Method.ToString()),
                new JProperty(PoPClaimTypes.Host, _httpRequestMessage.RequestUri.Host),
                new JProperty(PoPClaimTypes.Path, _httpRequestMessage.RequestUri.AbsolutePath),
                new JProperty(PoPClaimTypes.Nonce, CreateNonce()),
                new JProperty(PoPClaimTypes.Cnf, new JObject(
                    new JProperty(PoPClaimTypes.JWK, popAssertion))));

            string popToken =  CreateJWS(payload.ToString(Json.Formatting.None), header.ToString(Json.Formatting.None));

            // For POP, we can also update the HttpRequest with the authentication header
            _httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                AuthorizationHeaderPrefix,
                popToken);

            return popToken;
        }

        private static string CreateNonce()
        {
            // Guid with no hyphens
#if NETSTANDARD 
            return Guid.NewGuid().ToString("N");
#else
            return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
#endif
        }


        private string ComputeReqCnf()
        {
            // There are 4 possible formats for a JWK, but Evo supports only this one for simplicity
            var jwk = $@"{{""{JsonWebKeyParameterNames.Kid}"":""{KeyId}""}}";
            return Base64UrlHelpers.Encode(jwk);
        }

        /// <summary>
        /// A key ID that uniquely describes a public / private key pair. While KeyID is not normally
        /// strict, AAD support for PoP requires that we use the base64 encoded JWK thumbprint, as described by 
        /// https://tools.ietf.org/html/rfc7638
        /// </summary>
        private static byte[] ComputeRsaThumbprint(string cannonicalJwk)
        {
            // Cannot be easily generalized in UAP and NetStandard 1.3
            using (SHA256 hash = SHA256.Create())
            {
                return hash.ComputeHash(Encoding.UTF8.GetBytes(cannonicalJwk));
            }
        }

        /// <summary>
        /// Creates a JWS (json web signature) as per: https://tools.ietf.org/html/rfc7515
        /// Format: header.payload.signed_payload
        /// </summary>
        private string CreateJWS(string payload, string header)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Base64UrlHelpers.Encode(Encoding.UTF8.GetBytes(header)));
            sb.Append(".");
            sb.Append(Base64UrlHelpers.Encode(payload));
            string headerAndPayload = sb.ToString();

            sb.Append(".");
            sb.Append(Base64UrlHelpers.Encode(_popCryptoProvider.Sign(Encoding.UTF8.GetBytes(headerAndPayload))));

            return sb.ToString();
        }
    }
}
