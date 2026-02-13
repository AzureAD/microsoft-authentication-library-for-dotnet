// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Central resolver for credential material resolution.
    /// Single authority for:
    /// - Invoking credentials (exactly once per logical request)
    /// - Validating material structure
    /// - Enforcing mTLS constraints
    /// </summary>
    internal static class CredentialMaterialResolver
    {
        /// <summary>
        /// Resolves credential material with full validation.
        /// Called exactly once per logical token request.
        /// </summary>
        public static async Task<CredentialMaterial> ResolveAsync(
            IClientCredential credential,
            CredentialContext context,
            CancellationToken cancellationToken)
        {
            if (credential == null)
                throw new ArgumentNullException(nameof(credential));

            // Invoke credential exactly once
            var material = await credential.GetCredentialMaterialAsync(
                context, 
                cancellationToken)
                .ConfigureAwait(false);

            // Validate structure - these are invariant violations
            if (material == null)
            {
                throw new InvalidOperationException(
                    "Credential returned null material.");
            }

            if (material.TokenRequestParameters == null)
            {
                throw new InvalidOperationException(
                    "Credential material TokenRequestParameters cannot be null.");
            }

            // Validate mTLS constraints
            if (context.Mode == ClientAuthMode.MtlsMode)
            {
                if (material.ResolvedCertificate is null)
                {
                    throw new MsalClientException(
                        MsalError.MtlsCertificateNotProvided,
                        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
                }

                // AAD requires region when using mTLS PoP
                if (context.AuthorityType == AuthorityType.Aad &&
                    context.AzureRegion is null)
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
