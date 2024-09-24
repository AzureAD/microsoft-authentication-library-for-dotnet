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
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.AuthScheme;
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

namespace Microsoft.Identity.Client
{
    //Temporary location
    public class CdtAuthenticationScheme : IAuthenticationScheme
    {
        //CDT
        public const string CdtKey = "xms_ds_cnf";
        public const string CdtNonce = "xms_ds_nonce";
        public const string CdtEncKey = "xms_ds_enc";
        public const string NoAlgorythmPrefix = "none";
        public const string JasonWebTokenType = "JWT";
        public const string CdtEncryptedAlgoryth = "dir";
        public const string CdtEncryptedValue = "A256CBC-HS256";
        public const string CdtRequestConfirmation = "req_ds_cnf";

        private readonly CdtCryptoProvider _cdtCryptoProvider;
        private readonly string _constraints;
        private readonly string _dsReqCnf;

        /// <summary>
        /// Creates Cdt tokens, i.e. tokens that are bound to an HTTP request and are digitally signed.
        /// </summary>
        /// <remarks>
        /// Currently the signing credential algorithm is hard-coded to RSA with SHA256. Extensibility should be done
        /// by integrating Wilson's SigningCredentials
        /// </remarks>
        public CdtAuthenticationScheme(string constraints, X509Certificate2 certificate)
        {
            _constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));

            _cdtCryptoProvider = new CdtCryptoProvider(certificate);

            _dsReqCnf = _cdtCryptoProvider.CannonicalPublicKeyJwk;
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
            var temp = "eyJrdHkiOiAiUlNBIiwgIm4iOiAiMUNMeDNXRW1NWlQ3el92Szc2ZHBaVVNwX2kyMEEza0Y0OWVpemtCQTBFSTJ4el9pcldTcm9BamJrRTk4dlp3SFM0QVlQV2I5WEd2YTFPYVNMX0RqQTFPTG1nSll4Uk45cU5jd1lKeGhsN3hqaGJlU25RMUMtR1NNS3ZWRzJnaDdQUlhaaU1xVXFuOWt3UzBXa1RoNDhSREMxR0xhTFFfNzZmb0dZMmo0MlNvel9XYnNRemtnVGo0TDVaVTZTWjJ3QTFwMlZ6WFliOVd1M3A4U2VuV3JCTDUzOWhUZjVGelp0b1E0R2IxNzMzVzFmWVFsUkotYUZVMTFfdEc1Umx2Ui1nSWFweHJMWkFKM1NHM28wQ2ZPa2FaejdKT2RETnJHNnE4akF3ZmJOdFJ1eDYzbnJZZ0FHc3VhemlXalZxRnZiclNMX2Mya3dZaDlZUl9uYVJFOG1RPT0iLCAiZSI6ICJBUUFCIn0%3D";
            return new Dictionary<string, string>() {
                { OAuth2Parameter.TokenType, Constants.BearerAuthHeaderPrefix},
                { CdtRequestConfirmation, Base64UrlHelpers.Encode(_dsReqCnf)}
            };
        }

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            var header = new JObject();
            header[JsonWebTokenConstants.Type] = JasonWebTokenType;
            header[JsonWebTokenConstants.Algorithm] = NoAlgorythmPrefix;

            //TODO: determine what happens if nonce is not present
            authenticationResult.AdditionalResponseParameters.TryGetValue(CdtNonce, out string nonce);
            var body = CreateCdtBody(authenticationResult.AccessToken, nonce);

            string constraintToken = CreateJWS(JsonHelper.JsonObjectToString(body), JsonHelper.JsonObjectToString(header), false);
            authenticationResult.AccessToken = constraintToken;
        }

        //public string FormatAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        //{
        //    var header = new JObject();
        //    header[JsonWebTokenConstants.Type] = Constants.JasonWebTokenType;
        //    header[JsonWebTokenConstants.Algorithm] = Constants.NoAlgorythmPrefix;

        //    var body = CreateCdtBody(msalAccessTokenCacheItem);

        //    string constraintToken = CreateJWS(JsonHelper.JsonObjectToString(body), JsonHelper.JsonObjectToString(header), false);
        //    return constraintToken;
        //}

        private JObject CreateCdtBody(string secret, string nonce)
        {
            //string encryptionKey = GetEncryptionKeyFromToken(msalAccessTokenCacheItem);
            var body = new JObject
            {
                // Mandatory parameters
                [CdtClaimTypes.Ticket] = secret,
                [CdtClaimTypes.ConstraintsToken] = CreateCdtConstraintsJwT(nonce)
                //[CdtClaimTypes.ConstraintsToken] = string.IsNullOrEmpty(encryptionKey) 
                //                                    ? CreateCdtConstraintsJwT(msalAccessTokenCacheItem) :
                //                                      CreateEncryptedCdtConstraintsJwT(msalAccessTokenCacheItem, encryptionKey)
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

        private JToken CreateCdtConstraintsJwT(string nonce)
        {
            var header = new JObject();
            header[JsonWebTokenConstants.Algorithm] = _cdtCryptoProvider.CryptographicAlgorithm;
            header[JsonWebTokenConstants.Type] = JasonWebTokenType;
            header[CdtClaimTypes.Nonce] = nonce;

            var body = new JObject
            {
                // Mandatory parameters
                [CdtClaimTypes.Constraints] = _constraints
            };

            string cdtConstraintToken = CreateJWS(JsonHelper.JsonObjectToString(body), JsonHelper.JsonObjectToString(header));
            return cdtConstraintToken;
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

    public static class CdtClaimTypes
    {
        #region JSON keys for Http request

        /// <summary>
        /// Access token with response cnf
        /// 
        /// </summary>
        public const string Ticket = "t";

        /// <summary>
        /// Constraints specified by the client
        /// 
        /// </summary>
        public const string ConstraintsToken = "c";

        /// <summary>
        /// Constraints specified by the client
        /// 
        /// </summary>
        public const string Constraints = "constraints";

        /// <summary>
        /// Non-standard claim representing a nonce that protects against replay attacks.
        /// </summary>
        public const string Nonce = "nonce";

        /// <summary>
        /// 
        /// </summary>
        public const string Type = "typ";

        #endregion
    }

    //TODO: Add support for ECD keys
    public class CdtCryptoProvider
    {
        private readonly X509Certificate2 _cert;

        public CdtCryptoProvider(X509Certificate2 cert)
        {
            _cert = cert ?? throw new ArgumentNullException(nameof(cert));

            RSA provider = _cert.GetRSAPublicKey();
            RSAParameters publicKeyParams = provider.ExportParameters(false);
            CannonicalPublicKeyJwk = ComputeCanonicalJwk(publicKeyParams);
        }

        public byte[] Sign(byte[] payload)
        {
            using (RSA key = _cert.GetRSAPrivateKey())
            {
                return key.SignData(
                    payload,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss);
            }
        }

        public string CannonicalPublicKeyJwk { get; }

        public string CryptographicAlgorithm { get => "PS256"; }

        /// <summary>
        /// Creates the canonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        private static string ComputeCanonicalJwk(RSAParameters rsaPublicKey)
        {
            return $@"{{""e"":""{Base64UrlHelpers.Encode(rsaPublicKey.Exponent)}"",""kty"":""RSA"",""n"":""{Base64UrlHelpers.Encode(rsaPublicKey.Modulus)}""}}";
        }
    }

    /// <summary>
    /// Delagated Constraint
    /// </summary>
    public class ConstraintDict
    {
        public Dictionary<string, string> Constraints { get; set; } = new Dictionary<string, string>();
    }

    public class Constraint
    {
        public string Version { get; set; }
        public string Type { get; set; }
        public string Action { get; set; }
        public List<ConstraintTarget> Targets { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; }
    }

    public class ConstraintTarget
    {
        public string Value { get; set; }
        public string Policy { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; }
        public ConstraintTarget(string value, string policy)
        {
            Value = value;
            Policy = policy;
        }
    }

}
