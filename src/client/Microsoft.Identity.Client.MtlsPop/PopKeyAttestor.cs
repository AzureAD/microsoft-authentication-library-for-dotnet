// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.MtlsPop.Attestation;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.MtlsPop
{
    /// <summary>
    /// Static facade for attesting a KeyGuard/CNG key and getting a JWT back.
    /// Key discovery / rotation is the caller's responsibility.
    /// </summary>
    public static class PopKeyAttestor
    {
        /// <summary>
        /// Asynchronously attests a KeyGuard/CNG key with the remote attestation service and returns a JWT.
        /// Wraps the synchronous <see cref="AttestationClient.Attest"/> in a Task.Run so callers can
        /// avoid blocking. Cancellation only applies before the native call starts.
        /// </summary>
        /// <param name="endpoint">Attestation service endpoint (required).</param>
        /// <param name="keyHandle">Valid SafeNCryptKeyHandle (must remain valid for duration of call).</param>
        /// <param name="clientId">Optional client identifier (may be null/empty).</param>
        /// <param name="cancellationToken">Cancellation token (cooperative before scheduling / start).</param>
        public static Task<AttestationResult> AttestKeyGuardAsync(
            string endpoint,
            SafeNCryptKeyHandle keyHandle,
            string clientId,
            CancellationToken cancellationToken = default)
        {
            if (keyHandle is null || keyHandle.IsInvalid)
                throw new ArgumentException("keyHandle must be a valid SafeNCryptKeyHandle", nameof(keyHandle));

            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("endpoint must be provided", nameof(endpoint));

            cancellationToken.ThrowIfCancellationRequested();

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using var client = new AttestationClient();
                    return client.Attest(endpoint, keyHandle, clientId ?? string.Empty);
                }
                catch (Exception ex)
                {
                    // Map any managed exception to AttestationStatus.Exception for consistency.
                    return new AttestationResult(AttestationStatus.Exception, string.Empty, -1, ex.Message);
                }
            }, cancellationToken);
        }
    }
}
