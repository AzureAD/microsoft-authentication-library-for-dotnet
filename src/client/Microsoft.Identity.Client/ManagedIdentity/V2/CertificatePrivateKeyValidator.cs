// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Validates that a certificate's private key is accessible by attempting
    /// a signing operation.  KeyGuard-backed keys may become inaccessible
    /// after machine restarts, VBS failures, or corruption; the standard
    /// <see cref="X509Certificate2.HasPrivateKey"/> check only verifies the
    /// presence of a key reference, not actual usability.
    /// </summary>
    internal static class CertificatePrivateKeyValidator
    {
        // Minimal payload for the signing validation; the content is irrelevant –
        // only the ability to sign matters.
        private static readonly byte[] s_testData = new byte[32];

        /// <summary>
        /// Returns <c>true</c> when the certificate's RSA private key can
        /// successfully sign data; <c>false</c> otherwise.
        /// </summary>
        internal static bool IsPrivateKeyAccessible(
            X509Certificate2 cert,
            ILoggerAdapter logger = null)
        {
            if (cert is null)
            {
                logger?.Verbose(() => "[PersistentCert] Private key validation skipped: certificate is null.");
                return false;
            }

            try
            {
                using var rsa = cert.GetRSAPrivateKey();

                if (rsa is null)
                {
                    logger?.Verbose(() => "[PersistentCert] Private key validation failed: GetRSAPrivateKey returned null.");
                    return false;
                }

                rsa.SignData(s_testData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return true;
            }
            catch (Exception ex)
            {
                logger?.Verbose(() =>
                    "[PersistentCert] Private key validation failed: " + ex.Message + ". Certificate may need re-provisioning.");
                return false;
            }
        }
    }
}
