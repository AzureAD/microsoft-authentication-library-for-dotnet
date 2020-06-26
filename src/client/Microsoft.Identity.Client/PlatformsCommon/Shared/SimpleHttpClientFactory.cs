// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// A simple implementation of the HttpClient factory that uses a managed HttpClientHandler
    /// </summary>
    /// <remarks>
    /// This implementation is not suitable for high-scale applications / confidential client scenarios
    /// because creating new HttpClients leads to a port exhaustion issue. 
    /// Mobile platforms should use HttpClientHandlers that are platform specific.
    /// See platform specific implementations for details on these issues.
    /// </remarks>
    internal class SimpleHttpClientFactory : IMsalHttpClientFactory
    {
        private readonly HttpClient _httpClient;


        public SimpleHttpClientFactory()
        {
            _httpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
            HttpClientConfig.ConfigureRequestHeadersAndSize(_httpClient);
        }

        public HttpClient GetHttpClient()
        {
            return _httpClient;
        }

    }
}
