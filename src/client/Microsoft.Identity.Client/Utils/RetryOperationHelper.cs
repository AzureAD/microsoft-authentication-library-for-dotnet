// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Utils
{
    /// <summary>
    /// Provides functionality to automatically try the given piece of logic some number of times before re-throwing the exception. 
    /// This is useful for any piece of code which may experience transient failures. Be cautious of passing code with two distinct 
    /// actions given that if the second or subsequent piece of logic fails, the first will also be retried upon each retry. 
    /// </summary>
    /// <remarks>
    /// Copied from https://github.com/microsoft/RetryOperationHelper/blob/master/RetryOperationHelper/RetryOperationHelper.cs
    /// </remarks>
    internal static class RetryOperationHelper
    {
        /// <summary>Executes asynchronous function with retry logic.</summary>
        /// <param name="func">The asynchronous function to be executed.</param>
        /// <param name="maxAttempts">The maximum number of attempts.</param>
        /// <param name="retryInterval">Timespan to wait between attempts of the operation</param>
        /// <param name="onAttemptFailed">The callback executed when an attempt is failed.</param>
        /// <typeparam name="T">The result type.</typeparam>
        /// <returns>The <see cref="Task"/> producing the result.</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> func, int maxAttempts, TimeSpan? retryInterval = null, Action<int, Exception> onAttemptFailed = null)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "The maximum number of attempts must not be less than 1.");
            }

            var attempt = 0;

            while (true)
            {
                if (attempt > 0 && retryInterval != null)
                {
                    await Task.Delay(retryInterval.Value).ConfigureAwait(true);
                }

                try
                {
                    //Call the function passed in by the caller. 
                    return await func().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    attempt++;

                    onAttemptFailed?.Invoke(attempt, exception);

                    if (attempt >= maxAttempts)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>Executes asynchronous function with retry logic.</summary>
        /// <param name="func">The asynchronous function to be executed.</param>
        /// <param name="maxAttempts">The maximum number of attempts.</param>
        /// <param name="retryInterval">Timespan to wait between attempts of the operation</param>
        /// <param name="onAttemptFailed">The retry handler.</param>
        /// <returns>The <see cref="Task"/> producing the result.</returns>
        public static async Task ExecuteWithRetryAsync(Func<Task> func, int maxAttempts, TimeSpan? retryInterval = null, Action<int, Exception> onAttemptFailed = null)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            Func<Task<bool>> wrapper = async () =>
            {
                await func().ConfigureAwait(false);
                return true;
            };

            await ExecuteWithRetryAsync(wrapper, maxAttempts, retryInterval, onAttemptFailed).ConfigureAwait(true);
        }
    }
}
