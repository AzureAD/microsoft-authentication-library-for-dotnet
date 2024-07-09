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
        private static readonly ConcurrentDictionary<string, HttpClient> s_httpClientPool = new ConcurrentDictionary<string, HttpClient>();
        private static readonly object s_cacheLock = new object();

        private static HttpClient CreateHttpClient()
        {
            CheckAndManageCache();

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
#if SUPPORTS_MTLS
            CheckAndManageCache();

            if (bindingCertificate == null)
            {
                throw new ArgumentNullException(nameof(bindingCertificate), "A valid X509 certificate must be provided for mTLS.");
            }

            //Create an HttpClientHandler and configure it to use the client certificate
            HttpClientHandler handler = new HttpClientHandler();

            handler.ClientCertificates.Add(bindingCertificate);
            var httpClient = new HttpClient(handler);
            HttpClientConfig.ConfigureRequestHeadersAndSize(httpClient);

            return httpClient;
#else
            throw new NotSupportedException("mTLS is not supported on this platform.");
#endif
        }

        public HttpClient GetHttpClient()
        {
            return s_httpClientPool.GetOrAdd("non_mtls", CreateHttpClient());
        }

        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            if (x509Certificate2 == null)
            {
                return GetHttpClient();
            }

            string key = x509Certificate2.Thumbprint;
            return s_httpClientPool.GetOrAdd(key, CreateMtlsHttpClient(x509Certificate2));
        }

        private static void CheckAndManageCache()
        {
            lock (s_cacheLock)
            {
                if (s_httpClientPool.Count >= 1000)
                {
                    s_httpClientPool.Clear();
                }
            }
        }
    }
}
