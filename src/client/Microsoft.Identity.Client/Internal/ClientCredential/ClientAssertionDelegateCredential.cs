// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Handles client assertions supplied via a delegate that returns
    /// <see cref="AssertionResponse"/> (JWT + optional certificate).
    /// </summary>
    internal sealed class ClientAssertionDelegateCredential : IClientCredential
    {
        private readonly Func<AssertionRequestOptions, CancellationToken, Task<AssertionResponse>> _assertionDelegate;

        public ClientAssertionDelegateCredential(
            Func<AssertionRequestOptions, CancellationToken, Task<AssertionResponse>> assertionDelegate)
        {
            _assertionDelegate = assertionDelegate ?? throw new ArgumentNullException(nameof(assertionDelegate));
        }

        public AssertionType AssertionType => AssertionType.ClientAssertion;

        private X509Certificate2 _cachedCertificate;

        internal X509Certificate2 PeekCertificate(string clientId)
        {
            // Return the cached value if we already probed once
            if (_cachedCertificate != null)
            {
                return _cachedCertificate;
            }

            try
            {
                var probeOpts = new AssertionRequestOptions
                {
                    ClientID = clientId,
                };

                var resp = _assertionDelegate(probeOpts, CancellationToken.None)
                                 .ConfigureAwait(false)
                                 .GetAwaiter()
                                 .GetResult();

                _cachedCertificate = resp?.TokenBindingCertificate;
            }
            catch
            {
            }

            return _cachedCertificate;
        }

        public async Task AddConfidentialClientParametersAsync(
            OAuth2Client oAuth2Client,
            AuthenticationRequestParameters requestParameters,
            ICryptographyManager cryptographyManager,
            string tokenEndpoint,
            CancellationToken cancellationToken)
        {
            // Build the same AssertionRequestOptions old code produced
            var opts = new AssertionRequestOptions
            {
                CancellationToken = cancellationToken,
                ClientID = requestParameters.AppConfig.ClientId,
                TokenEndpoint = tokenEndpoint,
                ClientCapabilities = requestParameters.RequestContext.ServiceBundle.Config.ClientCapabilities,
                Claims = requestParameters.Claims,
                ClientAssertionFmiPath = requestParameters.ClientAssertionFmiPath
            };

            // Execute delegate
            AssertionResponse resp = await _assertionDelegate(opts, cancellationToken)
                                            .ConfigureAwait(false);

            // Empty JWT is not allowed
            if (string.IsNullOrWhiteSpace(resp.Assertion))
            {
                throw new ArgumentException(
                    "The assertion delegate returned an empty JWT.",
                    nameof(_assertionDelegate));
            }

            // Set assertion type
            if (resp.TokenBindingCertificate != null)
            {
                oAuth2Client.AddBodyParameter(
                    OAuth2Parameter.ClientAssertionType,
                    OAuth2AssertionType.JwtPop);
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
