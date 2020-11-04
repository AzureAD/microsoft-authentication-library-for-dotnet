// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests.Silent;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class SilentBrokerAuthStrategy : ISilentAuthRequestStrategy
    {
        internal AuthenticationRequestParameters _authenticationRequestParameters;
        protected IServiceBundle _serviceBundle;
        private readonly AcquireTokenSilentParameters _silentParameters;
        private readonly SilentRequest _silentRequest;
        internal IBroker Broker { get; }
        private readonly ICoreLogger _logger;

        public SilentBrokerAuthStrategy(
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
            Broker = broker;
            _logger = authenticationRequestParameters.RequestContext.Logger;
        }

        public async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            MsalTokenResponse response = await SendTokenRequestToBrokerAsync().ConfigureAwait(false);
            return await _silentRequest.CacheTokenResponseAndCreateAuthenticationResultAsync(response).ConfigureAwait(false);
        }

        public async Task<MsalTokenResponse> SendTokenRequestToBrokerAsync()
        {
            if (Broker != null && !Broker.IsBrokerInstalledAndInvokable())
            {
                throw new MsalClientException(MsalError.BrokerApplicationRequired, MsalErrorMessage.AndroidBrokerCannotBeInvoked);
            }

            _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CanInvokeBrokerAcquireTokenWithBroker);

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

                if (msalTokenResponse.Error == BrokerResponseConst.NoTokenFound)
                {
                    throw new MsalUiRequiredException(msalTokenResponse.Error, msalTokenResponse.ErrorDescription);
                }

                throw MsalServiceExceptionFactory.FromBrokerResponse(msalTokenResponse.Error,
                                                     MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription,
                                                     string.IsNullOrEmpty(msalTokenResponse.SubError) ?
                                                     MsalError.UnknownBrokerError : msalTokenResponse.SubError,
                                                     msalTokenResponse.CorrelationId,
                                                     msalTokenResponse.HttpResponse);
            }

            _logger.Info(LogMessages.UnknownErrorReturnedInBrokerResponse);
            throw new MsalServiceException(MsalError.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);
        }

        public Task PreRunAsync()
        {
            return Task.Delay(0);
        }      
    }
}
