// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Http.Retry
{
    internal class LinearRetryStrategy
    {
        /// <summary>
        /// Calculates the number of milliseconds to sleep based on the `Retry-After` HTTP header.
        /// </summary>
        /// <param name="retryHeader">The value of the `Retry-After` HTTP header. This can be either a number of seconds or an HTTP date string.</param>
        /// <param name="minimumDelay">The minimum delay in milliseconds to return if the header is not present or invalid.</param>
        /// <returns>The number of milliseconds to sleep before retrying the request.</returns>
        public int calculateDelay(string retryHeader, int minimumDelay)
        {
            if (string.IsNullOrEmpty(retryHeader))
            {
                return minimumDelay;
            }

            // Try parsing the retry-after header as seconds
            if (double.TryParse(retryHeader, out double seconds))
            {
                int millisToSleep = (int)Math.Round(seconds * 1000);
                return Math.Max(minimumDelay, millisToSleep);
            }

            // If parsing as seconds fails, try parsing as an HTTP date
            if (DateTime.TryParse(retryHeader, out DateTime retryDate))
            {
                DateTime.TryParse(DateTime.UtcNow.ToString("R"), out DateTime nowDate);

                int millisToSleep = (int)(retryDate - nowDate).TotalMilliseconds;
                return Math.Max(minimumDelay, millisToSleep);
            }

            // If all parsing fails, return the minimum delay
            return minimumDelay;
        }
    }
}
