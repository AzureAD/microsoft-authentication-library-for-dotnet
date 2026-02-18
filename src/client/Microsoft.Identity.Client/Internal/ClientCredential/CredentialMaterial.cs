// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Specifies the source of credential material.
    /// </summary>
    internal enum CredentialSource
    {
        /// <summary>
        /// Credential is static (e.g., a client secret or static certificate).
        /// </summary>
        Static,

        /// <summary>
        /// Credential is obtained via a callback/delegate.
        /// </summary>
        Callback
    }

    /// <summary>
    /// Normalized output from credential resolution containing token request parameters
    /// and optional resolved certificate for mTLS scenarios.
    /// </summary>
    internal sealed class CredentialMaterial
    {
        public CredentialMaterial(
            Dictionary<string, string> tokenRequestParameters,
            CredentialSource credentialSource,
            X509Certificate2 resolvedCertificate = null)
        {
            TokenRequestParameters = tokenRequestParameters ?? new Dictionary<string, string>();
            CredentialSource = credentialSource;
            ResolvedCertificate = resolvedCertificate;
        }

        /// <summary>
        /// Dictionary of OAuth2 parameters to add to the token request body.
        /// </summary>
        public Dictionary<string, string> TokenRequestParameters { get; }

        /// <summary>
        /// The source of this credential (Static or Callback).
        /// </summary>
        public CredentialSource CredentialSource { get; }

        /// <summary>
        /// Optional certificate resolved during credential resolution.
        /// Used for mTLS scenarios where the credential provides a certificate.
        /// </summary>
        public X509Certificate2 ResolvedCertificate { get; }
    }
}
