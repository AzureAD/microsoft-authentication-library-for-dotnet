// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.KeyAttestation
{
    /// <summary>
    /// Extension methods for enabling Credential Guard attestation support in managed identity mTLS PoP flows.
    /// </summary>
    public static class ManagedIdentityAttestationExtensions
    {
        /// <summary>
        /// Enables Credential Guard attestation support for managed identity mTLS Proof-of-Possession flows.
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

            // Set the attestation provider delegate
            builder.CommonParameters.AttestationTokenProvider = async (endpoint, keyHandle, clientId, ct) =>
            {
                var result = await PopKeyAttestor.AttestCredentialGuardAsync(
                    endpoint,
                    keyHandle,
                    clientId,
                    ct).ConfigureAwait(false);

                // Return JWT on success, null for non-attested flow on failure
                return result.Status == Attestation.AttestationStatus.Success ? result.Jwt : null;
            };

            return builder;
        }
    }
}
