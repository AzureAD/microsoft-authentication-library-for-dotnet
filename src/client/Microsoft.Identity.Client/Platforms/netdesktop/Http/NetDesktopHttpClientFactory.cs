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
        //Prior to the changes needed in order to make MSAL's httpClients thread safe (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2046/files),
        //the httpClient had the possibility to throw an exception stating "Properties can only be modified before sending the first request".
        //MSAL's httpClient will no longer throw this exception after 4.19.0 (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/releases/tag/4.19.0)
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
