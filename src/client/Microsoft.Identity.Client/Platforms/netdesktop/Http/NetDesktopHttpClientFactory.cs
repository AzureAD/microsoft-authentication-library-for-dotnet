// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.Platforms.net45.Http
{
    internal class NetDesktopHttpClientFactory : IMsalHttpClientFactory
    {
        //Please see (https://aka.ms/msal-httpclient-info) for important information regarding the HttpClient.
        private static readonly Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => InitializeClient());

        public HttpClient GetHttpClient()
        {
            return _httpClient.Value;
        }

        private static HttpClient InitializeClient()
        {
            var client = new HttpClient(new DnsSensitiveClientHandler());

            HttpClientConfig.ConfigureRequestHeadersAndSize(client);

            return client;
        }
    }
}
