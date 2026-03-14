// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;

namespace Microsoft.Identity.Client.TestOnly
{
    /// <summary>
    /// A mock HTTP manager that intercepts outgoing HTTP requests and returns pre-queued responses.
    /// Use this in unit tests to verify that MSAL makes the expected HTTP calls without hitting real endpoints.
    /// </summary>
    /// <remarks>
    /// <para>This class is <b>not thread-safe</b> and is intended for single-test usage.</para>
    /// <para>
    /// Add mock handlers in the order they are expected to be consumed.
    /// On <see cref="Dispose"/>, any unconsumed handlers cause an <see cref="InvalidOperationException"/>
    /// so you can detect unexpected extra HTTP calls.
    /// </para>
    /// <example>
    /// <code>
    /// using var httpManager = new MockHttpManager();
    /// httpManager.AddMockHandler(MockHelpers.CreateMsiTokenHandler("mock-token"));
    ///
    /// var app = ManagedIdentityApplicationBuilder
    ///     .Create(ManagedIdentityId.SystemAssigned)
    ///     .WithHttpManager(httpManager)
    ///     .Build();
    ///
    /// var result = await app
    ///     .AcquireTokenForManagedIdentity("https://management.azure.com/.default")
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// </remarks>
    public sealed class MockHttpManager : IHttpManager, IDisposable
    {
        private readonly IHttpManager _httpManager;
        private readonly ConcurrentQueue<HttpClientHandler> _httpMessageHandlerQueue;

        /// <summary>
        /// Initializes a new instance of <see cref="MockHttpManager"/>.
        /// </summary>
        /// <param name="disableInternalRetries">
        /// When <see langword="true"/>, MSAL's internal retry-on-5xx behaviour is disabled.
        /// Defaults to <see langword="false"/>.
        /// </param>
        public MockHttpManager(bool disableInternalRetries = false)
        {
            _httpMessageHandlerQueue = new ConcurrentQueue<HttpClientHandler>();
            _httpManager = HttpManagerFactory.GetHttpManager(
                new MockHttpClientFactory(_httpMessageHandlerQueue),
                disableInternalRetries);
        }

        /// <summary>
        /// Gets the number of mock handlers that have not yet been consumed.
        /// </summary>
        public int QueueSize => _httpMessageHandlerQueue.Count;

        /// <summary>
        /// Adds a <see cref="MockHttpMessageHandler"/> to the end of the response queue.
        /// Handlers are dequeued in FIFO order as MSAL makes outgoing requests.
        /// </summary>
        /// <param name="handler">The mock handler to enqueue.</param>
        /// <returns>The same <paramref name="handler"/> so calls can be chained.</returns>
        public MockHttpMessageHandler AddMockHandler(MockHttpMessageHandler handler)
        {
            _httpMessageHandlerQueue.Enqueue(handler);
            return handler;
        }

        /// <summary>
        /// Removes all pending mock handlers from the queue without consuming them.
        /// </summary>
        public void ClearQueue()
        {
            while (_httpMessageHandlerQueue.TryDequeue(out _))
            {
            }
        }

        /// <inheritdoc/>
        public long LastRequestDurationInMs => _httpManager.LastRequestDurationInMs;

        /// <inheritdoc/>
        Task<HttpResponse> IHttpManager.SendRequestAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            ILoggerAdapter logger,
            bool doNotThrow,
            X509Certificate2 mtlsCertificate,
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validateServerCert,
            CancellationToken cancellationToken,
            IRetryPolicy retryPolicy,
            int retryCount)
        {
            return _httpManager.SendRequestAsync(
                endpoint,
                headers,
                body,
                method,
                logger,
                doNotThrow,
                mtlsCertificate,
                validateServerCert,
                cancellationToken,
                retryPolicy,
                retryCount);
        }

        /// <summary>
        /// Verifies that all queued mock handlers have been consumed.
        /// Throws <see cref="InvalidOperationException"/> if any remain.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when at least one mock handler was not consumed during the test.
        /// </exception>
        public void Dispose()
        {
            if (!_httpMessageHandlerQueue.IsEmpty)
            {
                int remaining = _httpMessageHandlerQueue.Count;
                var urls = _httpMessageHandlerQueue
                    .OfType<MockHttpMessageHandler>()
                    .Select(h => h.ExpectedUrl ?? "(no url)");
                throw new InvalidOperationException(
                    $"{remaining} mock handler(s) were not consumed. Remaining URLs: {string.Join(", ", urls)}");
            }
        }
    }
}
