// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.net45
{

    /// <summary>
    /// This provider is based on the RSACryptoServiceProvider container concept. 
    /// It will create and store a pair of keys in the context of the current user. Subsequent calls to this provider will reuse this key.
    /// </summary>
    /// <remarks>
    /// Key creation and storage only works on Windows. See https://stackoverflow.com/questions/41986995/implement-rsa-in-net-core/42006084 for more details.
    /// </remarks>
    internal class NetDesktopPoPCryptoProvider : IPoPCryptoProvider, IDisposable
    {
        internal /* internal for test only */ const int RsaKeySize = 2048;
        internal /* internal for test only */ const string ContainerName = "com.microsoft.msal";
        private static RSACryptoServiceProvider s_SigningKey;
        private bool s_PersistKey = false;
        private DateTime s_KeyTimeValidTo;
        private readonly uint s_DefaultKeyExpirationTime = 60 * 60 * 8; // Eight Hours

        // This is a singleton because the key is the same on a device
        private static readonly Lazy<NetDesktopPoPCryptoProvider> lazyInstance =
            new Lazy<NetDesktopPoPCryptoProvider>(() => new NetDesktopPoPCryptoProvider());

        private RSACryptoServiceProvider RsaKey
        {
            get
            {
                if (!s_PersistKey)
                {
                    if (CheckKeyExpiration())
                    {
                        s_SigningKey = new RSACryptoServiceProvider();
                        s_SigningKey.KeySize = RsaKeySize;
                        s_KeyTimeValidTo = DateTime.Now + TimeSpan.FromSeconds(s_DefaultKeyExpirationTime);
                        return s_SigningKey;
                    }

                    return s_SigningKey;
                }

                return GetOrCreatePersistededKey(ContainerName);
            }
        }

        public static NetDesktopPoPCryptoProvider Instance { get { return lazyInstance.Value; } }

        private NetDesktopPoPCryptoProvider(bool persistKey = false)
        {
            s_PersistKey = persistKey;
            
            RSAParameters publicKeyInfo = RsaKey.ExportParameters(false);

            CannonicalPublicKeyJwk = ComputeCannonicalJwk(publicKeyInfo);
        }

        public string CannonicalPublicKeyJwk { get; }

        public byte[] Sign(byte[] payload)
        {
            return RsaKey.SignData(payload, CryptoConfig.MapNameToOID("SHA256"));
        }

        private static RSACryptoServiceProvider GetOrCreatePersistededKey(string containerName)
        {
            var reuseKeyParams = new CspParameters
            {
                Flags = CspProviderFlags.UseExistingKey,
                KeyContainerName = containerName
            };

            try
            {
                return new RSACryptoServiceProvider(RsaKeySize, reuseKeyParams);
            }
            catch (CryptographicException)
            {
                var newKeyparams = new CspParameters
                {
                    KeyContainerName = containerName
                };

                return new RSACryptoServiceProvider(RsaKeySize, newKeyparams);
            }
        }

        /// <summary>
        /// Creates the cannonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        private static string ComputeCannonicalJwk(RSAParameters rsaPublicKey)
        {
            return $@"{{""{JsonWebKeyParameterNames.E}"":""{Base64UrlHelpers.Encode(rsaPublicKey.Exponent)}"",""{JsonWebKeyParameterNames.Kty}"":""{JsonWebAlgorithmsKeyTypes.RSA}"",""{JsonWebKeyParameterNames.N}"":""{Base64UrlHelpers.Encode(rsaPublicKey.Modulus)}""}}";
        }

        public void Dispose()
        {
            s_SigningKey.Dispose();
        }

        private bool CheckKeyExpiration()
        {
            return ConvertToTimeT(s_KeyTimeValidTo) < ConvertToTimeT(DateTime.Now);
        }

        internal static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)diff.TotalSeconds;
        }
    }
}
