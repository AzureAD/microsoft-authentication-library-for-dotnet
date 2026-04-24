// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// RSA certificate-based implementation of <see cref="IPoPCryptoProvider"/> for signing PoP payloads and exposing the canonical public JWK.
    /// </summary>
    public class RSACertificatePopCryptoProvider : IPoPCryptoProvider
    {
        private readonly X509Certificate2 _cert;

        /// <summary>
        /// Creates a new instance of the RSACertificatePopCryptoProvider with the given certificate. The certificate must contain an RSA public key and a corresponding private key for signing. The constructor extracts the RSA public key parameters from the certificate, computes the canonical JWK representation, and initializes the properties accordingly. If the certificate is null, an ArgumentNullException is thrown.
        /// </summary>
        /// <param name="cert">The X509Certificate2 containing the RSA keys.</param>
        /// <exception cref="ArgumentNullException">Thrown if the certificate is null.</exception>
        public RSACertificatePopCryptoProvider(X509Certificate2 cert)
        {
            _cert = cert ?? throw new ArgumentNullException(nameof(cert));

            RSA provider = _cert.GetRSAPublicKey();
            RSAParameters publicKeyParams = provider.ExportParameters(false);
            CannonicalPublicKeyJwk = ComputeCanonicalJwk(publicKeyParams);
        }

        /// <summary>
        /// Signs the provided payload using the RSA private key from the certificate. The method retrieves the RSA private key from the certificate, uses it to sign the input payload with SHA-256 hashing and PSS padding, and returns the resulting signature as a byte array. If the certificate does not contain a valid RSA private key, an exception will be thrown when attempting to retrieve it.
        /// </summary>
        /// <param name="payload">The data to be signed.</param>
        /// <returns>The signature as a byte array.</returns>
        public byte[] Sign(byte[] payload)
        {
            using (RSA key = _cert.GetRSAPrivateKey())
            {
                return key.SignData(
                    payload,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss);
            }
        }

        /// <summary>
        /// Cannonical JWK representation of the RSA public key from the certificate, used for generating the PoP token's cnf claim. The value is computed in the constructor by extracting the RSA public key parameters from the certificate and formatting them into a canonical JWK string as defined by RFC 7638. This string includes the "e" (exponent), "kty" (key type), and "n" (modulus) parameters, ordered lexicographically, which is important for consistent thumbprint generation.
        /// </summary>
        public string CannonicalPublicKeyJwk { get; }

        /// <summary>
        /// Cryptographic algorithm identifier for RSA signatures, used in the PoP token's cnf claim. This property returns the string "RS256", indicating that the RSA signature is generated using SHA-256 hashing. This value is standardized in JOSE (JSON Object Signing and Encryption) specifications for representing RSA signatures with SHA-256.
        /// </summary>
        public string CryptographicAlgorithm { get => "RS256"; }

        /// <summary>
        /// Creates the canonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        private static string ComputeCanonicalJwk(RSAParameters rsaPublicKey)
        {
            return $@"{{""e"":""{Base64UrlHelpers.Encode(rsaPublicKey.Exponent)}"",""kty"":""RSA"",""n"":""{Base64UrlHelpers.Encode(rsaPublicKey.Modulus)}""}}";
        }
    }
}
