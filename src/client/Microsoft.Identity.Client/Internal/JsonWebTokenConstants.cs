// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal
{
    internal class JsonWebTokenConstants
    {
        public const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes
        public const string JWTHeaderType = "JWT";

        internal class Algorithms
        {
            public const string RsaSha256 = "RS256";
            public const string None = "none";
        }

        internal class ReservedClaims
        {
            public const string Audience = "aud";
            public const string Issuer = "iss";
            public const string Subject = "sub";
            public const string NotBefore = "nbf";
            public const string ExpiresOn = "exp";
            public const string JwtIdentifier = "jti";
        }

        internal static class ReservedHeaderParameters
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
}
