// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Central authority for invoking <see cref="IClientCredential.GetCredentialMaterialAsync"/>.
    /// Builds a <see cref="CredentialContext"/> from the active request, invokes
    /// <see cref="IClientCredential.GetCredentialMaterialAsync"/> exactly once, and validates
    /// the returned <see cref="CredentialMaterial"/> before handing it back to the
    /// <see cref="TokenClient"/>.
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
        /// <returns><see cref="CredentialMaterial"/> produced by the credential.</returns>
        /// <exception cref="MsalClientException">
        /// Thrown when the credential/mode combination is not supported
        /// (e.g., <see cref="CredentialTransportProtocol.Mtls"/> with a secret credential).
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

            Debug.Assert(material != null, $"Credential '{credential.GetType().Name}' returned null CredentialMaterial.");
            Debug.Assert(material.TokenRequestParameters != null, $"Credential '{credential.GetType().Name}' returned null TokenRequestParameters.");

            requestParams.RequestContext.Logger.Verbose(() => $"[CredentialMaterialResolver] Credential material " +
            $"resolved. HasCertificate={material.ResolvedCertificate != null}");

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
                    ? CredentialTransportProtocol.Mtls
                    : CredentialTransportProtocol.OAuth,
                Claims = requestParams.Claims,
                ClientCapabilities = requestParams.AppConfig.ClientCapabilities,
                CryptographyManager = requestParams.RequestContext.ServiceBundle.PlatformProxy.CryptographyManager,
                SendX5C = requestParams.SendX5C,
                UseSha2 = requestParams.AuthorityManager.Authority.AuthorityInfo.IsSha2CredentialSupported,
                ExtraClientAssertionClaims = requestParams.ExtraClientAssertionClaims,
                ClientAssertionFmiPath = requestParams.ClientAssertionFmiPath,
                Authority = requestParams.AuthorityManager.Authority.AuthorityInfo.CanonicalAuthority?.ToString(),
                TenantId = requestParams.AuthorityManager.Authority.TenantId,
                CorrelationId = requestParams.RequestContext.CorrelationId,
                Logger = requestParams.RequestContext.Logger
            };
        }
    }
}
