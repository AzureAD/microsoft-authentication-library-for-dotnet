// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
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
            ILoggerAdapter logger,
            IServiceBundle serviceBundle,
            ApiEvent apiEvent,
            string callerSdkId,
            string callerSdkVersion,
            Action<ExecutionResult, IList<KeyValuePair<string, object>>> tagsEnricher = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var authResult = await fetchAction().ConfigureAwait(false);

                    // Only materialize an ExecutionResult when an enricher is configured to consume it.
                    ExecutionResult executionResult = tagsEnricher == null
                        ? null
                        : new ExecutionResult { Successful = true, Result = authResult };

                    serviceBundle.PlatformProxy.OtelInstrumentation.IncrementSuccessCounter(
                        serviceBundle.PlatformProxy.GetProductName(),
                        apiEvent.ApiId,
                        callerSdkId,
                        callerSdkVersion,
                        TokenSource.IdentityProvider,
                        CacheRefreshReason.ProactivelyRefreshed,
                        Cache.CacheLevel.None,
                        logger,
                        apiEvent.TokenType,
                        executionResult,
                        tagsEnricher);

                    serviceBundle.PlatformProxy.OtelInstrumentation.LogRemainingTokenLifetime(
                        serviceBundle.PlatformProxy.GetProductName(),
                        apiEvent.ApiId,
                        TokenSource.IdentityProvider,
                        Cache.CacheLevel.None,
                        CacheRefreshReason.ProactivelyRefreshed,
                        apiEvent.TokenType,
                        authResult.ExpiresOn,
                        logger,
                        executionResult,
                        tagsEnricher);

                    serviceBundle.PlatformProxy.OtelInstrumentation.LogSuccessHttpDuration(
                        serviceBundle.PlatformProxy.GetProductName(),
                        apiEvent.ApiId,
                        authResult.AuthenticationResultMetadata,
                        logger,
                        executionResult,
                        tagsEnricher);
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

                    LogBackgroundFailureTelemetry(serviceBundle, apiEvent, callerSdkId, callerSdkVersion,
                        ex.ErrorCode, ex.StatusCode, ex.ErrorCodes?.FirstOrDefault(), ex, tagsEnricher, logger);
                }
                catch (OperationCanceledException ex)
                {
                    logger.WarningPiiWithPrefix(ex, ProactiveRefreshCancellationError);
                    LogBackgroundFailureTelemetry(serviceBundle, apiEvent, callerSdkId, callerSdkVersion,
                        ex.GetType().Name, httpStatusCode: 0, tagsEnricher: tagsEnricher, logger: logger);
                }
                catch (Exception ex)
                {
                    logger.ErrorPiiWithPrefix(ex, ProactiveRefreshGeneralError);
                    LogBackgroundFailureTelemetry(serviceBundle, apiEvent, callerSdkId, callerSdkVersion,
                        ex.GetType().Name, httpStatusCode: 0, tagsEnricher: tagsEnricher, logger: logger);
                }
            });
        }

        // Records telemetry for a fire-and-forget background refresh failure: increments the
        // failure counter and records V2 HTTP duration when an HTTP exchange happened.
        // Total duration is deliberately not recorded — the foreground user already received
        // their token from cache, so this latency is not user-facing.
        private static void LogBackgroundFailureTelemetry(
            IServiceBundle serviceBundle,
            ApiEvent apiEvent,
            string callerSdkId,
            string callerSdkVersion,
            string errorCode,
            int httpStatusCode,
            string rawStsErrorCode = null,
            MsalException exception = null,
            Action<ExecutionResult, IList<KeyValuePair<string, object>>> tagsEnricher = null,
            ILoggerAdapter logger = null)
        {
            var otel = serviceBundle.PlatformProxy.OtelInstrumentation;
            var platform = serviceBundle.PlatformProxy.GetProductName();

            // Only materialize an ExecutionResult when an enricher is configured to consume it.
            ExecutionResult executionResult = tagsEnricher == null
                ? null
                : new ExecutionResult { Successful = false, Exception = exception };

            otel.IncrementFailureCounter(
                platform, errorCode, apiEvent.ApiId, callerSdkId, callerSdkVersion,
                CacheRefreshReason.ProactivelyRefreshed, apiEvent.TokenType, rawStsErrorCode,
                logger, executionResult, tagsEnricher);

            otel.LogFailureHttpDuration(
                platform, apiEvent, httpStatusCode, logger, executionResult, tagsEnricher);
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
