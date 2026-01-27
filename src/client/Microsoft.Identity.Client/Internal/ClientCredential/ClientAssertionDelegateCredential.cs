// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme.PoP;
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
    internal sealed class ClientAssertionDelegateCredential : IClientCredential
    {
        private readonly Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> _provider;

        internal Task<ClientSignedAssertion> GetAssertionAsync(
                AssertionRequestOptions options,
                CancellationToken cancellationToken) =>
            _provider(options, cancellationToken);

        internal ClientAssertionDelegateCredential(
            Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public AssertionType AssertionType => AssertionType.ClientAssertion;

        // ──────────────────────────────────
        //  Main hook for token requests
        // ──────────────────────────────────
        public async Task AddConfidentialClientParametersAsync(
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

            ClientSignedAssertion resp = await _provider(opts, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(resp?.Assertion))
            {
                throw new MsalClientException(MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            // ──────────────────────────────
            // Decide bearer vs mTLS PoP
            // ───────────────────────────────
            bool useClientAssertionJwtPop = p.UseClientAssertionJwtPop;

            if (useClientAssertionJwtPop)
            {
                oAuth2Client.AddBodyParameter(
                    OAuth2Parameter.ClientAssertionType,
                    OAuth2AssertionType.JwtPop /* constant added in OAuth2AssertionType */);
            }
            else
            {
                oAuth2Client.AddBodyParameter(
                    OAuth2Parameter.ClientAssertionType,
                    OAuth2AssertionType.JwtBearer);
            }

            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, resp.Assertion);
        }
    }
}
