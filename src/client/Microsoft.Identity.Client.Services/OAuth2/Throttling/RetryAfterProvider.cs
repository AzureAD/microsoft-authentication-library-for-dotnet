// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    /// <summary>
    /// The Retry-After provider observes all service exceptions from all flows and looks for a header like: RetryAfter X seconds.
    /// It then enforces this header, by throttling for X seconds.
    /// </summary>
    internal class RetryAfterProvider : IThrottlingProvider
    {

        internal ThrottlingCache ThrottlingCache { get; } // internal for test only

        internal static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(3600); // internal for test only

        public RetryAfterProvider()
        {
            ThrottlingCache = new ThrottlingCache();
        }

        public void RecordException(
            AuthenticationRequestParameters requestParams, 
            IReadOnlyDictionary<string, string> bodyParams, 
            MsalServiceException ex)
        {
            if (TryGetRetryAfterValue(ex.Headers, out TimeSpan retryAfterTimespan))
            {
                retryAfterTimespan = GetSafeValue(retryAfterTimespan);

                var logger = requestParams.RequestContext.Logger;
                logger.Info($"[Throttling] Retry-After header detected, " +
                    $"value: {retryAfterTimespan.TotalSeconds} seconds");

                string thumbprint = ThrottleCommon.GetRequestStrictThumbprint(
                    bodyParams,
                    requestParams.AuthorityInfo.CanonicalAuthority.ToString(),
                    requestParams.Account?.HomeAccountId?.Identifier);
                var entry = new ThrottlingCacheEntry(ex, retryAfterTimespan);

                ThrottlingCache.AddAndCleanup(thumbprint, entry, logger);
            }
        }

        public void ResetCache()
        {
            ThrottlingCache.Clear();
        }

        public void TryThrottle(
            AuthenticationRequestParameters requestParams, 
            IReadOnlyDictionary<string, string> bodyParams)
        {
            if (!ThrottlingCache.IsEmpty())
            {
                var logger = requestParams.RequestContext.Logger;

                string strictThumbprint = ThrottleCommon.GetRequestStrictThumbprint(
                    bodyParams,
                    requestParams.AuthorityInfo.CanonicalAuthority.ToString(),
                    requestParams.Account?.HomeAccountId?.Identifier);

                ThrottleCommon.TryThrowServiceException(strictThumbprint, ThrottlingCache, logger, nameof(RetryAfterProvider));
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
