// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
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

namespace Microsoft.Identity.Client.AuthScheme.CDT
{
    internal class CdtAuthenticationScheme : IAuthenticationScheme
    {
        private readonly ICdtCryptoProvider _cdtCryptoProvider;
        private readonly string _constraints;
        private readonly string _reqCnf;

        /// <summary>
        /// Creates Cdt tokens, i.e. tokens that are bound to an HTTP request and are digitally signed.
        /// </summary>
        /// <remarks>
        /// Currently the signing credential algorithm is hard-coded to RSA with SHA256. Extensibility should be done
        /// by integrating Wilson's SigningCredentials
        /// </remarks>
        public CdtAuthenticationScheme(string constraints, IServiceBundle serviceBundle, X509Certificate2 certificate)
        {
            _constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));

            _cdtCryptoProvider = (ICdtCryptoProvider)(certificate == null ? serviceBundle.PlatformProxy.GetDefaultPoPCryptoProvider() : new CdtCryptoProvider(certificate));

            var keyThumbprint = ComputeThumbprint(_cdtCryptoProvider.CannonicalPublicKeyJwk);
            KeyId = Base64UrlHelpers.Encode(keyThumbprint);

            _reqCnf = ComputeReqCnf();
        }

        public TokenType TelemetryTokenType => TokenType.Bearer;

        public string AuthorizationHeaderPrefix => Constants.BearerAuthHeaderPrefix;

        public string AccessTokenType => Constants.BearerAuthHeaderPrefix;

        /// <summary>
        /// For Cdt, we chose to use the base64(jwk_thumbprint)
        /// </summary>
        public string KeyId { get; }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>() {
                { OAuth2Parameter.TokenType, Constants.BearerAuthHeaderPrefix},
                { Constants.RequestConfirmation, _reqCnf}
            };
        }

        public string FormatAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            var header = new JObject();
            header[JsonWebTokenConstants.Type] = Constants.BearerAuthHeaderPrefix;
            header[JsonWebTokenConstants.Algorithm] = Constants.NoAlgorythmPrefix;
            
            var body = CreateCdtBody(msalAccessTokenCacheItem);

            string constraintToken = CreateJWS(JsonHelper.JsonObjectToString(body), JsonHelper.JsonObjectToString(header), false);
            return constraintToken;
        }

        private JObject CreateCdtBody(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            //string encryptionKey = GetEncryptionKeyFromToken(msalAccessTokenCacheItem);
            var body = new JObject
            {
                // Mandatory parameters
                [CdtClaimTypes.Ticket] = $"{msalAccessTokenCacheItem.Secret}[ds_cnf={_reqCnf}]",
                //[CdtClaimTypes.ConstraintsToken] = string.IsNullOrEmpty(encryptionKey) 
                //                                    ? CreateCdtConstraintsJwT(msalAccessTokenCacheItem) :
                //                                      CreateEncryptedCdtConstraintsJwT(msalAccessTokenCacheItem, encryptionKey)
                [CdtClaimTypes.ConstraintsToken] = CreateCdtConstraintsJwT(msalAccessTokenCacheItem)
            };

            return body;
        }

        //private JToken CreateEncryptedCdtConstraintsJwT(MsalAccessTokenCacheItem msalAccessTokenCacheItem, string encryptionKey)
        //{
        //    var header = new JObject();
        //    header[JsonWebTokenConstants.Algorithm] = Constants.CdtEncryptedAlgoryth;
        //    header[JsonWebTokenConstants.CdtEncrypt] = Constants.CdtEncryptedValue;

        //    var body = new JObject
        //    {
        //        // TODO: ENCRYPT JWT
        //        [CdtClaimTypes.Constraints] = CreateCdtConstraintsJwT(msalAccessTokenCacheItem)
        //    };

        //    string cdtConstraintToken = CreateJWS(JsonHelper.JsonObjectToString(body), JsonHelper.JsonObjectToString(header));
        //    return cdtConstraintToken;
        //}

        private JToken CreateCdtConstraintsJwT(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            var header = new JObject();
            header[JsonWebTokenConstants.Algorithm] = _cdtCryptoProvider.CryptographicAlgorithm;
            header[JsonWebTokenConstants.Type] = Constants.JasonWebTokenType;
            header[CdtClaimTypes.Nonce] = msalAccessTokenCacheItem.CdtNonce;

            var body = new JObject
            {
                // Mandatory parameters
                [CdtClaimTypes.Constraints] = _constraints
            };

            string cdtConstraintToken = CreateJWS(JsonHelper.JsonObjectToString(body), JsonHelper.JsonObjectToString(header));
            return cdtConstraintToken;
        }

//        private string GetNonceFromToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
//        {
//            var decodedToken = Base64UrlHelpers.Decode(msalAccessTokenCacheItem.Secret);
//            var jsonHeader = JsonHelper.ParseIntoJsonObject(decodedToken.Split('.')[0]);
//            JToken value;
//#if SUPPORTS_SYSTEM_TEXT_JSON

//            JsonHelper.TryGetValue(jsonHeader, "nonce", out value);
//#else
//            JsonHelper.TryGetValue(jsonHeader, "nonce", out value);
//#endif
//            return value?.ToString();
//        }

//        private string GetEncryptionKeyFromToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
//        {
//            var decodedToken = Base64UrlHelpers.Decode(msalAccessTokenCacheItem.Secret);
//            var jsonHeader = JsonHelper.ParseIntoJsonObject(decodedToken.Split('.')[0]);
//            JToken value;
//#if SUPPORTS_SYSTEM_TEXT_JSON

//            JsonHelper.TryGetValue(jsonHeader, "ds_enc", out value);
//#else
//            JsonHelper.TryGetValue(jsonHeader, "ds_enc", out value);
//#endif
//            return value?.ToString();
//        }

        private static string CreateSimpleNonce()
        {
            // Guid with no hyphens
            return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        }

        private string ComputeReqCnf()
        {
            // There are 4 possible formats for a JWK, but Evo supports only this one for simplicity
            var jwk = $@"{{""{JsonWebKeyParameterNames.Kid}"":""{KeyId}""}}";
            return Base64UrlHelpers.Encode(jwk);
        }

        /// <summary>
        /// A key ID that uniquely describes a public / private key pair. While KeyID is not normally
        /// strict, AAD support for Cdt requires that we use the base64 encoded JWK thumbprint, as described by 
        /// https://tools.ietf.org/html/rfc7638
        /// </summary>
        private static byte[] ComputeThumbprint(string canonicalJwk)
        {
            using (SHA256 hash = SHA256.Create())
            {
                return hash.ComputeHash(Encoding.UTF8.GetBytes(canonicalJwk));
            }
        }

        /// <summary>
        /// Creates a JWS (json web signature) as per: https://tools.ietf.org/html/rfc7515
        /// Format: header.payload.signed_payload
        /// </summary>
        private string CreateJWS(string payload, string header, bool signPayload = true)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Base64UrlHelpers.Encode(Encoding.UTF8.GetBytes(header)));
            sb.Append('.');
            sb.Append(Base64UrlHelpers.Encode(payload));
            string headerAndPayload = sb.ToString();

            if (signPayload)
            {
                sb.Append('.');
                sb.Append(Base64UrlHelpers.Encode(_cdtCryptoProvider.Sign(Encoding.UTF8.GetBytes(headerAndPayload))));
            }

            return sb.ToString();
        }
    }
}
