// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Client.MtlsPop.Attestation
{
    /// <summary>
    /// Produces attestation tokens. Implementations may call native code and/or use a cache.
    /// </summary>
    internal interface IAttestationProvider
    {
        Task<AttestationTokenResponse> GetAsync(AttestationTokenInput input, CancellationToken ct);
    }
}
