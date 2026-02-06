// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Handles client assertions supplied via a delegate that returns an
    /// <see cref="ClientSignedAssertion"/> (JWT + optional certificate bound for mTLS‑PoP).
    /// </summary>
    internal sealed class ClientAssertionDelegateCredential : IClientCredential, IClientSignedAssertionProvider
    {
        private readonly Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> _provider;

        internal ClientAssertionDelegateCredential(
            Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        // Private helper for internal readability
        private Task<ClientSignedAssertion> GetAssertionAsync(
            AssertionRequestOptions options,
            CancellationToken cancellationToken) =>
            _provider(options, cancellationToken);

        // Capability interface (only used where we intentionally cast to check the capability)
        Task<ClientSignedAssertion> IClientSignedAssertionProvider.GetAssertionAsync(
            AssertionRequestOptions options,
            CancellationToken cancellationToken) =>
            GetAssertionAsync(options, cancellationToken);

        public AssertionType AssertionType => AssertionType.ClientAssertion;

        // ──────────────────────────────────
        //  Main hook for token requests
        // ──────────────────────────────────
        public async Task<ClientCredentialApplicationResult> AddConfidentialClientParametersAsync(
            OAuth2Client oAuth2Client,
            AuthenticationRequestParameters p,
            ICryptographyManager _,
            string tokenEndpoint,
            CancellationToken ct)
        {
            var opts = new AssertionRequestOptions
            {
                CancellationToken = ct,
                ClientID = p.AppConfig.ClientId,
                TokenEndpoint = tokenEndpoint,
                ClientCapabilities = p.RequestContext.ServiceBundle.Config.ClientCapabilities,
                Claims = p.Claims,
                ClientAssertionFmiPath = p.ClientAssertionFmiPath
            };

            ClientSignedAssertion resp = await GetAssertionAsync(opts, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(resp?.Assertion))
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            bool hasCert = resp.TokenBindingCertificate != null;

            // If PoP was explicitly requested, we must have a certificate.
            // (Preflight should enforce this too, but keep this defensive.)
            if (p.IsMtlsPopRequested && !hasCert)
            {
                throw new MsalClientException(
                    MsalError.MtlsCertificateNotProvided,
                    MsalErrorMessage.MtlsCertificateNotProvidedMessage);
            }

            // JWT-PoP if explicit PoP was requested OR delegate returned a cert (implicit bearer-over-mTLS)
            bool useJwtPop = p.IsMtlsPopRequested || hasCert;

            oAuth2Client.AddBodyParameter(
                OAuth2Parameter.ClientAssertionType,
                useJwtPop ? OAuth2AssertionType.JwtPop : OAuth2AssertionType.JwtBearer);

            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, resp.Assertion);

            // Only return a cert if we actually have one.
            return hasCert
                ? new ClientCredentialApplicationResult(useJwtPopClientAssertion: useJwtPop, mtlsCertificate: resp.TokenBindingCertificate)
                : ClientCredentialApplicationResult.None;
        }
    }
}
