// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.KeyAttestation.Attestation;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.KeyAttestation
{
    /// <summary>
    /// Static facade for attesting a Credential Guard/CNG key and getting a JWT back.
    /// Key discovery / rotation is the caller's responsibility.
    /// </summary>
    internal static class PopKeyAttestor
    {
        /// <summary>
        /// Test hook to inject a mock attestation provider for unit testing.
        /// When set, this delegate is called instead of loading the native DLL.
        /// </summary>
        /// <remarks>
        /// This field is internal and accessible only via InternalsVisibleTo for test assemblies.
        /// Tests should not run in parallel when using this hook to avoid race conditions.
        /// </remarks>
        internal static Func<string, SafeHandle, string, CancellationToken, Task<AttestationResult>> s_testAttestationProvider;
        /// <summary>
        /// Asynchronously attests a Credential Guard/CNG key with the remote attestation service and returns a JWT.
        /// Wraps the synchronous <see cref="AttestationClient.Attest"/> in a Task.Run so callers can
        /// avoid blocking. Cancellation only applies before the native call starts.
        /// </summary>
        /// <param name="endpoint">Attestation service endpoint (required).</param>
        /// <param name="keyHandle">Valid SafeNCryptKeyHandle (must remain valid for duration of call).</param>
        /// <param name="clientId">Optional client identifier (may be null/empty).</param>
        /// <param name="cancellationToken">Cancellation token (cooperative before scheduling / start).</param>
        public static Task<AttestationResult> AttestCredentialGuardAsync(
            string endpoint,
            SafeHandle keyHandle,
            string clientId,
            CancellationToken cancellationToken = default)
        {
            if (keyHandle is null)
                throw new ArgumentNullException(nameof(keyHandle));

            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentNullException(nameof(endpoint));

            if (keyHandle.IsInvalid)
                throw new ArgumentException("keyHandle is invalid", nameof(keyHandle));

            var safeNCryptKeyHandle = keyHandle as SafeNCryptKeyHandle
                ?? throw new ArgumentException("keyHandle must be a SafeNCryptKeyHandle. Only Windows CNG keys are supported.", nameof(keyHandle));

            cancellationToken.ThrowIfCancellationRequested();

            // Check for test provider to avoid loading native DLL in unit tests
            if (s_testAttestationProvider != null)
            {
                return s_testAttestationProvider(endpoint, keyHandle, clientId, cancellationToken);
            }

            return Task.Run(() =>
            {
                try
                {
                    using var client = new AttestationClient();
                    return client.Attest(endpoint, safeNCryptKeyHandle, clientId ?? string.Empty);
                }
                catch (Exception ex)
                {
                    // Map any managed exception to AttestationStatus.Exception for consistency.
                    return new AttestationResult(AttestationStatus.Exception, null, string.Empty, -1, ex.Message);
                }
            }, cancellationToken);
        }
    }
}
