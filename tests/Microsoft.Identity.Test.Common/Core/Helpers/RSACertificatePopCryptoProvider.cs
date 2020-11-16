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
        private readonly X509Certificate2 _cert;

        public byte[] Sign(byte[] payload)
        {
            using (RSA key = _cert.GetRSAPrivateKey())
            {
                return key.SignData(
                    payload,
                    HashAlgorithmName.SHA256, 
                    RSASignaturePadding.Pkcs1);
            }
        }

        public RSACertificatePopCryptoProvider(X509Certificate2 cert)
        {
            _cert = cert ?? throw new ArgumentNullException(nameof(cert));

            RSACryptoServiceProvider provider = _cert.GetRSAPublicKey() as RSACryptoServiceProvider;
            RSAParameters publicKeyInfo = provider.ExportParameters(false);
            ComputeCannonicalJwk(publicKeyInfo);
        }


        public string CannonicalPublicKeyJwk { get; }

        public string CryptographicAlgorithm { get => "RS256"; }


        /// <summary>
        /// Creates the cannonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        private static string ComputeCannonicalJwk(RSAParameters rsaPublicKey)
        {
            return $@"{{""{JsonWebKeyParameterNames.E}"":""{Base64UrlHelpers.Encode(rsaPublicKey.Exponent)}"",""{JsonWebKeyParameterNames.Kty}"":""{JsonWebAlgorithmsKeyTypes.RSA}"",""{JsonWebKeyParameterNames.N}"":""{Base64UrlHelpers.Encode(rsaPublicKey.Modulus)}""}}";
        }


    }
}
