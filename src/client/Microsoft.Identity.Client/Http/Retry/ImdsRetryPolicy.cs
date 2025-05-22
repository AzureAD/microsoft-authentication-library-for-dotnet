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
        private const int HttpStatusGoneRetryAfterMsInternal = 10 * 1000; // 10 seconds

        // referenced in unit tests
        public const int ExponentialStrategyNumRetries = 3;
        public const int LinearStrategyNumRetries = 7;

        // used for comparison, in the unit tests
        // will be reset after every test
        public static int NumRetries { get; set; } = 0;

        // overridden in the unit tests so that they run faster
        public static int MinExponentialBackoffMs { get; set; } = 1000;
        public static int MaxExponentialBackoffMs { get; set; } = 4000;
        public static int ExponentialDeltaBackoffMs { get; set; } = 2000;
        public static int HttpStatusGoneRetryAfterMs { get; set; } = HttpStatusGoneRetryAfterMsInternal;

        private int MaxRetries;

        private ExponentialRetryStrategy _exponentialRetryStrategy = new ExponentialRetryStrategy(
            ImdsRetryPolicy.MinExponentialBackoffMs,
            ImdsRetryPolicy.MaxExponentialBackoffMs,
            ImdsRetryPolicy.ExponentialDeltaBackoffMs
        );

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
                await Task.Delay(retryAfterDelay).ConfigureAwait(false);

                return true;
            }

            // If the status code is not retriable or max retries have been reached, do not retry
            return false;
        }
    }
}
