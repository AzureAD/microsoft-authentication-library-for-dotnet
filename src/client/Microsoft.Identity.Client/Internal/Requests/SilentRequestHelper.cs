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
#if iOS
using Microsoft.Identity.Client.Platforms.iOS;
#endif

namespace Microsoft.Identity.Client.Internal
{
    internal static class SilentRequestHelper
    {
        internal const string MamEnrollmentIdKey = "microsoft_enrollment_id";
        internal const string ProactiveRefreshServiceError = "Proactive token refresh failed with MsalServiceException.";
        internal const string ProactiveRefreshGeneralError = "Proactive token refresh failed with exception.";
        internal const string ProactiveRefreshCancellationError = "Proactive token refresh was canceled.";

        internal static async Task<MsalTokenResponse> RefreshAccessTokenAsync(MsalRefreshTokenCacheItem msalRefreshTokenItem, RequestBase request, AuthenticationRequestParameters authenticationRequestParameters, CancellationToken cancellationToken)
        {
            authenticationRequestParameters.RequestContext.Logger.Verbose(() => "Refreshing access token...");
            await authenticationRequestParameters.AuthorityManager.RunInstanceDiscoveryAndValidationAsync().ConfigureAwait(false);

            var dict = GetBodyParameters(msalRefreshTokenItem.Secret);
#if iOS
                var realEnrollmentId = IntuneEnrollmentIdHelper.GetEnrollmentId(authenticationRequestParameters.RequestContext.Logger);
                if(!string.IsNullOrEmpty(realEnrollmentId))
                {
                    dict[MamEnrollmentIdKey] = realEnrollmentId;
                }
#endif
            var msalTokenResponse = await request.SendTokenRequestAsync(dict, cancellationToken)
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
            ILoggerAdapter logger)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await fetchAction().ConfigureAwait(false);
                }
                catch (MsalServiceException ex)
                {
                    string logMsg = $"{ProactiveRefreshServiceError} Is exception retryable? {ex.IsRetryable}";
                    if (ex.StatusCode == 400)
                    {
                        logger.ErrorPiiWithPrefix(ex, logMsg);
                    }
                    else
                    {
                        logger.ErrorPiiWithPrefix(ex, logMsg);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    logger.WarningPiiWithPrefix(ex, ProactiveRefreshCancellationError);
                }
                catch (Exception ex)
                {
                    logger.ErrorPiiWithPrefix(ex, ProactiveRefreshGeneralError);
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
