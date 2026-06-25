// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsBearerToken(
            this AcquireTokenForManagedIdentityParameterBuilder builder)
        {
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
            builder.CommonParameters.IsMtlsBearerRequested = true;
            return builder;
#endif
        }
    }
}
