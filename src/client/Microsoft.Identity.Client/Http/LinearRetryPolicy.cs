// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Http
{
    internal class LinearRetryPolicy : IRetryPolicy
    {
        
        private int _maxRetries;
        private readonly Func<HttpResponse, Exception, bool> _retryCondition;
        public int DelayInMilliseconds { private set; get; }

        public LinearRetryPolicy(int delayMilliseconds, int maxRetries, Func<HttpResponse, Exception, bool> retryCondition)
        {
            DelayInMilliseconds = delayMilliseconds;
            _maxRetries = maxRetries;
            _retryCondition = retryCondition;
        }

        public bool pauseForRetry(HttpResponse response, Exception exception, int retryCount)
        {
            return retryCount < _maxRetries && _retryCondition(response, exception);
        }
    }
}
