// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms
    internal class JsonWebTokenConstants
    {
        public const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes
        public const string HeaderType = "JWT";

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

        internal class ReservedHeaderParameters
        {
            public const string Algorithm = "alg";
            public const string Type = "typ";
            public const string X509CertificateThumbprint = "kid";
            public const string X509CertificatePublicCertValue = "x5c";
        }
    }
#endif
}
