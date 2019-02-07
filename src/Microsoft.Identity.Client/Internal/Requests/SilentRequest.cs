// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Internal.Broker;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class SilentRequest : RequestBase
    {
        private readonly AcquireTokenSilentParameters _silentParameters;
        private AuthenticationRequestParameters _authenticationRequestParameters;
        BrokerFactory brokerFactory = new BrokerFactory();
        IBroker Broker;

        public SilentRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters silentParameters)
            : base(serviceBundle, authenticationRequestParameters, silentParameters)
        {
            _silentParameters = silentParameters;
            _authenticationRequestParameters = authenticationRequestParameters;
        }

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!CacheManager.HasCache)
            {
                throw new MsalUiRequiredException(
                    MsalUiRequiredException.TokenCacheNullError,
                    "Token cache is set to null. Silent requests cannot be executed.");
            }

            MsalAccessTokenCacheItem msalAccessTokenItem = null;

            // Look for access token
            if (!_silentParameters.ForceRefresh)
            {
                msalAccessTokenItem =
                    await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);
            }

            if (msalAccessTokenItem != null)
            {
                var msalIdTokenItem = CacheManager.GetIdTokenCacheItem(msalAccessTokenItem.GetIdTokenItemKey());

                return new AuthenticationResult(msalAccessTokenItem, msalIdTokenItem);
            }

            var msalRefreshTokenItem = await CacheManager.FindRefreshTokenAsync().ConfigureAwait(false);

            if (msalRefreshTokenItem == null)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Verbose("No Refresh Token was found in the cache");

                throw new MsalUiRequiredException(
                    MsalUiRequiredException.NoTokensFoundError,
                    "No Refresh Token found in the cache");
            }

            AuthenticationRequestParameters.RequestContext.Logger.Verbose("Refreshing access token...");
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);

            var msalTokenResponse = await CheckForBrokerAndSendTokenRequestAsync(msalRefreshTokenItem, cancellationToken)
                                        .ConfigureAwait(false);

            if (msalTokenResponse.RefreshToken == null)
            {
                msalTokenResponse.RefreshToken = msalRefreshTokenItem.Secret;
                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
            }

            return CacheTokenResponseAndCreateAuthenticationResult(msalTokenResponse);
        }

        protected override void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            if (_silentParameters.LoginHint != null)
            {
                apiEvent.LoginHint = _silentParameters.LoginHint;
            }
        }

        private Dictionary<string, string> GetBodyParameters(string refreshTokenSecret)
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.RefreshToken,
                [OAuth2Parameter.RefreshToken] = refreshTokenSecret
            };

            return dict;
        }

        private async Task<MsalTokenResponse> CheckForBrokerAndSendTokenRequestAsync(MsalRefreshTokenCacheItem msalRefreshTokenItem, CancellationToken cancellationToken)
        {
            Broker = brokerFactory.CreateBrokerFacade(ServiceBundle.DefaultLogger);
            MsalTokenResponse msalTokenResponse;

            if (Broker.CanInvokeBroker(new ApiConfig.OwnerUiParent(), ServiceBundle))
            {
                msalTokenResponse = await Broker.AcquireTokenUsingBrokerAsync(
                    CreateBrokerPayload(),
                    ServiceBundle).ConfigureAwait(false);
            }
            else
            {
               msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(msalRefreshTokenItem.Secret), cancellationToken)
                                        .ConfigureAwait(false);
            }

            return msalTokenResponse;
        }

        private Dictionary<string, string> CreateBrokerPayload()
        {
            Dictionary<string, string> brokerPayload = _authenticationRequestParameters.CreateSilentRequestParametersForBroker();
            return brokerPayload;
        }
    }
}