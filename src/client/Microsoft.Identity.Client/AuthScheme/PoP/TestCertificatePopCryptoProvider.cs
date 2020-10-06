using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{

    /// <summary>
    /// The default implementation will store a key in memory    
    /// </summary>
    internal class CertificatePopCryptoProvider : IPoPCryptoProvider
    {
        internal /* internal for test only */ const int RsaKeySize = 2048;
        private X509Certificate2 _cert;

#if NET45
        private RSACryptoServiceProvider _signingKey;
#else
        private RSA _signingKey;
#endif

        public CertificatePopCryptoProvider(X509Certificate2 cert)
        {
            _cert = cert;
            InitializeSigningKey();
        }

        public string CannonicalPublicKeyJwk { get; private set; }

        private void InitializeSigningKey()
        {
#if NET45
            _signingKey = _cert.PrivateKey as RSACryptoServiceProvider;
#else
            _signingKey = _cert.PrivateKey as RSA;
            _signingKey.KeySize = RsaKeySize;
#endif
            RSAParameters publicKeyInfo = _signingKey.ExportParameters(false);

            CannonicalPublicKeyJwk = ComputeCannonicalJwk(publicKeyInfo);
        }

        public byte[] Sign(byte[] payload)
        {
            return Sign(_signingKey, payload);
        }

        /// <summary>
        /// Creates the cannonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        private static string ComputeCannonicalJwk(RSAParameters rsaPublicKey)
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
