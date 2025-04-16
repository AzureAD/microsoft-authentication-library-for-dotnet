// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http
{
    internal class ImdsRetryPolicy : IRetryPolicy
    {
        public const int EXPONENTIAL_STRATEGY_NUM_RETRIES = 3; // referenced in unit tests
        public const int LINEAR_STRATEGY_NUM_RETRIES = 7; // referenced in unit tests
        private const int HTTP_STATUS_GONE_RETRY_AFTER_MS_INTERNAL = 10 * 1000; // 10 seconds

        // these will be overridden in the unit tests so that they run faster
        public static int MIN_EXPONENTIAL_BACKOFF_MS { get; set; } = 1000;
        public static int MAX_EXPONENTIAL_BACKOFF_MS { get; set; } = 4000;
        public static int EXPONENTIAL_DELTA_BACKOFF_MS { get; set; } = 2000;
        public static int HTTP_STATUS_GONE_RETRY_AFTER_MS { get; set; } = HTTP_STATUS_GONE_RETRY_AFTER_MS_INTERNAL;

        private int _maxRetries;

        private ExponentialRetryStrategy _exponentialRetryStrategy = new ExponentialRetryStrategy(
            ImdsRetryPolicy.MIN_EXPONENTIAL_BACKOFF_MS,
            ImdsRetryPolicy.MAX_EXPONENTIAL_BACKOFF_MS,
            ImdsRetryPolicy.EXPONENTIAL_DELTA_BACKOFF_MS
        );

        public async Task<bool> PauseForRetryAsync(HttpResponse response, Exception exception, int retryCount, ILoggerAdapter logger)
        {
            int httpStatusCode = (int)response.StatusCode;

            if (retryCount == 0)
            {
                // Calculate the maxRetries based on the status code, once per request
                _maxRetries = httpStatusCode == (int)HttpStatusCode.Gone
                    ? LINEAR_STRATEGY_NUM_RETRIES
                    : EXPONENTIAL_STRATEGY_NUM_RETRIES;
            }

            // Check if the status code is retriable and if the current retry count is less than max retries
            if (HttpRetryConditions.Imds(response, exception) &&
                retryCount < _maxRetries)
            {
                int retryAfterDelay = httpStatusCode == (int)HttpStatusCode.Gone
                    ? HTTP_STATUS_GONE_RETRY_AFTER_MS
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
