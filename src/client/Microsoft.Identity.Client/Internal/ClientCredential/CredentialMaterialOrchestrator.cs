// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Central orchestrator for credential material resolution.
    /// Single authority for:
    /// - Invoking credentials (exactly once per logical request)
    /// - Validating material structure
    /// - Enforcing mTLS constraints
    /// </summary>
    internal static class CredentialMaterialOrchestrator
    {
        /// <summary>
        /// Resolves credential material with full validation.
        /// Called exactly once per logical token request.
        /// </summary>
        public static async Task<CredentialMaterial> ResolveAsync(
            IClientCredential credential,
            CredentialRequestContext requestContext,
            MtlsValidationContext mtlsValidationContext,
            CancellationToken cancellationToken)
        {
            if (credential == null)
                throw new ArgumentNullException(nameof(credential));

            var sw = Stopwatch.StartNew();

            // Invoke credential exactly once
            var material = await credential.GetCredentialMaterialAsync(
                requestContext, 
                cancellationToken)
                .ConfigureAwait(false);

            sw.Stop();

            // Validate structure
            if (material == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    "Credential returned null material.");
            }

            if (material.TokenRequestParameters == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    "Credential material TokenRequestParameters cannot be null.");
            }

            // Validate no reserved key collisions
            CredentialMaterialHelper.ValidateTokenParametersNoReservedKeys(
                material.TokenRequestParameters);

            // Validate mTLS constraints
            if (requestContext.MtlsRequired)
            {
                if (material.MtlsCertificate is null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                // AAD requires region when using mTLS PoP
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
