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
    /// This implementation is not suitable for high-scale applications / confidential client scenarios
    /// because a static HttpClient might run into DNS issues (and creating new HttpClients leads to a port exhaustion issue).
    /// Mobile platforms should use HttpClientHandlers that are platform specific.
    /// .NET Core should use the IHttpClientFactory, but MSAL cannot take a dependency on it as it has too many dependencies itself.
    /// </remarks>
    internal class SimpleHttpClientFactory : IMsalHttpClientFactory
    {
        //Prior to the changes needed in order to make MSAL's httpClients thread safe (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2046/files),
        //the httpClient had the possibility to throw an exception stating "Properties can only be modified before sending the first request".
        //MSAL's httpClient will no longer throw this exception after 4.19.0 (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/releases/tag/4.19.0)
        private static readonly Lazy<HttpClient> s_httpClient = new Lazy<HttpClient>(() => InitializeClient());

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
