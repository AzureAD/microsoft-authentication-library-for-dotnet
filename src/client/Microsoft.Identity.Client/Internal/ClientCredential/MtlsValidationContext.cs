// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Instance;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Constraints for mTLS validation by the orchestrator.
    /// </summary>
    internal readonly record struct MtlsValidationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MtlsValidationContext"/> struct.
        /// </summary>
        /// <param name="authorityType">The type of authority (AAD, ADFS, etc.)</param>
        /// <param name="azureRegion">The optional Azure region</param>
        public MtlsValidationContext(AuthorityType authorityType, string? azureRegion)
        {
            AuthorityType = authorityType;
            AzureRegion = azureRegion;
        }

        /// <summary>
        /// Gets the type of authority.
        /// </summary>
        public AuthorityType AuthorityType { get; }

        /// <summary>
        /// Gets the optional Azure region.
        /// </summary>
        public string? AzureRegion { get; }
    }
}
