// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Internal interface for attestation providers. 
    /// The KeyAttestation package implements this to provide attestation functionality.
    /// </summary>
    internal interface IAttestationProvider
    {
        /// <summary>
        /// Attests a KeyGuard key and returns an attestation JWT.
        /// </summary>
        /// <param name="attestationEndpoint">The attestation endpoint URL.</param>
        /// <param name="keyHandle">The KeyGuard key handle (must be SafeNCryptKeyHandle).</param>
        /// <param name="clientId">The client ID to include in the attestation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An attestation result containing the JWT or error information.</returns>
        Task<AttestationResult> AttestKeyGuardAsync(
            string attestationEndpoint,
            SafeHandle keyHandle,
            string clientId,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Result of an attestation operation.
    /// </summary>
    internal class AttestationResult
    {
        public AttestationStatus Status { get; set; }
        public string Jwt { get; set; }
        public string ErrorMessage { get; set; }
        public int NativeErrorCode { get; set; }
    }

    /// <summary>
    /// Status codes for attestation operations.
    /// </summary>
    internal enum AttestationStatus
    {
        Success = 0,
        Failed = 1
    }
}
