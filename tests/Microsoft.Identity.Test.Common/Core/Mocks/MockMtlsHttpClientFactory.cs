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
    internal sealed class MockMtlsHttpClientFactory : IMsalMtlsHttpClientFactory, IDisposable
    {
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
                string remainingMocks = string.Join(
                    " ",
                    _httpMessageHandlerQueue.Select(
                        h => (h as MockHttpMessageHandler)?.ExpectedUrl ?? string.Empty));

                Assert.IsNotNull(_httpMessageHandlerQueue);
            }
        }

        public MockHttpMessageHandler AddMockHandler(MockHttpMessageHandler handler)
        {
            _httpMessageHandlerQueue.Enqueue(handler);
            return handler;
        }

        private Queue<HttpMessageHandler> _httpMessageHandlerQueue = new Queue<HttpMessageHandler>();

        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            return GetHttpClientInternal(x509Certificate2);
        }

        public HttpClient GetHttpClient()
        {
            return GetHttpClientInternal(null);
        }

        public HttpClient GetHttpClientInternal(X509Certificate2 mtlsBindingCert)
        {
            HttpClientHandler messageHandler;

            Assert.IsNotNull(_httpMessageHandlerQueue);

            if (!_httpMessageHandlerQueue.Any() || !(_httpMessageHandlerQueue.Dequeue() is HttpClientHandler))
            {
                Assert.Fail("The MockHttpManager's queue is empty or does not contain the expected handler type. Cannot serve another response");
            }

            messageHandler = (HttpClientHandler)_httpMessageHandlerQueue.Dequeue();

            var httpClient = new HttpClient(messageHandler);

            if (mtlsBindingCert != null)
            {
                messageHandler.ClientCertificates.Add(mtlsBindingCert);
            }

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }
    }
}
