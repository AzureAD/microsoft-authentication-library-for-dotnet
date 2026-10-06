// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.CompilerServices;
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
    internal class SimpleHttpClientFactory :
        IMsalMtlsHttpClientFactory,
        IMsalSFHttpClientFactory,
        IMsalWsTrustHttpClientFactory
    {
        //Please see (https://aka.ms/msal-httpclient-info) for important information regarding the HttpClient.
        private static readonly ConcurrentDictionary<string, HttpClient> s_httpClientPool = new ConcurrentDictionary<string, HttpClient>();

        // The handler retains the supplied certificate instance, so equivalent certificate bytes
        // must not cause a client bound to a different instance to be returned. Weak keys allow
        // both the certificate and its client to be collected when the caller releases the certificate.
        private static readonly MtlsHttpClientCache s_mtlsHttpClientPool =
            new MtlsHttpClientCache(CreateMtlsHttpClient);
        private static readonly object s_cacheLock = new object();

        private static HttpClient CreateHttpClient(
            bool allowAutoRedirect,
            bool useDefaultCredentials)
        {
            CheckAndManageCache();

            var httpClient = new HttpClient(new HttpClientHandler()
            {
                /* important for IWA */
                UseDefaultCredentials = useDefaultCredentials,
                AllowAutoRedirect = allowAutoRedirect
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
            return GetHttpClient(
                allowAutoRedirect: true,
                useDefaultCredentials: true);
        }

        HttpClient IMsalWsTrustHttpClientFactory.GetHttpClient(bool useDefaultCredentials)
        {
            return GetHttpClient(
                allowAutoRedirect: false,
                useDefaultCredentials);
        }

        private static HttpClient GetHttpClient(
            bool allowAutoRedirect,
            bool useDefaultCredentials)
        {
            string key = useDefaultCredentials
                ? allowAutoRedirect
                    ? "non_mtls"
                    : "non_mtls_no_redirect"
                : allowAutoRedirect
                    ? "non_mtls_no_default_credentials"
                    : "non_mtls_no_redirect_no_default_credentials";
            return s_httpClientPool.GetOrAdd(
                key,
                _ => CreateHttpClient(
                    allowAutoRedirect,
                    useDefaultCredentials));
        }

        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            if (x509Certificate2 is null)
            {
                return GetHttpClient();
            }

            return s_mtlsHttpClientPool.GetOrCreate(x509Certificate2);
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

            string key = handler.GetHashCode().ToString();
            return s_httpClientPool.GetOrAdd(key, new HttpClient(handler));
#else
            return GetHttpClient();
#endif
        }
    }

    internal sealed class MtlsHttpClientCache
    {
        private readonly ConditionalWeakTable<X509Certificate2, Lazy<HttpClient>> _httpClients =
            new ConditionalWeakTable<X509Certificate2, Lazy<HttpClient>>();
        private readonly Func<X509Certificate2, HttpClient> _createHttpClient;

        internal MtlsHttpClientCache(Func<X509Certificate2, HttpClient> createHttpClient)
        {
            _createHttpClient = createHttpClient ?? throw new ArgumentNullException(nameof(createHttpClient));
        }

        internal HttpClient GetOrCreate(X509Certificate2 certificate)
        {
            return _httpClients.GetValue(
                certificate,
                key => new Lazy<HttpClient>(() => _createHttpClient(key), isThreadSafe: true)).Value;
        }
    }
}
