// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.Broker
{
    internal class BrokerSilentRequest
    {
        public Dictionary<string, string> BrokerPayload
            = new Dictionary<string, string>();
        internal IBroker Broker { get; }
        private readonly AcquireTokenSilentParameters _silentParameters;
        private readonly AuthenticationRequestParameters _authenticationRequestParameters;
        private readonly IServiceBundle _serviceBundle;
        private readonly ICoreLogger _logger;

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
            _logger = _authenticationRequestParameters.RequestContext.Logger;
        }

        public async Task<MsalTokenResponse> SendTokenRequestToBrokerAsync()
        {
            if(!Broker.IsBrokerInstalledAndInvokable())
            {
                throw new MsalClientException(MsalError.BrokerApplicationRequired, MsalErrorMessage.AndroidBrokerCannotBeInvoked);
            }

            _logger.Info(LogMessages.CanInvokeBrokerAcquireTokenWithBroker);

            MsalTokenResponse msalTokenResponse =
              await Broker.AcquireTokenSilentAsync(
                  _authenticationRequestParameters, 
                  _silentParameters).ConfigureAwait(false);

            ValidateResponseFromBroker(msalTokenResponse);
            return msalTokenResponse;
        }

        internal void ValidateResponseFromBroker(MsalTokenResponse msalTokenResponse)
        {
            _logger.Info(LogMessages.CheckMsalTokenResponseReturnedFromBroker);
            if (msalTokenResponse.AccessToken != null)
            {
                _logger.Info("Success. Response contains an access token");
                return;
            }

            if (msalTokenResponse.Error != null)
            {
                _logger.Info(
                    LogMessages.ErrorReturnedInBrokerResponse(msalTokenResponse.Error));
                throw new MsalServiceException(msalTokenResponse.Error, MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription);
            }

            _logger.Info(LogMessages.UnknownErrorReturnedInBrokerResponse);
            throw new MsalServiceException(MsalError.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);
        }
    }
}
