// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Where credential material was sourced from.
    /// </summary>
    internal enum CredentialSource
    {
        /// <summary>
        /// Credential was provided statically at app construction time.
        /// </summary>
        Static,

        /// <summary>
        /// Credential was resolved dynamically via a callback/delegate.
        /// </summary>
        Callback
    }

    /// <summary>
    /// Normalized output of credential resolution.
    /// Decouples "what a credential produces" from "how it's used".
    /// Immutable by design.
    /// </summary>
    internal sealed class CredentialMaterial
    {
        /// <summary>
        /// Creates a new CredentialMaterial.
        /// </summary>
        public CredentialMaterial(
            IReadOnlyDictionary<string, string> tokenRequestParameters,
            CredentialSource source,
            X509Certificate2 resolvedCertificate = null)
        {
            if (tokenRequestParameters == null)
                throw new ArgumentNullException(nameof(tokenRequestParameters));

            TokenRequestParameters = tokenRequestParameters;
            Source = source;
            ResolvedCertificate = resolvedCertificate;
        }

        /// <summary>
        /// OAuth2 token endpoint authentication parameters (e.g., client_secret, client_assertion).
        /// Never null. Empty dictionary is valid (e.g., for bearer token flows).
        /// </summary>
        public IReadOnlyDictionary<string, string> TokenRequestParameters { get; }

        /// <summary>
        /// Where the credential material was sourced from (Static or Callback).
        /// </summary>
        public CredentialSource Source { get; }

        /// <summary>
        /// Optional X.509 certificate for mTLS proof-of-possession / TLS channel binding.
        /// </summary>
        public X509Certificate2 ResolvedCertificate { get; }
    }
}
