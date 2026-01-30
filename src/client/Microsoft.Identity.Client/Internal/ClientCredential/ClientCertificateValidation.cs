// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Shared validation utilities for client certificate scenarios.
    /// </summary>
    internal static class ClientCertificateValidation
    {
        /// <summary>
        /// Validates that a certificate is suitable for MTLS Proof-of-Possession.
        /// </summary>
        /// <param name="certificate">The certificate to validate.</param>
        /// <param name="serviceBundle">Service bundle containing configuration.</param>
        /// <exception cref="MsalClientException">
        /// Thrown if the certificate is null or if MTLS PoP is requested for AAD without regional configuration.
        /// </exception>
        public static void ValidateForMtlsPoP(
            X509Certificate2 certificate,
            IServiceBundle serviceBundle)
        {
            // Validate certificate presence
            if (certificate == null)
            {
                throw new MsalClientException(
                    MsalError.MtlsCertificateNotProvided,
                    MsalErrorMessage.MtlsCertificateNotProvidedMessage);
            }

            // Region validation: MTLS PoP for AAD requires regional endpoint configuration
            if (serviceBundle.Config.Authority.AuthorityInfo.AuthorityType == AuthorityType.Aad &&
                serviceBundle.Config.AzureRegion == null)
            {
                throw new MsalClientException(
                    MsalError.MtlsPopWithoutRegion,
                    MsalErrorMessage.MtlsPopWithoutRegion);
            }
        }
    }
}
