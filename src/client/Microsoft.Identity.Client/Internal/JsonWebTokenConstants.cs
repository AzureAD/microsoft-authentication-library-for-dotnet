// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal
{
    internal class JsonWebTokenConstants
    {
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
    }
}
