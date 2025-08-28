// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Ignore Spelling: Attestor

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Pop;
using Microsoft.Identity.Client.MtlsPop.Attestation;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.MtlsPop
{
    /// <summary>
    /// Real attestor implemented in the POP plug‑in.  Core discovers it via
    /// the static‑ctor registration below.
    /// </summary>
    public sealed class PopKeyAttestor : IManagedIdentityKeyProvider
    {
        Task<KeyInfo> IManagedIdentityKeyProvider.GetOrCreateKeyAsync(KeyRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
