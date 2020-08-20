// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.PlatformsCommon
{
#if DESKTOP || NET_CORE
    /// <summary>
    /// The default implementation will store a key in memory for a period of 8 hours and recreate it to ensure the key is kept fresh.
    /// Subsequent calls to this provider will reuse this key.
    /// </summary>
    class NetSharedPoPCryptoProvider : IPoPCryptoProvider
    {
        internal /* internal for test only */ const int RsaKeySize = 2048;

#if DESKTOP
        private static RSACryptoServiceProvider s_SigningKey;
#elif NET_CORE
        private static RSA s_SigningKey;
#endif
        private DateTime _keyTimeValidTo;
        private readonly uint _defaultKeyExpirationTime = 60 * 60 * 8; // Eight Hours
        private ITimeService _timer;

        public string CannonicalPublicKeyJwk { get; private set; }

        private RSA RsaKey
        {
            get
            {
                lock (s_SigningKey)
                {
                    if (CheckKeyExpiration(_keyTimeValidTo, _timer))
                    {
#if DESKTOP
                        s_SigningKey = new RSACryptoServiceProvider();
#elif NET_CORE
                        s_SigningKey = RSA.Create();
#endif
                        s_SigningKey.KeySize = RsaKeySize;
                        _keyTimeValidTo = _timer.GetUtcNow() + TimeSpan.FromSeconds(_defaultKeyExpirationTime);

                        RSAParameters publicKeyInfo = s_SigningKey.ExportParameters(false);

                        CannonicalPublicKeyJwk = ComputeCannonicalJwk(publicKeyInfo);

                        return s_SigningKey;
                    }

                    return s_SigningKey;
                }
            }
        }

        public NetSharedPoPCryptoProvider(/*for testing only*/ITimeService timer = null)
        {
            _timer = timer ?? new TimeService();
        }

        public byte[] Sign(byte[] payload)
        {
            return Sign(RsaKey, payload);
        }

        internal static bool CheckKeyExpiration(DateTime KeyTimeValidTo, ITimeService timer)
        {
            return DateTime.Compare(KeyTimeValidTo, timer.GetUtcNow()) < 0;
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
#if DESKTOP
            return ((RSACryptoServiceProvider)RsaKey).SignData(payload, CryptoConfig.MapNameToOID("SHA256"));
#elif NET_CORE
            return RsaKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#else
            throw new PlatformNotSupportedException(
                "Proof of possesion flows are not available on mobile platforms or on Mac.");
#endif
        }
    }
#endif
}
