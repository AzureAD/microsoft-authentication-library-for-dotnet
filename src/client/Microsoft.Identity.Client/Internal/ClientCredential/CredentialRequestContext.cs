// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Minimal per-request context provided to credential implementations.
    /// Contains only the information credentials need to produce material.
    /// </summary>
    internal readonly record struct CredentialRequestContext
    {
        /// <summary>
        /// Client ID of the application making the request.
        /// </summary>
        public string ClientId { get; init; }

        /// <summary>
        /// Token endpoint URL (used as audience for JWT-based credentials).
        /// </summary>
        public string TokenEndpoint { get; init; }

        /// <summary>
        /// Optional claims to include in the request. May be null.
        /// </summary>
        public string Claims { get; init; }

        /// <summary>
        /// Optional client capabilities to include in the request. May be null.
        /// </summary>
        public IReadOnlyCollection<string> ClientCapabilities { get; init; }

        /// <summary>
        /// True when mTLS proof-of-possession is required for this request.
        /// Credentials that support mTLS must return a certificate when this is true.
        /// </summary>
        public bool MtlsRequired { get; init; }

        /// <summary>
        /// Cancellation token for the request.
        /// </summary>
        public CancellationToken CancellationToken { get; init; }
    }
}
