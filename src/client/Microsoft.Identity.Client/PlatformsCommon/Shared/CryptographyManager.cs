// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal static class CryptographyManager
    {
        private static readonly ConcurrentDictionary<string, AsymmetricAlgorithm> s_certificateToAsymmetricAlgorithmMap = new ConcurrentDictionary<string, AsymmetricAlgorithm>();
        private static readonly int s_maximumMapSize = 1000;

        public static byte[] SignWithCertificate(string message, X509Certificate2 certificate)
        {

            if (!s_certificateToAsymmetricAlgorithmMap.TryGetValue(certificate.Thumbprint, out AsymmetricAlgorithm asymmetricAlgorithm))
            {
                if (s_certificateToAsymmetricAlgorithmMap.Count >= s_maximumMapSize)
                {
                    s_certificateToAsymmetricAlgorithmMap.Clear();
                }

                asymmetricAlgorithm = certificate.GetRSAPrivateKey();
#if !NETSTANDARD1_3
                asymmetricAlgorithm = asymmetricAlgorithm ?? certificate.GetECDsaPrivateKey();
#endif
#if NET5_WIN
                asymmetricAlgorithm = asymmetricAlgorithm ?? certificate.GetDSAPrivateKey();
#endif

                if (asymmetricAlgorithm == null)
                {
                    throw new NotSupportedException(MsalErrorMessage.CertificateKeyAlgorithmNotSupported);
                }
            }

            byte[] signedData = getSignedData(asymmetricAlgorithm, message);

            // Cache only valid crypto providers, which are able to sign data successfully
            s_certificateToAsymmetricAlgorithmMap[certificate.Thumbprint] = asymmetricAlgorithm;
            return signedData;
        }

        private static byte[] getSignedData(AsymmetricAlgorithm asymmetricAlgorithm, string message)
        {
            byte[] signedData;
            if (asymmetricAlgorithm is RSA rsa)
            {
                signedData = rsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
#if !NETSTANDARD1_3
            else if (asymmetricAlgorithm is ECDsa eCDsa)
            {
                signedData = eCDsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256);

            }
#endif
#if NET5_WIN
            else if (asymmetricAlgorithm is DSA dsa)
            {
                signedData = dsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256);
            }
#endif
            else
            {
                throw new NotSupportedException(MsalErrorMessage.CertificateKeyAlgorithmNotSupported);
            }

            return signedData;
        }
    }
}
