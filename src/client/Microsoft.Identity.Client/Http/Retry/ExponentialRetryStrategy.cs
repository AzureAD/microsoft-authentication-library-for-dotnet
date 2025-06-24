// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Http.Retry
{
    internal class ExponentialRetryStrategy
    {
        // Minimum backoff time in milliseconds
        private int _minExponentialBackoff;
        // Maximum backoff time in milliseconds
        private int _maxExponentialBackoff;
        // Maximum backoff time in milliseconds
        private int _exponentialDeltaBackoff;

        public ExponentialRetryStrategy(int minExponentialBackoff, int maxExponentialBackoff, int exponentialDeltaBackoff)
        {
            _minExponentialBackoff = minExponentialBackoff;
            _maxExponentialBackoff = maxExponentialBackoff;
            _exponentialDeltaBackoff = exponentialDeltaBackoff;
        }

        /// <summary>
        /// Calculates the exponential delay based on the current retry attempt.
        /// </summary>
        /// <param name="currentRetry">The current retry attempt number.</param>
        /// <returns>The calculated exponential delay in milliseconds.</returns>
        /// <remarks>
        /// The delay is calculated using the formula:
        /// - If <paramref name="currentRetry"/> is 0, it returns the minimum backoff time.
        /// - Otherwise, it calculates the delay as the minimum of:
        ///   - (2^(currentRetry - 1)) * deltaBackoff
        ///   - maxBackoff
        /// This ensures that the delay increases exponentially with each retry attempt,
        /// but does not exceed the maximum backoff time.
        /// </remarks>
        public int CalculateDelay(int currentRetry)
        {
            // Attempt 1
            if (currentRetry == 0)
            {
                return _minExponentialBackoff;
            }

            // Attempt 2+
            int exponentialDelay = Math.Min(
                (int)(Math.Pow(2, currentRetry - 1) * _exponentialDeltaBackoff),
                _maxExponentialBackoff
            );

            return exponentialDelay;
        }
    }
}
