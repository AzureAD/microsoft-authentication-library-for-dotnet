// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.TestOnly.Http.Internal;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    /// <summary>
    /// An <see cref="IHttpManager"/> for MSAL unit tests.
    /// Delegates all behaviour to
    /// <see cref="Microsoft.Identity.Client.TestOnly.Http.Internal.MockHttpManager"/>,
    /// which lives in the <c>Microsoft.Identity.Client.TestOnly</c> package and is the
    /// single source of truth for mock HTTP infrastructure.
    /// </summary>
    internal sealed class MockHttpManager : IHttpManager, IDisposable
    {
        private readonly Microsoft.Identity.Client.TestOnly.Http.Internal.MockHttpManager _inner;

        public MockHttpManager(
            bool disableInternalRetries = false,
            string testName = null,
            Func<Microsoft.Identity.Client.TestOnly.Http.MockHttpMessageHandler> messageHandlerFunc = null,
            bool invokeNonMtlsHttpManagerFactory = false)
        {
            _inner = new Microsoft.Identity.Client.TestOnly.Http.Internal.MockHttpManager(
                disableInternalRetries,
                testName,
                messageHandlerFunc,
                invokeNonMtlsHttpManagerFactory);
        }

        /// <summary>Gets the number of handlers still pending in the queue.</summary>
        public int QueueSize => _inner.QueueSize;

        /// <inheritdoc/>
        public long LastRequestDurationInMs => _inner.LastRequestDurationInMs;

        /// <summary>Enqueues a handler to be returned by the next HTTP request.</summary>
        /// <returns>The same <paramref name="handler"/>, for fluent chaining.</returns>
        public MockHttpMessageHandler AddMockHandler(MockHttpMessageHandler handler)
        {
            _inner.AddMockHandler(handler);
            return handler;
        }

        /// <summary>Removes all handlers from the queue.</summary>
        public void ClearQueue() => _inner.ClearQueue();

        /// <inheritdoc/>
        public void Dispose() => _inner.Dispose();

        /// <inheritdoc/>
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
            return _inner.SendRequestAsync(
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
