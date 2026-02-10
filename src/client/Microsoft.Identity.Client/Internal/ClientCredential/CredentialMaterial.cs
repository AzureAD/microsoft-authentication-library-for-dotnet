// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
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
            X509Certificate2 mtlsCertificate = null,
            CredentialMaterialMetadata metadata = null)
        {
            if (tokenRequestParameters == null)
                throw new ArgumentNullException(nameof(tokenRequestParameters));

            TokenRequestParameters = tokenRequestParameters;
            MtlsCertificate = mtlsCertificate;
            Metadata = metadata;
        }

        /// <summary>
        /// OAuth2 token endpoint authentication parameters (e.g., client_secret, client_assertion).
        /// Never null. Empty dictionary is valid (e.g., for bearer token flows).
        /// </summary>
        public IReadOnlyDictionary<string, string> TokenRequestParameters { get; }

        /// <summary>
        /// Optional X.509 certificate for mTLS proof-of-possession / TLS channel binding.
        /// </summary>
        public X509Certificate2 MtlsCertificate { get; }

        /// <summary>
        /// Optional metadata for telemetry (credential type, source, timing, cert correlation).
        /// Must not contain secrets or PII.
        /// </summary>
        public CredentialMaterialMetadata Metadata { get; }
    }

    /// <summary>
    /// Telemetry-safe metadata about resolved credential material.
    /// Immutable by design.
    /// </summary>
    internal sealed class CredentialMaterialMetadata
    {
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
        /// Type of credential (Secret, Certificate, Assertion, etc.).
        /// </summary>
        public CredentialType CredentialType { get; }

        /// <summary>
        /// Where the credential came from (e.g., "callback", "cert-store", "key-vault").
        /// Must not include sensitive identifiers or URLs.
        /// </summary>
        public string CredentialSource { get; }

        /// <summary>
        /// Hash prefix of mTLS certificate (first 8-16 chars of SHA-256 hash of RawData).
        /// Not full thumbprint; used for correlation only.
        /// </summary>
        public string MtlsCertificateIdHashPrefix { get; }

        /// <summary>
        /// Whether mTLS binding was requested (MtlsRequired in context).
        /// </summary>
        public bool MtlsCertificateRequested { get; }

        /// <summary>
        /// Time to resolve credential material (milliseconds).
        /// Includes all signing/encryption operations.
        /// </summary>
        public long ResolutionTimeMs { get; }
    }

    internal enum CredentialType
    {
        ClientSecret,
        ClientCertificate,
        ClientAssertion,
        FederatedIdentityCredential,
        ManagedIdentity
    }
}
