// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// This class decides the workflow of an interactive request. The business rules are: 
    /// 
    /// 1. If WithBroker is set to true
    /// 1.1. Attempt to invoke the broker and get the token
    /// 1.2. If this fails, e.g. if broker is not installed, the use a web view (goto 2)
    /// 
    /// 2. Use a webview and get an auth code and look at the auth code
    /// 2.1. If the auth code has a special format, showing that a broker is needed then. Invoke the broker flow (step 1) with a broker installation URL
    /// 2.2. Otherwise exchange the auth code for tokens (normal authorize_code grant)
    /// </summary>
    internal class InteractiveRequest : RequestBase
    {
        private readonly AuthenticationRequestParameters _requestParams;
        private readonly AcquireTokenInteractiveParameters _interactiveParameters;
        private readonly IServiceBundle _serviceBundle;
        private readonly ICoreLogger _logger;

        #region For Test
        private readonly IAuthCodeRequestComponent _authCodeRequestComponentOverride;
        private readonly ITokenRequestComponent _authCodeExchangeComponentOverride;
        private readonly ITokenRequestComponent _brokerInteractiveComponent;
        #endregion

        public InteractiveRequest(
            AuthenticationRequestParameters requestParams,
            AcquireTokenInteractiveParameters interactiveParameters,
            /* for test */ IAuthCodeRequestComponent authCodeRequestComponentOverride = null,
            /* for test */ ITokenRequestComponent authCodeExchangeComponentOverride = null, 
            /* for test */ ITokenRequestComponent brokerExchangeComponentOverride = null) :
            base(requestParams?.RequestContext?.ServiceBundle,
                requestParams,
                interactiveParameters)
        {
            _requestParams = requestParams ?? throw new ArgumentNullException(nameof(requestParams));
            _interactiveParameters = interactiveParameters ?? throw new ArgumentNullException(nameof(interactiveParameters));
            _authCodeRequestComponentOverride = authCodeRequestComponentOverride;
            _authCodeExchangeComponentOverride = authCodeExchangeComponentOverride;
            _brokerInteractiveComponent = brokerExchangeComponentOverride;
            _serviceBundle = requestParams.RequestContext.ServiceBundle;
            _logger = requestParams.RequestContext.Logger;
        }

        #region RequestBase hooks
        protected override async Task<AuthenticationResult> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            MsalTokenResponse tokenResponse = await GetTokenResponseAsync(cancellationToken)
                .ConfigureAwait(false);
            return await CacheTokenResponseAndCreateAuthenticationResultAsync(tokenResponse)
                .ConfigureAwait(false);
        }

        protected override void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            apiEvent.Prompt = _interactiveParameters.Prompt.PromptValue;
            if (_interactiveParameters.LoginHint != null)
            {
                apiEvent.LoginHint = _interactiveParameters.LoginHint;
            }
        }
        #endregion

        private async Task<MsalTokenResponse> FetchTokensFromBrokerAsync(string brokerInstallUrl, CancellationToken cancellationToken)
        {
            IBroker broker = _serviceBundle.PlatformProxy.CreateBroker(
                _serviceBundle.Config,
                _interactiveParameters.UiParent);

            ITokenRequestComponent brokerInteractiveRequest =
                _brokerInteractiveComponent ??
                new BrokerInteractiveRequestComponent(
                    _requestParams,
                    _interactiveParameters,
                    broker,
                    brokerInstallUrl);

            return await brokerInteractiveRequest.FetchTokensAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<MsalTokenResponse> GetTokenResponseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_requestParams.IsBrokerConfigured)
            {
                _logger.Info("Broker is configured. Starting broker flow without knowing the broker installation app link. ");

                MsalTokenResponse tokenResponse = await FetchTokensFromBrokerAsync(
                    null, // we don't have an installation URI yet
                    cancellationToken)
                    .ConfigureAwait(false);

                // if we don't get back a result, then continue with the WebUi 
                if (tokenResponse != null)
                {
                    _logger.Info("Broker attempt completed successfully. ");
                    return tokenResponse;
                }

                _logger.Info("Broker attempt did not complete, most likely because the broker is not installed. Attempting to use a browser / web UI. ");
                cancellationToken.ThrowIfCancellationRequested();
            }

            IAuthCodeRequestComponent authorizationFetcher = 
                _authCodeRequestComponentOverride ??
                new AuthCodeRequestComponent(
                    _requestParams,
                    _interactiveParameters);

            var result = await authorizationFetcher.FetchAuthCodeAndPkceVerifierAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.Info("An authorization code was retrieved from the /authorize endpoint. ");
            string authCode = result.Item1;
            string pkceCodeVerifier = result.Item2;

            if (BrokerInteractiveRequestComponent.IsBrokerRequiredAuthCode(authCode, out string brokerInstallUri))
            {
                return await RunBrokerWithInstallUriAsync(brokerInstallUri, cancellationToken).ConfigureAwait(false);
            }

            _logger.Info("Exchanging the auth code for tokens. ");
            var authCodeExchangeComponent =
                _authCodeExchangeComponentOverride ??
                new AuthCodeExchangeComponent(
                    _requestParams,
                    _interactiveParameters,
                    authCode,
                    pkceCodeVerifier);

            return await authCodeExchangeComponent.FetchTokensAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<MsalTokenResponse> RunBrokerWithInstallUriAsync(string brokerInstallUri, CancellationToken cancellationToken)
        {
            _logger.Info("Based on the auth code, the broker flow is required. " + 
                "Starting broker flow knowing the broker installation app link. ");

            cancellationToken.ThrowIfCancellationRequested();

            var tokenResponse = await FetchTokensFromBrokerAsync(
                brokerInstallUri,
                cancellationToken).ConfigureAwait(false);

            _logger.Info("Broker attempt completed successfully " + (tokenResponse != null));
            return tokenResponse;
        }
    }
}
