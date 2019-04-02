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

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Broker
{
    internal class BrokerInteractiveRequest
    {
        internal Dictionary<string, string> _brokerPayload = new Dictionary<string, string>();
        readonly BrokerFactory brokerFactory = new BrokerFactory();
        private IBroker Broker;
        private readonly AcquireTokenInteractiveParameters _interactiveParameters;
        private readonly AuthenticationRequestParameters _authenticationRequestParameters;
        private readonly IServiceBundle _serviceBundle;
        private readonly AuthorizationResult _authorizationResult;

        internal BrokerInteractiveRequest(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters,
            IServiceBundle serviceBundle,
            AuthorizationResult authorizationResult)
        {
            _authenticationRequestParameters = authenticationRequestParameters;
            _interactiveParameters = acquireTokenInteractiveParameters;
            _serviceBundle = serviceBundle;
            _authorizationResult = authorizationResult;
        }

        public async Task<MsalTokenResponse> SendTokenRequestToBrokerAsync()
        {
            Broker = brokerFactory.CreateBrokerFacade(_serviceBundle);

            if (Broker.CanInvokeBroker(_interactiveParameters.UiParent))
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CanInvokeBrokerAcquireTokenWithBroker);

                return await SendAndVerifyResponseAsync().ConfigureAwait(false);
            }
            else
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.AddBrokerInstallUrlToPayload);
                _brokerPayload[BrokerParameter.BrokerInstallUrl] = _authorizationResult.Code;

                return await SendAndVerifyResponseAsync().ConfigureAwait(false);
            }
        }

        private async Task<MsalTokenResponse> SendAndVerifyResponseAsync()
        {
            CreateRequestParametersForBroker();

            MsalTokenResponse msalTokenResponse =
                await Broker.AcquireTokenUsingBrokerAsync(_brokerPayload).ConfigureAwait(false);

            ValidateResponseFromBroker(msalTokenResponse);
            return msalTokenResponse;
        }

        internal void CreateRequestParametersForBroker()
        {
            _brokerPayload.Clear();
            _brokerPayload.Add(BrokerParameter.Authority, _authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);
            string scopes = EnumerableExtensions.AsSingleString(_authenticationRequestParameters.Scope);

            _brokerPayload.Add(BrokerParameter.RequestScopes, scopes);
            _brokerPayload.Add(BrokerParameter.ClientId, _authenticationRequestParameters.ClientId);
            _brokerPayload.Add(BrokerParameter.CorrelationId, _authenticationRequestParameters.RequestContext.Logger.CorrelationId.ToString());
            _brokerPayload.Add(BrokerParameter.ClientVersion, MsalIdHelper.GetMsalVersion());
            _brokerPayload.Add(BrokerParameter.Force, "NO");
            _brokerPayload.Add(BrokerParameter.RedirectUri, _authenticationRequestParameters.RedirectUri.AbsoluteUri);

            string extraQP = string.Join("&", _authenticationRequestParameters.ExtraQueryParameters.Select(x => x.Key + "=" + x.Value));
            _brokerPayload.Add(BrokerParameter.ExtraQp, extraQP);

            _brokerPayload.Add(BrokerParameter.Username, _authenticationRequestParameters.Account?.Username ?? string.Empty);
            _brokerPayload.Add(BrokerParameter.ExtraOidcScopes, BrokerParameter.OidcScopesValue);
        }

        internal bool IsBrokerInvocationRequired()
        {
            if (_authorizationResult.Code != null &&
               !string.IsNullOrEmpty(_authorizationResult.Code) &&
               _authorizationResult.Code.StartsWith(BrokerParameter.AuthCodePrefixForEmbeddedWebviewBrokerInstallRequired, StringComparison.OrdinalIgnoreCase) ||
               _authorizationResult.Code.StartsWith(_serviceBundle.Config.RedirectUri, StringComparison.OrdinalIgnoreCase))
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.BrokerInvocationRequired);
                return true;
            }

            _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.BrokerInvocationNotRequired);
            return false;
        }

        internal void ValidateResponseFromBroker(MsalTokenResponse msalTokenResponse)
        {
            _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CheckMsalTokenResponseReturnedFromBroker);
            if (msalTokenResponse.AccessToken != null)
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(
                    LogMessages.BrokerResponseContainsAccessToken +
                    msalTokenResponse.AccessToken.Count());
                return;
            }
            else if (msalTokenResponse.Error != null)
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(
                    LogMessages.ErrorReturnedInBrokerResponse(msalTokenResponse.Error));
                throw new MsalServiceException(msalTokenResponse.Error, MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription);
            }
            else
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.UnknownErrorReturnedInBrokerResponse);
                throw new MsalServiceException(MsalServiceException.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);
            }
        }
    }
}
