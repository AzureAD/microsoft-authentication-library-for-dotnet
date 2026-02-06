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
    /// Client assertion provided as a string JWT. Cannot return TokenBindingCertificate (no mTLS preflight).
    /// </summary>
    internal sealed class ClientAssertionStringDelegateCredential : IClientCredential
    {
        private readonly Func<AssertionRequestOptions, CancellationToken, Task<string>> _provider;

        internal ClientAssertionStringDelegateCredential(
            Func<AssertionRequestOptions, CancellationToken, Task<string>> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public AssertionType AssertionType => AssertionType.ClientAssertion;

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

            string assertion = await _provider(opts, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(assertion))
            {
                throw new MsalClientException(
                    MsalError.InvalidClientAssertion,
                    MsalErrorMessage.InvalidClientAssertionEmpty);
            }

            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
            oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, assertion);

            return ClientCredentialApplicationResult.None;
        }
    }
}
