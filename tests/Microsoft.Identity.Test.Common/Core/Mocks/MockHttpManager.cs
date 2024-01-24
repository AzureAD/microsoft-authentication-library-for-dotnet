// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal sealed class MockHttpManager : IHttpManager,
                                            IDisposable
    {
        private readonly TestContext _testContext;

        private readonly IHttpManager _httpManager;

        public MockHttpManager(TestContext testContext = null, bool isManagedIdentity = false, Func<MockHttpMessageHandler> messageHandlerFunc = null) :
            this(true, testContext, isManagedIdentity, messageHandlerFunc)
        { }

        public MockHttpManager(bool retryOnce, TestContext testContext = null, bool isManagedIdentity = false, Func<MockHttpMessageHandler> messageHandlerFunc = null)
        {
            _httpManager = HttpManagerFactory.GetHttpManager(new MockHttpClientFactory(messageHandlerFunc,
                _httpMessageHandlerQueue, testContext), retryOnce, isManagedIdentity);

            _testContext = testContext;
        }

        private ConcurrentQueue<HttpMessageHandler> _httpMessageHandlerQueue
        {
            get;
            set;
        } = new ConcurrentQueue<HttpMessageHandler>();

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
                    _httpMessageHandlerQueue.Select(GetExpectedUrlFromHandler));
                Assert.AreEqual(0, _httpMessageHandlerQueue.Count,
                    "All mocks should have been consumed. Remaining mocks are for: " + remainingMocks);
            }
        }

        public MockHttpMessageHandler AddMockHandler(MockHttpMessageHandler handler)
        {
            string testName = _testContext?.TestName ?? "";
            Trace.WriteLine($"Test {testName} adds an HttpMessageHandler for { GetExpectedUrlFromHandler(handler) }");
            _httpMessageHandlerQueue.Enqueue(handler);           

            return handler;
        }

        public int QueueSize => _httpMessageHandlerQueue.Count;

        public long LastRequestDurationInMs => 3000;

        

        private string GetExpectedUrlFromHandler(HttpMessageHandler handler)
        {
            return (handler as MockHttpMessageHandler)?.ExpectedUrl ?? "";
        }

        public async Task<HttpResponse> SendPostAsync(Uri endpoint, IDictionary<string, string> headers, IDictionary<string, string> bodyParameters, ILoggerAdapter logger, CancellationToken cancellationToken = default)
        {
            return await _httpManager.SendPostAsync(endpoint, headers, bodyParameters, logger, cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpResponse> SendPostAsync(Uri endpoint, IDictionary<string, string> headers, HttpContent body, ILoggerAdapter logger, CancellationToken cancellationToken = default)
        {
            return await _httpManager.SendPostAsync(endpoint, headers, body, logger, cancellationToken).ConfigureAwait(false);
        }

        public Task<HttpResponse> SendGetAsync(Uri endpoint, IDictionary<string, string> headers, ILoggerAdapter logger, bool retry = true, CancellationToken cancellationToken = default)
        {
            return _httpManager.SendGetAsync(endpoint, headers, logger, retry, cancellationToken);
        }

        public async Task<HttpResponse> SendPostForceResponseAsync(Uri uri, IDictionary<string, string> headers, StringContent body, ILoggerAdapter logger, CancellationToken cancellationToken = default)
        {
            return await _httpManager.SendPostForceResponseAsync(uri, headers, body, logger, cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpResponse> SendPostForceResponseAsync(Uri uri, IDictionary<string, string> headers, IDictionary<string, string> bodyParameters, ILoggerAdapter logger, CancellationToken cancellationToken = default)
        {
            return await _httpManager.SendPostForceResponseAsync(uri, headers, bodyParameters, logger, cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpResponse> SendGetForceResponseAsync(Uri endpoint, IDictionary<string, string> headers, ILoggerAdapter logger, bool retry = true, CancellationToken cancellationToken = default)
        {
            return await _httpManager.SendGetForceResponseAsync(endpoint, headers, logger, retry, cancellationToken).ConfigureAwait(false); 
        }
    }

    internal class MockHttpClientFactory : IMsalHttpClientFactory
    {
        Func<MockHttpMessageHandler> MessageHandlerFunc;
        ConcurrentQueue<HttpMessageHandler> HttpMessageHandlerQueue;
        TestContext TestContext;

        public MockHttpClientFactory(Func<MockHttpMessageHandler> messageHandlerFunc,
            ConcurrentQueue<HttpMessageHandler> httpMessageHandlerQueue, TestContext testContext)
        {
            MessageHandlerFunc = messageHandlerFunc;
            HttpMessageHandlerQueue = httpMessageHandlerQueue;
            TestContext = testContext;
        }

        public HttpClient GetHttpClient()
        {
            HttpMessageHandler messageHandler;

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

            Trace.WriteLine($"Test {TestContext?.TestName ?? ""} dequeued a mock handler for {GetExpectedUrlFromHandler(messageHandler)}");

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
}
