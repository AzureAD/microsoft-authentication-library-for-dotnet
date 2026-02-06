// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Minimal per-request input provided to credential providers.
    /// Contains only the information needed to resolve and validate credentials.
    /// </summary>
    internal sealed class CredentialRequestContext
    {
        /// <summary>
        /// The client ID of the application.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The token endpoint URL (used as audience for JWT-based credentials).
        /// </summary>
        public string TokenEndpoint { get; set; }

        /// <summary>
        /// Optional claims to include in client assertions.
        /// </summary>
        public string Claims { get; set; }

        /// <summary>
        /// Optional client capabilities to include in client assertions.
        /// </summary>
        public IReadOnlyCollection<string> ClientCapabilities { get; set; }

        /// <summary>
        /// Indicates whether an mTLS certificate is required for this request.
        /// If true, credential resolution must provide a certificate or fail.
        /// </summary>
        public bool MtlsRequired { get; set; }

        /// <summary>
        /// Cancellation token for async operations.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Cryptography manager for signing operations.
        /// Required for certificate-based credentials that need to sign JWTs.
        /// </summary>
        public ICryptographyManager CryptographyManager { get; set; }

        /// <summary>
        /// Indicates if SHA-256 should be used for signing (vs SHA-1).
        /// </summary>
        public bool UseSha2 { get; set; }

        /// <summary>
        /// Indicates whether to send X5C (certificate chain) in the JWT header.
        /// </summary>
        public bool SendX5C { get; set; }

        /// <summary>
        /// Optional tenant ID for multi-tenant scenarios.
        /// </summary>
        public string TenantId { get; set; }
    }
}
