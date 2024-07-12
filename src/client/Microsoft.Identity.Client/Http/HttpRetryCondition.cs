// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Http
{
    internal static class HttpRetryConditions
    {
        /// <summary>
        /// Retry policy specific to managed identity flow.
        /// Avoid changing this, as it's breaking change.
        /// </summary>
        public static bool ManagedIdentity(HttpResponse response, Exception exception)
        {
            if (exception != null)
            {
                return exception is TaskCanceledException ? true : false;
            } 

            return (int)response.StatusCode switch
            {
                //Not Found
                404 or 408 or 429 or 500 or 503 or 504 => true,
                _ => false,
            };
        }

        /// <summary>
        /// Retry condition for /token and /authorize endpoints
        /// </summary>
        /// <param name="response"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static bool Sts(HttpResponse response, Exception exception)
        {
            if (exception != null)
            {
                return exception is TaskCanceledException ? true : false;
            }

            var retryAfter = response?.Headers?.RetryAfter;
            bool hasRetryAfterHeader = retryAfter != null &&
                (retryAfter.Delta.HasValue || retryAfter.Date.HasValue);

            // Don't retry if the STS told us to back off
            if (hasRetryAfterHeader)
                return false;

            int statusCode = (int)response.StatusCode;

            return statusCode >= 500 && statusCode < 600;
        }
    }
}
