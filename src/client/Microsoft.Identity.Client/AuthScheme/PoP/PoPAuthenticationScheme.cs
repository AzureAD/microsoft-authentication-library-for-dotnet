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
#if SUPPORTS_SYSTEM_TEXT_JSON
using JObject = System.Text.Json.Nodes.JsonObject;
using JToken = System.Text.Json.Nodes.JsonNode;
#else
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    internal class PopAuthenticationScheme : IAuthenticationScheme
    {
        private readonly PoPAuthenticationConfiguration _popAuthenticationConfiguration;
        private readonly IPoPCryptoProvider _popCryptoProvider;

        /// <summary>
        /// Creates POP tokens, i.e. tokens that are bound to an HTTP request and are digitally signed.
        /// </summary>
        /// <remarks>
        /// Currently the signing credential algorithm is hard-coded to RSA with SHA256. Extensibility should be done
        /// by integrating Wilson's SigningCredentials
        /// </remarks>
        public PopAuthenticationScheme(PoPAuthenticationConfiguration popAuthenticationConfiguration, IServiceBundle serviceBundle)
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

        public TokenType TelemetryTokenType => TokenType.Pop;

        public string AuthorizationHeaderPrefix => Constants.PoPAuthHeaderPrefix;

        public string AccessTokenType => Constants.PoPTokenType;

        /// <summary>
        /// For PoP, we chose to use the base64(jwk_thumbprint)
        /// </summary>
        public string KeyId { get; }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>() {
                { OAuth2Parameter.TokenType, Constants.PoPTokenType},
                { Constants.RequestConfirmation, ComputeReqCnf()}
            };
        }

        public string FormatAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            if (!_popAuthenticationConfiguration.SignHttpRequest)
            {
                return msalAccessTokenCacheItem.Secret;
            }

            var header = new JObject();
            header[JsonWebTokenConstants.ReservedHeaderParameters.Algorithm] = _popCryptoProvider.CryptographicAlgorithm;
            header[JsonWebTokenConstants.ReservedHeaderParameters.KeyId] = KeyId;
            header[JsonWebTokenConstants.ReservedHeaderParameters.Type] = Constants.PoPTokenType;

            var body = CreateBody(msalAccessTokenCacheItem);

            string popToken = CreateJWS(JsonHelper.JsonObjectToString(body), JsonHelper.JsonObjectToString(header));
            return popToken;
        }

        private JObject CreateBody(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            var publicKeyJwk = JToken.Parse(_popCryptoProvider.CannonicalPublicKeyJwk);
            var body = new JObject
            {
                // Mandatory parameters
                [PoPClaimTypes.Cnf] = new JObject
                {
                    [PoPClaimTypes.JWK] = publicKeyJwk
                },
                [PoPClaimTypes.Ts] = DateTimeHelpers.CurrDateTimeInUnixTimestamp(),
                [PoPClaimTypes.At] = msalAccessTokenCacheItem.Secret,
                [PoPClaimTypes.Nonce] = _popAuthenticationConfiguration.Nonce ?? CreateSimpleNonce(),
            };

            if (_popAuthenticationConfiguration.HttpMethod != null)
            {
                body[PoPClaimTypes.HttpMethod] = _popAuthenticationConfiguration.HttpMethod?.ToString();
            }

            if (!string.IsNullOrEmpty(_popAuthenticationConfiguration.HttpHost))
            {
                body[PoPClaimTypes.Host] = _popAuthenticationConfiguration.HttpHost;
            }

            if (!string.IsNullOrEmpty(_popAuthenticationConfiguration.HttpPath))
            {
                body[PoPClaimTypes.Path] = _popAuthenticationConfiguration.HttpPath;
            }

            return body;
        }

        private static string CreateSimpleNonce()
        {
            // Guid with no hyphens
#if WINDOWS_APP
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
        private static byte[] ComputeThumbprint(string canonicalJwk)
        {
            // Cannot be easily generalized in UAP and NetStandard 1.3
            using (SHA256 hash = SHA256.Create())
            {
                return hash.ComputeHash(Encoding.UTF8.GetBytes(canonicalJwk));
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
            sb.Append('.');
            sb.Append(Base64UrlHelpers.Encode(payload));
            string headerAndPayload = sb.ToString();

            sb.Append('.');
            sb.Append(Base64UrlHelpers.Encode(_popCryptoProvider.Sign(Encoding.UTF8.GetBytes(headerAndPayload))));

            return sb.ToString();
        }
    }
}
