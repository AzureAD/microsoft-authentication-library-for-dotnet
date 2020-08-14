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
    internal class PopCryptoHelper
    {
        internal static bool CheckKeyExpiration(DateTime KeyTimeValidTo, IClock clock)
        {
            return DateTime.Compare(KeyTimeValidTo, clock.Now) < 0;
        }

        /// <summary>
        /// Creates the cannonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        internal static string ComputeCannonicalJwk(RSAParameters rsaPublicKey)
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

    internal class SystemClock : IClock
    {
        public DateTime Now { get { return DateTime.UtcNow; } }
    }
}
