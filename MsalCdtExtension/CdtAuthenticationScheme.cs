// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JObject = System.Text.Json.Nodes.JsonObject;
using JToken = System.Text.Json.Nodes.JsonNode;

namespace MsalCdtExtension
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
        public const string CdtTokenType = "CDT";
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

        public int TelemetryTokenType => 5;

        public string AuthorizationHeaderPrefix => Constants.BearerAuthHeaderPrefix;

        public string AccessTokenType => Constants.BearerAuthHeaderPrefix;

        /// <summary>
        /// For Cdt, we chose to use the base64(jwk_thumbprint)
        /// </summary>
        public string KeyId { get; }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>() {
                { Constants.TokenType, Constants.BearerAuthHeaderPrefix},
                { CdtRequestConfirmation, Base64UrlEncoder.Encode(_dsReqCnf)}
            };
        }

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            var header = new JObject();
            header[Constants.Algorithm] = NoAlgorythmPrefix;
            header[Constants.Type] = CdtTokenType;

            //TODO: determine what happens if nonce is not present
            authenticationResult.AdditionalResponseParameters.TryGetValue(CdtNonce, out string nonce);
            var body = CreateCdtBody(authenticationResult.AccessToken, nonce);

            string constraintToken = CreateJWS(body.ToJsonString(), header.ToJsonString(), false);
            authenticationResult.AccessToken = constraintToken;
        }

        private JObject CreateCdtBody(string secret, string nonce)
        {
            //string encryptionKey = GetEncryptionKeyFromToken(msalAccessTokenCacheItem);
            var body = new JObject
            {
                // Mandatory parameters
                [Constants.Ticket] = secret,
                [Constants.ConstraintsToken] = CreateCdtConstraintsJwT(nonce)
                //[CdtClaimTypes.ConstraintsToken] = string.IsNullOrEmpty(encryptionKey) 
                //                                    ? CreateCdtConstraintsJwT(msalAccessTokenCacheItem) :
                //                                      CreateEncryptedCdtConstraintsJwT(msalAccessTokenCacheItem, encryptionKey)
            };

            return body;
        }

        private JToken CreateCdtConstraintsJwT(string nonce)
        {
            var header = new
            {
                Alg = _cdtCryptoProvider.CryptographicAlgorithm,
                Type = JasonWebTokenType
            };

            var body = new
            {
                Nonce = nonce,
                Constraints = _constraints
            };

            string headerJson = JsonSerializer.Serialize(header);
            string bodyJson = JsonSerializer.Serialize(body);
            JsonWebToken cdtToken = new JsonWebToken(headerJson, bodyJson);

            return cdtToken.EncodedToken;
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
            sb.Append(Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(header)));
            sb.Append('.');
            sb.Append(Base64UrlEncoder.Encode(payload));
            string headerAndPayload = sb.ToString();

            if (signPayload)
            {
                sb.Append('.');
                sb.Append(Base64UrlEncoder.Encode(_cdtCryptoProvider.Sign(Encoding.UTF8.GetBytes(headerAndPayload))));
            }

            return sb.ToString();
        }
    }
}
