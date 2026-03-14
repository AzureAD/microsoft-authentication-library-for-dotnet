// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;

namespace Microsoft.Identity.Client.TestOnly.Http.Internal
{
    /// <summary>
    /// An internal implementation of <see cref="IHttpManager"/> that uses a queue of
    /// <see cref="MockHttpMessageHandler"/> instances to serve pre-configured responses during
    /// MSAL unit tests.
    /// </summary>
    /// <remarks>
    /// This type is internal. MSAL test projects access it via <c>InternalsVisibleTo</c>.
    /// External consumers should use <see cref="MockHttpClientFactory"/> directly via the
    /// <c>WithHttpClientFactory</c> builder method instead.
    /// </remarks>
    internal sealed class MockHttpManager : IHttpManager, IDisposable
    {
        private readonly string _testName;
        private readonly IHttpManager _httpManager;

        private readonly ConcurrentQueue<HttpClientHandler> _httpMessageHandlerQueue
            = new ConcurrentQueue<HttpClientHandler>();

        /// <summary>
        /// Initializes a new <see cref="MockHttpManager"/>.
        /// </summary>
        /// <param name="disableInternalRetries">
        /// When <c>true</c> the underlying HTTP manager suppresses its internal retry logic so
        /// tests control retry behaviour explicitly.
        /// </param>
        /// <param name="testName">Test name used in trace output.</param>
        /// <param name="messageHandlerFunc">
        /// Optional factory delegate. When provided it is called for every request instead of
        /// dequeuing from the internal queue.
        /// </param>
        /// <param name="invokeNonMtlsHttpManagerFactory">
        /// When <c>true</c> the backing <see cref="IHttpManager"/> is created with a non-mTLS
        /// factory (only <see cref="IMsalHttpClientFactory"/>). Use this when the test targets
        /// non-mTLS code paths.
        /// </param>
        public MockHttpManager(
            bool disableInternalRetries = false,
            string testName = null,
            Func<MockHttpMessageHandler> messageHandlerFunc = null,
            bool invokeNonMtlsHttpManagerFactory = false)
        {
            _testName = testName;

            IMsalHttpClientFactory factory = invokeNonMtlsHttpManagerFactory
                ? (IMsalHttpClientFactory)new MockNonMtlsHttpClientFactory(
                    messageHandlerFunc, _httpMessageHandlerQueue, testName)
                : new MockHttpClientFactory(
                    messageHandlerFunc, _httpMessageHandlerQueue, testName);

            _httpManager = HttpManagerFactory.GetHttpManager(factory, disableInternalRetries);
        }

        /// <summary>Gets the number of handlers still pending in the queue.</summary>
        public int QueueSize => _httpMessageHandlerQueue.Count;

        /// <inheritdoc />
        public long LastRequestDurationInMs => 3000;

        /// <summary>
        /// Enqueues a handler to be returned by the next HTTP request.
        /// </summary>
        /// <param name="handler">The handler to enqueue.</param>
        /// <returns>The same handler, for fluent chaining.</returns>
        public MockHttpMessageHandler AddMockHandler(MockHttpMessageHandler handler)
        {
            Trace.WriteLine(
                $"Test {_testName} adds an HttpMessageHandler for {handler?.ExpectedUrl ?? string.Empty}");
            _httpMessageHandlerQueue.Enqueue(handler);
            return handler;
        }

        /// <summary>
        /// Removes all handlers from the queue. For use in tests that spin many threads.
        /// Not thread-safe with respect to concurrent <see cref="AddMockHandler"/> calls.
        /// </summary>
        public void ClearQueue()
        {
            while (_httpMessageHandlerQueue.TryDequeue(out _))
            {
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_httpMessageHandlerQueue.IsEmpty)
            {
                return;
            }

            // Only throw when there is no active exception propagating, to avoid masking
            // the original test failure with a secondary "queue not empty" diagnostic.
            // Marshal.GetExceptionCode() is marked obsolete but still available on all platforms;
            // on non-Windows it always returns 0 (no Windows structured-exception model),
            // which means the queue check always runs — acceptable for test-only code.
#pragma warning disable CS0618 // Marshal.GetExceptionCode is intentionally used here for test diagnostics
            if (Marshal.GetExceptionCode() != 0)
            {
                return;
            }
#pragma warning restore CS0618

            string remainingMocks = string.Join(" ",
                _httpMessageHandlerQueue.Select(m => (m as MockHttpMessageHandler)?.ExpectedUrl ?? string.Empty));

            throw new MockHttpValidationException(
                "All mocks should have been consumed. Remaining mocks are for: " + remainingMocks);
        }

        /// <inheritdoc />
        public Task<HttpResponse> SendRequestAsync(
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
            int retryCount = 0)
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
    }
}
