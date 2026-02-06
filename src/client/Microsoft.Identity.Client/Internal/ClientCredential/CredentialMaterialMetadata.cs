// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Telemetry-safe metadata about how a credential was resolved.
    /// Does not contain sensitive data like secrets or full certificate thumbprints.
    /// </summary>
    internal sealed class CredentialMaterialMetadata
    {
        /// <summary>
        /// The type of credential that was used.
        /// </summary>
        public AssertionType CredentialType { get; set; }

        /// <summary>
        /// Optional description of where the credential came from.
        /// Examples: "callback", "cert-store", "key-vault", "static"
        /// </summary>
        public string CredentialSource { get; set; }

        /// <summary>
        /// Optional prefix of the SHA-256 hash of the mTLS certificate identifier.
        /// Contains only the first 8-16 characters for telemetry, not the full thumbprint.
        /// </summary>
        public string MtlsCertificateIdHashPrefix { get; set; }

        /// <summary>
        /// Indicates whether an mTLS certificate was requested in this credential resolution.
        /// </summary>
        public bool MtlsCertificateRequested { get; set; }

        /// <summary>
        /// Time taken to resolve the credential in milliseconds.
        /// </summary>
        public long ResolutionTimeMs { get; set; }
    }
}
