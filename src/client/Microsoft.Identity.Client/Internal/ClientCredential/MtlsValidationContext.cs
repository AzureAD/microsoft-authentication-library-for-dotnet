// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Instance;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Context for orchestrator-level mTLS validation constraints.
    /// Contains information about the authority type and Azure region requirements.
    /// </summary>
    internal sealed class MtlsValidationContext
    {
        /// <summary>
        /// The type of authority being used (AAD, ADFS, B2C, etc.).
        /// </summary>
        public AuthorityType AuthorityType { get; set; }

        /// <summary>
        /// The Azure region for AAD regional endpoints.
        /// Required when AuthorityType is AAD and mTLS PoP is being used.
        /// </summary>
        public string AzureRegion { get; set; }
    }
}
