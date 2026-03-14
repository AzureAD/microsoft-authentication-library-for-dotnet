// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.TestOnly
{
    /// <summary>
    /// An <see cref="IMsalMtlsHttpClientFactory"/> that dequeues pre-configured <see cref="MockHttpMessageHandler"/>
    /// instances to supply <see cref="HttpClient"/> instances during tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This factory is used internally by <see cref="MockHttpManager"/>.
    /// For most test scenarios, prefer <see cref="MockHttpManager"/> over this class directly.
    /// </para>
    /// <example>
    /// <code>
    /// var factory = new MockHttpClientFactory();
    /// factory.AddMockHandler(new MockHttpMessageHandler
    /// {
    ///     ExpectedUrl    = "https://my-service/token",
    ///     ExpectedMethod = HttpMethod.Post,
    ///     ResponseMessage = MockHelpers.CreateSuccessResponseMessage("{\"access_token\":\"t\"}")
    /// });
    ///
    /// var app = ManagedIdentityApplicationBuilder
    ///     .Create(ManagedIdentityId.SystemAssigned)
    ///     .WithHttpClientFactory(factory)
    ///     .Build();
    /// </code>
    /// </example>
    /// </remarks>
    public class MockHttpClientFactory : IMsalMtlsHttpClientFactory
    {
        private readonly ConcurrentQueue<HttpClientHandler> _queue;

        /// <summary>
        /// Initializes a new instance of <see cref="MockHttpClientFactory"/> with an empty handler queue.
        /// </summary>
        public MockHttpClientFactory()
        {
            _queue = new ConcurrentQueue<HttpClientHandler>();
        }

        internal MockHttpClientFactory(ConcurrentQueue<HttpClientHandler> sharedQueue)
        {
            _queue = sharedQueue;
        }

        /// <summary>
        /// Adds a <see cref="MockHttpMessageHandler"/> to the end of the response queue.
        /// </summary>
        /// <param name="handler">The handler to enqueue.</param>
        public void AddMockHandler(MockHttpMessageHandler handler)
        {
            _queue.Enqueue(handler);
        }

        /// <summary>
        /// Returns an <see cref="HttpClient"/> backed by the next queued <see cref="MockHttpMessageHandler"/>
        /// (without mTLS certificate binding).
        /// </summary>
        /// <returns>An <see cref="HttpClient"/> that will return the pre-configured mock response.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the handler queue is empty.
        /// </exception>
        public HttpClient GetHttpClient()
        {
            return GetHttpClientInternal(null);
        }

        /// <summary>
        /// Returns an <see cref="HttpClient"/> backed by the next queued <see cref="MockHttpMessageHandler"/>
        /// with the given mTLS binding certificate attached.
        /// </summary>
        /// <param name="mtlsBindingCert">The certificate to attach to the client.</param>
        /// <returns>An <see cref="HttpClient"/> with the certificate pre-configured.</returns>
        public HttpClient GetHttpClient(X509Certificate2 mtlsBindingCert)
        {
            return GetHttpClientInternal(mtlsBindingCert);
        }

        private HttpClient GetHttpClientInternal(X509Certificate2 mtlsBindingCert)
        {
            if (!_queue.TryDequeue(out var messageHandler))
            {
                throw new InvalidOperationException(
                    "The MockHttpClientFactory queue is empty. " +
                    "Add more mock handlers with AddMockHandler() before this call.");
            }

            if (mtlsBindingCert != null)
            {
                messageHandler.ClientCertificates.Add(mtlsBindingCert);
            }

            var httpClient = new HttpClient(messageHandler)
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
