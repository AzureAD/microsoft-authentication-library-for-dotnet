// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.IdentityModel.Tokens;
using JsonWebKeyParameterNames = Microsoft.IdentityModel.Tokens.JsonWebKeyParameterNames;

namespace Microsoft.Identity.Test.Integration.net47
{
    public class ECDCertificatePopCryptoProvider : IPoPCryptoProvider
    {
        public byte[] Sign(byte[] payload)
        {
            return Sign(_signingKey, payload);
        }

        public ECDCertificatePopCryptoProvider()
        {
            InitializeSigningKey();
        }

        private ECDsa _signingKey;

        public string CannonicalPublicKeyJwk { get; private set; }

        private void InitializeSigningKey()
        {
            ECCurve eCCurve = ECCurve.CreateFromFriendlyName(ECCurve.NamedCurves.nistP256.Oid.FriendlyName);
            _signingKey = ECDsa.Create(eCCurve);

            ECParameters publicKeyInfo = _signingKey.ExportParameters(false);

            CannonicalPublicKeyJwk = ComputeCannonicalJwk(publicKeyInfo);
        }

        /// <summary>
        /// Creates the cannonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        private static string ComputeCannonicalJwk(ECParameters ecdPublicKey)
        {
            string x = ecdPublicKey.Q.X != null ? Base64UrlEncoder.Encode(ecdPublicKey.Q.X) : null;
            string y = ecdPublicKey.Q.Y != null ? Base64UrlEncoder.Encode(ecdPublicKey.Q.Y) : null;
            return $@"{{""{JsonWebKeyParameterNames.Crv}"":""{GetCrvParameterValue(ecdPublicKey.Curve)}"",""{JsonWebKeyParameterNames.Kty}"":""{"EC"}"",""{JsonWebKeyParameterNames.X}"":""{x}"",""{JsonWebKeyParameterNames.Y}"":""{y}""}}";
        }

        public static byte[] Sign(ECDsa EcdKey, byte[] payload)
        {
            return EcdKey.SignData(payload, HashAlgorithmName.SHA256);
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
        /// Constants for JsonWebKey Elliptical Curve Types
        /// https://tools.ietf.org/html/rfc7518#section-6.2.1.1
        /// </summary>
        private static class JsonWebKeyECTypes
        {
#pragma warning disable 1591
            public const string P256 = "P-256";
            public const string P384 = "P-384";
            public const string P512 = "P-512";
            public const string P521 = "P-521";
#pragma warning restore 1591
        }
    }
}
