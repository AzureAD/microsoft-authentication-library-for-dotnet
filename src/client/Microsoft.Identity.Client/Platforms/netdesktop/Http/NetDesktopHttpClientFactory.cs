// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.Platforms.net45.Http
{
    internal class NetDesktopHttpClientFactory : IMsalHttpClientFactory
    {
        private static HttpClient s_httpClient;
        private static object s_lock = new object();

        public HttpClient GetHttpClient()
        {
            EnsureInitialized();
            return s_httpClient;
        }

        private static void EnsureInitialized()
        {
            if (s_httpClient == null)
            {
                lock (s_lock)
                {
                    s_httpClient = new HttpClient(new DnsSensitiveClientHandler());

                    HttpClientConfig.ConfigureRequestHeadersAndSize(s_httpClient);
                }
            }
        }       
    }
}
