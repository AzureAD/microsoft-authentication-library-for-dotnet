// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
        public Func<OnBeforeTokenRequestData, Task> OnBeforeTokenRequestHandler { get; internal set; }
        public X509Certificate2 MtlsCertificate { get; internal set; }
        public List<string> AdditionalCacheParameters { get; set; }
        public SortedList<string, string> CacheKeyComponents { get; internal set; }
        public string FmiPathSuffix { get; internal set; }
        public string ClientAssertionFmiPath { get; internal set; }
        public bool IsMtlsPopRequested { get; set; }

        internal async Task ValidateAndWireMtlsPopAsync(IServiceBundle serviceBundle, CancellationToken ct)
        {
            if (!IsMtlsPopRequested)
            {
                return; // PoP not requested
            }

            // ────────────────────────────────────
            // Case 1 – Certificate credential
            // ────────────────────────────────────
            if (serviceBundle.Config.ClientCredential is CertificateClientCredential certCred)
            {
                if (certCred.Certificate == null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                return;
            }

            // ────────────────────────────────────
            // Case 2 – Client‑assertion delegate
            // ────────────────────────────────────
            if (serviceBundle.Config.ClientCredential is ClientAssertionDelegateCredential cadc)
            {
                var opts = new AssertionRequestOptions
                {
                    ClientID = serviceBundle.Config.ClientId,
                    ClientCapabilities = serviceBundle.Config.ClientCapabilities,
                    Claims = Claims,
                    CancellationToken = ct
                };

                ClientAssertion ar = await cadc.GetAssertionAsync(opts, ct).ConfigureAwait(false);

                if (ar.TokenBindingCertificate == null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                Wire(ar.TokenBindingCertificate, serviceBundle);
                return;
            }

            // ────────────────────────────────────
            // Case 3 – Any other credential (client‑secret etc.)
            // ────────────────────────────────────
            throw new MsalClientException(
                MsalError.MtlsCertificateNotProvided,
                MsalErrorMessage.MtlsCertificateNotProvidedMessage);
        }

        private void Wire(X509Certificate2 cert, IServiceBundle serviceBundle)
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
    }
}
