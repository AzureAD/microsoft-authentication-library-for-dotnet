// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsalCdtExtension
{
    internal class Constants
    {
        public const string BearerAuthHeaderPrefix = "Bearer";

        //OAuth2.0 related constants
        public const string TokenType = "token_type";

        //JsonWebToken related constants
        /// <summary>
        /// Encryption algorithm used, e.g. ES256
        /// https://tools.ietf.org/html/rfc7515#section-4.1.1
        /// </summary>
        public const string Algorithm = "alg";

        /// <summary>
        /// The type of token e.g. JWT
        /// https://tools.ietf.org/html/rfc7519#section-5.1
        /// </summary>
        public const string Type = "typ";

        /// <summary>
        /// Key ID, can be an X509 cert thumbprint. When used with a JWK, the "kid" value is used to match a JWK "kid"
        /// parameter value
        /// https://tools.ietf.org/html/rfc7515#section-4.1.4
        /// </summary>
        public const string KeyId = "kid";

        public const string X509CertificateThumbprint = "x5t";

        public const string X509CertificatePublicCertValue = "x5c";

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
        public const string Nonce = "xms_ds_nonce ";

        #endregion
    }
}
