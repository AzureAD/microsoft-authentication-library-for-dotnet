// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Pop
{
    /// <summary>
    /// Call this to obtain the KeyGuard attestation JWT 
    /// </summary>
    internal interface IPopKeyAttestor
    {
        Task<byte[]> AttestAsync(
            SafeNCryptKeyHandle keyHandle,
            string attestationUrl,
            string clientId,
            CancellationToken cancellationToken);
    }
}
