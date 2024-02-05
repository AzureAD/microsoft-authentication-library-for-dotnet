// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
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
    internal class SimpleHttpClientFactory : IMsalMtlsHttpClientFactory
    {
        //Please see (https://aka.ms/msal-httpclient-info) for important information regarding the HttpClient.
        private static readonly ConcurrentDictionary<string,HttpClient> s_httpClient = new ConcurrentDictionary<string, HttpClient>();

        private static HttpClient CreateNonMtlsClient()
        {
            var httpClient = new HttpClient(new HttpClientHandler()
            {
                /* important for IWA */
                UseDefaultCredentials = true
            });
            HttpClientConfig.ConfigureRequestHeadersAndSize(httpClient);

            return httpClient;
        }

        private static HttpClient CreateMtlsHttpClient(X509Certificate2 bindingCertificate)
        {
            if (s_httpClient.Count > 1000)
                s_httpClient.Clear();

            //Create an HttpClientHandler and configure it to use the client certificate
            HttpClientHandler handler = new();
            //To-Do need to refine this when Managed Identity V2 TFMs are defined
#if SUPPORTS_MIV2
            handler.ClientCertificates.Add(bindingCertificate);
#endif
            var httpClient = new HttpClient(handler);
            HttpClientConfig.ConfigureRequestHeadersAndSize(httpClient);

            return httpClient;
        }

        public HttpClient GetHttpClient()
        {
            return s_httpClient.GetOrAdd("non_mtls", CreateNonMtlsClient());
        }

        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            if (x509Certificate2 == null)
            {
                return GetHttpClient();
            }

            string key = x509Certificate2.Thumbprint;
            return s_httpClient.GetOrAdd(key, CreateMtlsHttpClient(x509Certificate2));
        }
    }
}
