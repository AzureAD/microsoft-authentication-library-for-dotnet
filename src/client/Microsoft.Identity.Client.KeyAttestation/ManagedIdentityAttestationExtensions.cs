// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Client.KeyAttestation
{
    /// <summary>
    /// Extension methods for enabling KeyGuard attestation support in managed identity mTLS PoP flows.
    /// </summary>
    public static class ManagedIdentityAttestationExtensions
    {
        /// <summary>
        /// Enables KeyGuard attestation support for managed identity mTLS Proof-of-Possession flows.
        /// This method should be called after <see cref="ManagedIdentityPopExtensions.WithMtlsProofOfPossession"/>.
        /// </summary>
        /// <param name="builder">The AcquireTokenForManagedIdentityParameterBuilder instance.</param>
        /// <returns>The builder to chain .With methods.</returns>
        public static AcquireTokenForManagedIdentityParameterBuilder WithAttestationSupport(
            this AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Register the attestation token provider
            return builder.WithAttestationProviderForTests(async (req, ct) =>
            {
                // Get the caller-provided KeyGuard/CNG handle
                var keyHandle = req.KeyHandle;

                if (keyHandle == null)
                {
                    throw new MsalClientException(
                        "attestation_key_handle_missing",
                        "KeyHandle is required for attestation but was not provided.");
                }

                // Call the native interop via PopKeyAttestor
                AttestationResult attestationResult = await PopKeyAttestor.AttestKeyGuardAsync(
                    req.AttestationEndpoint.AbsoluteUri,
                    keyHandle,
                    req.ClientId ?? string.Empty,
                    ct).ConfigureAwait(false);

                // Map to MSAL's internal response
                if (attestationResult != null &&
                    attestationResult.Status == AttestationStatus.Success &&
                    !string.IsNullOrWhiteSpace(attestationResult.Jwt))
                {
                    return new AttestationTokenResponse { AttestationToken = attestationResult.Jwt };
                }

                throw new MsalClientException(
                    "attestation_failure",
                    $"Key Attestation failed " +
                    $"(status={attestationResult?.Status}, " +
                    $"code={attestationResult?.NativeErrorCode}). {attestationResult?.ErrorMessage}");
            });
        }
    }
}
