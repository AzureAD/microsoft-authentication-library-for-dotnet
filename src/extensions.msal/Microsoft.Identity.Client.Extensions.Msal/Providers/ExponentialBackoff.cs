// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <summary>
    /// ExponentialBackoff implements an exponential backoff for a task
    /// </summary>
    internal class ExponentialBackoff
    {
        private readonly int _maxRetries, _maxPower;
        private readonly long _delayTicks, _maxDelayTicks;
        private int _retries;

        /// <summary>
        /// Create an instance of an ExponentialBackoff
        /// </summary>
        /// <param name="maxRetries">maximum number of retries</param>
        /// <param name="delayMilliseconds">initial delay in milliseconds</param>
        /// <param name="maxDelayMilliseconds">maximum delay in milliseconds</param>
        public ExponentialBackoff(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds)
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
            _delayTicks = delayMilliseconds * TimeSpan.TicksPerMillisecond;
            _maxDelayTicks = maxDelayMilliseconds * TimeSpan.TicksPerMillisecond;
            _retries = 0;
            _maxPower = 30 - (int)Math.Ceiling(Math.Log(delayMilliseconds, 2));
        }

        /// <summary>
        /// DelayAsync will create an exponentially growing delay task
        /// </summary>
        /// <returns>a task to be delayed</returns>
        /// <exception cref="TimeoutException">thrown upon exceeding the max number of retries</exception>
        public async Task DelayAsync()
        {
            if (_retries == _maxRetries)
            {
                throw new TooManyRetryAttemptsException();
            }

            _retries++;
            var delay = GetDelay(_retries);
            await Task.Delay(delay).ConfigureAwait(false);
        }

        internal TimeSpan GetDelay(int retryCount)
        {
            var ticks = long.MaxValue;
            if(retryCount < _maxPower)
            {
                ticks = (long)Math.Pow(2, retryCount) *_delayTicks;
            }
            var waitTicks = Math.Min(ticks, _maxDelayTicks);
            return TimeSpan.FromTicks(waitTicks);
        }
    }
}
