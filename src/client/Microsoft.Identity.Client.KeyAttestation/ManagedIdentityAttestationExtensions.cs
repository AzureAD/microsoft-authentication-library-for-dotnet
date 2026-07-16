// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.KeyAttestation.Attestation;

namespace Microsoft.Identity.Client.KeyAttestation
{
    /// <summary>
    /// Extension methods for enabling Credential Guard attestation support in managed identity mTLS PoP flows.
    /// </summary>
    public static class ManagedIdentityAttestationExtensions
    {
        // Error code surfaced when Credential Guard / KeyGuard attestation is requested but fails.
        // The failure originates from the attestation (MAA) service, so it is surfaced as a service error.
        // Matches the error code used by the IMDSv2 consumer so callers see a single, consistent code.
        private const string AttestationFailedErrorCode = "attestation_failed";

        /// <summary>
        /// Enables Credential Guard attestation support for managed identity mTLS Proof-of-Possession flows.
        /// This method should be called after <see cref="ManagedIdentityPopExtensions.WithMtlsProofOfPossession(AcquireTokenForManagedIdentityParameterBuilder)"/>.
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

            builder.CommonParameters.AttestationTokenProvider = async (endpoint, keyHandle, clientId, keyId, logger, ct) =>
            {
                AttestationResult result = await PopKeyAttestor.AttestCredentialGuardAsync(
                    endpoint,
                    keyHandle,
                    clientId,
                    keyId,
                    logger,
                    ct).ConfigureAwait(false);

                if (result.Status == AttestationStatus.Success && !string.IsNullOrWhiteSpace(result.Jwt))
                {
                    return result.Jwt;
                }

                // Attestation failed. Surface the reason to the caller instead of returning null —
                // returning null would cause an empty/non-attested certificate request to be sent to
                // IMDS, silently dropping the real failure (e.g. an MAA policy-evaluation deny). The
                // failure originates from the attestation (MAA) service, so it is a service exception.
                string reason = string.IsNullOrEmpty(result.ErrorMessage)
                    ? "(no additional detail available)"
                    : result.ErrorMessage;

                throw new MsalServiceException(
                    AttestationFailedErrorCode,
                    $"Key Guard attestation failed; no attestation token was produced. " +
                    $"Status: {result.Status}, NativeErrorCode: {result.NativeErrorCode}, Reason: {reason}");
            };

            return builder;
        }
    }
}
