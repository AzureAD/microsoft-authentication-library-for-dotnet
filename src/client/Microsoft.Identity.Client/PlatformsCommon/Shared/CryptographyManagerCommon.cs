// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
#if !NET45
    internal static class CryptographyManagerCommon
    {
        private static readonly ConcurrentDictionary<string, RSA> s_certificateToRsaMap = new ConcurrentDictionary<string, RSA>();
        private static readonly int s_maximumMapSize = 1000;

        public static byte[] SignWithCertificate(string message, X509Certificate2 certificate)
        {
            if (!s_certificateToRsaMap.TryGetValue(certificate.Thumbprint, out RSA rsa))
            {
                if (s_certificateToRsaMap.Count >= s_maximumMapSize)
                    s_certificateToRsaMap.Clear();

                rsa = certificate.GetRSAPrivateKey();
            }

            var signedData = rsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            // Cache only valid RSA crypto providers, which are able to sign data successfully
            s_certificateToRsaMap[certificate.Thumbprint] = rsa;
            return signedData;
        }
    }
#endif
}
