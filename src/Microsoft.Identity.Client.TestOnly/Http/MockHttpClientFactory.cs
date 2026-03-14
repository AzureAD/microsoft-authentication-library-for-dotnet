// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.TestOnly.Http
{
    /// <summary>
    /// A test HTTP client factory that implements <see cref="IMsalHttpClientFactory"/>,
    /// <see cref="IMsalMtlsHttpClientFactory"/>, and <see cref="IMsalSFHttpClientFactory"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Callers enqueue <see cref="MockHttpMessageHandler"/> instances via
    /// <see cref="AddMockHandler"/>. Each call to <see cref="GetHttpClient()"/> or
    /// <see cref="GetHttpClient(X509Certificate2)"/> dequeues the next handler, optionally
    /// binds a client certificate to it, wraps it in an <see cref="HttpClient"/>, and returns it.
    /// </para>
    /// <para>
    /// Alternatively, supply a <c>messageHandlerFactory</c> delegate at construction time.
    /// When a factory delegate is provided it is called on every request instead of dequeuing
    /// from the internal queue.
    /// </para>
    /// </remarks>
    public sealed class MockHttpClientFactory : IMsalHttpClientFactory, IMsalMtlsHttpClientFactory, IMsalSFHttpClientFactory
    {
        private readonly Func<MockHttpMessageHandler> _messageHandlerFunc;
        private readonly string _testName;

        internal ConcurrentQueue<HttpClientHandler> InternalQueue { get; }

        /// <summary>
        /// Initializes a new <see cref="MockHttpClientFactory"/> with an empty handler queue.
        /// </summary>
        public MockHttpClientFactory() : this(null, new ConcurrentQueue<HttpClientHandler>(), null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="MockHttpClientFactory"/> backed by a factory delegate.
        /// The delegate is called on every request instead of dequeuing from the queue.
        /// </summary>
        /// <param name="messageHandlerFactory">
        /// A factory that produces a new <see cref="MockHttpMessageHandler"/> for each request.
        /// </param>
        public MockHttpClientFactory(Func<MockHttpMessageHandler> messageHandlerFactory)
            : this(messageHandlerFactory, new ConcurrentQueue<HttpClientHandler>(), null)
        {
        }

        /// <summary>
        /// Internal constructor. Allows <see cref="Internal.MockHttpManager"/> to share an
        /// externally owned queue and test name.
        /// </summary>
        internal MockHttpClientFactory(
            Func<MockHttpMessageHandler> messageHandlerFunc,
            ConcurrentQueue<HttpClientHandler> sharedQueue,
            string testName)
        {
            _messageHandlerFunc = messageHandlerFunc;
            InternalQueue = sharedQueue ?? new ConcurrentQueue<HttpClientHandler>();
            _testName = testName;
        }

        /// <summary>
        /// Enqueues a handler to be returned by the next <see cref="GetHttpClient()"/> call.
        /// </summary>
        /// <param name="handler">The handler to enqueue.</param>
        /// <returns>The same handler, for fluent chaining.</returns>
        public MockHttpMessageHandler AddMockHandler(MockHttpMessageHandler handler)
        {
            Trace.WriteLine($"Test {_testName} adds an HttpMessageHandler for {handler?.ExpectedUrl ?? string.Empty}");
            InternalQueue.Enqueue(handler);
            return handler;
        }

        /// <summary>Returns a snapshot of the handlers still waiting in the queue.</summary>
        public IReadOnlyCollection<MockHttpMessageHandler> GetPendingMocks()
        {
            return InternalQueue.OfType<MockHttpMessageHandler>().ToList().AsReadOnly();
        }

        /// <summary>Removes all handlers from the queue.</summary>
        public void ClearQueue()
        {
            while (InternalQueue.TryDequeue(out _))
            {
            }
        }

        /// <inheritdoc cref="IMsalHttpClientFactory.GetHttpClient()"/>
        public HttpClient GetHttpClient()
        {
            return GetHttpClientInternal(null);
        }

        /// <inheritdoc cref="IMsalMtlsHttpClientFactory.GetHttpClient(X509Certificate2)"/>
        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            return GetHttpClientInternal(x509Certificate2);
        }

        /// <inheritdoc cref="IMsalSFHttpClientFactory.GetHttpClient(Func{HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool})"/>
        public HttpClient GetHttpClient(
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validateServerCert)
        {
            // Service Fabric validation callback is not exercised by mock — ignore callback.
            return GetHttpClientInternal(null);
        }

        private HttpClient GetHttpClientInternal(X509Certificate2 mtlsBindingCert)
        {
            HttpClientHandler messageHandler;

            if (_messageHandlerFunc != null)
            {
                messageHandler = _messageHandlerFunc();
            }
            else
            {
                if (!InternalQueue.TryDequeue(out messageHandler))
                {
                    throw new MockHttpValidationException(
                        $"The {nameof(MockHttpClientFactory)}'s queue is empty. Cannot serve another response.");
                }
            }

            Trace.WriteLine(
                $"Test {_testName} dequeued a mock handler for {GetExpectedUrl(messageHandler)}");

            if (mtlsBindingCert != null)
            {
                messageHandler.ClientCertificates.Add(mtlsBindingCert);
            }

            var httpClient = new HttpClient(messageHandler)
            {
                MaxResponseContentBufferSize = 1024 * 1024 // 1 MB — matches HttpClientConfig
            };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        private static string GetExpectedUrl(HttpMessageHandler handler)
        {
            return (handler as MockHttpMessageHandler)?.ExpectedUrl ?? string.Empty;
        }
    }
}
