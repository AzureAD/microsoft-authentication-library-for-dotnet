// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// A simple implementation of the HttpClient factory that uses a managed HttpClientHandler
    /// </summary>
    /// <remarks>
    /// .NET should use the IHttpClientFactory, but MSAL cannot take a dependency on it.
    /// .NET should use SocketHandler, but UseDefaultCredentials doesn't work with it 
    /// </remarks>
    internal class SimpleHttpClientFactory : IMsalMtlsHttpClientFactory, IMsalSFHttpClientFactory
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
            HttpClientConfig.Configure(httpClient);

#if NET5_0_OR_GREATER
            // Enable HTTP/2 with fallback to HTTP/1.1
            httpClient.DefaultRequestVersion = new Version(2, 0);
            httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
#endif

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
            HttpClientHandler handler = new();

            handler.ClientCertificates.Add(bindingCertificate);
            var httpClient = new HttpClient(handler);
            HttpClientConfig.Configure(httpClient);

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

        // This method is used for Service Fabric scenarios where a custom server certificate validation callback is required.
        // It allows the caller to provide a custom HttpClientHandler with the callback.
        // The server cert rotates so we need a new HttpClient for each call.
        public HttpClient GetHttpClient(Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validateServerCert)
        {
            if (validateServerCert == null)
            {
                return GetHttpClient();
            }

#if NET471_OR_GREATER || NETSTANDARD || NET
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    return validateServerCert(message, cert, chain, sslPolicyErrors);
                }
            };

            var httpClient = new HttpClient(handler);
            HttpClientConfig.Configure(httpClient);

            string key = handler.GetHashCode().ToString();
            return s_httpClientPool.GetOrAdd(key, httpClient);
#else
            return GetHttpClient();
#endif
        }
    }
}
