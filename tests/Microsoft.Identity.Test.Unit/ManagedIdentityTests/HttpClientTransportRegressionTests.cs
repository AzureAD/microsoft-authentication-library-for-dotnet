// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Regression guard for the transport-selection contract behind
    /// https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/6124
    /// (recurrence of #5286): a caller-supplied custom <see cref="IMsalHttpClientFactory"/> —
    /// the shape Azure Identity's <c>HttpClientTransport</c> surfaces as — must be used for
    /// EVERY request. MSAL must never silently fabricate an internal/default <see cref="HttpClient"/>
    /// and bypass the caller's configured transport.
    ///
    /// The pre-existing tests only assert an implementation detail
    /// (Service Fabric returns a validation callback; other sources return null) and drive the
    /// flow through <c>MockHttpManager</c>, whose mock factory implements every factory interface —
    /// so they cannot catch a path that bypasses a caller supplying ONLY the normal factory
    /// interface. This test uses a real <see cref="HttpManager"/> with a plain tracking factory and
    /// verifies the factory is actually invoked (creation/usage count), which is the invariant that
    /// regressed.
    /// </summary>
    [TestClass]
    public class HttpClientTransportRegressionTests : TestBase
    {
        [TestMethod]
        public async Task CustomHttpClientFactory_MustNotBeBypassed_WhenServerCertificateValidationCallbackPresentAsync()
        {
            // Arrange
            // A custom factory implementing ONLY IMsalHttpClientFactory (exactly what Azure Identity's
            // HttpClientTransport surfaces as - it is NOT an IMsalSFHttpClientFactory / IMsalMtlsHttpClientFactory).
            var trackingHandler = new TrackingHttpMessageHandler();
            var trackingFactory = new PlainTrackingHttpClientFactory(trackingHandler);

            var httpManager = new HttpManager(trackingFactory, disableInternalRetries: true);

            // A non-null server-certificate validation callback is what the Managed Identity path
            // supplies for Service Fabric. It must NOT cause MSAL to discard the caller's transport.
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validateServerCert =
                (message, certificate, chain, errors) => true;

            Exception thrown = null;

            // Act
            try
            {
                await httpManager.SendRequestAsync(
                    // Unroutable endpoint: only ever contacted if MSAL bypasses the tracking factory and
                    // creates its own HttpClient. Connection is refused immediately (no external network).
                    endpoint: new Uri("https://127.0.0.1:1/token"),
                    headers: new Dictionary<string, string>(),
                    body: null,
                    method: HttpMethod.Get,
                    logger: Substitute.For<ILoggerAdapter>(),
                    doNotThrow: true,
                    bindingCertificate: null,
                    validateServerCert: validateServerCert,
                    cancellationToken: CancellationToken.None,
                    retryPolicy: new TestDefaultRetryPolicy(RequestType.STS))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // On the regressed code path the fabricated HttpClient tries to reach 127.0.0.1:1 and throws.
                thrown = ex;
            }

            // Assert
            // The invariant: the configured custom transport must have been used for the request.
            Assert.AreEqual(
                1,
                trackingFactory.GetHttpClientCallCount,
                "The caller-supplied IMsalHttpClientFactory was bypassed - MSAL fabricated its own HttpClient " +
                "instead of using the configured transport (regression of issue #6124 / #5286). " +
                $"Unexpected exception from the fabricated client: {thrown}");

            Assert.AreEqual(
                1,
                trackingHandler.RequestCount,
                "The caller-supplied transport did not receive the request; a default HttpClient was used instead.");
        }

        /// <summary>
        /// A custom factory that implements ONLY <see cref="IMsalHttpClientFactory"/> (no Service Fabric
        /// or mTLS interface), matching what Azure Identity's <c>HttpClientTransport</c> provides.
        /// Counts how many times MSAL asks it for an <see cref="HttpClient"/>.
        /// </summary>
        private sealed class PlainTrackingHttpClientFactory : IMsalHttpClientFactory
        {
            private readonly HttpClient _httpClient;

            public int GetHttpClientCallCount { get; private set; }

            public PlainTrackingHttpClientFactory(HttpMessageHandler handler)
            {
                _httpClient = new HttpClient(handler);
            }

            public HttpClient GetHttpClient()
            {
                GetHttpClientCallCount++;
                return _httpClient;
            }
        }

        /// <summary>
        /// Records how many requests actually flow through the caller's transport and returns a canned 200.
        /// </summary>
        private sealed class TrackingHttpMessageHandler : HttpMessageHandler
        {
            public int RequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });
            }
        }
    }
}
