// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http.Retry
{
    // Interface for implementing retry logic for HTTP requests. 
    // Determines if a retry should occur and handles pause logic between retries.
    internal interface IRetryPolicy
    {
        /// <summary>
        /// Determines whether a retry should be attempted for a given HTTP response or exception,
        /// and performs any necessary pause or delay logic before the next retry attempt.
        /// </summary>
        /// <param name="response">The HTTP response received from the request.</param>
        /// <param name="exception">The exception encountered during the request.</param>
        /// <param name="retryCount">The current retry attempt count.</param>
        /// <param name="logger">The logger used for diagnostic and informational messages.</param>
        /// <returns>A task that returns true if a retry should be performed; otherwise, false.</returns>
        Task<bool> PauseForRetryAsync(HttpResponse response, Exception exception, int retryCount, ILoggerAdapter logger);
    }
}
