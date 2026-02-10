// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Immutable metadata about credential material resolution for telemetry purposes.
    /// Must not contain secrets, PII, or full thumbprints.
    /// </summary>
    internal sealed class CredentialMaterialMetadata
    {
        /// <summary>
        /// Creates credential metadata with optional telemetry information.
        /// </summary>
        /// <param name="credentialType">Type of credential that produced the material.</param>
        /// <param name="credentialSource">Optional source identifier (e.g., "certificate-provider", "user-delegate"). May be null.</param>
        /// <param name="mtlsCertificateIdHashPrefix">Optional prefix of certificate hash for correlation (not full thumbprint). May be null.</param>
        /// <param name="mtlsCertificateRequested">True if mTLS certificate was requested by the context.</param>
        /// <param name="resolutionTimeMs">Time taken to resolve credential material, including signing operations.</param>
        public CredentialMaterialMetadata(
            CredentialType credentialType,
            string credentialSource = null,
            string mtlsCertificateIdHashPrefix = null,
            bool mtlsCertificateRequested = false,
            long resolutionTimeMs = 0)
        {
            CredentialType = credentialType;
            CredentialSource = credentialSource;
            MtlsCertificateIdHashPrefix = mtlsCertificateIdHashPrefix;
            MtlsCertificateRequested = mtlsCertificateRequested;
            ResolutionTimeMs = resolutionTimeMs;
        }

        /// <summary>
        /// Type of credential that produced this material.
        /// </summary>
        public CredentialType CredentialType { get; }

        /// <summary>
        /// Optional source identifier for telemetry (e.g., "static-certificate", "user-delegate").
        /// Must not contain secrets or PII. May be null.
        /// </summary>
        public string CredentialSource { get; }

        /// <summary>
        /// Optional prefix of the certificate hash for correlation in telemetry and cache binding.
        /// This is NOT a full thumbprint to avoid security concerns with telemetry.
        /// Typically the first 16 characters of a base64url-encoded SHA-256 hash. May be null.
        /// </summary>
        public string MtlsCertificateIdHashPrefix { get; }

        /// <summary>
        /// True if the request context indicated that mTLS certificate was required.
        /// </summary>
        public bool MtlsCertificateRequested { get; }

        /// <summary>
        /// Time in milliseconds taken to resolve the credential material,
        /// including any signing operations (e.g., JWT creation).
        /// </summary>
        public long ResolutionTimeMs { get; }
    }
}
