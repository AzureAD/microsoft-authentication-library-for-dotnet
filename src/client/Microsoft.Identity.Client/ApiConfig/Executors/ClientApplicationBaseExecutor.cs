// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class ClientApplicationBaseExecutor : AbstractExecutor, IClientApplicationBaseExecutor
    {
        private readonly ClientApplicationBase _clientApplicationBase;

        public ClientApplicationBaseExecutor(IServiceBundle serviceBundle, ClientApplicationBase clientApplicationBase)
            : base(serviceBundle, clientApplicationBase)
        {
            _clientApplicationBase = clientApplicationBase;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenSilentParameters silentParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId);

            var requestParameters = _clientApplicationBase.CreateRequestParameters(
                commonParameters,
                requestContext,
                _clientApplicationBase.UserTokenCacheInternal);

            requestParameters.SendX5C = silentParameters.SendX5C;

            var handler = new SilentRequest(ServiceBundle, requestParameters, silentParameters);
            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenByRefreshTokenParameters refreshTokenParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId);
            if (commonParameters.Scopes == null || !commonParameters.Scopes.Any())
            {
                commonParameters.Scopes = new SortedSet<string>
                {
                    _clientApplicationBase.AppConfig.ClientId + "/.default"
                };
                requestContext.Logger.Info(LogMessages.NoScopesProvidedForRefreshTokenRequest);
            }

            var requestParameters = _clientApplicationBase.CreateRequestParameters(
                commonParameters,
                requestContext,
                _clientApplicationBase.UserTokenCacheInternal);

            requestParameters.IsRefreshTokenRequest = true;

            requestContext.Logger.Info(LogMessages.UsingXScopesForRefreshTokenRequest(commonParameters.Scopes.Count()));

            requestParameters.SendX5C = refreshTokenParameters.SendX5C;

            var handler = new ByRefreshTokenRequest(ServiceBundle, requestParameters, refreshTokenParameters);
            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
