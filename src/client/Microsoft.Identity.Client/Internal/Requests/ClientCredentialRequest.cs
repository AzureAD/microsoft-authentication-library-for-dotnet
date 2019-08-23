// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using System;
using Microsoft.Identity.Client.Cache.Items;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class ClientCredentialRequest : RequestBase
    {
        private readonly AcquireTokenForClientParameters _clientParameters;

        public ClientCredentialRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForClientParameters clientParameters)
            : base(serviceBundle, authenticationRequestParameters, clientParameters)
        {
            _clientParameters = clientParameters;
        }

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            MsalAccessTokenCacheItem cachedAccessTokenItem = null;
            var logger = AuthenticationRequestParameters.RequestContext.Logger;

            if (!_clientParameters.ForceRefresh)
            {
                cachedAccessTokenItem = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

                if (cachedAccessTokenItem != null && !cachedAccessTokenItem.NeedsRefresh())
                {
                    return new AuthenticationResult(cachedAccessTokenItem, null, AuthenticationRequestParameters.RequestContext.CorrelationId);
                }
            }

            // No AT in the cache or AT needs to be refreshed
            try
            {
                return await FetchNewAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MsalServiceException e)
            {
                bool isAadUnavailable = e.IsAadUnavailable();
                logger.Warning($"Fetching a new AT failed. Is AAD down? {isAadUnavailable}. Is there an AT in the cache that is usable? {cachedAccessTokenItem != null}");

                if (cachedAccessTokenItem != null && isAadUnavailable)
                {
                    logger.Info("Returning existing access token. It is not expired, but should be refreshed.");
                    return new AuthenticationResult(cachedAccessTokenItem, null, AuthenticationRequestParameters.RequestContext.CorrelationId);
                }

                logger.Warning("Either the exception does not indicate a problem with AAD or the token cache does not have an AT that is usable.");
                throw;
            }
        }

        private async Task<AuthenticationResult> FetchNewAccessTokenAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);
            var msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(), cancellationToken).ConfigureAwait(false);
            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        protected override void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            apiEvent.IsConfidentialClient = true;
        }

        private Dictionary<string, string> GetBodyParameters()
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.ClientCredentials,
                [OAuth2Parameter.Scope] = AuthenticationRequestParameters.Scope.AsSingleString()
            };
            return dict;
        }
    }
}
