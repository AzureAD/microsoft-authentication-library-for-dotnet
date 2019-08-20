// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal sealed class MockHttpManager : HttpManager,
                                            IDisposable
    {
        private readonly TestContext _testContext;

        public MockHttpManager(TestContext testContext = null)
        {
            _testContext = testContext;
        }

        private Queue<HttpMessageHandler> _httpMessageHandlerQueue
        {
            get;
            set;
        } = new Queue<HttpMessageHandler>();

        /// <inheritdoc />
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
                Assert.AreEqual(0, _httpMessageHandlerQueue.Count,
                    "All mocks should have been consumed. Remaining mocks are for: " + remainingMocks);
            }
        }

        public void AddMockHandler(HttpMessageHandler handler)
        {
            string testName = _testContext?.TestName ?? "";
            Trace.WriteLine($"Test {testName} adds an HttpMessageHandler for { GetExpectedUrlFromHandler(handler) }");
            _httpMessageHandlerQueue.Enqueue(handler);
        }

        public int QueueSize => _httpMessageHandlerQueue.Count;

        /// <inheritdoc />

        protected override HttpClient GetHttpClient()
        {
            if (_httpMessageHandlerQueue.Count == 0)
            {
                Assert.Fail("The MockHttpManager's queue is empty. Cannot serve another response");
            }

            var messageHandler = _httpMessageHandlerQueue.Dequeue();

            Trace.WriteLine($"Test {_testContext?.TestName ?? ""} dequeued a mock handler for { GetExpectedUrlFromHandler(messageHandler) }");

            var httpClient = new HttpClient(messageHandler)
            {
                MaxResponseContentBufferSize = HttpClientFactory.MaxResponseContentBufferSizeInBytes
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
