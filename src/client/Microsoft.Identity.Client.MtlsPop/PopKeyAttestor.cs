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
    public sealed class PopKeyAttestor : IPopKeyAttestor
    {
        /// <summary>
        /// Attest the key against the MAA endpoint and return the JWT.
        /// </summary>
        /// <param name="keyHandle"></param>
        /// <param name="attestationUrl"></param>
        /// <param name="clientId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<byte[]> AttestAsync(
        SafeNCryptKeyHandle keyHandle,
        string attestationUrl,
        string clientId,
        CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                using var ac = new AttestationClient();
                var res = ac.Attest(attestationUrl, keyHandle, clientId);

                return Task.FromResult(
                    res.Status == AttestationStatus.Success && !string.IsNullOrEmpty(res.Jwt)
                        ? Encoding.UTF8.GetBytes(res.Jwt)
                        : Array.Empty<byte>());
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                return Task.FromResult(Array.Empty<byte>());
            }
        }
    }
}
