using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Azure
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
            catch (Exception ex) when (ex is TimeoutException || ex is HttpRequestException || ex is TaskCanceledException || ex is TransientManagedIdentityException)
            {
                Debug.WriteLine(ex.ToString());
                await backoff.DelayAsync().ConfigureAwait(false);
                goto retry;
            }
        }
    }

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
            if(delayMilliseconds <= 0)
            {
                throw new ArgumentException("delayMilliseconds must be greater than 0");
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

            var delay = GetDelay(++_retries);
            await Task.Delay(delay).ConfigureAwait(false);
        }

        internal TimeSpan GetDelay(int retryCount)
        {
            var ticks = long.MaxValue;
            if(retryCount < _maxPower)
            {
                ticks = (long)Math.Pow(2, retryCount) *_delayTicks;
            }
            long waitTicks = Math.Min(ticks, _maxDelayTicks);
            return TimeSpan.FromTicks(waitTicks);
        }
    }

    /// <summary>
    /// TooManyRetryAttemptsException occurs when a retry strategy exceeds the max number of retries
    /// </summary>
    public class TooManyRetryAttemptsException : MsalClientException
    {
        private const string Code = "max_retries_exhausted";
        private const string ErrorMessage = "max retry attempts exceeded.";

        /// <summary>
        /// Create a TooManyRetryAttemptsException
        /// </summary>
        public TooManyRetryAttemptsException() : base(Code, ErrorMessage) { }

        /// <summary>
        /// Create a TooManyRetryAttemptsException with an error message
        /// </summary>
        public TooManyRetryAttemptsException(string errorMessage) : base(Code, errorMessage) { }
    }

    /// <summary>
    /// TransientManagedIdentityException occurs when a 404, 429 or a 500 series error is encountered.
    ///
    /// see: https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-use-vm-token#error-handling
    /// </summary>
    public class TransientManagedIdentityException : MsalClientException
    {
        private const string Code = "transient_managed_identity_error";
        private const string ErrorMessage = "encountered a transient error response from the managed identity service";

        /// <summary>
        /// Create a TransientManagedIdentityException
        /// </summary>
        public TransientManagedIdentityException() : base(Code, ErrorMessage) { }

        /// <summary>
        /// Create a TransientManagedIdentityException with an error message
        /// </summary>
        public TransientManagedIdentityException(string errorMessage) : base(Code, errorMessage) { }
    }

    /// <summary>
    /// BadManagedIdentityException occurs when a 400 is returned from the managed identity service
    ///
    /// see: https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-use-vm-token#error-handling
    /// </summary>
    public class BadRequestManagedIdentityException : MsalServiceException
    {
        private const string Code = "bad_managed_identity_error";
        private const string ErrorMessage = "invalid resource; the application was not found in the tenant.";

        /// <summary>
        /// Create a BadManagedIdentityException
        /// </summary>
        public BadRequestManagedIdentityException() : base(Code, ErrorMessage) { }

        /// <summary>
        /// Create a BadManagedIdentityException with an error message
        /// </summary>
        /// <param name="errorMessage">exception error message</param>
        public BadRequestManagedIdentityException(string errorMessage) : base(Code, errorMessage) { }
    }
}
