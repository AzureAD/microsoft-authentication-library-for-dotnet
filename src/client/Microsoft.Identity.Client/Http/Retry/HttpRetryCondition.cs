// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Http.Retry
{
    internal static class HttpRetryConditions
    {
        /// <summary>
        /// Retry policy specific to managed identity flow.
        /// Avoid changing this, as it's a breaking change.
        /// </summary>
        public static bool DefaultManagedIdentity(HttpResponse response, Exception exception)
        {
            if (exception != null)
            {
                return exception is TaskCanceledException ? true : false;
            } 

            return (int)response.StatusCode switch
            {
                // Not Found, Request Timeout, Too Many Requests, Server Error, Service Unavailable, Gateway Timeout
                404 or 408 or 429 or 500 or 503 or 504 => true,
                _ => false,
            };
        }

        /// <summary>
        /// Retry policy specific to IMDS Managed Identity.
        /// </summary>
        public static bool Imds(HttpResponse response, Exception exception)
        {
            if (exception != null)
            {
                return exception is TaskCanceledException ? true : false;
            }

            return (int)response.StatusCode switch
            {
                // Not Found, Request Timeout, Gone, Too Many Requests
                404 or 408 or 410 or 429 => true,
                // Server Error range
                >= 500 and <= 599 => true,
                _ => false,
            };
        }

        /// <summary>
        /// Retry policy specific to Region Discovery.
        /// Extends Imds retry policy but excludes 404 and 408 status codes.
        /// </summary>
        public static bool RegionDiscovery(HttpResponse response, Exception exception)
        {
            if (!Imds(response, exception))
            {
                return false;
            }

            // If Imds would retry but the status code is 404 or 408, don't retry
            return (int)response.StatusCode is not (404 or 408);
        }

        /// <summary>
        /// Retry policy specific to Region Discovery.
        /// Extends Imds retry policy but excludes 404 and 408 status codes.
        /// </summary>
        public static bool CsrMetadataProbe(HttpResponse response, Exception exception)
        {
            if (!Imds(response, exception))
            {
                return false;
            }

            // If Imds would retry but the status code is 404 or 408, don't retry
            return (int)response.StatusCode is not 404;
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
