// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
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
            var chain = new X509Chain
            {
                ChainPolicy = BuildChainPolicyChained(),
            };

            var certificateIsValid = chain.Build(certificate);
            if (!certificateIsValid)
            {
                var chainErrors = string.Join(", ", chain.ChainStatus.Select(e => e.Status.ToString()));
                throw new InvalidOperationException($"An invalid certificate was supplied. {chainErrors}");
            }

            chain.Dispose();

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

        private static X509ChainPolicy BuildChainPolicyChained(
            X509VerificationFlags verificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority,
            X509RevocationFlag revocationFlag = X509RevocationFlag.EndCertificateOnly,
            X509RevocationMode revocationMode = X509RevocationMode.NoCheck)
        {
            var chainPolicy = new X509ChainPolicy
            {
                VerificationFlags = verificationFlags,
                RevocationFlag = revocationFlag,
                RevocationMode = revocationMode,
            };

            // Digital Signature
            _ = chainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.4.1.311.10.3.12"));
            return chainPolicy;
        }
    }
}
