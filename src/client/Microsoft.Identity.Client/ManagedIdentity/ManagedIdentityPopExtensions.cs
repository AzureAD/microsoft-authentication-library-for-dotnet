// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extension methods for enabling mTLS Proof-of-Possession in managed identity flows.
    /// </summary>
    public static class ManagedIdentityPopExtensions
    {
        /// <summary>
        /// Enables mTLS Proof-of-Possession for managed identity token acquisition.
        /// When attestation is required (KeyGuard scenarios), use the Msal.KeyAttestation package
        /// and call .WithAttestationSupport() after this method.
        /// </summary>
        /// <param name="builder">The AcquireTokenForManagedIdentityParameterBuilder instance.</param>
        /// <returns>The builder to chain .With methods.</returns>
        public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
            this AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            return builder.WithMtlsProofOfPossession(new PoPOptions());
        }

        /// <summary>
        /// Enables mTLS Proof-of-Possession for managed identity token acquisition with additional
        /// PoP options, such as a minimum required binding strength.
        /// When attestation is required (KeyGuard scenarios), use the Msal.KeyAttestation package
        /// and call .WithAttestationSupport() after this method.
        /// </summary>
        /// <param name="builder">The AcquireTokenForManagedIdentityParameterBuilder instance.</param>
        /// <param name="options">
        /// PoP options controlling the request. Use <see cref="PoPOptions.MinStrength"/> to require
        /// a minimum host binding strength; the request fails with
        /// <see cref="MsalError.MinStrengthNotMet"/> when the host cannot meet the floor.
        /// </param>
        /// <returns>The builder to chain .With methods.</returns>
        public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
            this AcquireTokenForManagedIdentityParameterBuilder builder,
            PoPOptions options)
        {
            if (options == null)
            {
                throw new System.ArgumentNullException(nameof(options));
            }

            if (!DesktopOsHelper.IsWindows())
            {
                throw new MsalClientException(
                    MsalError.MtlsNotSupportedForManagedIdentity,
                    MsalErrorMessage.MtlsNotSupportedForNonWindowsMessage);
            }

#if NET462
            throw new MsalClientException(
                MsalError.MtlsNotSupportedForManagedIdentity,
                MsalErrorMessage.MtlsNotSupportedForManagedIdentityMessage);
#else
            builder.CommonParameters.IsMtlsPopRequested = true;
            builder.CommonParameters.MtlsPopMinStrength = options.MinStrength;
            return builder;
#endif
        }
    }
}
