// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Http
{
    internal class LinearRetryPolicy : IRetryPolicy
    {
        // referenced in unit tests, cannot be private
        public static int numRetries { get; private set; } = 0;
        public const int DefaultStsMaxRetries = 1;
        // this will be overridden in the unit tests so that they run faster
        public static int DefaultStsRetryDelayMs { get; set; } = 1000;

        private int _maxRetries;
        private readonly Func<HttpResponse, Exception, bool> _retryCondition;
        public int DelayInMilliseconds { private set; get; }

        public LinearRetryPolicy(int delayMilliseconds, int maxRetries, Func<HttpResponse, Exception, bool> retryCondition)
        {
            DelayInMilliseconds = delayMilliseconds;
            _maxRetries = maxRetries;
            _retryCondition = retryCondition;
        }

        public bool PauseForRetry(HttpResponse response, Exception exception, int retryCount)
        {
            // referenced in the unit tests
            numRetries = retryCount + 1;

            return retryCount < _maxRetries && _retryCondition(response, exception);
        }
    }
}
