// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace Microsoft.Identity.Client.TestOnly.Http.Internal
{
    /// <summary>
    /// An internal HTTP client factory that implements only <see cref="IMsalHttpClientFactory"/>
    /// (no mTLS support). Used by <see cref="MockHttpManager"/> when the non-mTLS code path is
    /// under test.
    /// </summary>
    internal sealed class MockNonMtlsHttpClientFactory : IMsalHttpClientFactory
    {
        private readonly Func<MockHttpMessageHandler> _messageHandlerFunc;
        private readonly ConcurrentQueue<HttpClientHandler> _queue;
        private readonly string _testName;

        internal MockNonMtlsHttpClientFactory(
            Func<MockHttpMessageHandler> messageHandlerFunc,
            ConcurrentQueue<HttpClientHandler> queue,
            string testName)
        {
            _messageHandlerFunc = messageHandlerFunc;
            _queue = queue;
            _testName = testName;
        }

        public HttpClient GetHttpClient()
        {
            HttpClientHandler messageHandler;

            if (_messageHandlerFunc != null)
            {
                messageHandler = _messageHandlerFunc();
            }
            else
            {
                if (!_queue.TryDequeue(out messageHandler))
                {
                    throw new MockHttpValidationException(
                        $"The {nameof(MockNonMtlsHttpClientFactory)}'s queue is empty. Cannot serve another response.");
                }
            }

            Trace.WriteLine(
                $"Test {_testName} dequeued a mock handler for {GetExpectedUrl(messageHandler)}");

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
