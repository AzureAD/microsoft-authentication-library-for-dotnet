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
        private const string TheOnlyFamilyId = "1";
        private ISilentAuthStrategy strategy;

        public SilentRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters silentParameters)
            : base(serviceBundle, authenticationRequestParameters, silentParameters)
        {
            _silentParameters = silentParameters;
            strategy = SilentAuthStrategyFactory.GetSilentAuthStrategy(this, serviceBundle, authenticationRequestParameters, silentParameters);
        }

        internal async override Task PreRunAsync()
        {
            await strategy.PreRunAsync().ConfigureAwait(false);
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            return await strategy.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            if (_silentParameters.LoginHint != null)
            {
                apiEvent.LoginHint = _silentParameters.LoginHint;
            }
        }
    }
}
