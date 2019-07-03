// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
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
        readonly BrokerFactory _brokerFactory = new BrokerFactory();
        private IBroker _broker;
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
            _broker = _brokerFactory.CreateBrokerFacade(_serviceBundle);

            if (_broker.CanInvokeBroker(_interactiveParameters.UiParent))
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
                await _broker.AcquireTokenUsingBrokerAsync(_brokerPayload).ConfigureAwait(false);

            ValidateResponseFromBroker(msalTokenResponse);
            return msalTokenResponse;
        }

        internal void CreateRequestParametersForBroker()
        {
            _brokerPayload.Clear();
            _brokerPayload.Add(BrokerParameter.Authority, _authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);
            string scopes = EnumerableExtensions.AsSingleString(_authenticationRequestParameters.Scope);

            _brokerPayload.Add(BrokerParameter.Scope, scopes);
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
                throw new MsalServiceException(MsalError.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);
            }
        }
    }
}
