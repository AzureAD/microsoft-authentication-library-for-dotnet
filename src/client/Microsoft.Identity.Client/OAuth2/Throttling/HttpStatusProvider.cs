// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    internal class HttpStatusProvider : IThrottlingProvider
    {
        /// <summary>
        /// Default timespan that blocks an application, if HTTP 429 and HTTP 5xx was received and Retry-After HTTP header was NOT returned by AAD.
        /// </summary>
        internal static readonly TimeSpan s_throttleDuration = TimeSpan.FromSeconds(60); // internal for test

        /// <summary>
        /// Exposed only for testing purposes
        /// </summary>
        internal ThrottlingCache ThrottlingCache { get; }

        public HttpStatusProvider()
        {
            ThrottlingCache = new ThrottlingCache();
        }

        public void RecordException(
            AuthenticationRequestParameters requestParams, 
            IReadOnlyDictionary<string, string> bodyParams, 
            MsalServiceException ex)
        {
            var logger = requestParams.RequestContext.Logger;

            if (IsRequestSupported(requestParams) &&
                (ex.StatusCode == 429 || (ex.StatusCode >= 500 && ex.StatusCode < 600)) &&
                // if a retry-after header is present, another provider will take care of this
                !RetryAfterProvider.TryGetRetryAfterValue(ex.Headers, out _)) 
            {
                logger.Info(() => $"[Throttling] HTTP status code {ex.StatusCode} encountered - " +
                    $"throttling for {s_throttleDuration.TotalSeconds} seconds. ");

                string authority = requestParams.AuthorityInfo.CanonicalAuthority.ToString();
                string homeAccountId = requestParams.Account?.HomeAccountId?.Identifier;

                // HTTP 5xx is a server/credential error that can be specific to a single user
                // (e.g. a federated STS returning HTTP 500 for one user's bad password). Key it
                // per-user so it does not throttle other users.
                // HTTP 429 is service-directed rate limiting for the whole application, so it keeps
                // the app-wide strict thumbprint.
                bool isServerError = ex.StatusCode >= 500 && ex.StatusCode < 600;
                var thumbprint = isServerError
                    ? ThrottleCommon.GetRequestUserAwareThumbprint(bodyParams, authority, homeAccountId)
                    : ThrottleCommon.GetRequestStrictThumbprint(bodyParams, authority, homeAccountId);

                var entry = new ThrottlingCacheEntry(ex, s_throttleDuration);
                ThrottlingCache.AddAndCleanup(thumbprint, entry, logger);
            }
        }

        public void ResetCache()
        {
            ThrottlingCache.Clear();
        }

        public void TryThrottle(AuthenticationRequestParameters requestParams, IReadOnlyDictionary<string, string> bodyParams)
        {
            if (!ThrottlingCache.IsEmpty() &&
                IsRequestSupported(requestParams))
            {
                var logger = requestParams.RequestContext.Logger;

                string authority = requestParams.AuthorityInfo.CanonicalAuthority.ToString();
                string homeAccountId = requestParams.Account?.HomeAccountId?.Identifier;

                // App-wide key catches service-directed 429 throttles.
                string appWideThumbprint = ThrottleCommon.GetRequestStrictThumbprint(
                    bodyParams,
                    authority,
                    homeAccountId);

                ThrottleCommon.TryThrowServiceException(appWideThumbprint, ThrottlingCache, logger, nameof(HttpStatusProvider));

                // Per-user key catches error-class (5xx) throttles so one user does not block another.
                // Only check it when it actually differs from the app-wide key (i.e. there is a user component).
                string userAwareThumbprint = ThrottleCommon.GetRequestUserAwareThumbprint(
                    bodyParams,
                    authority,
                    homeAccountId);

                if (!string.Equals(userAwareThumbprint, appWideThumbprint, System.StringComparison.Ordinal))
                {
                    ThrottleCommon.TryThrowServiceException(userAwareThumbprint, ThrottlingCache, logger, nameof(HttpStatusProvider));
                }
            }
        }

        private static bool IsRequestSupported(AuthenticationRequestParameters requestParameters)
        {
            return !requestParameters.AppConfig.IsConfidentialClient;
        }
    }
}
