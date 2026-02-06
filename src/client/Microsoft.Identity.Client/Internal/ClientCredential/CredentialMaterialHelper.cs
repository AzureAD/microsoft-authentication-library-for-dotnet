// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Helper methods for credential material processing.
    /// </summary>
    internal static class CredentialMaterialHelper
    {
        /// <summary>
        /// Gets a prefix of the certificate ID hash for telemetry and token cache binding.
        /// This hashes the certificate raw data (DER-encoded certificate) and returns a prefix.
        /// The hash is aligned with MSAL token cache binding semantics.
        /// </summary>
        /// <param name="certificate">The certificate to hash</param>
        /// <param name="prefixLength">The length of the prefix to return (default: 8)</param>
        /// <returns>A hex-encoded prefix of the SHA-256 hash of the certificate raw data</returns>
        public static string GetCertificateIdHashPrefix(X509Certificate2 certificate, int prefixLength = 8)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            // Hash the full DER-encoded certificate (RawData)
            // This is aligned with MSAL token cache binding and RFC 8705 (x5t#S256)
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(certificate.RawData);
                string fullHash = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                
                // Return a prefix for telemetry (safe, non-PII)
                return fullHash.Substring(0, Math.Min(prefixLength, fullHash.Length));
            }
        }
    }
}
