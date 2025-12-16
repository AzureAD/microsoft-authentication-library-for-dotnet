// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.KeyAttestation.Attestation;
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

            // Ensure the provider is registered (triggers static constructor on older frameworks)
            Attestation.AttestationProviderInitializer.Initialize();

            // Set the flag to enable attestation
            builder.CommonParameters.IsAttestationRequested = true;
            return builder;
        }
    }
}
