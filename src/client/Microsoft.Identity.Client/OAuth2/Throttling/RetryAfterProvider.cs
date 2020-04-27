// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    internal class RetryAfterProvider : IThrottlingProvider
    {

        internal ThrottlingCache Cache { get; } // internal for test only

        internal static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(3600); // internal for test only

        public RetryAfterProvider()
        {
            Cache = new ThrottlingCache();
        }

        public void RecordException(
            AuthenticationRequestParameters requestParams, 
            IReadOnlyDictionary<string, string> bodyParams, 
            MsalServiceException ex)
        {
            if (ThrottleCommon.IsRetryAfterAndHttpStatusThrottlingSupported(requestParams) &&
                TryGetRetryAfterValue(ex.Headers, out TimeSpan retryAfterTimespan))
            {
                retryAfterTimespan = GetSafeValue(retryAfterTimespan);

                var logger = requestParams.RequestContext.Logger;
                logger.Info($"[Throttling] Retry-After header detected, " +
                    $"value: {retryAfterTimespan.TotalSeconds} seconds");

                string thumbprint = ThrottleCommon.GetRequestStrictThumbprint(
                    bodyParams,
                    requestParams.AuthorityInfo.CanonicalAuthority,
                    requestParams.Account?.HomeAccountId?.Identifier);
                var entry = new ThrottlingCacheEntry(ex, retryAfterTimespan);

                Cache.AddAndCleanup(thumbprint, entry, logger);
            }
        }

        public void ResetCache()
        {
            Cache.Clear();
        }

        public void TryThrottle(
            AuthenticationRequestParameters requestParams, 
            IReadOnlyDictionary<string, string> bodyParams)
        {
            if (!Cache.IsEmpty() && 
                ThrottleCommon.IsRetryAfterAndHttpStatusThrottlingSupported(requestParams))
            {
                var logger = requestParams.RequestContext.Logger;

                string strictThumbprint = ThrottleCommon.GetRequestStrictThumbprint(
                    bodyParams,
                    requestParams.AuthorityInfo.CanonicalAuthority,
                    requestParams.Account?.HomeAccountId?.Identifier);

                ThrottleCommon.TryThrow(strictThumbprint, Cache, logger, nameof(RetryAfterProvider));
            }
        }

        public static bool TryGetRetryAfterValue(HttpResponseHeaders headers, out TimeSpan retryAfterTimespan)
        {
            retryAfterTimespan = TimeSpan.Zero;

            var date = headers?.RetryAfter?.Date;
            if (date.HasValue)
            {
                retryAfterTimespan = date.Value - DateTimeOffset.Now;
                return true;
            }

            var delta = headers?.RetryAfter?.Delta;
            if (delta.HasValue)
            {
                retryAfterTimespan = delta.Value;
                return true;
            }
            return false;
        }

        private static TimeSpan GetSafeValue(TimeSpan headerValue)
        {
            if (headerValue > MaxRetryAfter)
            {
                return MaxRetryAfter;
            }

            return headerValue;
        }
    }
}
