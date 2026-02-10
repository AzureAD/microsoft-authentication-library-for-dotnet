// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Minimal per-request context passed to credentials.
    /// Contains only what credentials need to produce material.
    /// Immutable by design.
    /// </summary>
    internal readonly record struct CredentialRequestContext
    {
        /// <summary>
        /// Application's client ID.
        /// </summary>
        public string ClientId { get; init; }

        /// <summary>
        /// Token endpoint URL (audience for JWT-based credentials).
        /// </summary>
        public string TokenEndpoint { get; init; }

        /// <summary>
        /// Additional claims to include in assertions (if supported by credential).
        /// </summary>
        public string Claims { get; init; }

        /// <summary>
        /// Client capabilities (for client assertion JWT generation).
        /// </summary>
        public IReadOnlyCollection<string> ClientCapabilities { get; init; }

        /// <summary>
        /// True when mTLS proof-of-possession is required.
        /// Constraint: if true, credential MUST return MtlsCertificate.
        /// </summary>
        public bool MtlsRequired { get; init; }

        /// <summary>
        /// Cancellation token for async operations.
        /// </summary>
        public CancellationToken CancellationToken { get; init; }

        /// <summary>
        /// Cryptography manager for signing operations (JWT, etc.).
        /// Required for certificate-based credentials.
        /// </summary>
        public ICryptographyManager CryptographyManager { get; init; }

        /// <summary>
        /// Whether to send X5C header in JWT.
        /// </summary>
        public bool SendX5C { get; init; }

        /// <summary>
        /// Whether the authority supports SHA-2 credentials.
        /// </summary>
        public bool UseSha2 { get; init; }
    }
}
