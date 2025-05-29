// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using static Microsoft.Identity.Client.Internal.Constants;

namespace Microsoft.Identity.Client.Http.Retry
{
    class DefaultRetryPolicy : IRetryPolicy
    {
        private LinearRetryStrategy linearRetryStrategy = new LinearRetryStrategy();

        // referenced in unit tests
        public const int DefaultStsMaxRetries = 1;
        public const int DefaultManagedIdentityMaxRetries = 3;

        // used for comparison, in the unit tests
        // will be reset after every test
        public static int NumRetries { get; set; } = 0;

        private const int _DefaultStsRetryDelayMs = 1000;
        private const int _DefaultManagedIdentityRetryDelayMs = 1000;

        public static int DefaultRetryDelayMs;
        private int _maxRetries;
        private readonly Func<HttpResponse, Exception, bool> RetryCondition;

        public DefaultRetryPolicy(RequestType requestType)
        {
            switch (requestType)
            {
                case RequestType.ManagedIdentityDefault:
                    DefaultRetryDelayMs = _DefaultManagedIdentityRetryDelayMs;
                    _maxRetries = DefaultManagedIdentityMaxRetries;
                    RetryCondition = HttpRetryConditions.DefaultManagedIdentity;
                    break;
                case RequestType.STS:
                    DefaultRetryDelayMs = _DefaultStsRetryDelayMs;
                    _maxRetries = DefaultStsMaxRetries;
                    RetryCondition = HttpRetryConditions.Sts;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestType), requestType, "Unknown request type");
            }
        }

        internal virtual Task DelayAsync(int milliseconds)
        {
            return Task.Delay(milliseconds);
        }

        public async Task<bool> PauseForRetryAsync(HttpResponse response, Exception exception, int retryCount, ILoggerAdapter logger)
        {
            // Check if the status code is retriable and if the current retry count is less than max retries
            if (RetryCondition(response, exception) &&
                retryCount < _maxRetries)
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
                await DelayAsync(retryAfterDelay).ConfigureAwait(false);

                return true;
            }

            // If the status code is not retriable or max retries have been reached, do not retry
            return false;
        }
    }
}
