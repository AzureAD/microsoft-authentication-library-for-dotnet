// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests.Silent;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class BrokerSilentStrategy
        : ISilentAuthRequestStrategy
    {
        internal AuthenticationRequestParameters _authenticationRequestParameters;
        protected IServiceBundle _serviceBundle;
        private readonly AcquireTokenSilentParameters _silentParameters;
        private readonly SilentRequest _silentRequest;
        internal IBroker Broker { get; }
        private readonly ICoreLogger _logger;

        public BrokerSilentStrategy(
            SilentRequest request,
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters silentParameters,
            IBroker broker)
        {
            _authenticationRequestParameters = authenticationRequestParameters;
            _silentParameters = silentParameters;
            _serviceBundle = serviceBundle;
            _silentRequest = request;
            Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _logger = authenticationRequestParameters.RequestContext.Logger;
        }

        public async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!Broker.IsBrokerInstalledAndInvokable(_authenticationRequestParameters.AuthorityInfo.AuthorityType))
            {
                _logger.Warning("Broker is not installed. Cannot respond to silent request.");
                return null;
            }

            MsalTokenResponse response = await SendTokenRequestToBrokerAsync().ConfigureAwait(false);
            if (response != null)
            {
                ValidateResponseFromBroker(response);
                Metrics.IncrementTotalAccessTokensFromBroker();
                return await _silentRequest.CacheTokenResponseAndCreateAuthenticationResultAsync(response).ConfigureAwait(false);
            }

            return null;
        }

        private async Task<MsalTokenResponse> SendTokenRequestToBrokerAsync()
        {
            _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CanInvokeBrokerAcquireTokenWithBroker);

            MsalTokenResponse msalTokenResponse;

            if (!PublicClientApplication.IsOperatingSystemAccount(_authenticationRequestParameters.Account))
            {
                msalTokenResponse = await Broker.AcquireTokenSilentAsync(
                      _authenticationRequestParameters,
                      _silentParameters).ConfigureAwait(false);
            }
            else
            {
                msalTokenResponse = await Broker.AcquireTokenSilentDefaultUserAsync(
                     _authenticationRequestParameters,
                     _silentParameters).ConfigureAwait(false);
            }

            return msalTokenResponse;
        }

        internal /* internal for test */ void ValidateResponseFromBroker(MsalTokenResponse msalTokenResponse)
        {
            _logger.Info(LogMessages.CheckMsalTokenResponseReturnedFromBroker);
            if (msalTokenResponse.AccessToken != null)
            {
                _logger.Info("Success. Response contains an access token. ");
                return;
            }

            if (msalTokenResponse.Error != null)
            {
                _logger.Info(
                    LogMessages.ErrorReturnedInBrokerResponse(msalTokenResponse.Error));

                if (msalTokenResponse.Error == BrokerResponseConst.AndroidNoTokenFound ||
                    msalTokenResponse.Error == BrokerResponseConst.AndroidNoAccountFound ||
                    msalTokenResponse.Error == BrokerResponseConst.AndroidInvalidRefreshToken)
                {
                    throw new MsalUiRequiredException(msalTokenResponse.Error, msalTokenResponse.ErrorDescription);
                }

                throw MsalServiceExceptionFactory.FromBrokerResponse(msalTokenResponse,
                                                     MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription);
            }

            _logger.Info(LogMessages.UnknownErrorReturnedInBrokerResponse);
            throw new MsalServiceException(MsalError.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);
        }
    }
}
