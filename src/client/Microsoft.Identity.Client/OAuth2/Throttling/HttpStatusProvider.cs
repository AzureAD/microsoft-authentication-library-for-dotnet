// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    internal class HttpStatusProvider : IThrottlingProvider
    {
        /// <summary>
        /// Default timespan that blocks an application, if HTTP 429 and HTTP 5xx was recieved and Retry-After HTTP header was NOT returned by AAD.
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

            if (ThrottleCommon.IsRetryAfterAndHttpStatusThrottlingSupported(requestParams) &&
                (ex.StatusCode == 429 || (ex.StatusCode >= 500 && ex.StatusCode < 600)) &&
                // if a retry-after header is present, another provider will take care of this
                !RetryAfterProvider.TryGetRetryAfterValue(ex.Headers, out _)) 
            {
                logger.Info($"[Throttling] Http status code {ex.StatusCode} encountered - " +
                    $"throttling for {s_throttleDuration.TotalSeconds} seconds");

                var thumbprint = ThrottleCommon.GetRequestStrictThumbprint(bodyParams,
                    requestParams.AuthorityInfo.CanonicalAuthority,
                    requestParams.Account?.HomeAccountId?.Identifier);
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
                ThrottleCommon.IsRetryAfterAndHttpStatusThrottlingSupported(requestParams))
            {
                var logger = requestParams.RequestContext.Logger;

                string strictThumbprint = ThrottleCommon.GetRequestStrictThumbprint(
                    bodyParams,
                    requestParams.AuthorityInfo.CanonicalAuthority,
                    requestParams.Account?.HomeAccountId?.Identifier);

                ThrottleCommon.TryThrow(strictThumbprint, ThrottlingCache, logger, nameof(HttpStatusProvider));
            }
        }
    }
}
