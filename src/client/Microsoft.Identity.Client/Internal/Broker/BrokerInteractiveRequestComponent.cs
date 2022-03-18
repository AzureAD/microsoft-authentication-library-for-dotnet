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

    internal class BrokerInteractiveRequestComponent : ITokenRequestComponent
    {
        internal IBroker Broker { get; }
        private readonly AcquireTokenInteractiveParameters _interactiveParameters;
        private readonly string _optionalBrokerInstallUrl; // can be null
        private readonly AuthenticationRequestParameters _authenticationRequestParameters;
        private readonly IServiceBundle _serviceBundle;
        private readonly ICoreLogger _logger;

        public BrokerInteractiveRequestComponent(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters,
            IBroker broker,
            string optionalBrokerInstallUrl)
        {
            _authenticationRequestParameters = authenticationRequestParameters;
            _interactiveParameters = acquireTokenInteractiveParameters;
            _serviceBundle = authenticationRequestParameters.RequestContext.ServiceBundle;
            Broker = broker;
            _optionalBrokerInstallUrl = optionalBrokerInstallUrl;
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
                if (string.IsNullOrEmpty(_optionalBrokerInstallUrl))
                {
                    _logger.Info("Broker is required but not installed. An app URI has not been provided. MSAL will fallback to use a browser.");
                    return null;
                }

                _logger.Info(LogMessages.AddBrokerInstallUrlToPayload);
                Broker.HandleInstallUrl(_optionalBrokerInstallUrl);                
            }

            var tokenResponse = await Broker.AcquireTokenInteractiveAsync(
                _authenticationRequestParameters, 
                _interactiveParameters)
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
                _logger.Info(
                    LogMessages.ErrorReturnedInBrokerResponse(msalTokenResponse.Error));

                throw MsalServiceExceptionFactory.FromBrokerResponse(msalTokenResponse,
                                                                     MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription);
            }

            _logger.Info(LogMessages.UnknownErrorReturnedInBrokerResponse);
            throw new MsalServiceException(MsalError.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);

        }

        // Example auth code that shows that broker is required:
        // msauth://wpj?username=joe@contoso.onmicrosoft.com&app_link=itms%3a%2f%2fitunes.apple.com%2fapp%2fazure-authenticator%2fid983156458%3fmt%3d8
        public static bool IsBrokerRequiredAuthCode(string authCode, out string installationUri)
        {
            if (authCode.StartsWith(BrokerParameter.AuthCodePrefixForEmbeddedWebviewBrokerInstallRequired, StringComparison.OrdinalIgnoreCase))
            //|| authCode.StartsWith(_serviceBundle.Config.RedirectUri, StringComparison.OrdinalIgnoreCase) // TODO: what is this?!
            {
                installationUri = ExtractAppLink(authCode);
                return (installationUri != null);
            }

            installationUri = null;
            return false;
        }

        private static string ExtractAppLink(string authCode)
        {
            Uri authCodeUri = new Uri(authCode);
            string query = authCodeUri.Query;

            if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Substring(1);
            }

            Dictionary<string, string> queryDict = CoreHelpers.ParseKeyValueList(query, '&', true, true, null);

            if (!queryDict.ContainsKey(BrokerParameter.AppLink))
            {
                return null;
            }

            return queryDict[BrokerParameter.AppLink];
        }
    }
}
