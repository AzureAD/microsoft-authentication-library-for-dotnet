// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    /// <summary>
    /// Central place for validating mTLS Proof‑of‑Possession pre‑conditions
    /// and wiring up <see cref="MtlsPopAuthenticationOperation"/>.
    /// </summary>
    internal static class PopBindingResolver
    {
        /// <summary>
        /// Ensures a certificate is available, region settings are correct,
        /// and populates <see cref="AcquireTokenCommonParameters.AuthenticationOperation"/>
        /// and <see cref="AcquireTokenCommonParameters.MtlsCertificate"/>.
        /// </summary>
        internal static async Task ValidateAndWireAsync(IServiceBundle serviceBundle,
            AcquireTokenCommonParameters commonParameters,
            CancellationToken ct)
        {
            if (!commonParameters.IsPopEnabled)
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
                    Claims = commonParameters.Claims,
                    CancellationToken = ct
                };

                AssertionResponse ar = await cadc.GetAssertionAsync(opts, ct).ConfigureAwait(false);

                if (ar.TokenBindingCertificate == null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                Wire(commonParameters, ar.TokenBindingCertificate, serviceBundle);
                return;
            }

            // ────────────────────────────────────
            // Case 3 – Any other credential (client‑secret etc.)
            // ────────────────────────────────────
            throw new MsalClientException(
                MsalError.MtlsCertificateNotProvided,
                MsalErrorMessage.MtlsCertificateNotProvidedMessage);
        }

        /// <summary>
        /// Common wiring + region check.
        /// </summary>
        private static void Wire(
            AcquireTokenCommonParameters commonParameters,
            X509Certificate2 cert,
            IServiceBundle serviceBundle)
        {
            // Region requirement (AAD only)
            if (serviceBundle.Config.Authority.AuthorityInfo.AuthorityType == AuthorityType.Aad &&
                serviceBundle.Config.AzureRegion == null)
            {
                throw new MsalClientException(
                    MsalError.MtlsPopWithoutRegion,
                    MsalErrorMessage.MtlsPopWithoutRegion);
            }

            commonParameters.AuthenticationOperation = new MtlsPopAuthenticationOperation(cert);
            commonParameters.MtlsCertificate = cert;

            commonParameters.CacheKeyComponents ??= new SortedList<string, string>(StringComparer.Ordinal);

            commonParameters.CacheKeyComponents[Constants.CertSerialNumber] = cert.SerialNumber;

            serviceBundle.Config.CertificateIdToAssociateWithToken = cert.Thumbprint;
        }
    }
}
