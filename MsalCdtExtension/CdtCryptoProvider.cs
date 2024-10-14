// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Client
{

    //TODO: Add support for ECD keys
    public class CdtCryptoProvider
    {
        //private readonly RSA key;
        private readonly ECDsa _signingKey;
        internal const int RsaKeySize = 2048;

        public CdtCryptoProvider()
        {
            // This method was obsolete in .NET,
            // but Create() on .NET FWK defaults to PKCS1 padding.
            _signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            ECParameters publicKeyInfo = _signingKey.ExportParameters(false);

            CannonicalPublicKeyJwk = ComputeCanonicalJwk(publicKeyInfo);
            SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(_signingKey), CryptographicAlgorithm);

            KeyId = Base64UrlEncoder.Encode(ComputeKeyId(CannonicalPublicKeyJwk));

            CannonicalPublicKeyJwk = Base64UrlEncoder.Encode(CannonicalPublicKeyJwk);
        }

        public string KeyId { get; }

        public string CannonicalPublicKeyJwk { get; }

        public string CryptographicAlgorithm { get => "ES256"; }

        public SigningCredentials SigningCredentials { get; }

        /// <summary>
        /// Creates the canonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        private static string ComputeCanonicalJwk(ECParameters ecdPublicKey)
        {
            string x = ecdPublicKey.Q.X != null ? Base64UrlEncoder.Encode(ecdPublicKey.Q.X) : null;
            string y = ecdPublicKey.Q.Y != null ? Base64UrlEncoder.Encode(ecdPublicKey.Q.Y) : null;
            return $@"{{""{JsonWebKeyParameterNames.Crv}"":""{GetCrvParameterValue(ecdPublicKey.Curve)}"",""{JsonWebKeyParameterNames.Kty}"":""EC"",""{JsonWebKeyParameterNames.X}"":""{x}"",""{JsonWebKeyParameterNames.Y}"":""{y}""}}";
        }

        private static string GetCrvParameterValue(ECCurve curve)
        {
            if (string.Equals(curve.Oid.Value, ECCurve.NamedCurves.nistP256.Oid.Value, StringComparison.Ordinal) || string.Equals(curve.Oid.FriendlyName, ECCurve.NamedCurves.nistP256.Oid.FriendlyName, StringComparison.Ordinal))
                return JsonWebKeyECTypes.P256;
            else if (string.Equals(curve.Oid.Value, ECCurve.NamedCurves.nistP384.Oid.Value, StringComparison.Ordinal) || string.Equals(curve.Oid.FriendlyName, ECCurve.NamedCurves.nistP384.Oid.FriendlyName, StringComparison.Ordinal))
                return JsonWebKeyECTypes.P384;
            else if (string.Equals(curve.Oid.Value, ECCurve.NamedCurves.nistP521.Oid.Value, StringComparison.Ordinal) || string.Equals(curve.Oid.FriendlyName, ECCurve.NamedCurves.nistP521.Oid.FriendlyName, StringComparison.Ordinal))
                return JsonWebKeyECTypes.P521;
            else
                throw new ArgumentException();
        }

        /// <summary>
        /// A key ID that uniquely describes a public / private key pair. While KeyID is not normally
        /// strict, AAD support for PoP requires that we use the base64 encoded JWK thumbprint, as described by 
        /// https://tools.ietf.org/html/rfc7638
        /// </summary>
        private static byte[] ComputeKeyId(string canonicalJwk)
        {
            using (SHA256 hash = SHA256.Create())
            {
                return hash.ComputeHash(Encoding.UTF8.GetBytes(canonicalJwk));
            }
        }
    }
}
