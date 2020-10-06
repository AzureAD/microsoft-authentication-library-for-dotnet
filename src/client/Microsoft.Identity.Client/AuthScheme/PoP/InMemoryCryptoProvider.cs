// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{

    /// <summary>
    /// The default implementation will store a key in memory    
    /// </summary>
    internal class InMemoryCryptoProvider : IPoPCryptoProvider
    {
        internal /* internal for test only */ const int RsaKeySize = 2048;

#if NET45
        private RSACryptoServiceProvider _signingKey;
#else
        private RSA _signingKey;
#endif

        public InMemoryCryptoProvider()
        {
            InitializeSigningKey();
        }

        public RSAParameters PublicKeyInfo { get; private set; }

        private void InitializeSigningKey()
        {
#if NET45
            _signingKey = new RSACryptoServiceProvider(RsaKeySize);
#else
            _signingKey = RSA.Create();
            _signingKey.KeySize = RsaKeySize;
#endif
            PublicKeyInfo = _signingKey.ExportParameters(false);
        }

        public byte[] Sign(byte[] payload)
        {
            return Sign(_signingKey, payload);
        }

        public static byte[] Sign(RSA RsaKey, byte[] payload)
        {
#if NET45
            return ((RSACryptoServiceProvider)RsaKey).SignData(payload, CryptoConfig.MapNameToOID("SHA256"));
#else
            return RsaKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#endif
        }
    }
}
