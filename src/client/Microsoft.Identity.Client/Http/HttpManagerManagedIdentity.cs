// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Http
{
    /// <remarks>
    /// HTTP Manager specific to managed identity to implement the retry for specific HTTP status codes.
    /// </remarks>
    internal class HttpManagerManagedIdentity : HttpManagerWithRetry
    {
        public HttpManagerManagedIdentity(IMsalHttpClientFactory httpClientFactory) : 
            base(httpClientFactory) { }

        /// <summary>
        /// Retry policy specific to managed identity flow.
        /// Avoid changing this, as it's breaking change.
        /// </summary>
        protected override bool IsRetryableStatusCode(int statusCode)
        {
            switch (statusCode)
            {
                case 404: //Not Found
                case 408: // Request Timeout
                case 429: // Too Many Requests
                case 500: // Internal Server Error
                case 503: // Service Unavailable
                case 504: // Gateway Timeout
                    return true;
                default:
                    return false;
            }             
        }
    }
}
