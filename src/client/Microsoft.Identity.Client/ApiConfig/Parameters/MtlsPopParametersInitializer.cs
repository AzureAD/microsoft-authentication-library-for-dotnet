// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    /// <summary>
    /// Encapsulates the mTLS/PoP initialization logic for token requests.
    /// Keeps AcquireTokenCommonParameters lean and makes the init logic testable in isolation.
    /// </summary>
    internal static class MtlsPopParametersInitializer
    {
        internal static async Task TryInitAsync(
            AcquireTokenCommonParameters p,
            IServiceBundle serviceBundle,
            CancellationToken ct)
        {
            if (p.IsMtlsPopRequested)
            {
                await InitExplicitMtlsPopAsync(p, serviceBundle, ct).ConfigureAwait(false);
                return;
            }

            await TryInitImplicitBearerOverMtlsAsync(p, serviceBundle, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// NON-PoP request:
        /// We may still need mTLS transport if the credential can return a TokenBindingCertificate.
        /// </summary>
        private static async Task TryInitImplicitBearerOverMtlsAsync(
            AcquireTokenCommonParameters p,
            IServiceBundle serviceBundle,
            CancellationToken ct)
        {
            if (p.MtlsCertificate != null)
            {
                ThrowIfRegionMissingForImplicitMtls(serviceBundle);
                return;
            }

            // Only cert-capable credentials implement this capability interface.
            if (serviceBundle.Config.ClientCredential is IClientSignedAssertionProvider signedProvider)
            {
                var opts = CreateAssertionRequestOptions(p, serviceBundle, ct);

                ClientSignedAssertion ar =
                    await signedProvider.GetAssertionAsync(opts, ct).ConfigureAwait(false);

                if (ar?.TokenBindingCertificate != null)
                {
                    p.MtlsCertificate = ar.TokenBindingCertificate;
                    ThrowIfRegionMissingForImplicitMtls(serviceBundle);
                }
            }
        }

        /// <summary>
        /// EXPLICIT PoP requested:
        /// Validate and initialize PoP parameters (auth scheme + cert + region check).
        /// </summary>
        private static async Task InitExplicitMtlsPopAsync(
            AcquireTokenCommonParameters p,
            IServiceBundle serviceBundle,
            CancellationToken ct)
        {
            // Case 1 – Certificate credential
            if (serviceBundle.Config.ClientCredential is CertificateClientCredential certCred)
            {
                if (certCred.Certificate == null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                InitMtlsPopParameters(p, certCred.Certificate, serviceBundle);
                return;
            }

            // Case 2 – Signed assertion provider (JWT + optional cert)
            if (serviceBundle.Config.ClientCredential is IClientSignedAssertionProvider signedProvider)
            {
                var opts = CreateAssertionRequestOptions(p, serviceBundle, ct);

                ClientSignedAssertion ar =
                    await signedProvider.GetAssertionAsync(opts, ct).ConfigureAwait(false);

                if (ar?.TokenBindingCertificate == null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                InitMtlsPopParameters(p, ar.TokenBindingCertificate, serviceBundle);
                return;
            }

            // Case 3 – Any other credential (client-secret etc.)
            throw new MsalClientException(
                MsalError.MtlsCertificateNotProvided,
                MsalErrorMessage.MtlsCertificateNotProvidedMessage);
        }

        private static AssertionRequestOptions CreateAssertionRequestOptions(
            AcquireTokenCommonParameters p,
            IServiceBundle serviceBundle,
            CancellationToken ct)
        {
            return new AssertionRequestOptions
            {
                ClientID = serviceBundle.Config.ClientId,
                ClientCapabilities = serviceBundle.Config.ClientCapabilities,
                Claims = p.Claims,
                CancellationToken = ct,
                ClientAssertionFmiPath = p.ClientAssertionFmiPath,

                // Best-effort context. IMPORTANT: use AbsoluteUri, not Uri.Authority (host only).
                TokenEndpoint = serviceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority.AbsoluteUri
            };
        }

        private static void InitMtlsPopParameters(
            AcquireTokenCommonParameters p,
            X509Certificate2 cert,
            IServiceBundle serviceBundle)
        {
            // region check (AAD only)
            if (serviceBundle.Config.Authority.AuthorityInfo.AuthorityType == AuthorityType.Aad &&
                serviceBundle.Config.AzureRegion == null)
            {
                throw new MsalClientException(
                    MsalError.MtlsPopWithoutRegion,
                    MsalErrorMessage.MtlsPopWithoutRegion);
            }

            p.AuthenticationOperation = new MtlsPopAuthenticationOperation(cert);
            p.MtlsCertificate = cert;
        }

        private static void ThrowIfRegionMissingForImplicitMtls(IServiceBundle serviceBundle)
        {
            // Implicit bearer-over-mTLS requires region only for AAD
            if (serviceBundle.Config.Authority.AuthorityInfo.AuthorityType == AuthorityType.Aad &&
                serviceBundle.Config.AzureRegion == null)
            {
                throw new MsalClientException(
                    MsalError.MtlsBearerWithoutRegion,
                    MsalErrorMessage.MtlsBearerWithoutRegion);
            }
        }
    }
}
