// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Helper utilities for credential material handling.
    /// Centralizes hashing, validation, and common operations.
    /// </summary>
    internal static class CredentialMaterialHelper
    {
        /// <summary>
        /// Reserved OAuth2 parameter names that credentials must not provide.
        /// These are owned by MSAL and its request builder.
        /// </summary>
        public static readonly HashSet<string> ReservedTokenParameters = 
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                OAuth2Parameter.GrantType,
                OAuth2Parameter.Scope,
                OAuth2Parameter.ClientId,
                OAuth2Parameter.Claims
            };

        /// <summary>
        /// Computes a short hash prefix of the certificate's raw data.
        /// Used for certificate correlation in telemetry and cache binding.
        /// Hashes RawData (DER-encoded certificate), not SPKI.
        /// </summary>
        public static string GetCertificateIdHashPrefix(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(certificate.RawData);
            string fullHash = Base64UrlHelpers.Encode(hash);
            return fullHash.Substring(0, Math.Min(16, fullHash.Length));
        }

        /// <summary>
        /// Validates that TokenRequestParameters does not contain reserved keys.
        /// Throws if violation detected.
        /// </summary>
        public static void ValidateTokenParametersNoReservedKeys(
            IReadOnlyDictionary<string, string> tokenParameters)
        {
            if (tokenParameters == null)
                return;

            foreach (var key in tokenParameters.Keys)
            {
                if (ReservedTokenParameters.Contains(key))
                {
                    throw new MsalClientException(
                        MsalError.InvalidCredentialMaterial,
                        $"Credential cannot provide reserved OAuth2 parameter: '{key}'. " +
                        "Credential may only provide authentication parameters (e.g., client_secret, client_assertion).");
                }
            }
        }
    }
}
