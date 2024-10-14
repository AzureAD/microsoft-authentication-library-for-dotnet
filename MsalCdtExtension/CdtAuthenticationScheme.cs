// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Text.Json;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Client
{
    public sealed class CdtAuthenticationOperation : IAuthenticationOperation
    {
        //CDT
        public const string CdtKey = "xms_ds_cnf";
        public const string CdtNonce = "xms_ds_nonce";
        public const string CdtEncKey = "xms_ds_enc";
        public const string NoAlgorythmPrefix = "none";
        public const string JsonWebTokenType = "JWT";
        public const string CdtTokenType = "CDT";
        public const string CdtEncryptedAlgoryth = "dir";
        public const string CdtEncryptedValue = "A256CBC-HS256";
        public const string CdtRequestConfirmation = "req_ds_cnf";
        public const string CdtConstraints = "constraints";

        private static CdtCryptoProvider? s_cdtCryptoProvider;
        private readonly string _constraints;

        /// <summary>
        /// Creates Cdt tokens, i.e. tokens that are bound to an HTTP request and are digitally signed.
        /// </summary>
        public CdtAuthenticationOperation(string constraints)
        {
            _constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));

            s_cdtCryptoProvider ??= new CdtCryptoProvider();
        }

        public int TelemetryTokenType => 5; //represents CDT token type in MSAL telemetry

        public string AuthorizationHeaderPrefix => "Bearer";

        public string AccessTokenType => "Bearer";

        /// <summary>
        /// For Cdt, we chose to use the base64(jwk_thumbprint)
        /// </summary>
        public string? KeyId => s_cdtCryptoProvider?.KeyId;

        int IAuthenticationOperation.TelemetryTokenType => 4;

        /// <summary>
        /// Represents additional parameters to be sent to Ests for the Cdt token request.
        /// </summary>
        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>() {
                { "token_type", AccessTokenType},
                { CdtRequestConfirmation, s_cdtCryptoProvider?.CannonicalPublicKeyJwk!}
            };
        }

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            //TODO: determine what happens if nonce is not present
            authenticationResult.AdditionalResponseParameters.TryGetValue(CdtNonce, out string? nonce);

            string constraintToken = CreateCdtConstraintsJwT(nonce!);
            authenticationResult.AccessToken = CreateCdtJwT(authenticationResult.AccessToken, constraintToken);
        }

        private string CreateCdtConstraintsJwT(string nonce)
        {
            // Signed token like the CDT constraints.
            JsonWebTokenHandler jsonWebTokenHandler = new JsonWebTokenHandler();

            SecurityTokenDescriptor securityTokenDescriptor = new SecurityTokenDescriptor()
            {
                Claims = new Dictionary<string, object>()
                {
                  {CdtConstraints, _constraints },
                  {CdtNonce, nonce }
               },
                AdditionalHeaderClaims = new Dictionary<string, object>()
                {
                },
                TokenType = JsonWebTokenType,
                SigningCredentials = s_cdtCryptoProvider!.SigningCredentials
            };
            jsonWebTokenHandler.SetDefaultTimesOnTokenCreation = false;

            return jsonWebTokenHandler.CreateToken(securityTokenDescriptor);
        }

        private string CreateCdtJwT(string accessToken, string constraintsToken)
        {
            var header = new
            {
                typ = "CDT+JWT",
                alg = "none",
            };

            var body = new
            {
                t = accessToken,
                c = constraintsToken
            };

            string headerJson = JsonSerializer.Serialize(header);
            string bodyJson = JsonSerializer.Serialize(body);
            JsonWebToken cdtToken = new JsonWebToken(headerJson, bodyJson);
            return cdtToken.EncodedToken;
        }

        //For testing purposes
        internal static void ClearKey()
        {
            s_cdtCryptoProvider = null;
        }
    }
}
