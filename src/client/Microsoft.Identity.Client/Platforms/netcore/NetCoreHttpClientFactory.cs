// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.Platforms.netcore
{
    internal class NetCoreHttpClientFactory : IMsalHttpClientFactory
    {
        private static readonly Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => InitializeClient());

        private static HttpClient InitializeClient()
        {
            var handler = new SocketsHttpHandler
            {
                // https://github.com/dotnet/corefx/issues/26895
                // https://github.com/dotnet/corefx/issues/26331
                // https://github.com/dotnet/corefx/pull/26839
                PooledConnectionLifetime = HttpClientConfig.ConnectionLifeTime,
                PooledConnectionIdleTimeout = HttpClientConfig.ConnectionLifeTime,
                MaxConnectionsPerServer = HttpClientConfig.MaxConnections,
            };

            var client = new HttpClient(handler);

            HttpClientConfig.ConfigureRequestHeadersAndSize(client);

            return client;
        }

        public HttpClient GetHttpClient()
        {
            return _httpClient.Value;
        }
    }
}
