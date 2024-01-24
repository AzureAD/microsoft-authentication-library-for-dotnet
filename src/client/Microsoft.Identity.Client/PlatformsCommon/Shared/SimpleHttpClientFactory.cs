// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// A simple implementation of the HttpClient factory that uses a managed HttpClientHandler
    /// </summary>
    /// <remarks>
    /// .NET should use the IHttpClientFactory, but MSAL cannot take a dependency on it.
    /// .NET should use SocketHandler, but UseDefaultCredentials doesn't work with it 
    /// </remarks>
    internal class SimpleHttpClientFactory : IMsalHttpClientFactory
    {
        //Please see (https://aka.ms/msal-httpclient-info) for important information regarding the HttpClient.
        private static readonly Lazy<HttpClient> s_httpClient = new Lazy<HttpClient>(InitializeClient);

        private static HttpClient InitializeClient()
        {
            var httpClient = new HttpClient(new HttpClientHandler() { 
                /* important for IWA */ UseDefaultCredentials = true });
            HttpClientConfig.ConfigureRequestHeadersAndSize(httpClient);

            return httpClient;
        }

        public HttpClient GetHttpClient()
        {
            return s_httpClient.Value;
        }
    }
}
