// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Telemetry-safe metadata about credential material resolution.
    /// Must not contain secrets or PII.
    /// </summary>
    internal sealed class CredentialMaterialMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CredentialMaterialMetadata"/> class.
        /// </summary>
        /// <param name="assertionType">The type of assertion/credential used</param>
        /// <param name="credentialSource">Optional source of the credential (e.g., "static", "delegate", "provider")</param>
        /// <param name="mtlsCertificateIdHashPrefix">Optional prefix of the certificate ID hash (for token binding)</param>
        /// <param name="mtlsCertificateRequested">Whether an mTLS certificate was requested</param>
        /// <param name="resolutionTimeMs">Time taken to resolve the credential in milliseconds</param>
        public CredentialMaterialMetadata(
            AssertionType assertionType,
            string? credentialSource = null,
            string? mtlsCertificateIdHashPrefix = null,
            bool mtlsCertificateRequested = false,
            long resolutionTimeMs = 0)
        {
            AssertionType = assertionType;
            CredentialSource = credentialSource;
            MtlsCertificateIdHashPrefix = mtlsCertificateIdHashPrefix;
            MtlsCertificateRequested = mtlsCertificateRequested;
            ResolutionTimeMs = resolutionTimeMs;
        }

        /// <summary>
        /// Gets the type of assertion/credential used.
        /// </summary>
        public AssertionType AssertionType { get; }

        /// <summary>
        /// Gets the optional source of the credential (e.g., "static", "delegate", "provider").
        /// </summary>
        public string? CredentialSource { get; }

        /// <summary>
        /// Gets the optional prefix of the certificate ID hash used for token cache binding.
        /// This is a short prefix (e.g., first 8 characters) of the hash, safe for telemetry.
        /// </summary>
        public string? MtlsCertificateIdHashPrefix { get; }

        /// <summary>
        /// Gets a value indicating whether an mTLS certificate was requested.
        /// </summary>
        public bool MtlsCertificateRequested { get; }

        /// <summary>
        /// Gets the time taken to resolve the credential in milliseconds.
        /// </summary>
        public long ResolutionTimeMs { get; }
    }
}

#nullable restore
