// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Instance;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Validation context for mTLS constraints used by the orchestrator.
    /// Contains authority and region information for validation.
    /// </summary>
    internal readonly record struct MtlsValidationContext
    {
        /// <summary>
        /// Type of authority (AAD, ADFS, B2C, etc.).
        /// Used to determine mTLS support and constraints.
        /// </summary>
        public AuthorityType AuthorityType { get; init; }

        /// <summary>
        /// Azure region for regional endpoints (required for AAD mTLS PoP).
        /// Null when not using regional endpoints. May be null.
        /// </summary>
        public string AzureRegion { get; init; }
    }
}
