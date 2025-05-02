// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http
{
    class DefaultRetryPolicy : IRetryPolicy
    {
        private LinearRetryStrategy linearRetryStrategy = new LinearRetryStrategy();

        // referenced in unit tests
        public const int DefaultStsMaxRetries = 1;
        public const int DefaultManagedIdentityMaxRetries = 3;

        // overridden in the unit tests so that they run faster
        public static int DefaultStsRetryDelayMs { get; set; } = 1000;
        public static int DefaultManagedIdentityRetryDelayMs { get; set; } = 1000;

        // used for comparison, in the unit tests
        // will be reset after every test
        public static int NumRetries { get; set; } = 0;

        public static int DefaultRetryDelayMs;
        private int MaxRetries;
        private readonly Func<HttpResponse, Exception, bool> RetryCondition;

        public DefaultRetryPolicy(
            int retryDelayMs,
            int maxRetries,
            Func<HttpResponse, Exception, bool> retryCondition)
        {
            DefaultRetryDelayMs = retryDelayMs;
            MaxRetries = maxRetries;
            RetryCondition = retryCondition;
        }

        public async Task<bool> PauseForRetryAsync(HttpResponse response, Exception exception, int retryCount, ILoggerAdapter logger)
        {
            // Check if the status code is retriable and if the current retry count is less than max retries
            if (RetryCondition(response, exception) &&
                retryCount < MaxRetries)
            {
                // used below in the log statement, also referenced in the unit tests
                NumRetries = retryCount + 1;

                // Use HeadersAsDictionary to check for "Retry-After" header
                string retryAfter = string.Empty;
                if (response?.HeadersAsDictionary != null)
                {
                    response.HeadersAsDictionary.TryGetValue("Retry-After", out retryAfter);
                }

                int retryAfterDelay = linearRetryStrategy.calculateDelay(retryAfter, DefaultRetryDelayMs);

                logger.Warning($"Retrying request in {retryAfterDelay}ms (retry attempt: {NumRetries})");

                // Pause execution for the calculated delay
                await Task.Delay(retryAfterDelay).ConfigureAwait(false);

                return true;
            }

            // If the status code is not retriable or max retries have been reached, do not retry
            return false;
        }
    }
}
