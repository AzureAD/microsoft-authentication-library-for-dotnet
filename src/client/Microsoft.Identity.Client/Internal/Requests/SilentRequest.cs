// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using System.Linq;
using System;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal.Broker;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class SilentRequest : RequestBase
    {
        private readonly AcquireTokenSilentParameters _silentParameters;
        private ISilentAuthStrategy clientStrategy;
        private Lazy<ISilentAuthStrategy> brokerStrategy;
        private ICoreLogger _logger;

        public SilentRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters silentParameters)
            : base(serviceBundle, authenticationRequestParameters, silentParameters)
        {
            _silentParameters = silentParameters;

            brokerStrategy = new Lazy<ISilentAuthStrategy>(() => new SilentBrokerAuthStretegy(this, serviceBundle, authenticationRequestParameters, silentParameters));
            clientStrategy = new SilentClientAuthStretegy(this, serviceBundle, authenticationRequestParameters, silentParameters);

            _logger = authenticationRequestParameters.RequestContext.Logger;
        }

        internal async override Task PreRunAsync()
        {
            if (!AuthenticationRequestParameters.IsBrokerConfigured && ServiceBundle.PlatformProxy.CanBrokerSupportSilentAuth())
            {
                await clientStrategy.PreRunAsync().ConfigureAwait(false);
            }
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Info("Attempting to acquire token using client auth strategy...");
                return await clientStrategy.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch(MsalException ex)
            {
                if (ex is MsalUiRequiredException || ex is MsalClientException)
                {
                    var errorCode = ex.ErrorCode;

                    if (errorCode == MsalError.InvalidGrantError
                     || errorCode == MsalError.NoTokensFoundError
                     || errorCode == MsalError.NoAccountForLoginHint)
                    {
                        _logger.Info("client auth strategy failed to acquire a token. Attempting to use broker strategy");
                        return await brokerStrategy.Value.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                throw ex;
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
    }
}
