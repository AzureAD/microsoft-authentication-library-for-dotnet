// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// TooManyRetryAttemptsException occurs when a retry strategy exceeds the max number of retries
    /// </summary>
    public class TooManyRetryAttemptsException : MsalClientException
    {
        private const string Code = "max_retries_exhausted";
        private const string ErrorMessage = "max retry attempts exceeded.";

        /// <inheritdoc />
        /// <summary>
        /// Create a TooManyRetryAttemptsException
        /// </summary>
        public TooManyRetryAttemptsException() : base(Code, ErrorMessage) { }

        /// <summary>
        /// Create a TooManyRetryAttemptsException with an error message
        /// </summary>
        public TooManyRetryAttemptsException(string errorMessage) : base(Code, errorMessage) { }
    }
}
