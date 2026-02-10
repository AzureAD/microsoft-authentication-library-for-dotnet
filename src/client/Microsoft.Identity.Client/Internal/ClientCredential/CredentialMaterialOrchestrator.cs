// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Orchestrates credential material resolution with validation and constraint enforcement.
    /// This is the single authority for calling credential implementations during token requests.
    /// </summary>
    internal static class CredentialMaterialOrchestrator
    {
        /// <summary>
        /// Resolves credential material with validation and constraint enforcement.
        /// Called exactly once per logical token request (not per retry).
        /// </summary>
        /// <param name="credential">The credential implementation to resolve.</param>
        /// <param name="requestContext">Minimal per-request context for credential resolution.</param>
        /// <param name="mtlsValidationContext">Authority and region context for mTLS validation.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Validated credential material.</returns>
        /// <exception cref="MsalClientException">
        /// Thrown when:
        /// - Material is null
        /// - TokenRequestParameters is null
        /// - Reserved parameters are present
        /// - mTLS certificate is required but not provided
        /// - AAD authority with mTLS requires a region
        /// </exception>
        public static async Task<CredentialMaterial> ResolveAsync(
            IClientCredential credential,
            CredentialRequestContext requestContext,
            MtlsValidationContext mtlsValidationContext,
            CancellationToken cancellationToken)
        {
            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            var sw = Stopwatch.StartNew();

            // Step 1: Call credential exactly once
            var material = await credential.GetCredentialMaterialAsync(requestContext, cancellationToken)
                .ConfigureAwait(false);

            sw.Stop();

            // Step 2: Validate structure
            if (material == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    "Credential returned null material. Credentials must return a valid CredentialMaterial instance.");
            }

            if (material.TokenRequestParameters == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    "TokenRequestParameters cannot be null. Return an empty dictionary if no parameters are needed.");
            }

            // Step 3: Validate no reserved keys
            CredentialMaterialHelper.ValidateTokenParametersNoReservedKeys(material.TokenRequestParameters);

            // Step 4: Validate mTLS constraints
            if (requestContext.MtlsRequired)
            {
                // mTLS PoP requires a certificate
                if (material.MtlsCertificate is null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                // AAD mTLS PoP requires a region
                if (mtlsValidationContext.AuthorityType == AuthorityType.Aad &&
                    mtlsValidationContext.AzureRegion is null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsPopWithoutRegion,
                        MsalErrorMessage.MtlsPopWithoutRegion);
                }
            }

            return material;
        }
    }
}
