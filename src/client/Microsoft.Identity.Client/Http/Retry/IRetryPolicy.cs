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
        Task<bool> PauseForRetryAsync(HttpResponse response, Exception exception, int retryCount, ILoggerAdapter logger);
    }
}
