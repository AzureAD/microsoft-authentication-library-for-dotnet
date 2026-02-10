// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Validation constraints for mTLS scenarios.
    /// Used by orchestrator to enforce environment rules.
    /// </summary>
    internal readonly record struct MtlsValidationContext
    {
        /// <summary>
        /// Authority type (AAD, B2C, ADFS, etc.).
        /// Some authority types have mTLS restrictions.
        /// </summary>
        public AuthorityType AuthorityType { get; init; }

        /// <summary>
        /// Azure region configured for this client.
        /// Required when using mTLS with AAD.
        /// </summary>
        public string AzureRegion { get; init; }
    }
}
