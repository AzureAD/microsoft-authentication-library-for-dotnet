// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.AuthScheme.Bearer;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using static Microsoft.Identity.Client.Extensibility.AbstractConfidentialClientAcquireTokenParameterBuilderExtension;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenCommonParameters
    {
        public ApiEvent.ApiIds ApiId { get; set; } = ApiEvent.ApiIds.None;
        public Guid CorrelationId { get; set; }
        public Guid UserProvidedCorrelationId { get; set; }
        public bool UseCorrelationIdFromUser { get; set; }
        public IEnumerable<string> Scopes { get; set; }
        public IDictionary<string, string> ExtraQueryParameters { get; set; }
        public string Claims { get; set; }
        public AuthorityInfo AuthorityOverride { get; set; }
        public IAuthenticationOperation AuthenticationOperation { get; set; } = new BearerAuthenticationOperation();
        public IDictionary<string, string> ExtraHttpHeaders { get; set; }
        public PoPAuthenticationConfiguration PopAuthenticationConfiguration { get; set; }
        public IList<Func<OnBeforeTokenRequestData, Task>> OnBeforeTokenRequestHandler { get; internal set; }
        public X509Certificate2 MtlsCertificate { get; internal set; }
        public List<string> AdditionalCacheParameters { get; set; }
        public SortedList<string, Func<CancellationToken, Task<string>>> CacheKeyComponents { get; internal set; }
        public string FmiPathSuffix { get; internal set; }
        public string ClientAssertionFmiPath { get; internal set; }
        public bool IsMtlsPopRequested { get; set; }
        public string ExtraClientAssertionClaims { get; internal set; }
        internal bool IsEffectiveMtlsPop => IsMtlsPopRequested || MtlsCertificate != null;

        /// <summary>
        /// Optional delegate for obtaining attestation JWT for Credential Guard keys.
        /// Set by the KeyAttestation package via .WithAttestationSupport().
        /// Returns null for non-attested flows.
        /// </summary>
        public Func<string, SafeHandle, string, CancellationToken, Task<string>> AttestationTokenProvider { get; set; }

        /// <summary>
        /// This tries to see if the token request should be done over mTLS or over normal HTTP 
        /// and set the correct parameters
        /// </summary>
        /// <param name="serviceBundle"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="MsalClientException"></exception>
        internal async Task TryInitMtlsPopParametersAsync(IServiceBundle serviceBundle, CancellationToken ct)
        {
            // ─────────────────────────────────────────────────────────────
            // NON-PoP request:
            // We may still need mTLS transport if the client-assertion delegate
            // returns a TokenBindingCertificate (implicit bearer-over-mTLS).
            // This behavior is required by existing unit/integration tests.
            // ─────────────────────────────────────────────────────────────
            if (!IsMtlsPopRequested)
            {
                // If a cert is already known, just enforce policy and return.
                if (MtlsCertificate != null)
                {
                    ThrowIfRegionMissingForImplicitMtls(serviceBundle);
                    return;
                }

                // Only the assertion delegate can dynamically return a token-binding cert.
                if (serviceBundle.Config.ClientCredential is ClientAssertionDelegateCredential cadc)
                {
                    var opts = new AssertionRequestOptions
                    {
                        ClientID = serviceBundle.Config.ClientId,
                        ClientCapabilities = serviceBundle.Config.ClientCapabilities,
                        Claims = Claims,
                        CancellationToken = ct,
                        TokenEndpoint = serviceBundle.Config.Authority.AuthorityInfo.CanonicalAuthority.Authority
                    };

                    ClientSignedAssertion ar = await cadc.GetAssertionAsync(opts, ct).ConfigureAwait(false);

                    if (ar?.TokenBindingCertificate != null)
                    {
                        MtlsCertificate = ar.TokenBindingCertificate;

                        // Implicit bearer-over-mTLS policy check
                        ThrowIfRegionMissingForImplicitMtls(serviceBundle);
                    }
                }

                return; // IMPORTANT: do not run explicit PoP logic
            }

            // ────────────────────────────────────
            // EXPLICIT PoP requested:
            // Validate and initialize PoP parameters (auth scheme + cert + region check).
            // ────────────────────────────────────

            // Case 1 – Certificate credential
            if (serviceBundle.Config.ClientCredential is CertificateClientCredential certCred)
            {
                if (certCred.Certificate == null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                // IMPORTANT: initialize auth scheme + MtlsCertificate
                InitMtlsPopParameters(certCred.Certificate, serviceBundle);
                return;
            }

            // Case 2 – Client-assertion delegate
            if (serviceBundle.Config.ClientCredential is ClientAssertionDelegateCredential cadc2)
            {
                var opts = new AssertionRequestOptions
                {
                    ClientID = serviceBundle.Config.ClientId,
                    ClientCapabilities = serviceBundle.Config.ClientCapabilities,
                    Claims = Claims,
                    CancellationToken = ct
                };

                ClientSignedAssertion ar = await cadc2.GetAssertionAsync(opts, ct).ConfigureAwait(false);

                if (ar?.TokenBindingCertificate == null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                InitMtlsPopParameters(ar.TokenBindingCertificate, serviceBundle);
                return;
            }

            // Case 3 – Any other credential (client-secret etc.)
            throw new MsalClientException(
                MsalError.MtlsCertificateNotProvided,
                MsalErrorMessage.MtlsCertificateNotProvidedMessage);
        }

        private void InitMtlsPopParameters(X509Certificate2 cert, IServiceBundle serviceBundle)
        {
            // region check (AAD only)
            if (serviceBundle.Config.Authority.AuthorityInfo.AuthorityType == AuthorityType.Aad &&
                serviceBundle.Config.AzureRegion == null)
            {
                throw new MsalClientException(MsalError.MtlsPopWithoutRegion, MsalErrorMessage.MtlsPopWithoutRegion);
            }

            AuthenticationOperation = new MtlsPopAuthenticationOperation(cert);
            MtlsCertificate = cert;
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
