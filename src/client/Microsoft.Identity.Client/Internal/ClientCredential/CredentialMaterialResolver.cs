// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Central authority for credential invocation.
    /// Builds a <see cref="CredentialContext"/> from the active request, invokes the credential
    /// exactly once, and validates the returned <see cref="CredentialMaterial"/> before handing
    /// it back to the <see cref="TokenClient"/>.
    /// </summary>
    internal static class CredentialMaterialResolver
    {
        /// <summary>
        /// Resolves credential material for the given request.
        /// </summary>
        /// <param name="credential">The credential implementation to invoke.</param>
        /// <param name="requestParams">Current authentication request parameters.</param>
        /// <param name="tokenEndpoint">Resolved token endpoint URL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validated <see cref="CredentialMaterial"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the credential returns <see langword="null"/> or when
        /// <see cref="CredentialMaterial.TokenRequestParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="MsalClientException">
        /// Thrown when the credential/mode combination is not supported
        /// (e.g., <see cref="ClientAuthMode.MtlsMode"/> with a secret credential).
        /// </exception>
        internal static async Task<CredentialMaterial> ResolveAsync(
            IClientCredential credential,
            AuthenticationRequestParameters requestParams,
            string tokenEndpoint,
            CancellationToken cancellationToken)
        {
            var context = BuildContext(requestParams, tokenEndpoint);

            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(context, cancellationToken)
                .ConfigureAwait(false);

            if (material == null)
            {
                throw new InvalidOperationException(
                    $"Credential '{credential.GetType().Name}' returned null from GetCredentialMaterialAsync. " +
                    "This is an internal error; credential implementations must never return null.");
            }

            // TokenRequestParameters is validated inside CredentialMaterial's constructor,
            // but add an explicit guard here to surface a clear message if a future refactor
            // allows a null reference to slip through.
            if (material.TokenRequestParameters == null)
            {
                throw new InvalidOperationException(
                    $"Credential '{credential.GetType().Name}' returned CredentialMaterial with null " +
                    "TokenRequestParameters. TokenRequestParameters must not be null.");
            }

            return material;
        }

        private static CredentialContext BuildContext(
            AuthenticationRequestParameters requestParams,
            string tokenEndpoint)
        {
            return new CredentialContext
            {
                ClientId = requestParams.AppConfig.ClientId,
                TokenEndpoint = tokenEndpoint,
                Mode = requestParams.MtlsCertificate != null || requestParams.IsMtlsPopRequested
                    ? ClientAuthMode.MtlsMode
                    : ClientAuthMode.Regular,
                Claims = requestParams.Claims,
                ClientCapabilities = requestParams.AppConfig.ClientCapabilities,
                CryptographyManager = requestParams.RequestContext.ServiceBundle.PlatformProxy.CryptographyManager,
                SendX5C = requestParams.SendX5C,
                UseSha2 = requestParams.AuthorityManager.Authority.AuthorityInfo.IsSha2CredentialSupported,
                ExtraClientAssertionClaims = requestParams.ExtraClientAssertionClaims,
                ClientAssertionFmiPath = requestParams.ClientAssertionFmiPath,
                AuthorityType = requestParams.AppConfig.Authority.AuthorityInfo.AuthorityType,
                AzureRegion = requestParams.AppConfig.AzureRegion
            };
        }
    }
}
