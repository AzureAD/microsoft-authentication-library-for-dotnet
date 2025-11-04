// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http.Retry
{
    // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/docs/imds_retry_based_on_errors.md
    internal class ImdsRetryPolicy : IRetryPolicy
    {
        // referenced in unit tests
        public const int ExponentialStrategyNumRetries = 3;
        public const int LinearStrategyNumRetries = 7;

        private const int MinExponentialBackoffMs = 1000;
        private const int MaxExponentialBackoffMs = 4000;
        private const int ExponentialDeltaBackoffMs = 2000;
        private const int HttpStatusGoneRetryAfterMs = 10000;

        private int _maxRetries;

        private readonly ExponentialRetryStrategy _exponentialRetryStrategy = new ExponentialRetryStrategy(
            ImdsRetryPolicy.MinExponentialBackoffMs,
            ImdsRetryPolicy.MaxExponentialBackoffMs,
            ImdsRetryPolicy.ExponentialDeltaBackoffMs
        );

        internal virtual Task DelayAsync(int milliseconds)
        {
            return Task.Delay(milliseconds);
        }

        protected virtual bool ShouldRetry(HttpResponse response, Exception exception)
        {
            return HttpRetryConditions.Imds(response, exception);
        }

        public async Task<bool> PauseForRetryAsync(HttpResponse response, Exception exception, int retryCount, ILoggerAdapter logger)
        {
            int httpStatusCode = (int)response.StatusCode;

            if (retryCount == 0)
            {
                // Calculate the maxRetries based on the status code, once per request
                _maxRetries = httpStatusCode == (int)HttpStatusCode.Gone
                    ? LinearStrategyNumRetries
                    : ExponentialStrategyNumRetries;
            }

            // Check if the status code is retriable and if the current retry count is less than max retries
            if (ShouldRetry(response, exception) &&
                retryCount < _maxRetries)
            {
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
