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

        public static void ConfigureRequestHeadersAndSize(HttpClient httpClient)
        {
            httpClient.MaxResponseContentBufferSize = MaxResponseContentBufferSizeInBytes;
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
