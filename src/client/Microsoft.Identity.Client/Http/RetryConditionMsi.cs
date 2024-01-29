// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Http
{
    internal static class HttpRetryConditions
    {
        public static bool NoRetry(HttpResponse response)
        {
            return false;
        }

        /// <summary>
        /// Retry policy specific to managed identity flow.
        /// Avoid changing this, as it's breaking change.
        /// </summary>
        public static bool Msi(HttpResponse response)
        {
            switch ((int)response.StatusCode)
            {
                case 404: //Not Found
                case 408: // Request Timeout
                case 429: // Too Many Requests
                case 500: // Internal Server Error
                case 503: // Service Unavailable
                case 504: // Gateway Timeout
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Retry condition for /token and /authorize endpoints
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool Sts(HttpResponse response)
        {
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
