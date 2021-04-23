// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal static class CryptographyManager
    {
        private static readonly ConcurrentDictionary<string, AsymmetricAlgorithm> s_certificateToRsaMap = new ConcurrentDictionary<string, AsymmetricAlgorithm>();
        private static readonly int s_maximumMapSize = 1000;

        public static byte[] SignWithCertificate(string message, X509Certificate2 certificate)
        {
            if (!s_certificateToRsaMap.TryGetValue(certificate.Thumbprint, out AsymmetricAlgorithm alg))
            {
                if (s_certificateToRsaMap.Count >= s_maximumMapSize)
                {
                    s_certificateToRsaMap.Clear();
                }

#if NET5_0_OR_GREATER
                alg = certificate.GetRSAPrivateKey() ?? certificate.GetECDsaPrivateKey() ?? (AsymmetricAlgorithm?)certificate.GetDSAPrivateKey() ?? throw new NotSupportedException("The certificate key algorithm is not supported.");
#elif !NETSTANDARD1_3
                alg = certificate.GetRSAPrivateKey() ?? (AsymmetricAlgorithm)certificate.GetECDsaPrivateKey() ?? throw new NotSupportedException("The certificate key algorithm is not supported.");
#else
                alg = certificate.GetRSAPrivateKey() ?? throw new NotSupportedException("The certificate key algorithm is not supported.");
#endif
            }

            byte[] signedData;
            if (alg is RSA rsa)
            {
                signedData = rsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
#if !NETSTANDARD1_3
            else if (alg is ECDsa eCDsa)
            {
                signedData = eCDsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256);

            }
#endif
#if NET5_0_OR_GREATER
            else if (alg is DSA dsa)
            {
                signedData = dsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256);
            }
#endif
            else
            {
                throw new NotSupportedException("The certificate key algorithm is not supported.");
            }

            // Cache only valid crypto providers, which are able to sign data successfully
            s_certificateToRsaMap[certificate.Thumbprint] = alg;
            return signedData;
        }
    }
}
