// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http.Retry
{
    internal class RegionDiscoveryRetryPolicy : IRetryPolicy
    {
        // Referenced in unit tests
        public const int NumRetries = 3;

        private const int MinExponentialBackoffMs = 1000;
        private const int MaxExponentialBackoffMs = 4000;
        private const int ExponentialDeltaBackoffMs = 2000;

        private readonly ExponentialRetryStrategy _exponentialRetryStrategy = new ExponentialRetryStrategy(
            MinExponentialBackoffMs,
            MaxExponentialBackoffMs,
            ExponentialDeltaBackoffMs
        );

        internal virtual Task DelayAsync(int milliseconds)
        {
            return Task.Delay(milliseconds);
        }

        public async Task<bool> PauseForRetryAsync(HttpResponse response, Exception exception, int retryCount, ILoggerAdapter logger)
        {
            // Check if the status code is retriable and if the current retry count is less than max retries
            if (HttpRetryConditions.RegionDiscovery(response, exception) &&
                retryCount < NumRetries)
            {
                int retryAfterDelay = _exponentialRetryStrategy.CalculateDelay(retryCount);

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
