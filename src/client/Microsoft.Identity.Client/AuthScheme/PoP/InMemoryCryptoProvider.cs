// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    /// <summary>
    /// The default implementation will store a key in memory    
    /// </summary>
    internal class InMemoryCryptoProvider : IPoPCryptoProvider
    {
        internal /* internal for test only */ const int RsaKeySize = 2048;
        private RSA _signingKey;

        public InMemoryCryptoProvider()
        {
            InitializeSigningKey();
        }

        public string CannonicalPublicKeyJwk { get; private set; }

        public string CryptographicAlgorithm { get => "PS256"; }

        private void InitializeSigningKey()
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

        public byte[] Sign(byte[] payload)
        {
            return _signingKey.SignData(
                payload,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);
        }

        /// <summary>
        /// Creates the canonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3.
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint.
        /// </summary>
        private static string ComputeCanonicalJwk(RSAParameters rsaPublicKey)
        {
            //Important: This format cannot be modified as it needs to be the same as what is used in the service when calculating hashes.
            return $@"{{""{JsonWebKeyParameterNames.E}"":""{Base64UrlHelpers.Encode(rsaPublicKey.Exponent)}"",""{JsonWebKeyParameterNames.Kty}"":""{JsonWebAlgorithmsKeyTypes.RSA}"",""{JsonWebKeyParameterNames.N}"":""{Base64UrlHelpers.Encode(rsaPublicKey.Modulus)}""}}";
        }

        /// <summary>
        /// For internal testing only
        /// </summary>
        internal RSAParameters GetPublicKeyParameters()
        {
            return _signingKey.ExportParameters(false);
        }
    }
}
