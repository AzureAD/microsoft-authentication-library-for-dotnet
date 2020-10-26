// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using System;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal.Requests.Silent
{
    internal class SilentRequest : RequestBase
    {
        private readonly AcquireTokenSilentParameters _silentParameters;
        private readonly ISilentAuthRequestStrategy _clientStrategy;
        private readonly Lazy<ISilentAuthRequestStrategy> _brokerStrategyLazy;
        private readonly ICoreLogger _logger;

        public SilentRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters silentParameters,
            ISilentAuthRequestStrategy clientStrategyOverride = null,
            ISilentAuthRequestStrategy brokerStrategyOverride = null)
            : base(serviceBundle, authenticationRequestParameters, silentParameters)
        {
            _silentParameters = silentParameters;

            _brokerStrategyLazy = new Lazy<ISilentAuthRequestStrategy>(() => brokerStrategyOverride
#if MSAL_DESKTOP || MSAL_XAMARIN
              ??  new SilentBrokerAuthStrategy(this,
                    serviceBundle,
                    authenticationRequestParameters,
                    silentParameters,
                    serviceBundle.PlatformProxy.CreateBroker(null))
#endif
            );
            _clientStrategy = clientStrategyOverride ?? new SilentClientAuthStretegy(this, serviceBundle, authenticationRequestParameters, silentParameters);

            _logger = authenticationRequestParameters.RequestContext.Logger;
        }

        internal async override Task PreRunAsync()
        {
            await _clientStrategy.PreRunAsync().ConfigureAwait(false);
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Info("Attempting to acquire token using using local cache...");
                return await _clientStrategy.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MsalException ex)
            {
#if MSAL_DESKTOP || MSAL_XAMARIN
                if (ex is MsalUiRequiredException || ex is MsalClientException)
                {
                    var errorCode = ex.ErrorCode;

                    if ((errorCode == MsalError.InvalidGrantError
                     || errorCode == MsalError.NoTokensFoundError
                     || errorCode == MsalError.NoAccountForLoginHint)
                     && AuthenticationRequestParameters.IsBrokerConfigured
                     && ServiceBundle.PlatformProxy.CanBrokerSupportSilentAuth())
                    {
                        _logger.Info("Failed to acquire a token using local cache. Attempting to use broker instead");
                        return await _brokerStrategyLazy.Value.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
#endif
                _logger.Verbose("Failed to obtain a token using the token cache - " + ex.ErrorCode);
                throw;
            }
        }

        protected override void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            if (_silentParameters.LoginHint != null)
            {
                apiEvent.LoginHint = _silentParameters.LoginHint;
            }
        }

        internal new async Task<AuthenticationResult> CacheTokenResponseAndCreateAuthenticationResultAsync(MsalTokenResponse response)
        {
            return await base.CacheTokenResponseAndCreateAuthenticationResultAsync(response).ConfigureAwait(false);
        }

        //internal for test
        internal async Task<AuthenticationResult> ExecuteTestAsync(CancellationToken cancellationToken)
        {
            return await ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
