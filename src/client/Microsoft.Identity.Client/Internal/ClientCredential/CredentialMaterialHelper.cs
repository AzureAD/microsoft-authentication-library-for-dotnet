// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Centralized utilities for credential material handling.
    /// Provides hash computation and parameter validation.
    /// </summary>
    internal static class CredentialMaterialHelper
    {
        /// <summary>
        /// Reserved parameter names that MSAL owns and credentials must not override.
        /// These parameters are set by MSAL's request infrastructure, not by credentials.
        /// </summary>
        public static readonly HashSet<string> ReservedTokenParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            OAuth2Parameter.GrantType,
            OAuth2Parameter.Scope,
            OAuth2Parameter.ClientId,
            OAuth2Parameter.Claims
        };

        /// <summary>
        /// Computes a hash prefix of the certificate's raw data for correlation.
        /// Used for certificate correlation in telemetry and cache binding.
        /// Returns the first 16 characters of the base64url-encoded SHA-256 hash.
        /// </summary>
        /// <param name="certificate">The X.509 certificate to hash.</param>
        /// <returns>A 16-character hash prefix for correlation purposes.</returns>
        public static string GetCertificateIdHashPrefix(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(certificate.RawData);
            string fullHash = Base64UrlHelpers.Encode(hash);
            return fullHash.Substring(0, Math.Min(16, fullHash.Length));
        }

        /// <summary>
        /// Validates that credential-provided token parameters do not include reserved keys.
        /// Throws MsalClientException if any reserved key is found.
        /// </summary>
        /// <param name="tokenParameters">Token request parameters from a credential.</param>
        /// <exception cref="MsalClientException">Thrown when a reserved parameter key is found.</exception>
        public static void ValidateTokenParametersNoReservedKeys(IReadOnlyDictionary<string, string> tokenParameters)
        {
            if (tokenParameters == null)
            {
                return;
            }

            foreach (var key in tokenParameters.Keys)
            {
                if (ReservedTokenParameters.Contains(key))
                {
                    throw new MsalClientException(
                        MsalError.InvalidClientAssertion,
                        $"Credential cannot provide reserved OAuth2 parameter: {key}. " +
                        $"Reserved parameters are managed by MSAL: {string.Join(", ", ReservedTokenParameters)}");
                }
            }
        }
    }
}
