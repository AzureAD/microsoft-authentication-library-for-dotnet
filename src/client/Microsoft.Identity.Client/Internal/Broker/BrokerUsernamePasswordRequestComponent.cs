// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Broker
{

    internal class BrokerUsernamePasswordRequestComponent : ITokenRequestComponent
    {
        internal IBroker Broker { get; }
        private readonly AcquireTokenByUsernamePasswordParameters _usernamePasswordParameters;
        private readonly string _optionalBrokerInstallUrl; // can be null
        private readonly AuthenticationRequestParameters _authenticationRequestParameters;
        private readonly IServiceBundle _serviceBundle;
        private readonly ILoggerAdapter _logger;

        public BrokerUsernamePasswordRequestComponent(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenByUsernamePasswordParameters acquireTokenByUsernamePasswordParameters,
            IBroker broker)
        {
            _authenticationRequestParameters = authenticationRequestParameters;
            _usernamePasswordParameters = acquireTokenByUsernamePasswordParameters;
            _serviceBundle = authenticationRequestParameters.RequestContext.ServiceBundle;
            Broker = broker;
            _logger = _authenticationRequestParameters.RequestContext.Logger;
        }

        public async Task<MsalTokenResponse> FetchTokensAsync(CancellationToken cancellationToken)
        {
            if (Broker.IsBrokerInstalledAndInvokable(_authenticationRequestParameters.AuthorityInfo.AuthorityType))
            {
                _logger.Info(LogMessages.CanInvokeBrokerAcquireTokenWithBroker);
            }
            else
            {
                // Since this is only implemented for the runtime broker other platforms are not supported for this flow.
                throw new PlatformNotSupportedException();                
            }

            var tokenResponse = await Broker.AcquireTokenByUsernamePasswordAsync(
                _authenticationRequestParameters, 
                _usernamePasswordParameters)
                .ConfigureAwait(false);

            ValidateResponseFromBroker(tokenResponse);

            return tokenResponse;
        }

        internal /* internal for test */ void ValidateResponseFromBroker(MsalTokenResponse msalTokenResponse)
        {
            _logger.Info(LogMessages.CheckMsalTokenResponseReturnedFromBroker);
            if (!string.IsNullOrEmpty(msalTokenResponse.AccessToken))
            {
                _logger.Info(
                    "Success. Broker response contains an access token. ");
                return;
            }

            if (msalTokenResponse.Error != null)
            {
                _logger.Error(
                    LogMessages.ErrorReturnedInBrokerResponse(msalTokenResponse.Error));

                throw MsalServiceExceptionFactory.FromBrokerResponse(msalTokenResponse,
                                                                     MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription);
            }

            _logger.Error(LogMessages.UnknownErrorReturnedInBrokerResponse);
            throw new MsalServiceException(MsalError.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);

        }
    }
}
