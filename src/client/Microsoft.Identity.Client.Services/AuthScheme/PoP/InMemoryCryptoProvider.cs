// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{

    /// <summary>
    /// The default implementation will store a key in memory    
    /// </summary>
    internal class InMemoryCryptoProvider : IPoPCryptoProvider
    {
        internal /* internal for test only */ const int RsaKeySize = 2048;

#if NET45
        private RSACryptoServiceProvider _signingKey;
#else
        private RSA _signingKey;
#endif

        public InMemoryCryptoProvider()
        {
            InitializeSigningKey();
        }

        public string CannonicalPublicKeyJwk { get; private set; }

        public string CryptographicAlgorithm { get => "RS256"; }

        private void InitializeSigningKey()
        {
#if NET45
            _signingKey = new RSACryptoServiceProvider(RsaKeySize);
#else
            _signingKey = RSA.Create();
            _signingKey.KeySize = RsaKeySize;
#endif
            RSAParameters publicKeyInfo = _signingKey.ExportParameters(false);

            CannonicalPublicKeyJwk = ComputeCanonicalJwk(publicKeyInfo);
        }

        public byte[] Sign(byte[] payload)
        {
            return Sign(_signingKey, payload);
        }

        /// <summary>
        /// Creates the canonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3.
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint.
        /// </summary>
        private static string ComputeCanonicalJwk(RSAParameters rsaPublicKey)
        {
            return $@"{{""{JsonWebKeyParameterNames.E}"":""{Base64UrlHelpers.Encode(rsaPublicKey.Exponent)}"",""{JsonWebKeyParameterNames.Kty}"":""{JsonWebAlgorithmsKeyTypes.RSA}"",""{JsonWebKeyParameterNames.N}"":""{Base64UrlHelpers.Encode(rsaPublicKey.Modulus)}""}}";
        }

        public static byte[] Sign(RSA RsaKey, byte[] payload)
        {
#if NET45
            return ((RSACryptoServiceProvider)RsaKey).SignData(payload, CryptoConfig.MapNameToOID("SHA256"));
#else
            return RsaKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#endif
        }
    }
}
