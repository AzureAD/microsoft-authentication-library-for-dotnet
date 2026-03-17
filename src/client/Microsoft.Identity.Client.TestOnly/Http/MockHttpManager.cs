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
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal sealed class MockHttpManager : IHttpManager,
                                            IDisposable
    {
        private readonly string _testName;

        private readonly IHttpManager _httpManager;

        /// <summary>
        /// mock implementation of <see cref="IHttpManager"/> that serves HTTP responses from a queue of <see cref="MockHttpMessageHandler"/> instances.
        /// </summary>
        /// <param name="disableInternalRetries">Indicates whether internal retries should be disabled.</param>
        /// <param name="testName">The name of the test.</param>
        /// <param name="messageHandlerFunc">A function to create a <see cref="MockHttpMessageHandler"/>.</param>
        /// <param name="invokeNonMtlsHttpManagerFactory">Indicates whether to invoke the non-MTLS HTTP manager factory.</param>
        public MockHttpManager(
            bool disableInternalRetries = false,
            string testName = null,
            Func<MockHttpMessageHandler> messageHandlerFunc = null,            
            bool invokeNonMtlsHttpManagerFactory = false)
        {
            _httpManager = invokeNonMtlsHttpManagerFactory
                ? HttpManagerFactory.GetHttpManager(
                    new MockNonMtlsHttpClientFactory(messageHandlerFunc, _httpMessageHandlerQueue, testName),
                    disableInternalRetries)
                : HttpManagerFactory.GetHttpManager(
                    new MockHttpClientFactory(messageHandlerFunc, _httpMessageHandlerQueue, testName),
                    disableInternalRetries);

            _testName = testName;
        }

        private ConcurrentQueue<HttpClientHandler> _httpMessageHandlerQueue
        {
            get;
            set;
        } = new ConcurrentQueue<HttpClientHandler>();

        /// <inheritdoc/>
        public void Dispose()
        {
            // This ensures we only check the mock queue on dispose when we're not in the middle of an
            // exception flow.  Otherwise, any early assertion will cause this to likely fail
            // even though it's not the root cause.
#pragma warning disable CS0618 // Type or member is obsolete - this is non-production code so it's fine
            if (Marshal.GetExceptionCode() == 0)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                string remainingMocks = string.Join(" ",
                    _httpMessageHandlerQueue.Select(m => GetExpectedUrlFromHandler(m)));
                Assert.IsEmpty(_httpMessageHandlerQueue,
                    "All mocks should have been consumed. Remaining mocks are for: " + remainingMocks);
            }
        }

        /// <summary>
        /// Adds a mock HTTP message handler to the queue.
        /// </summary>
        /// <param name="handler">The mock HTTP message handler to add.</param>
        /// <returns>The added mock HTTP message handler.</returns>
        public MockHttpMessageHandler AddMockHandler(MockHttpMessageHandler handler)
        {
            Trace.WriteLine($"Test {_testName} adds an HttpMessageHandler for {GetExpectedUrlFromHandler(handler)}");
            _httpMessageHandlerQueue.Enqueue(handler);

            return handler;
        }

        /// <summary>
        /// Gets the current size of the mock HTTP message handler queue.
        /// </summary>
        public int QueueSize => _httpMessageHandlerQueue.Count;

        /// <summary>
        /// For use only in tests that spin many threads. Not thread safe.
        /// </summary>
        public void ClearQueue()
        {
            while (_httpMessageHandlerQueue.TryDequeue(out _))
                ;
        }

        /// <summary>
        /// Gets the duration of the last HTTP request in milliseconds.
        /// </summary>
        public long LastRequestDurationInMs => 3000;

        private string GetExpectedUrlFromHandler(HttpMessageHandler handler)
        {
            return (handler as MockHttpMessageHandler)?.ExpectedUrl ?? "";
        }

        /// <summary>
        /// Sends an HTTP request asynchronously.
        /// </summary>
        /// <param name="endpoint">The endpoint URL.</param>
        /// <param name="headers">The HTTP headers.</param>
        /// <param name="body">The HTTP content body.</param>
        /// <param name="method">The HTTP method.</param>
        /// <param name="logger">The logger adapter.</param>
        /// <param name="doNotThrow">Indicates whether to suppress exceptions.</param>
        /// <param name="mtlsCertificate">The client certificate for mutual TLS.</param>
        /// <param name="validateServerCert">The server certificate validation callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="retryPolicy">The retry policy.</param>
        /// <param name="retryCount">The retry count.</param>
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
                validateServerCert, cancellationToken,
                retryPolicy,
                retryCount);
        }
    }

    /// <summary>
    /// Base class for test HTTP client factories that dequeue <see cref="MockHttpMessageHandler"/>
    /// instances from a shared queue and wrap them in <see cref="System.Net.Http.HttpClient"/> objects.
    /// </summary>
    public class MockHttpClientFactoryBase
    {
        /// <summary>
        /// message handler function to create a new <see cref="MockHttpMessageHandler"/> for each HTTP client, used in scenarios where tests want to generate handlers on the fly instead of pre-enqueuing them. If this function is provided, the factory will use it to create handlers instead of dequeuing from the shared queue.
        /// </summary>
        protected Func<MockHttpMessageHandler> MessageHandlerFunc { get; set; }
        /// <summary>
        /// http message handler queue shared across all factory instances. Factories will dequeue handlers from this queue to back their HTTP clients, allowing tests to enqueue a series of expected handlers that will be served in order as HTTP requests are made. Factories will throw an assertion failure if they attempt to dequeue from an empty queue, ensuring that tests don't make more HTTP requests than they have set up handlers for.
        /// </summary>
        protected ConcurrentQueue<HttpClientHandler> HttpMessageHandlerQueue { get; set; }
        /// <summary>
        /// The name of the test, used for logging purposes to correlate log messages with test execution. This is especially helpful in scenarios where multiple tests are running concurrently and sharing the same mock HTTP manager and handler queue, as it allows us to trace which test is adding or consuming which handlers from the queue. The test name is included in log messages when handlers are added to or dequeued from the queue, providing better visibility into the flow of HTTP requests and responses during test execution.
        /// </summary>
        protected string _testName { get; set; }

        /// <summary>
        /// mock HTTP client factory base class that provides common functionality for creating HTTP clients backed by <see cref="MockHttpMessageHandler"/> instances. This class is not intended to be used directly, but rather to be inherited by specific factory implementations such as <see cref="MockHttpClientFactory"/> and <see cref="MockNonMtlsHttpClientFactory"/>. The base class handles the logic of dequeuing handlers from the shared queue and creating HTTP clients, while the derived classes implement the specific interfaces required by MSAL.NET for mTLS and non-mTLS scenarios.
        /// </summary>
        /// <param name="messageHandlerFunc"></param>
        /// <param name="httpMessageHandlerQueue"></param>
        /// <param name="testName"></param>
        protected MockHttpClientFactoryBase(
            Func<MockHttpMessageHandler> messageHandlerFunc,
            ConcurrentQueue<HttpClientHandler> httpMessageHandlerQueue,
            string testName)
        {
            MessageHandlerFunc = messageHandlerFunc;
            HttpMessageHandlerQueue = httpMessageHandlerQueue;
            _testName = testName;
        }

        /// <summary>
        /// gets an <see cref="HttpClient"/> instance for use in MSAL.NET HTTP operations. The returned client is backed by a <see cref="MockHttpMessageHandler"/> dequeued from the shared queue, and if a client certificate is provided, it is attached to the client's handler for mutual TLS authentication. This method serves as the common implementation for creating HTTP clients in both mTLS and non-mTLS scenarios, allowing derived factory classes to reuse this logic while implementing their specific interfaces.
        /// </summary>
        /// <param name="mtlsBindingCert"></param>
        /// <returns></returns>
        protected HttpClient GetHttpClientInternal(X509Certificate2 mtlsBindingCert)
        {
            HttpClientHandler messageHandler;

            if (MessageHandlerFunc != null)
            {
                messageHandler = MessageHandlerFunc();
            }
            else
            {
                if (!HttpMessageHandlerQueue.TryDequeue(out messageHandler))
                {
                    Assert.Fail("The MockHttpManager's queue is empty. Cannot serve another response");
                }
            }

            Trace.WriteLine($"Test {_testName} dequeued a mock handler for {GetExpectedUrlFromHandler(messageHandler)}");

            if (mtlsBindingCert != null)
            {
                messageHandler.ClientCertificates.Add(mtlsBindingCert);
            }
            var httpClient = new HttpClient(messageHandler)
            {
                MaxResponseContentBufferSize = HttpClientConfig.MaxResponseContentBufferSizeInBytes
            };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        private string GetExpectedUrlFromHandler(HttpMessageHandler handler)
        {
            return (handler as MockHttpMessageHandler)?.ExpectedUrl ?? "";
        }
    }

    /// <summary>
    /// A test implementation of <see cref="IMsalMtlsHttpClientFactory"/> and
    /// <see cref="IMsalSFHttpClientFactory"/> that returns pre-configured
    /// <see cref="System.Net.Http.HttpClient"/> instances backed by <see cref="MockHttpMessageHandler"/>.
    /// Use with WithHttpClientFactory to inject HTTP mocks.
    /// </summary>
    public class MockHttpClientFactory : MockHttpClientFactoryBase, IMsalMtlsHttpClientFactory, IMsalSFHttpClientFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockHttpClientFactory"/> class.
        /// </summary>
        /// <param name="messageHandlerFunc">A function that returns a <see cref="MockHttpMessageHandler"/>.</param>
        /// <param name="httpMessageHandlerQueue">A queue of <see cref="HttpClientHandler"/> instances.</param>
        /// <param name="testName">The name of the test.</param>
        public MockHttpClientFactory(
            Func<MockHttpMessageHandler> messageHandlerFunc,
            ConcurrentQueue<HttpClientHandler> httpMessageHandlerQueue,
            string testName)
            : base(messageHandlerFunc, httpMessageHandlerQueue, testName)
        {
        }

        /// <summary>
        /// gets an <see cref="HttpClient"/> instance for use in MSAL.NET HTTP operations. The returned client is backed by a <see cref="MockHttpMessageHandler"/> dequeued from the shared queue.
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient()
        {
            return GetHttpClientInternal(null);
        }

        /// <summary>
        /// gets an <see cref="HttpClient"/> instance for use in MSAL.NET HTTP operations with a client certificate. The returned client is backed by a <see cref="MockHttpMessageHandler"/> dequeued from the shared queue, and the provided certificate is attached to the client's handler for mutual TLS authentication.
        /// </summary>
        /// <param name="mtlsBindingCert"></param>
        /// <returns></returns>
        public HttpClient GetHttpClient(X509Certificate2 mtlsBindingCert)
        {
            return GetHttpClientInternal(mtlsBindingCert);
        }

        /// <summary>
        /// gets an <see cref="HttpClient"/> instance for use in MSAL.NET HTTP operations with server certificate validation. The returned client is backed by a <see cref="MockHttpMessageHandler"/> dequeued from the shared queue, and the provided server certificate validation callback is used to validate server certificates during HTTPS requests.
        /// </summary>
        /// <param name="validateServerCert"></param>
        /// <returns></returns>
        public HttpClient GetHttpClient(Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validateServerCert)
        {
            return GetHttpClientInternal(null);
        }
    }

    /// <summary>
    /// A test implementation of <see cref="IMsalHttpClientFactory"/> (non-mTLS)
    /// backed by queued <see cref="MockHttpMessageHandler"/> instances.
    /// Use with WithHttpClientFactory to inject HTTP mocks.
    /// </summary>
    public class MockNonMtlsHttpClientFactory : MockHttpClientFactoryBase, IMsalHttpClientFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockNonMtlsHttpClientFactory"/> class.
        /// </summary>
        /// <param name="messageHandlerFunc">A function that returns a <see cref="MockHttpMessageHandler"/>.</param>
        /// <param name="httpMessageHandlerQueue">A queue of <see cref="HttpClientHandler"/> instances.</param>
        /// <param name="testName">The name of the test.</param>
        public MockNonMtlsHttpClientFactory(
            Func<MockHttpMessageHandler> messageHandlerFunc,
            ConcurrentQueue<HttpClientHandler> httpMessageHandlerQueue,
            string testName)
            : base(messageHandlerFunc, httpMessageHandlerQueue, testName)
        {
        }

        /// <summary>
        /// gets an <see cref="HttpClient"/> instance for use in MSAL.NET HTTP operations. The returned client is backed by a <see cref="MockHttpMessageHandler"/> dequeued from the shared queue.
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient()
        {
            return GetHttpClientInternal(null);
        }
    }

}
