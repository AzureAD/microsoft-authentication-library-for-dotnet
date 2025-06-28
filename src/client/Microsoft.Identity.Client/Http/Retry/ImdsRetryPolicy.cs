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
        private readonly bool _isProbe;

        private readonly ExponentialRetryStrategy _exponentialRetryStrategy = new ExponentialRetryStrategy(
            ImdsRetryPolicy.MinExponentialBackoffMs,
            ImdsRetryPolicy.MaxExponentialBackoffMs,
            ImdsRetryPolicy.ExponentialDeltaBackoffMs
        );

        /// <summary>
        /// Creates the standard IMDS retry policy.
        /// </summary>
        /// <param name="isProbe">
        ///     <c>false</c> (default) → use <see cref="HttpRetryConditions.Imds"/>.<br/>
        ///     <c>true </c>          → use <see cref="HttpRetryConditions.ImdsProbe"/>.
        /// </param>
        public ImdsRetryPolicy(bool isProbe = false)
        {
            _isProbe = isProbe;
        }

        internal virtual Task DelayAsync(int milliseconds)
        {
            return Task.Delay(milliseconds);
        }

        public async Task<bool> PauseForRetryAsync(
            HttpResponse response,
            Exception exception,
            int retryCount,
            ILoggerAdapter logger)
        {
            int statusCode = (int)response.StatusCode;

            if (retryCount == 0)
            {
                // compute once per request
                _maxRetries = statusCode == (int)HttpStatusCode.Gone
                    ? LinearStrategyNumRetries
                    : ExponentialStrategyNumRetries;
            }

            /* -------------- choose predicate based on _isProbe -------------- */
            bool shouldRetry = _isProbe
                ? HttpRetryConditions.ImdsProbe(response, exception)
                : HttpRetryConditions.Imds(response, exception);

            if (shouldRetry && retryCount < _maxRetries)
            {
                int delay = statusCode == (int)HttpStatusCode.Gone
                    ? HttpStatusGoneRetryAfterMs
                    : _exponentialRetryStrategy.CalculateDelay(retryCount);

                logger.Warning($"Retrying request in {delay}ms (retry attempt: {retryCount + 1})");
                await DelayAsync(delay).ConfigureAwait(false);
                return true;
            }

            // not retriable or max retries reached
            return false;
        }
    }
}
