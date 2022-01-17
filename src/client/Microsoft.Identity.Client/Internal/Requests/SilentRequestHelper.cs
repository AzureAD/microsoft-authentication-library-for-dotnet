// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal
{
    internal static class SilentRequestHelper
    {
        internal static async Task<MsalTokenResponse> RefreshAccessTokenAsync(MsalRefreshTokenCacheItem msalRefreshTokenItem, RequestBase request, AuthenticationRequestParameters authenticationRequestParameters, CancellationToken cancellationToken)
        {
            authenticationRequestParameters.RequestContext.Logger.Verbose("Refreshing access token...");
            await authenticationRequestParameters.AuthorityManager.RunInstanceDiscoveryAndValidationAsync().ConfigureAwait(false);

            var msalTokenResponse = await request.SendTokenRequestAsync(GetBodyParameters(msalRefreshTokenItem.Secret), cancellationToken)
                                    .ConfigureAwait(false);

            if (msalTokenResponse.RefreshToken == null)
            {
                msalTokenResponse.RefreshToken = msalRefreshTokenItem.Secret;
                authenticationRequestParameters.RequestContext.Logger.Warning(
                    "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead. ");
            }

            return msalTokenResponse;
        }

        private static Dictionary<string, string> GetBodyParameters(string refreshTokenSecret)
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.ClientInfo] = "1",
                [OAuth2Parameter.GrantType] = OAuth2GrantType.RefreshToken,
                [OAuth2Parameter.RefreshToken] = refreshTokenSecret
            };

            return dict;
        }

        internal static bool NeedsRefresh(MsalAccessTokenCacheItem oldAccessToken)
        {
            return NeedsRefresh(oldAccessToken, out _);
        }

        internal static bool NeedsRefresh(MsalAccessTokenCacheItem oldAccessToken, out DateTimeOffset? refreshOnWithJitter)
        {
            refreshOnWithJitter = GetRefreshOnWithJitter(oldAccessToken);
            if (refreshOnWithJitter.HasValue && refreshOnWithJitter.Value < DateTimeOffset.UtcNow)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Fire and forget the fetch action on a background thread.
        /// Do not change to Task and do not await it.
        /// </summary>
        internal static void ProcessFetchInBackground(
            MsalAccessTokenCacheItem oldAccessToken,
            Func<Task<AuthenticationResult>> fetchAction,
            ICoreLogger logger)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await fetchAction().ConfigureAwait(false);
                }
                catch (MsalServiceException ex)
                {
                    string logMsg = $"Background fetch failed with MsalServiceException. Is AAD down? { ex.IsAadUnavailable()}";
                    if (ex.StatusCode == 400)
                    {
                        logger.ErrorPiiWithPrefix(ex, logMsg);
                    }
                    else
                    {
                        logger.WarningPiiWithPrefix(ex, logMsg);
                    }
                }
                catch (Exception ex)
                {
                    string logMsg = $"Background fetch failed with exception.";
                    logger.WarningPiiWithPrefix(ex, logMsg);
                }
            });
        }

        private static Random s_random = new Random();
        private static DateTimeOffset? GetRefreshOnWithJitter(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            if (msalAccessTokenCacheItem.RefreshOn.HasValue)
            {
                int jitter = s_random.Next(-Constants.DefaultJitterRangeInSeconds, Constants.DefaultJitterRangeInSeconds);
                var refreshOnWithJitter = msalAccessTokenCacheItem.RefreshOn.Value + TimeSpan.FromSeconds(jitter);
                return refreshOnWithJitter;
            }

            
            return null;           
        }
    }
}
