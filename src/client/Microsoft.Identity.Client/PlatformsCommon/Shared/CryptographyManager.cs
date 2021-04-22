// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal static class CryptographyManager
    {
        private static readonly ConcurrentDictionary<string, RSA> s_certificateToRsaMap = new ConcurrentDictionary<string, RSA>();
        private static readonly int s_maximumMapSize = 1000;

        public static byte[] SignWithCertificate(string message, X509Certificate2 certificate)
        {
            using var chain = new X509Chain
            {
                ChainPolicy = BuildChainPolicyChained(),
            };

            var certificateIsValid = chain.Build(certificate);
            if (!certificateIsValid)
            {
                var chainErrors = string.Join(", ", chain.ChainStatus.Select(e => e.Status.ToString()));
                throw new InvalidOperationException($"An invalid certificate was supplied. {chainErrors}");
            }

            if (!s_certificateToRsaMap.TryGetValue(certificate.Thumbprint, out var alg))
            {
                if (s_certificateToRsaMap.Count >= s_maximumMapSize)
                {
                    s_certificateToRsaMap.Clear();
                }

#if NET5_0_OR_GREATER
                alg = certificate.GetRSAPrivateKey() ?? certificate.GetECDsaPrivateKey() ?? (AsymmetricAlgorithm?)certificate.GetDSAPrivateKey() ?? throw new NotSupportedException("The certificate key algorithm is not supported.");
#else
                alg = certificate.GetRSAPrivateKey() ?? (AsymmetricAlgorithm?)certificate.GetECDsaPrivateKey() ?? throw new NotSupportedException("The certificate key algorithm is not supported.");
#endif
            }

            var signedData = alg switch
            {
                RSA rsa => rsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                ECDsa eCDsa => eCDsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256),
#if NET5_0_OR_GREATER
                DSA dsa => dsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256),
#endif
                _ => throw new NotSupportedException("The certificate key algorithm is not supported."),
            };

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

            _ = chainPolicy.ApplicationPolicy.Add(OidLookup.DocumentSigning);
            return chainPolicy;
        }
    }
}
