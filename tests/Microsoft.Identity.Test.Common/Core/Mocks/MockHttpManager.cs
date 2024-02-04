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
using System.Security.Cryptography.X509Certificates;
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
        private readonly string _testName;

        private readonly IHttpManager _httpManager;

        public MockHttpManager(string testName = null, 
            bool isManagedIdentity = false, 
            Func<MockHttpMessageHandler> messageHandlerFunc = null, 
            bool invokeNonMtlsHttpManagerFactory = false) :
            this(true, testName, isManagedIdentity, messageHandlerFunc, invokeNonMtlsHttpManagerFactory)
        { }

        public MockHttpManager(
            bool retryOnce, 
            string testName = null, 
            bool isManagedIdentity = false, 
            Func<MockHttpMessageHandler> messageHandlerFunc = null,
            bool invokeNonMtlsHttpManagerFactory = false)
        {
            _httpManager = invokeNonMtlsHttpManagerFactory
                ? HttpManagerFactory.GetHttpManager(
                    new MockNonMtlsHttpClientFactory(messageHandlerFunc, _httpMessageHandlerQueue, testName),
                    retryOnce,
                    isManagedIdentity)
                : HttpManagerFactory.GetHttpManager(
                    new MockHttpClientFactory(messageHandlerFunc, _httpMessageHandlerQueue, testName),
                    retryOnce,
                    isManagedIdentity);

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
                    _httpMessageHandlerQueue.Select(GetExpectedUrlFromHandler));
                Assert.AreEqual(0, _httpMessageHandlerQueue.Count,
                    "All mocks should have been consumed. Remaining mocks are for: " + remainingMocks);
            }
        }

        public MockHttpMessageHandler AddMockHandler(MockHttpMessageHandler handler)
        {
            Trace.WriteLine($"Test {_testName} adds an HttpMessageHandler for { GetExpectedUrlFromHandler(handler) }");
            _httpMessageHandlerQueue.Enqueue(handler);           

            return handler;
        }

        public int QueueSize => _httpMessageHandlerQueue.Count;

        /// <summary>
        /// For use only in tests that spin many threads. Not thread safe.
        /// </summary>
        public void ClearQueue()
        {
            while (_httpMessageHandlerQueue.TryDequeue(out _))
                ;
        }

        public long LastRequestDurationInMs => 3000;

        private string GetExpectedUrlFromHandler(HttpMessageHandler handler)
        {
            return (handler as MockHttpMessageHandler)?.ExpectedUrl ?? "";
        }

        public Task<HttpResponse> SendRequestAsync(
            Uri endpoint, 
            Dictionary<string, string> headers, 
            HttpContent body, 
            HttpMethod method, 
            ILoggerAdapter logger, 
            bool doNotThrow, 
            bool retry, 
            X509Certificate2 mtlsCertificate, 
            CancellationToken cancellationToken)
        {
            return _httpManager.SendRequestAsync(
                endpoint, 
                headers, 
                body, 
                method, 
                logger, 
                doNotThrow, 
                retry, 
                mtlsCertificate, 
                cancellationToken);
        }
    }

    internal class MockHttpClientFactoryBase
    {
        protected Func<MockHttpMessageHandler> MessageHandlerFunc { get; set; }
        protected ConcurrentQueue<HttpClientHandler> HttpMessageHandlerQueue { get; set; }
        protected string _testName { get; set; }

        protected MockHttpClientFactoryBase(
            Func<MockHttpMessageHandler> messageHandlerFunc,
            ConcurrentQueue<HttpClientHandler> httpMessageHandlerQueue,
            string testName)
        {
            MessageHandlerFunc = messageHandlerFunc;
            HttpMessageHandlerQueue = httpMessageHandlerQueue;
            _testName = testName;
        }

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

    internal class MockHttpClientFactory : MockHttpClientFactoryBase, IMsalMtlsHttpClientFactory
    {
        public MockHttpClientFactory(
            Func<MockHttpMessageHandler> messageHandlerFunc,
            ConcurrentQueue<HttpClientHandler> httpMessageHandlerQueue,
            string testName)
            : base(messageHandlerFunc, httpMessageHandlerQueue, testName)
        {
        }

        public HttpClient GetHttpClient()
        {
            return GetHttpClientInternal(null);
        }

        public HttpClient GetHttpClient(X509Certificate2 mtlsBindingCert)
        {
            return GetHttpClientInternal(mtlsBindingCert);
        }
    }

    internal class MockNonMtlsHttpClientFactory : MockHttpClientFactoryBase, IMsalHttpClientFactory
    {
        public MockNonMtlsHttpClientFactory(
            Func<MockHttpMessageHandler> messageHandlerFunc,
            ConcurrentQueue<HttpClientHandler> httpMessageHandlerQueue,
            string testName)
            : base(messageHandlerFunc, httpMessageHandlerQueue, testName)
        {
        }

        public HttpClient GetHttpClient()
        {
            return GetHttpClientInternal(null);
        }
    }

}
