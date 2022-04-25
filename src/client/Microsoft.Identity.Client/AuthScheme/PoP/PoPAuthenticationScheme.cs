// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    internal class PoPAuthenticationScheme : IAuthenticationScheme
    {
        private static readonly DateTime s_jwtBaselineTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly PoPAuthenticationConfiguration _popAuthenticationConfiguration;
        private IPoPCryptoProvider _popCryptoProvider;

        /// <summary>
        /// Creates POP tokens, i.e. tokens that are bound to an HTTP request and are digitally signed.
        /// </summary>
        /// <remarks>
        /// Currently the signing credential algorithm is hard-coded to RSA with SHA256. Extensibility should be done
        /// by integrating Wilson's SigningCredentials
        /// </remarks>
        public PoPAuthenticationScheme(PoPAuthenticationConfiguration popAuthenticationConfiguration, IServiceBundle serviceBundle)
        {
            if (serviceBundle == null)
            {
                throw new ArgumentNullException(nameof(serviceBundle));
            }

            _popAuthenticationConfiguration = popAuthenticationConfiguration ?? throw new ArgumentNullException(nameof(popAuthenticationConfiguration));

            _popCryptoProvider = _popAuthenticationConfiguration.PopCryptoProvider ?? serviceBundle.PlatformProxy.GetDefaultPoPCryptoProvider();

            var keyThumbprint = ComputeThumbprint(_popCryptoProvider.CannonicalPublicKeyJwk);
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
            if (!_popAuthenticationConfiguration.SignHttpRequest)
            {
                return msalAccessTokenCacheItem.Secret;
            }

            JObject header = new JObject
            {
                { JsonWebTokenConstants.ReservedHeaderParameters.Algorithm, _popCryptoProvider.CryptographicAlgorithm },
                { JsonWebTokenConstants.ReservedHeaderParameters.KeyId, KeyId },
                { JsonWebTokenConstants.ReservedHeaderParameters.Type, PoPRequestParameters.PoPTokenType}
            };

            JObject body = CreateBody(msalAccessTokenCacheItem);

            string popToken = CreateJWS(body.ToString(Formatting.None), header.ToString(Formatting.None));
            return popToken;
        }

        private JObject CreateBody(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            JToken publicKeyJWK = JToken.Parse(_popCryptoProvider.CannonicalPublicKeyJwk);
            List<JProperty> properties = new List<JProperty>(8);
            
            // Mandatory parameters
            properties.Add(new JProperty(PoPClaimTypes.Cnf, new JObject(new JProperty(PoPClaimTypes.JWK, publicKeyJWK))));
            properties.Add(new JProperty(PoPClaimTypes.Ts, DateTimeHelpers.CurrDateTimeInUnixTimestamp()));
            properties.Add(new JProperty(PoPClaimTypes.At, msalAccessTokenCacheItem.Secret));
            properties.Add(new JProperty(PoPClaimTypes.Nonce, _popAuthenticationConfiguration.Nonce ?? CreateSimpleNonce()));

            if (_popAuthenticationConfiguration.HttpMethod != null)
            {
                properties.Add(new JProperty(PoPClaimTypes.HttpMethod, _popAuthenticationConfiguration.HttpMethod?.ToString()));
            }

            if (!string.IsNullOrEmpty(_popAuthenticationConfiguration.HttpHost))
            {
                properties.Add(new JProperty(PoPClaimTypes.Host, _popAuthenticationConfiguration.HttpHost));
            }

            if (!string.IsNullOrEmpty(_popAuthenticationConfiguration.HttpPath))
            {
                properties.Add(new JProperty(PoPClaimTypes.Path, _popAuthenticationConfiguration.HttpPath));
            }

            var payload = new JObject(properties);

            return payload;
        }

        private static string CreateSimpleNonce()
        {
            // Guid with no hyphens
#if NETSTANDARD || WINDOWS_APP
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
        private static byte[] ComputeThumbprint(string cannonicalJwk)
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
