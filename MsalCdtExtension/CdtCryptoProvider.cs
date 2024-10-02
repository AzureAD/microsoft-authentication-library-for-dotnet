// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Client
{

    //TODO: Add support for ECD keys
    public class CdtCryptoProvider
    {
        //private readonly X509Certificate2 _cert;
        private RSA _signingKey;
        internal const int RsaKeySize = 2048;

        public CdtCryptoProvider()
        {
#if NETFRAMEWORK
            // This method was obsolete in .NET,
            // but Create() on .NET FWK defaults to PKCS1 padding.
            _signingKey = RSA.Create("RSAPSS");
#else
            _signingKey = RSA.Create();
#endif

            _signingKey.KeySize = RsaKeySize;
            RSAParameters publicKeyInfo = _signingKey.ExportParameters(false);

            CannonicalPublicKeyJwk = ComputeCanonicalJwk(publicKeyInfo);
        }

        public RSA GetKey()
        {
            return _signingKey;
        }

        public string CannonicalPublicKeyJwk { get; }

        public string CryptographicAlgorithm { get => "PS256"; }

        /// <summary>
        /// Creates the canonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        private static string ComputeCanonicalJwk(RSAParameters rsaPublicKey)
        {
            return $@"{{""e"":""{Base64UrlEncoder.Encode(rsaPublicKey.Exponent)}"",""kty"":""RSA"",""n"":""{Base64UrlEncoder.Encode(rsaPublicKey.Modulus)}""}}";
        }
    }
}
