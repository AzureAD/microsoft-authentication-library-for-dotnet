// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.MtlsPop.Attestation;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.MtlsPop
{
    /// <summary>
    /// Static facade for attesting a KeyGuard/CNG key and getting a JWT back.
    /// All key management (KeyGuard discovery/creation/rotation) lives outside this package.
    /// </summary>
    public static class PopKeyAttestor
    {
        /// <summary>
        /// Attests a KeyGuard/CNG key with the remote attestation service and returns a JWT.
        /// The <paramref name="keyHandle"/> must be a valid SafeNCryptKeyHandle (e.g., from KeyGuard).
        /// </summary>
        public static Task<AttestationResult> AttestKeyGuardAsync(
            SafeNCryptKeyHandle keyHandle,
            string endpoint,
            string authToken,
            string clientPayload,
            string clientId,
            CancellationToken cancellationToken = default)
        {
            if (keyHandle is null || keyHandle.IsInvalid)
                throw new ArgumentException("keyHandle must be a valid SafeNCryptKeyHandle", nameof(keyHandle));
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("endpoint must be provided", nameof(endpoint));

            return Task.Run(() =>
            {
                using var client = new AttestationClient(); // ensures native lib is loaded/initialized
                IntPtr tokenPtr = IntPtr.Zero;
                try
                {
                    // Native call (returns 0 on success; non-zero error codes mapped to AttestationResultErrorCode)
                    int rc = AttestationClientLib.AttestKeyGuardImportKey(
                        endpoint,
                        authToken ?? string.Empty,
                        clientPayload ?? string.Empty,
                        keyHandle,
                        out tokenPtr,
                        clientId ?? string.Empty);

                    if (rc == 0)
                    {
                        // ANSI string from native; FreeAttestationToken must be called
                        string jwt = Marshal.PtrToStringAnsi(tokenPtr) ?? string.Empty;
                        return new AttestationResult(AttestationStatus.Success, jwt, rc, string.Empty);
                    }

                    var error = AttestationErrors.Describe((AttestationResultErrorCode)rc);
                    return new AttestationResult(AttestationStatus.Exception, string.Empty, rc, error);
                }
                finally
                {
                    if (tokenPtr != IntPtr.Zero)
                    {
                        AttestationClientLib.FreeAttestationToken(tokenPtr);
                    }
                }
            }, cancellationToken);
        }
    }
}
