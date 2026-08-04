// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Utils
{
    internal static class X509Certificate2Extensions
    {
        /// <summary>
        /// Computes the mTLS PoP cache KeyId (x5t#S256): base64url SHA-256 of the DER-encoded
        /// certificate (RFC 8705), matching what ESTS/MSS bind the token to.
        /// </summary>
        internal static string ComputeX5tS256KeyId(this X509Certificate2 certificate)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(certificate.RawData);
                return Base64UrlHelpers.Encode(hash);
            }
        }
    }
}
