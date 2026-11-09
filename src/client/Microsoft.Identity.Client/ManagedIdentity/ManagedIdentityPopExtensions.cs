// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
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
        /// With no attestation provider, MSAL selects the configured
        /// Windows RDP listener certificate internally for the non-attested protocol.
        /// The current identity must already have private-key access; MSAL does not create keys or change permissions.
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
        /// PoP options, such as a minimum required binding strength or a caller-owned certificate.
        /// When attestation is required (KeyGuard scenarios), use the Msal.KeyAttestation package
        /// and call .WithAttestationSupport() after this method.
        /// </summary>
        /// <param name="builder">The AcquireTokenForManagedIdentityParameterBuilder instance.</param>
        /// <param name="options">
        /// PoP options controlling the request. Use <see cref="PoPOptions.MinStrength"/> to require
        /// a minimum host binding strength; the request fails with
        /// <see cref="MsalError.MinStrengthNotMet"/> when the host cannot meet the floor.
        /// Set <see cref="PoPOptions.BindingCertificate"/> for the experimental non-attested
        /// protocol with an explicit caller-owned certificate. Without an explicit certificate,
        /// omitting the attestation provider selects the configured RDP certificate internally.
        /// Neither certificate path requires experimental-feature opt-in.
        /// </param>
        /// <returns>The builder to chain .With methods.</returns>
        public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
            this AcquireTokenForManagedIdentityParameterBuilder builder,
            PoPOptions options)
        {
            if (builder == null)
            {
                throw new System.ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new System.ArgumentNullException(nameof(options));
            }

            if (builder.CommonParameters.PreferMsiV2)
            {
                throw new MsalClientException(
                    "mtls_pop_and_bearer_exclusive",
                    "WithMtlsProofOfPossession() and WithRequestOverMtls() are mutually exclusive; call only one on a managed identity request.");
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
            if (options.BindingCertificate is not null)
            {
                ProvidedCertificateManagedIdentitySource.ValidateCertificate(options.BindingCertificate);
            }

            builder.CommonParameters.IsMtlsPopRequested = true;
            builder.CommonParameters.MtlsPopMinStrength = options.MinStrength;
            builder.Parameters.ProvidedBindingCertificate = options.BindingCertificate;
            return builder;
#endif
        }

        /// <summary>
        /// Uses the IMDSv2 attested flow (Credential Guard–issued certificate over mTLS) to acquire
        /// a standard bearer token. The mTLS certificate authenticates the connection to the ESTS
        /// token endpoint, but the returned token carries <c>token_type=bearer</c> and has no
        /// binding certificate in the <see cref="AuthenticationResult"/>.
        /// Requires Windows Credential Guard (VBS) to be enabled on the host.
        /// When attestation is required, call <c>.WithAttestationSupport()</c> (from the
        /// <c>Microsoft.Identity.Client.KeyAttestation</c> package) after this method.
        /// </summary>
        /// <param name="builder">The AcquireTokenForManagedIdentityParameterBuilder instance.</param>
        /// <returns>The builder to chain .With methods.</returns>
        public static AcquireTokenForManagedIdentityParameterBuilder WithRequestOverMtls(
            this AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            if (builder == null)
            {
                throw new System.ArgumentNullException(nameof(builder));
            }

            if (builder.CommonParameters.IsMtlsPopRequested)
            {
                throw new MsalClientException(
                    "mtls_pop_and_bearer_exclusive",
                    "WithMtlsProofOfPossession() and WithRequestOverMtls() are mutually exclusive; call only one on a managed identity request.");
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
            builder.CommonParameters.PreferMsiV2 = true;
            return builder;
#endif
        }
    }
}
