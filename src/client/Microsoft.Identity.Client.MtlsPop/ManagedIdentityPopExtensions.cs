// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Internal.Pop;

namespace Microsoft.Identity.Client.MtlsPop
{
    /// <summary>
    /// Builder‑level opt‑in to Mutual‑TLS Proof‑of‑Possession for all
    /// Managed‑Identity token requests produced by this application instance.
    /// </summary>
    /// <remarks>
    /// Requires the <c>Microsoft.Identity.Client.MtlsPop</c> package.<br/>
    /// </remarks>
    public static class ManagedIdentityApplicationPopExtension
    {
        /// <summary>
        /// Enables mTLS POP across the entire <see cref="IManagedIdentityApplication"/>.
        /// </summary>
        public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
                this AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            PopKeyAttestorProvider.Register(static () => new PopKeyAttestor());
            builder.CommonParameters.IsManagedIdentityPopEnabled = true;
            return builder;
        }
    }
}
