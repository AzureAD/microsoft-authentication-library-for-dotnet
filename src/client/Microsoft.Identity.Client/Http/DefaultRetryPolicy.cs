// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http
{
    class DefaultRetryPolicy : IRetryPolicy
    {
        // this will be overridden in the unit tests so that they run faster
        public static int RETRY_DELAY_MS { get; set; }

        private int _maxRetries;

        private LinearRetryStrategy linearRetryStrategy = new LinearRetryStrategy();

        private readonly Func<HttpResponse, Exception, bool> _retryCondition;

        public DefaultRetryPolicy(int retryDelayMs, int maxRetries, Func<HttpResponse, Exception, bool> retryCondition)
        {
            RETRY_DELAY_MS = retryDelayMs;
            _maxRetries = maxRetries;
            _retryCondition = retryCondition;
        }

        public async Task<bool> PauseForRetryAsync(HttpResponse response, Exception exception, int retryCount, ILoggerAdapter logger)
        {
            // Check if the status code is retriable and if the current retry count is less than max retries
            if (_retryCondition(response, exception) &&
                retryCount < _maxRetries)
            {
                // Use HeadersAsDictionary to check for "Retry-After" header
                response.HeadersAsDictionary.TryGetValue("Retry-After", out string retryAfter);

                int retryAfterDelay = linearRetryStrategy.calculateDelay(retryAfter, RETRY_DELAY_MS);

                logger.Warning($"Retrying request in {retryAfterDelay}ms (retry attempt: {retryCount + 1})");

                // Pause execution for the calculated delay
                await Task.Delay(retryAfterDelay).ConfigureAwait(false);
            }

            // If the status code is not retriable or max retries have been reached, do not retry
            return false;
        }
    }
}
