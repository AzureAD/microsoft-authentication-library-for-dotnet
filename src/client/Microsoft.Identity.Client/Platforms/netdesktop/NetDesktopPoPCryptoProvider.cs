// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.PlatformsCommon;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.net45
{

    /// <summary>
    /// The default implementation will store a key in memory for a period of 8 hours and recreate it to ensure the key is kept fresh.
    /// Subsequent calls to this provider will reuse this key.
    /// </summary>
    internal class NetDesktopPoPCryptoProvider : IPoPCryptoProvider, IDisposable
    {
        internal /* internal for test only */ const int RsaKeySize = 2048;
        internal /* internal for test only */ const string ContainerName = "com.microsoft.msal";
        private static RSACryptoServiceProvider s_SigningKey;
        private DateTime _KeyTimeValidTo;
        private readonly uint _DefaultKeyExpirationTime = 60 * 60 * 8; // Eight Hours

        internal /* internal for test only */ IClock Clock { get; set; }

        public string CannonicalPublicKeyJwk { get; private set; }

        private RSACryptoServiceProvider RsaKey
        {
            get
            {
                lock (s_SigningKey)
                {
                    if (PopCryptoHelper.CheckKeyExpiration(_KeyTimeValidTo, Clock))
                    {
                        s_SigningKey = new RSACryptoServiceProvider();
                        s_SigningKey.KeySize = RsaKeySize;
                        _KeyTimeValidTo = Clock.Now + TimeSpan.FromSeconds(_DefaultKeyExpirationTime);

                        RSAParameters publicKeyInfo = s_SigningKey.ExportParameters(false);

                        CannonicalPublicKeyJwk = PopCryptoHelper.ComputeCannonicalJwk(publicKeyInfo);

                        return s_SigningKey;
                    }

                    return s_SigningKey;
                }
            }
        }

        public NetDesktopPoPCryptoProvider()
        {
            Clock = new SystemClock();
        }

        public byte[] Sign(byte[] payload)
        {
            return PopCryptoHelper.Sign(RsaKey, payload);
        }

        public void Dispose()
        {
            s_SigningKey.Dispose();
        }
    }
}
