// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.TestOnly
{
    /// <summary>
    /// A minimal <see cref="IMsalMtlsHttpClientFactory"/> for tests that need explicit control over
    /// both plain and mTLS <see cref="HttpClient"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="MockHttpClientFactory"/>, this class accepts a single
    /// <see cref="MockHttpMessageHandler"/> at construction time and replays it for every call.
    /// Use it when you want the same handler to handle multiple requests, or when you want to
    /// verify that an mTLS certificate was (or was not) sent.
    /// </para>
    /// <example>
    /// <code>
    /// var handler = new MockHttpMessageHandler { ... };
    /// var factory = new MockMtlsHttpClientFactory(handler);
    ///
    /// var app = ManagedIdentityApplicationBuilder
    ///     .Create(ManagedIdentityId.SystemAssigned)
    ///     .WithHttpClientFactory(factory)
    ///     .Build();
    /// </code>
    /// </example>
    /// </remarks>
    public class MockMtlsHttpClientFactory : IMsalMtlsHttpClientFactory
    {
        private readonly MockHttpMessageHandler _handler;

        /// <summary>
        /// Initializes a new instance of <see cref="MockMtlsHttpClientFactory"/>.
        /// </summary>
        /// <param name="handler">The handler to use for all requests.</param>
        public MockMtlsHttpClientFactory(MockHttpMessageHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Returns an <see cref="HttpClient"/> backed by the configured handler without a certificate.
        /// </summary>
        public HttpClient GetHttpClient()
        {
            return BuildClient(null);
        }

        /// <summary>
        /// Returns an <see cref="HttpClient"/> backed by the configured handler with
        /// <paramref name="mtlsBindingCert"/> attached as a client certificate.
        /// </summary>
        /// <param name="mtlsBindingCert">The certificate to attach.</param>
        public HttpClient GetHttpClient(X509Certificate2 mtlsBindingCert)
        {
            return BuildClient(mtlsBindingCert);
        }

        private HttpClient BuildClient(X509Certificate2 cert)
        {
            if (cert != null)
            {
                _handler.ClientCertificates.Add(cert);
            }

            var httpClient = new HttpClient(_handler, disposeHandler: false)
            {
                MaxResponseContentBufferSize = HttpClientConfig.MaxResponseContentBufferSizeInBytes
            };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }
    }
}
