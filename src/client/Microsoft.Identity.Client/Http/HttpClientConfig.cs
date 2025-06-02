// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Identity.Client.Http
{
    internal static class HttpClientConfig
    {
        public const long MaxResponseContentBufferSizeInBytes = 1024 * 1024;
        public const int MaxConnections = 50; // default depends on runtime but it is much smaller
        public static readonly TimeSpan ConnectionLifeTime = TimeSpan.FromMinutes(1);

        public static void Configure(HttpClient httpClient)
        {
            httpClient.MaxResponseContentBufferSize = MaxResponseContentBufferSizeInBytes;
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

#if NET5_0_OR_GREATER
            // Enable HTTP/2 with fallback to HTTP/1.1
            httpClient.DefaultRequestVersion = new Version(2, 0);
            httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
#endif
        }
    }
}
