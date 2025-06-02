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
        // referenced in unit tests
        public const int DefaultStsMaxRetries = 1;
        public const int DefaultManagedIdentityMaxRetries = 3;

        private const int DefaultStsRetryDelayMs = 1000;
        private const int DefaultManagedIdentityRetryDelayMs = 1000;

        public readonly int _defaultRetryDelayMs;
        private readonly int _maxRetries;
        private readonly Func<HttpResponse, Exception, bool> _retryCondition;
        private readonly LinearRetryStrategy _linearRetryStrategy = new LinearRetryStrategy();

        public DefaultRetryPolicy(RequestType requestType)
        {
            switch (requestType)
            {
                case RequestType.ManagedIdentityDefault:
                    _defaultRetryDelayMs = DefaultManagedIdentityRetryDelayMs;
                    _maxRetries = DefaultManagedIdentityMaxRetries;
                    _retryCondition = HttpRetryConditions.DefaultManagedIdentity;
                    break;
                case RequestType.STS:
                    _defaultRetryDelayMs = DefaultStsRetryDelayMs;
                    _maxRetries = DefaultStsMaxRetries;
                    _retryCondition = HttpRetryConditions.Sts;
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
            if (_retryCondition(response, exception) &&
                retryCount < _maxRetries)
            {
                // Use HeadersAsDictionary to check for "Retry-After" header
                string retryAfter = string.Empty;
                if (response?.HeadersAsDictionary != null)
                {
                    response.HeadersAsDictionary.TryGetValue("Retry-After", out retryAfter);
                }

                int retryAfterDelay = _linearRetryStrategy.calculateDelay(retryAfter, _defaultRetryDelayMs);

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
