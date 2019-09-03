// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <summary>
    /// RetryWithExponentialBackoff runs a task with an exponential backoff
    /// </summary>
    internal sealed class RetryWithExponentialBackoff
    {
        private readonly int _maxRetries, _delayMilliseconds, _maxDelayMilliseconds;

        /// <summary>
        /// Create an instance of a RetryWithExponentialBackoff
        /// </summary>
        /// <param name="maxRetries">maximum number of retries (default 50)</param>
        /// <param name="delayMilliseconds">initial delay in milliseconds (default 100)</param>
        /// <param name="maxDelayMilliseconds">maximum delay in milliseconds (default 2000)</param>
        public RetryWithExponentialBackoff(int maxRetries = 50, int delayMilliseconds = 100, int maxDelayMilliseconds = 2000)
        {
            const string errMsgGe = "should be greater than or equal 0";
            const string errMsgG = "should be greater than 0";
            if (maxRetries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, errMsgGe);
            }

            if (delayMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), delayMilliseconds, errMsgG);
            }

            if (maxDelayMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelayMilliseconds), maxDelayMilliseconds, errMsgGe);
            }

            _maxRetries = maxRetries;
            _delayMilliseconds = delayMilliseconds;
            _maxDelayMilliseconds = maxDelayMilliseconds;
        }

        /// <summary>
        /// RunAsync will attempt to execute the a task with a exponential retry
        /// </summary>
        /// <param name="func">task to execute</param>
        /// <returns>exponentially backed off task</returns>
        public async Task RunAsync(Func<Task> func)
        {
            var backoff = new ExponentialBackoff(_maxRetries, _delayMilliseconds, _maxDelayMilliseconds);
        retry:
            try
            {
                await func().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is TimeoutException || ex is HttpRequestException || ex is TaskCanceledException || ex is OperationCanceledException || ex is TransientManagedIdentityException)
            {
                Debug.WriteLine(ex.ToString());
                await backoff.DelayAsync().ConfigureAwait(false);
                goto retry;
            }
        }
    }
}
