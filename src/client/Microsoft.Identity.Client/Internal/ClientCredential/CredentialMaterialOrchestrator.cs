// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Single authority for credential material resolution.
    /// Ensures credentials are invoked exactly once per request and validates constraints.
    /// </summary>
    internal static class CredentialMaterialOrchestrator
    {
        /// <summary>
        /// Resolves credential material from a client credential with validation.
        /// This is the single entry point for credential resolution in the token request pipeline.
        /// </summary>
        /// <param name="credential">The client credential to resolve</param>
        /// <param name="requestContext">The per-request context</param>
        /// <param name="mtlsValidationContext">The mTLS validation constraints</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The resolved credential material</returns>
        /// <exception cref="MsalClientException">Thrown if validation fails</exception>
        public static async Task<CredentialMaterial> ResolveAsync(
            IClientCredential credential,
            CredentialRequestContext requestContext,
            MtlsValidationContext mtlsValidationContext,
            CancellationToken cancellationToken)
        {
            // Single invocation - this is the ONLY place credentials are called
            var material = await credential.GetCredentialMaterialAsync(requestContext, cancellationToken)
                .ConfigureAwait(false);

            // Validate: if mTLS is required, certificate must be provided
            if (requestContext.MtlsRequired)
            {
                if (material.MtlsCertificate is null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                // Validate: AAD + mTLS requires region
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
