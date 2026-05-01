// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
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
        private static readonly ConcurrentDictionary<string, Lazy<HttpClient>> s_httpClientPool =
            new ConcurrentDictionary<string, Lazy<HttpClient>>();
        private static readonly object s_cacheLock = new object();

        private static int s_httpClientCreationCount;

        // referenced in unit tests
        internal static int HttpClientCreationCount => s_httpClientCreationCount;

        private static HttpClient CreateHttpClient()
        {
            Interlocked.Increment(ref s_httpClientCreationCount);
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
            Interlocked.Increment(ref s_httpClientCreationCount);
            CheckAndManageCache();

            if (bindingCertificate == null)
            {
                throw new ArgumentNullException(nameof(bindingCertificate), "A valid X509 certificate must be provided for mTLS.");
            }

            //Create an HttpClientHandler and configure it to use the client certificate
            HttpClientHandler handler = new();

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
            return s_httpClientPool.GetOrAdd(
                "non_mtls",
                _ => new Lazy<HttpClient>(CreateHttpClient, LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }

        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            if (x509Certificate2 == null)
            {
                return GetHttpClient();
            }

            string key = x509Certificate2.Thumbprint;
            return s_httpClientPool.GetOrAdd(
                key,
                _ => new Lazy<HttpClient>(() => CreateMtlsHttpClient(x509Certificate2), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
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

        // referenced in unit tests
        internal static void ResetStaticStateForTest()
        {
            lock (s_cacheLock)
            {
                foreach (Lazy<HttpClient> lazy in s_httpClientPool.Values)
                {
                    if (lazy.IsValueCreated)
                        lazy.Value?.Dispose();
                }

                s_httpClientPool.Clear();
                Interlocked.Exchange(ref s_httpClientCreationCount, 0);
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

            return new HttpClient(handler);
#else
            return GetHttpClient();
#endif
        }
    }
}
