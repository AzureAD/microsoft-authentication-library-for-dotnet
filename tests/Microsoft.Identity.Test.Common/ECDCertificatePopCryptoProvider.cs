// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    /// <summary>
    /// An <see cref="IPoPCryptoProvider"/> implementation backed by an ephemeral ECDSA key.
    /// Used by integration tests to sign PoP payloads and to expose the corresponding canonical public JWK.
    /// </summary>
    public class ECDCertificatePopCryptoProvider : IPoPCryptoProvider
    {
        private ECDsa _signingKey;

        /// <summary>
        /// Initializes a new instance of <see cref="ECDCertificatePopCryptoProvider"/>.
        /// </summary>
        /// <remarks>
        /// The constructor generates a new in-memory ECDSA key (P-256) and computes the canonical JWK representation
        /// of the public key for use in PoP cnf claims.
        /// </remarks>
        public ECDCertificatePopCryptoProvider()
        {
            InitializeSigningKey();
        }

        /// <summary>
        /// Gets the canonical public JWK for the generated ECDSA key.
        /// </summary>
        /// <remarks>
        /// Note: The property name is intentionally spelled "Cannonical" for historical compatibility.
        /// The value is a canonical JWK string as defined by RFC 7638.
        /// </remarks>
        public string CannonicalPublicKeyJwk { get; private set; }

        /// <summary>
        /// Gets the JOSE algorithm identifier used when signing PoP payloads.
        /// </summary>
        public string CryptographicAlgorithm => "ES256";

        /// <summary>
        /// Signs the provided payload using the instance's ECDSA private key.
        /// </summary>
        /// <param name="payload">The bytes to sign.</param>
        /// <returns>The ECDSA signature over <paramref name="payload"/> using SHA-256.</returns>
        public byte[] Sign(byte[] payload)
        {
            return Sign(_signingKey, payload);
        }

        private void InitializeSigningKey()
        {
            _signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            ECParameters publicKeyInfo = _signingKey.ExportParameters(false);

            CannonicalPublicKeyJwk = ComputeCanonicalJwk(publicKeyInfo);
        }

        /// <summary>
        /// Creates the canonical representation of the public JWK.
        /// </summary>
        /// <param name="ecdPublicKey">The public ECDSA parameters.</param>
        /// <returns>A canonical JWK string per RFC 7638.</returns>
        /// <remarks>
        /// See https://tools.ietf.org/html/rfc7638#section-3.
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint.
        /// </remarks>
        private static string ComputeCanonicalJwk(ECParameters ecdPublicKey)
        {
            string x = ecdPublicKey.Q.X != null ? Base64UrlHelpers.Encode(ecdPublicKey.Q.X) : null;
            string y = ecdPublicKey.Q.Y != null ? Base64UrlHelpers.Encode(ecdPublicKey.Q.Y) : null;

            return $@"{{""{JsonWebKeyParameterNames.Crv}"":""{GetCrvParameterValue(ecdPublicKey.Curve)}"",""{JsonWebKeyParameterNames.Kty}"":""{JsonWebKeyParameterNames.EC}"",""{JsonWebKeyParameterNames.X}"":""{x}"",""{JsonWebKeyParameterNames.Y}"":""{y}""}}";
        }

        /// <summary>
        /// Signs the provided payload using the provided ECDSA key.
        /// </summary>
        /// <param name="ecdKey">The ECDSA key to use for signing.</param>
        /// <param name="payload">The bytes to sign.</param>
        /// <returns>The ECDSA signature over <paramref name="payload"/> using SHA-256.</returns>
        public static byte[] Sign(ECDsa ecdKey, byte[] payload)
        {
            if (ecdKey == null)
            {
                throw new ArgumentNullException(nameof(ecdKey));
            }

            return ecdKey.SignData(payload, HashAlgorithmName.SHA256);
        }

        private static string GetCrvParameterValue(ECCurve curve)
        {
            if (string.Equals(curve.Oid.Value, ECCurve.NamedCurves.nistP256.Oid.Value, StringComparison.Ordinal) ||
                string.Equals(curve.Oid.FriendlyName, ECCurve.NamedCurves.nistP256.Oid.FriendlyName, StringComparison.Ordinal))
            {
                return JsonWebKeyECTypes.P256;
            }
            else if (string.Equals(curve.Oid.Value, ECCurve.NamedCurves.nistP384.Oid.Value, StringComparison.Ordinal) ||
                     string.Equals(curve.Oid.FriendlyName, ECCurve.NamedCurves.nistP384.Oid.FriendlyName, StringComparison.Ordinal))
            {
                return JsonWebKeyECTypes.P384;
            }
            else if (string.Equals(curve.Oid.Value, ECCurve.NamedCurves.nistP521.Oid.Value, StringComparison.Ordinal) ||
                     string.Equals(curve.Oid.FriendlyName, ECCurve.NamedCurves.nistP521.Oid.FriendlyName, StringComparison.Ordinal))
            {
                return JsonWebKeyECTypes.P521;
            }

            throw new ArgumentException("Unsupported elliptic curve.", nameof(curve));
        }

        /// <summary>
        /// Constants for JSON Web Key (JWK) elliptic curve types.
        /// </summary>
        /// <remarks>
        /// See https://tools.ietf.org/html/rfc7518#section-6.2.1.1.
        /// </remarks>
        private static class JsonWebKeyECTypes
        {
            public const string P256 = "P-256";
            public const string P384 = "P-384";
            public const string P521 = "P-521";
        }
    }
}
