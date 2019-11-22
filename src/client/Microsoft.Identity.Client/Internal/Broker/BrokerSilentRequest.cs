// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Broker
{
    internal class BrokerSilentRequest
    {
        private Dictionary<string, string> _brokerPayload = new Dictionary<string, string>();
        private IBroker Broker { get; }
        private readonly AcquireTokenSilentParameters _silentParameters;
        private readonly AuthenticationRequestParameters _authenticationRequestParameters;
        private readonly IServiceBundle _serviceBundle;

        internal BrokerSilentRequest(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters,
            IServiceBundle serviceBundle,
            IBroker broker)
        {
            _authenticationRequestParameters = authenticationRequestParameters;
            _silentParameters = acquireTokenSilentParameters;
            _serviceBundle = serviceBundle;
            Broker = broker;
        }

        public async Task<MsalTokenResponse> SendTokenRequestToBrokerAsync()
        {
            //TODO: call can invoke broker here
            _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CanInvokeBrokerAcquireTokenWithBroker);

            return await SendAndVerifyResponseAsync().ConfigureAwait(false);
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
            _brokerPayload.Add(BrokerParameter.Authority, _authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);
            string scopes = EnumerableExtensions.AsSingleString(_authenticationRequestParameters.Scope);

            _brokerPayload.Add(BrokerParameter.Scope, scopes);
            _brokerPayload.Add(BrokerParameter.ClientId, _authenticationRequestParameters.ClientId);
            _brokerPayload.Add(BrokerParameter.CorrelationId, _authenticationRequestParameters.RequestContext.Logger.CorrelationId.ToString());
            _brokerPayload.Add(BrokerParameter.ClientVersion, MsalIdHelper.GetMsalVersion());
            _brokerPayload.Add(BrokerParameter.RedirectUri, _serviceBundle.Config.RedirectUri);
            string extraQP = string.Join("&", _authenticationRequestParameters.ExtraQueryParameters.Select(x => x.Key + "=" + x.Value));
            _brokerPayload.Add(BrokerParameter.ExtraQp, extraQP);
            _brokerPayload.Add(BrokerParameter.ExtraOidcScopes, BrokerParameter.OidcScopesValue);
            _brokerPayload.Add(BrokerParameter.LoginHint, _silentParameters.LoginHint);
#pragma warning disable CA1305 // Specify IFormatProvider
            _brokerPayload.Add(BrokerParameter.ForceRefresh, _silentParameters.ForceRefresh.ToString());
#pragma warning restore CA1305 // Specify IFormatProvider
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
            if (msalTokenResponse.Error != null)
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(
                    LogMessages.ErrorReturnedInBrokerResponse(msalTokenResponse.Error));
                throw new MsalServiceException(msalTokenResponse.Error, MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription);
            }
        }
    }
}
