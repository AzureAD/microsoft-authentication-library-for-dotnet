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
    /// Key discovery / rotation is the caller's responsibility.
    /// </summary>
    public static class PopKeyAttestor
    {
        /// <summary>
        /// Asynchronously attests a KeyGuard/CNG key with the remote attestation service and returns a JWT.
        /// This is a synchronous native call executed on the thread pool; cancellation only applies
        /// before the operation starts (the native call cannot be cancelled mid-flight).
        /// </summary>
        /// <param name="keyHandle">Valid SafeNCryptKeyHandle (must remain valid for duration of call).</param>
        /// <param name="endpoint">Attestation service endpoint (required).</param>
        /// <param name="authToken">Optional authorization token.</param>
        /// <param name="clientPayload">Optional opaque client payload sent to the service.</param>
        /// <param name="clientId">Optional client identifier.</param>
        /// <param name="cancellationToken">Cancellation token (cooperative before native call starts).</param>
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

            // Fast path cancellation before scheduling.
            cancellationToken.ThrowIfCancellationRequested();

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var client = new AttestationClient(); // ensures native lib is loaded / initialized
                IntPtr tokenPtr = IntPtr.Zero;
                bool addRef = false;

                try
                {
                    // Protect handle from being closed while native code runs.
                    keyHandle.DangerousAddRef(ref addRef);

                    int rc = AttestationClientLib.AttestKeyGuardImportKey(
                        endpoint,
                        authToken ?? string.Empty,
                        clientPayload ?? string.Empty,
                        keyHandle,
                        out tokenPtr,
                        clientId ?? string.Empty);

                    if (rc != 0)
                    {
                        var error = AttestationErrors.Describe((AttestationResultErrorCode)rc);
                        return new AttestationResult(AttestationStatus.NativeError, string.Empty, rc, error);
                    }

                    if (tokenPtr == IntPtr.Zero)
                    {
                        return new AttestationResult(
                            AttestationStatus.TokenEmpty,
                            string.Empty,
                            rc,
                            "Native call succeeded (rc==0) but token pointer was null.");
                    }

                    string jwt = Marshal.PtrToStringAnsi(tokenPtr) ?? string.Empty;
                    if (jwt.Length == 0)
                    {
                        return new AttestationResult(
                            AttestationStatus.TokenEmpty,
                            string.Empty,
                            rc,
                            "JWT string was empty.");
                    }

                    return new AttestationResult(AttestationStatus.Success, jwt, rc, string.Empty);
                }
                catch (Exception ex)
                {
                    return new AttestationResult(AttestationStatus.Exception, string.Empty, -1, ex.Message);
                }
                finally
                {
                    if (tokenPtr != IntPtr.Zero)
                    {
                        AttestationClientLib.FreeAttestationToken(tokenPtr);
                    }

                    if (addRef)
                    {
                        keyHandle.DangerousRelease();
                    }
                }
            }, cancellationToken);
        }
    }
}
