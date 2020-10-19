using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Integration.net45.Infrastructure
{
    public class RSACertificatePopCryptoProvider : IPoPCryptoProvider
    {

        public byte[] Sign(byte[] payload)
        {
            return Sign(_signingKey, payload);
        }

        public RSACertificatePopCryptoProvider(X509Certificate2 cert)
        {
            _cert = cert;
            InitializeSigningKey();
        }

        private X509Certificate2 _cert;
        private RSACryptoServiceProvider _signingKey;

        public string CannonicalPublicKeyJwk { get; private set; }

        public string CryptographicAlgorithm { get => "RS256"; }

        private void InitializeSigningKey()
        {
            _signingKey = _cert.PrivateKey as RSACryptoServiceProvider;

            RSAParameters publicKeyInfo = _signingKey.ExportParameters(false);

            CannonicalPublicKeyJwk = ComputeCannonicalJwk(publicKeyInfo);
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
            return ((RSACryptoServiceProvider)RsaKey).SignData(payload, CryptoConfig.MapNameToOID("SHA256"));
        }

    }
}
