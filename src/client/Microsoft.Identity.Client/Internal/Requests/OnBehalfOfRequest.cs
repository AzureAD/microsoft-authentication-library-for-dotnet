// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class OnBehalfOfRequest : RequestBase
    {
        private readonly AcquireTokenOnBehalfOfParameters _onBehalfOfParameters;

        public OnBehalfOfRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenOnBehalfOfParameters onBehalfOfParameters)
            : base(serviceBundle, authenticationRequestParameters, onBehalfOfParameters)
        {
            _onBehalfOfParameters = onBehalfOfParameters;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (AuthenticationRequestParameters.Scope == null || AuthenticationRequestParameters.Scope.Count == 0)
            {
                throw new MsalClientException(
                    MsalError.ScopesRequired,
                    MsalErrorMessage.ScopesRequired);
            }

            await ResolveAuthorityAsync().ConfigureAwait(false);
            CacheInfoTelemetry cacheInfoTelemetry = CacheInfoTelemetry.None;
            MsalAccessTokenCacheItem msalAccessTokenItem = null;
            var logger = AuthenticationRequestParameters.RequestContext.Logger;

            if (!_onBehalfOfParameters.ForceRefresh)
            {
                // look for access token in the cache first.
                // no access token is found, then it means token does not exist
                // or new assertion has been passed. We should not use Refresh Token
                // for the user because the new incoming token may have updated claims
                // like MFA etc.
                msalAccessTokenItem = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);
                if (msalAccessTokenItem != null && !msalAccessTokenItem.NeedsRefresh())
                {
                    var msalIdTokenItem = await CacheManager.GetIdTokenCacheItemAsync(msalAccessTokenItem.GetIdTokenItemKey()).ConfigureAwait(false);
                    AuthenticationRequestParameters.RequestContext.Logger.Info(
                        "OBO found a valid access token in the cache. ID token also found? " + (msalIdTokenItem != null));

                    AuthenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;

                    return new AuthenticationResult(
                        msalAccessTokenItem,
                        msalIdTokenItem,
                        AuthenticationRequestParameters.AuthenticationScheme,
                        AuthenticationRequestParameters.RequestContext.CorrelationId,
                        TokenSource.Cache,
                        AuthenticationRequestParameters.RequestContext.ApiEvent);
                }

                cacheInfoTelemetry = (msalAccessTokenItem == null) ? CacheInfoTelemetry.NoCachedAT : CacheInfoTelemetry.RefreshIn;
            }
            else
            {
                logger.Info("Skipped looking for an Access Token in the cache because ForceRefresh or Claims were set. ");
                cacheInfoTelemetry = CacheInfoTelemetry.ForceRefresh;
            }

            if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == (int)CacheInfoTelemetry.None)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = (int)cacheInfoTelemetry;
            }

            // No AT in the cache or AT needs to be refreshed
            try
            {
                return await FetchNewAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MsalServiceException e)
            {
                return HandleTokenRefreshError(e, msalAccessTokenItem);
            }
        }

        private async Task<AuthenticationResult> FetchNewAccessTokenAsync(CancellationToken cancellationToken)
        {
            var msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(), cancellationToken).ConfigureAwait(false);
            if (msalTokenResponse.ClientInfo is null &&
                AuthenticationRequestParameters.AuthorityInfo.AuthorityType != AuthorityType.Adfs)
            {
                var logger = AuthenticationRequestParameters.RequestContext.Logger;
                logger.Info("This is an on behalf of request for a service principal as no client info returned in the token response.");
            }

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
                [OAuth2Parameter.GrantType] = _onBehalfOfParameters.UserAssertion.AssertionType,
                [OAuth2Parameter.Assertion] = _onBehalfOfParameters.UserAssertion.Assertion,
                [OAuth2Parameter.RequestedTokenUse] = OAuth2RequestedTokenUse.OnBehalfOf
            };
            return dict;
        }

        protected override KeyValuePair<string, string> GetCCSHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return new KeyValuePair<string, string>();
        }
    }
}
