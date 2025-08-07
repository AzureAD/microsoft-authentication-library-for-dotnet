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
    /// <see cref="AssertionResponse"/> (JWT + optional certificate bound for mTLS‑PoP).
    /// </summary>
    internal sealed class ClientAssertionDelegateCredential : IClientCredential
    {
        private readonly Func<AssertionRequestOptions, CancellationToken, Task<AssertionResponse>> _provider;

        internal Task<AssertionResponse> GetAssertionAsync(
                AssertionRequestOptions options,
                CancellationToken cancellationToken) =>
            _provider(options, cancellationToken);

        public ClientAssertionDelegateCredential(
            Func<AssertionRequestOptions, CancellationToken, Task<AssertionResponse>> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public AssertionType AssertionType => AssertionType.ClientAssertion;

        // ──────────────────────────────────
        //  Expose the certificate we used in the *last* call
        // ──────────────────────────────────
        private X509Certificate2 _lastCertificate;
        internal X509Certificate2 LastCertificate => _lastCertificate;

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

            AssertionResponse resp = await _provider(opts, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(resp?.Assertion))
            {
                throw new MsalClientException(MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            // Decide bearer vs PoP
            bool popEnabled = p.IsPopEnabled;

            if (popEnabled && resp.TokenBindingCertificate != null)
            {
                oAuth2Client.AddBodyParameter(
                    OAuth2Parameter.ClientAssertionType,
                    OAuth2AssertionType.JwtPop /* constant added in OAuth2AssertionType */);

                _lastCertificate = resp.TokenBindingCertificate;
            }
            else
            {
                oAuth2Client.AddBodyParameter(
                    OAuth2Parameter.ClientAssertionType,
                    OAuth2AssertionType.JwtBearer);

                _lastCertificate = null;
            }

            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, resp.Assertion);
        }
    }
}
