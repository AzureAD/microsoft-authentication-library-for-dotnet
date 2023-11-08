// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
    internal class MtlsHttpClientFactory : IMsalMtlsHttpClientFactory
    {
        // Please see (https://aka.ms/msal-httpclient-info) for important information regarding the HttpClient.

        // Pass the certificate in the constructor
        public MtlsHttpClientFactory(X509Certificate2 bindingCertificate)
        {
            BindingCertificate = bindingCertificate;
        }

        // Public property for the certificate
        public X509Certificate2 BindingCertificate { get; }

        private HttpClient InitializeClient()
        {
            // Create an HttpClientHandler and configure it to use the client certificate
            HttpClientHandler handler = new HttpClientHandler();
#if NET6_0 || NET6_WIN
            handler.ClientCertificates.Add(BindingCertificate);
#endif
            handler.UseDefaultCredentials = true;

            var httpClient = new HttpClient(handler);
            HttpClientConfig.ConfigureRequestHeadersAndSize(httpClient);

            return httpClient;
        }

        private readonly Lazy<HttpClient> _sHttpClient;

        public HttpClient GetHttpClient()
        {
            return _sHttpClient.Value;
        }

        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            return InitializeClient();
        }
    }
}
