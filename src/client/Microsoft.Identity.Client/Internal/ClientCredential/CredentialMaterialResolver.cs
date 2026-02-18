// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Central resolver that invokes credentials exactly once per request
    /// and validates the resulting credential material structure.
    /// </summary>
    internal static class CredentialMaterialResolver
    {
        /// <summary>
        /// Resolves credential material from a client credential.
        /// Ensures the credential is invoked exactly once and validates the output.
        /// </summary>
        /// <param name="credential">The client credential to resolve.</param>
        /// <param name="context">The credential context with all necessary parameters.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The resolved credential material.</returns>
        public static async Task<CredentialMaterial> ResolveAsync(
            IClientCredential credential,
            CredentialContext context,
            ILoggerAdapter logger,
            CancellationToken cancellationToken)
        {
            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            logger?.Verbose(() => $"[CredentialMaterialResolver] Resolving credential of type {credential.GetType().Name}");

            // Invoke the credential exactly once
            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(context, cancellationToken)
                .ConfigureAwait(false);

            // Validate the material
            ValidateMaterial(material, credential);

            logger?.Verbose(() => $"[CredentialMaterialResolver] Successfully resolved credential material");

            return material;
        }

        private static void ValidateMaterial(CredentialMaterial material, IClientCredential credential)
        {
            if (material == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    $"Credential {credential.GetType().Name} returned null CredentialMaterial");
            }

            if (material.TokenRequestParameters == null)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    $"Credential {credential.GetType().Name} returned null TokenRequestParameters");
            }

            // Validate that essential parameters are present
            bool hasClientSecret = material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientSecret);
            bool hasClientAssertion = material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertion);

            if (!hasClientSecret && !hasClientAssertion)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    $"Credential {credential.GetType().Name} must provide either client_secret or client_assertion");
            }

            if (hasClientSecret && hasClientAssertion)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    $"Credential {credential.GetType().Name} must not provide both client_secret and client_assertion");
            }

            // If client_assertion is present, client_assertion_type must also be present
            if (hasClientAssertion && !material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertionType))
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    $"Credential {credential.GetType().Name} provided client_assertion without client_assertion_type");
            }
        }
    }
}
