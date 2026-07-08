// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Abstraction for the IMDSv2 mTLS Proof-of-Possession binding source. IMDSv2 mints (or reuses)
    /// an mTLS binding (certificate + ESTS-R endpoint + canonical client_id) and then delegates the
    /// token leg to MSAL's internal <see cref="OAuth2.TokenClient"/> exchange. Because IMDSv2 does not
    /// use the <see cref="AbstractManagedIdentity"/> token template, it is modeled by this focused
    /// interface instead of inheriting that base.
    /// </summary>
    internal interface IImdsV2MtlsBindingSource
    {
        /// <summary>
        /// Performs the cert-mint flow and returns the resulting mTLS binding so the caller can
        /// delegate the token leg to the internal exchange path.
        /// </summary>
        /// <param name="parameters">The managed identity request parameters (carries the attestation provider).</param>
        /// <param name="forceRemint">When <c>true</c>, evicts any cached binding certificate and mints a fresh one.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task<MtlsBindingInfo> AcquireMtlsBindingForDelegationAsync(
            AcquireTokenForManagedIdentityParameters parameters,
            bool forceRemint,
            CancellationToken cancellationToken);
    }
}
