// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Represents the credential material resolved from a client credential.
    /// This is the normalized output of credential resolution.
    /// </summary>
    internal sealed class CredentialMaterial
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CredentialMaterial"/> class.
        /// </summary>
        /// <param name="tokenRequestParameters">OAuth2 token request parameters (never null, empty OK)</param>
        /// <param name="mtlsCertificate">Optional certificate for TLS binding</param>
        /// <param name="metadata">Optional telemetry-safe metadata</param>
        public CredentialMaterial(
            IReadOnlyDictionary<string, string> tokenRequestParameters,
            X509Certificate2? mtlsCertificate = null,
            CredentialMaterialMetadata? metadata = null)
        {
            TokenRequestParameters = tokenRequestParameters ?? new Dictionary<string, string>();
            MtlsCertificate = mtlsCertificate;
            Metadata = metadata;
        }

        /// <summary>
        /// Gets the OAuth2 token request parameters (e.g., client_secret, client_assertion).
        /// Never null; empty dictionary if no parameters.
        /// </summary>
        public IReadOnlyDictionary<string, string> TokenRequestParameters { get; }

        /// <summary>
        /// Gets the optional certificate for mTLS TLS channel binding.
        /// Null if the credential does not provide a certificate or mTLS is not applicable.
        /// </summary>
        public X509Certificate2? MtlsCertificate { get; }

        /// <summary>
        /// Gets the optional telemetry-safe metadata about credential resolution.
        /// Must not contain secrets or PII.
        /// </summary>
        public CredentialMaterialMetadata? Metadata { get; }
    }
}

#nullable restore
