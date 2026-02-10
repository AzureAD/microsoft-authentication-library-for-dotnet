// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Immutable credential material produced by <see cref="IClientCredential.GetCredentialMaterialAsync"/>.
    /// Contains OAuth2 token request parameters and optional mTLS certificate.
    /// </summary>
    internal sealed class CredentialMaterial
    {
        /// <summary>
        /// Creates credential material with authentication parameters and optional certificate.
        /// </summary>
        /// <param name="tokenRequestParameters">
        /// OAuth2 token request authentication parameters (e.g., client_secret, client_assertion).
        /// Must not be null; empty dictionary is valid for flows that don't require credential parameters.
        /// </param>
        /// <param name="mtlsCertificate">
        /// Optional X.509 certificate for mTLS proof-of-possession or TLS channel binding. May be null.
        /// </param>
        /// <param name="metadata">
        /// Optional metadata for telemetry (credential type, source, timing). May be null.
        /// Must not contain secrets or PII.
        /// </param>
        public CredentialMaterial(
            IReadOnlyDictionary<string, string> tokenRequestParameters,
            X509Certificate2 mtlsCertificate = null,
            CredentialMaterialMetadata metadata = null)
        {
            // Defensive: guarantee non-null dictionary to simplify consumer code
            TokenRequestParameters = tokenRequestParameters ?? new Dictionary<string, string>();
            MtlsCertificate = mtlsCertificate;
            Metadata = metadata;
        }

        /// <summary>
        /// OAuth2 token request authentication parameters (e.g., client_secret, client_assertion).
        /// Never null; empty dictionary is valid for flows that don't require credential parameters.
        /// Credentials must not provide reserved parameters like grant_type, scope, or client_id.
        /// </summary>
        public IReadOnlyDictionary<string, string> TokenRequestParameters { get; }

        /// <summary>
        /// Optional X.509 certificate for mTLS proof-of-possession / TLS channel binding.
        /// When present, this certificate will be used for the TLS connection to the token endpoint
        /// and may be bound to the resulting token. May be null.
        /// </summary>
        public X509Certificate2 MtlsCertificate { get; }

        /// <summary>
        /// Optional metadata for telemetry (credential type, source, timing).
        /// Must not contain secrets or PII. May be null.
        /// </summary>
        public CredentialMaterialMetadata Metadata { get; }
    }
}
