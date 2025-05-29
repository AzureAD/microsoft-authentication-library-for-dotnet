// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http.Retry
{
    internal class ImdsRetryPolicy : IRetryPolicy
    {
        // referenced in unit tests
        public const int ExponentialStrategyNumRetries = 3;
        public const int LinearStrategyNumRetries = 7;

        // used for comparison, in the unit tests
        // will be reset after every test
        public static int NumRetries { get; set; } = 0;

        private const int MinExponentialBackoffMs = 1000;
        private const int MaxExponentialBackoffMs = 4000;
        private const int ExponentialDeltaBackoffMs = 2000;
        private const int HttpStatusGoneRetryAfterMs = 10000;

        private int MaxRetries;

        private ExponentialRetryStrategy _exponentialRetryStrategy = new ExponentialRetryStrategy(
            ImdsRetryPolicy.MinExponentialBackoffMs,
            ImdsRetryPolicy.MaxExponentialBackoffMs,
            ImdsRetryPolicy.ExponentialDeltaBackoffMs
        );

        internal virtual Task DelayAsync(int milliseconds)
        {
            return Task.Delay(milliseconds);
        }

        public async Task<bool> PauseForRetryAsync(HttpResponse response, Exception exception, int retryCount, ILoggerAdapter logger)
        {
            int httpStatusCode = (int)response.StatusCode;

            if (retryCount == 0)
            {
                // Calculate the maxRetries based on the status code, once per request
                MaxRetries = httpStatusCode == (int)HttpStatusCode.Gone
                    ? LinearStrategyNumRetries
                    : ExponentialStrategyNumRetries;
            }

            // Check if the status code is retriable and if the current retry count is less than max retries
            if (HttpRetryConditions.Imds(response, exception) &&
                retryCount < MaxRetries)
            {
                // used below in the log statement, also referenced in the unit tests
                NumRetries = retryCount + 1;

                int retryAfterDelay = httpStatusCode == (int)HttpStatusCode.Gone
                    ? HttpStatusGoneRetryAfterMs
                    : _exponentialRetryStrategy.CalculateDelay(retryCount);

                logger.Warning($"Retrying request in {retryAfterDelay}ms (retry attempt: {retryCount + 1})");

                // Pause execution for the calculated delay
                await DelayAsync(retryAfterDelay).ConfigureAwait(false);

                return true;
            }

            // If the status code is not retriable or max retries have been reached, do not retry
            return false;
        }
    }
}
