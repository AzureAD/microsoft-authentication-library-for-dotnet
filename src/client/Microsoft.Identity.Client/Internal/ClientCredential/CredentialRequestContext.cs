// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Minimal per-request context passed to credential resolution.
    /// Does NOT contain crypto config or signing dependencies (those stay in credential constructors).
    /// </summary>
    internal readonly record struct CredentialRequestContext
    {
        /// <summary>
        /// Gets the client ID.
        /// </summary>
        public string ClientId { get; init; }

        /// <summary>
        /// Gets the token endpoint URL.
        /// </summary>
        public string TokenEndpoint { get; init; }

        /// <summary>
        /// Gets the optional claims to include in the request.
        /// </summary>
        public string? Claims { get; init; }

        /// <summary>
        /// Gets the optional client capabilities.
        /// </summary>
        public IReadOnlyCollection<string>? ClientCapabilities { get; init; }

        /// <summary>
        /// Gets a value indicating whether mTLS PoP is required for this request.
        /// </summary>
        public bool MtlsRequired { get; init; }

        /// <summary>
        /// Gets the cancellation token for the operation.
        /// </summary>
        public CancellationToken CancellationToken { get; init; }
    }
}
