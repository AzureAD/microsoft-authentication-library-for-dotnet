// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CoreTests.HttpTests
{
    [TestClass]
    public class HttpManagerTests
    {
        private readonly TestDefaultRetryPolicy _stsRetryPolicy = new TestDefaultRetryPolicy(RequestType.STS);

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public async Task MtlsCertAsync()
        {
            var bodyParameters = new Dictionary<string, string>
            {
                ["key1"] = "some value1",
                ["key2"] = "some value2"
            };
            var headers = new Dictionary<string, string>
            {
                ["key1"] = "qp1",
                ["key2"] = "qp2"
            };

            X509Certificate2 cert = CertHelper.GetOrCreateTestCert();

            using (var httpManager = new MockHttpManager())
            {
                HttpResponseMessage mock = MockHelpers.CreateSuccessTokenResponseMessage();
                MockHttpMessageHandler handler = httpManager.AddResponseMockHandlerForPost(mock, bodyParameters, headers);
                handler.ExpectedMtlsBindingCertificate = cert;
                string expectedContent = mock.Content.ReadAsStringAsync().Result;
                var response = await httpManager.SendRequestAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/v2.0/token?key1=qp1&key2=qp2"),
                    headers: null,
                    body: new FormUrlEncodedContent(bodyParameters),
                    method: HttpMethod.Post,
                    logger: Substitute.For<ILoggerAdapter>(),
                    doNotThrow: false,
                    mtlsCertificate: cert,
                    validateServerCert: null,
                    cancellationToken: default,
                    retryPolicy: _stsRetryPolicy)
                .ConfigureAwait(false);

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(expectedContent, response.Body);
            }
        }

        [TestMethod]
        public async Task MtlsCertAndValidateCallbackFailsAsync()
        {
            var bodyParameters = new Dictionary<string, string>
            {
                ["key1"] = "some value1",
                ["key2"] = "some value2"
            };
            var headers = new Dictionary<string, string>
            {
                ["key1"] = "qp1",
                ["key2"] = "qp2"
            };

            X509Certificate2 cert = CertHelper.GetOrCreateTestCert();

            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> customCallback = (sender, cert, chain, errors) => true;

            using (var httpManager = new MockHttpManager())
            {
                await Assert.ThrowsExceptionAsync<NotImplementedException>(() =>
                    httpManager.SendRequestAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/v2.0/token?key1=qp1&key2=qp2"),
                        headers: null,
                        body: new FormUrlEncodedContent(bodyParameters),
                        method: HttpMethod.Post,
                        logger: Substitute.For<ILoggerAdapter>(),
                        doNotThrow: false,
                        mtlsCertificate: cert,
                        validateServerCert: customCallback,
                        cancellationToken: default,
                        retryPolicy: _stsRetryPolicy))
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task TestHttpManagerWithValidationCallbackAsync()
        {
            var bodyParameters = new Dictionary<string, string>
            {
                ["key1"] = "some value1",
                ["key2"] = "some value2"
            };
            var headers = new Dictionary<string, string>
            {
                ["key1"] = "qp1",
                ["key2"] = "qp2"
            };

            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> customCallback = (sender, cert, chain, errors) => true;

            using (var httpManager = new MockHttpManager())
            {
                HttpResponseMessage mock = MockHelpers.CreateSuccessTokenResponseMessage();
                MockHttpMessageHandler handler = httpManager.AddResponseMockHandlerForPost(mock, bodyParameters, headers);
                handler.ServerCertificateCustomValidationCallback = customCallback;
                string expectedContent = mock.Content.ReadAsStringAsync().Result;
                var response = await httpManager.SendRequestAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/v2.0/token?key1=qp1&key2=qp2"),
                    headers: null,
                    body: new FormUrlEncodedContent(bodyParameters),
                    method: HttpMethod.Post,
                    logger: Substitute.For<ILoggerAdapter>(),
                    doNotThrow: false,
                    mtlsCertificate: null,
                    validateServerCert: customCallback,
                    cancellationToken: default,
                    retryPolicy: _stsRetryPolicy)
                .ConfigureAwait(false);

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(expectedContent, response.Body);
            }
        }

        [TestMethod]
        public async Task TestSendPostNullHeaderNullBodyAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var mock = MockHelpers.CreateSuccessTokenResponseMessage();
                string actualResponseBody = mock.Content.ReadAsStringAsync().Result;
                httpManager.AddResponseMockHandlerForPost(mock);

                var response = await httpManager.SendRequestAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/v2.0/token"),
                    headers: null,
                    body: null,
                    method: HttpMethod.Post,
                    logger: Substitute.For<ILoggerAdapter>(),
                    doNotThrow: false,
                    mtlsCertificate: null,
                    validateServerCert: null,
                    cancellationToken: default,
                    retryPolicy: _stsRetryPolicy)
                .ConfigureAwait(false);

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(
                    actualResponseBody,
                    response.Body);
            }
        }

        [TestMethod]
        public async Task TestSendPostNoFailureAsync()
        {
            var bodyParameters = new Dictionary<string, string>
            {
                ["key1"] = "some value1",
                ["key2"] = "some value2"
            };
            var headers = new Dictionary<string, string>
            {
                ["key1"] = "qp1",
                ["key2"] = "qp2"
            };

            using (var httpManager = new MockHttpManager())
            {
                var mock = MockHelpers.CreateSuccessTokenResponseMessage();
                string actualResponseBody = mock.Content.ReadAsStringAsync().Result;

                httpManager.AddResponseMockHandlerForPost(mock, bodyParameters, headers);

                var response = await httpManager.SendRequestAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/v2.0/token?key1=qp1&key2=qp2"),
                    headers: null,
                    body: new FormUrlEncodedContent(bodyParameters),
                    method: HttpMethod.Post,
                    logger: Substitute.For<ILoggerAdapter>(),
                    doNotThrow: false,
                    mtlsCertificate: null,
                    validateServerCert: null,
                    cancellationToken: default,
                    retryPolicy: _stsRetryPolicy)
                .ConfigureAwait(false);

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(actualResponseBody, response.Body);
            }
        }

        [TestMethod]
        public async Task TestSendGetNoFailureAsync()
        {
            var queryParams = new Dictionary<string, string>
            {
                ["key1"] = "qp1",
                ["key2"] = "qp2"
            };

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddSuccessTokenResponseMockHandlerForGet(queryParameters: queryParams);

                var response = await httpManager.SendRequestAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token?key1=qp1&key2=qp2"),
                    headers: null,
                    body: null,
                    method: HttpMethod.Get,
                    logger: Substitute.For<ILoggerAdapter>(),
                    doNotThrow: false,
                    mtlsCertificate: null,
                    validateServerCert: null,
                    cancellationToken: default,
                    retryPolicy: _stsRetryPolicy)
                .ConfigureAwait(false);

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }

        // This is a regression test for the bug https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3283
        [TestMethod]
        public async Task TestSendGetWithCanceledTokenAsync()
        {
            var queryParams = new Dictionary<string, string>
            {
                ["key1"] = "qp1",
                ["key2"] = "qp2"
            };

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddSuccessTokenResponseMockHandlerForGet(queryParameters: queryParams);

                CancellationTokenSource cts = new CancellationTokenSource();
                cts.Cancel();

                await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
                    httpManager.SendRequestAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token?key1=qp1&key2=qp2"),
                        headers: queryParams,
                        body: null,
                        method: HttpMethod.Get,
                        logger: Substitute.For<ILoggerAdapter>(),
                        doNotThrow: false,
                        mtlsCertificate: null,
                        validateServerCert: null,
                        cancellationToken: cts.Token,
                        retryPolicy: _stsRetryPolicy))
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task TestSendGetWithHttp500TypeFailureWithInternalRetriesDisabledAsync()
        {
            using (var httpManager = new MockHttpManager(disableInternalRetries: true))
            {
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Get, HttpStatusCode.GatewayTimeout);

                var ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(() =>
                    httpManager.SendRequestAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                        headers: null,
                        body: null,
                        method: HttpMethod.Get,
                        logger: Substitute.For<ILoggerAdapter>(),
                        doNotThrow: false,
                        mtlsCertificate: null,
                        validateServerCert: null,
                        cancellationToken: default,
                        retryPolicy: _stsRetryPolicy))
                   .ConfigureAwait(false);
                Assert.AreEqual(MsalError.ServiceNotAvailable, ex.ErrorCode);

                const int NumRequests = 1; // initial request + 0 retries
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade);
            }
        }

        [TestMethod]
        public async Task TestSendGetWithHttp500TypeFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Simulate permanent errors (to trigger the maximum number of retries)
                const int Num500Errors = 1 + TestDefaultRetryPolicy.DefaultStsMaxRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddResiliencyMessageMockHandler(HttpMethod.Get, HttpStatusCode.GatewayTimeout);
                }

                var ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(() =>
                    httpManager.SendRequestAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                        headers: null,
                        body: null,
                        method: HttpMethod.Get,
                        logger: Substitute.For<ILoggerAdapter>(),
                        doNotThrow: false,
                        mtlsCertificate: null,
                        validateServerCert: null,
                        cancellationToken: default,
                        retryPolicy: _stsRetryPolicy))
                    .ConfigureAwait(false);
                Assert.AreEqual(MsalError.ServiceNotAvailable, ex.ErrorCode);

                int requestsMade = Num500Errors - httpManager.QueueSize;
                Assert.AreEqual(Num500Errors, requestsMade);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task NoResiliencyIfRetryAfterHeaderPresentAsync(bool useTimeSpanRetryAfter)
        {
            using (var httpManager = new MockHttpManager())
            {
                var response = httpManager.AddResiliencyMessageMockHandler(HttpMethod.Get, HttpStatusCode.ServiceUnavailable);

                response.Headers.RetryAfter = useTimeSpanRetryAfter ?
                    new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1)) :
                    new System.Net.Http.Headers.RetryConditionHeaderValue(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2));

                var exc = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                    httpManager.SendRequestAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                        headers: null,
                        body: null,
                        method: HttpMethod.Get,
                        logger: Substitute.For<ILoggerAdapter>(),
                        doNotThrow: false,
                        mtlsCertificate: null,
                        validateServerCert: null,
                        cancellationToken: default,
                        retryPolicy: _stsRetryPolicy))
                    .ConfigureAwait(false);
                Assert.AreEqual(MsalError.ServiceNotAvailable, exc.ErrorCode);

                const int NumRequests = 1; // initial request + 0 retries
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade);
            }
        }

        [TestMethod]
        public async Task NoResiliencyIfHttpErrorNotRetriableAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Get, HttpStatusCode.BadRequest);

                var msalHttpResponse = await httpManager.SendRequestAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                    headers: null,
                    body: new StringContent("body"),
                    method: HttpMethod.Get,
                    logger: Substitute.For<ILoggerAdapter>(),
                    doNotThrow: true,
                    mtlsCertificate: null,
                    validateServerCert: null,
                    cancellationToken: default,
                    retryPolicy: _stsRetryPolicy)
                .ConfigureAwait(false);
                Assert.AreEqual(HttpStatusCode.BadRequest, msalHttpResponse.StatusCode);

                const int NumRequests = 1; // initial request + 0 retries
                int requestsMade = NumRequests - httpManager.QueueSize;
                Assert.AreEqual(NumRequests, requestsMade);
            }
        }

        [TestMethod]
        public async Task TestSendGetWithHttp500TypeFailure2Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Simulate permanent errors (to trigger the maximum number of retries)
                const int Num500Errors = 1 + TestDefaultRetryPolicy.DefaultStsMaxRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddResiliencyMessageMockHandler(HttpMethod.Get, HttpStatusCode.BadGateway);
                }

                var msalHttpResponse = await httpManager.SendRequestAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                    headers: null,
                    body: new StringContent("body"),
                    method: HttpMethod.Get,
                    logger: Substitute.For<ILoggerAdapter>(),
                    doNotThrow: true,
                    mtlsCertificate: null,
                    validateServerCert: null,
                    cancellationToken: default,
                    retryPolicy: _stsRetryPolicy)
                .ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.BadGateway, msalHttpResponse.StatusCode);

                int requestsMade = Num500Errors - httpManager.QueueSize;
                Assert.AreEqual(Num500Errors, requestsMade);
            }
        }

        [TestMethod]
        public async Task TestSendPostWithHttp500TypeFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Simulate permanent errors (to trigger the maximum number of retries)
                const int Num500Errors = 1 + TestDefaultRetryPolicy.DefaultStsMaxRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.ServiceUnavailable);
                }

                var exc = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                    httpManager.SendRequestAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                        headers: null,
                        body: null,
                        method: HttpMethod.Post,
                        logger: Substitute.For<ILoggerAdapter>(),
                        doNotThrow: false,
                        mtlsCertificate: null,
                        validateServerCert: null,
                        cancellationToken: default,
                        retryPolicy: _stsRetryPolicy))
                    .ConfigureAwait(false);
                Assert.AreEqual(MsalError.ServiceNotAvailable, exc.ErrorCode);

                int requestsMade = Num500Errors - httpManager.QueueSize;
                Assert.AreEqual(Num500Errors, requestsMade);
            }
        }

        [TestMethod]
        public async Task TestSendGetWithRetryOnTimeoutFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Simulate permanent errors (to trigger the maximum number of retries)
                const int Num500Errors = 1 + TestDefaultRetryPolicy.DefaultStsMaxRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Get);
                }

                var exc = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                    httpManager.SendRequestAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                        headers: null,
                        body: null,
                        method: HttpMethod.Get,
                        logger: Substitute.For<ILoggerAdapter>(),
                        doNotThrow: false,
                        mtlsCertificate: null,
                        validateServerCert: null,
                        cancellationToken: default,
                        retryPolicy: _stsRetryPolicy))
                    .ConfigureAwait(false);
                Assert.AreEqual(MsalError.RequestTimeout, exc.ErrorCode);
                Assert.IsTrue(exc.InnerException is TaskCanceledException);

                int requestsMade = Num500Errors - httpManager.QueueSize;
                Assert.AreEqual(Num500Errors, requestsMade);
            }
        }

        [TestMethod]
        public async Task TestSendPostWithRetryOnTimeoutFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Simulate permanent errors (to trigger the maximum number of retries)
                const int Num500Errors = 1 + TestDefaultRetryPolicy.DefaultStsMaxRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Post);
                }

                var exc = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                    httpManager.SendRequestAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                        headers: new Dictionary<string, string>(),
                        body: new FormUrlEncodedContent(new Dictionary<string, string>()),
                        method: HttpMethod.Post,
                        logger: Substitute.For<ILoggerAdapter>(),
                        doNotThrow: false,
                        mtlsCertificate: null,
                        validateServerCert: null,
                        cancellationToken: default,
                        retryPolicy: _stsRetryPolicy))
                    .ConfigureAwait(false);
                Assert.AreEqual(MsalError.RequestTimeout, exc.ErrorCode);
                Assert.IsTrue(exc.InnerException is TaskCanceledException);

                int requestsMade = Num500Errors - httpManager.QueueSize;
                Assert.AreEqual(Num500Errors, requestsMade);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestCorrelationIdWithRetryOnTimeoutFailureAsync(bool addCorrelationId)
        {
            using (var httpManager = new MockHttpManager())
            {
                // Simulate permanent errors (to trigger the maximum number of retries)
                const int Num500Errors = 1 + TestDefaultRetryPolicy.DefaultStsMaxRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Post);
                }

                Guid correlationId = Guid.NewGuid();
                var headers = new Dictionary<string, string>();

                if (addCorrelationId)
                {
                    headers.Add(OAuth2Header.CorrelationId, correlationId.ToString());
                }

                var exc = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                    httpManager.SendRequestAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                        headers: headers,
                        body: new FormUrlEncodedContent(new Dictionary<string, string>()),
                        method: HttpMethod.Post,
                        logger: Substitute.For<ILoggerAdapter>(),
                        doNotThrow: false,
                        mtlsCertificate: null,
                        validateServerCert: null,
                        cancellationToken: default,
                        retryPolicy: _stsRetryPolicy))
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.RequestTimeout, exc.ErrorCode);

                if (addCorrelationId)
                {
                    Assert.AreEqual($"Request to the endpoint timed out. CorrelationId: {correlationId.ToString()}", exc.Message);
                    Assert.AreEqual(correlationId.ToString(), exc.CorrelationId);
                }
                else
                {
                    Assert.AreEqual("Request to the endpoint timed out.", exc.Message);
                }
            }
        }

        [TestMethod]
        public async Task TestWithCorrelationId_RetryOnTimeoutFailureAsync()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                // Simulate permanent errors (to trigger the maximum number of retries)
                const int Num500Errors = 1 + TestDefaultRetryPolicy.DefaultStsMaxRetries; // initial request + maximum number of retries
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Post);
                }
                Guid correlationId = Guid.NewGuid();

                var app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithAuthority(TestConstants.AuthorityTestTenant)
                            .WithHttpManager(httpManager)
                            .WithClientSecret(TestConstants.ClientSecret)
                            .Build();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                // Act
                var exc = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                                    app.AcquireTokenForClient(TestConstants.s_scope)
                                    .WithCorrelationId(correlationId)
                                    .ExecuteAsync())
                                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual($"Request to the endpoint timed out. CorrelationId: {correlationId.ToString()}", exc.Message);
                Assert.AreEqual(correlationId.ToString(), exc.CorrelationId);
            }
        }

        private class CapturingHandler : HttpMessageHandler
        {
            public HttpRequestMessage CapturedRequest { get; private set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CapturedRequest = request;
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }

#if NET
        [TestMethod]
        public async Task SendRequestAsync_SetsHttp2VersionAndPolicy()
        {
            // Arrange
            var handler = new CapturingHandler();
            var httpClient = new HttpClient(handler);
            var httpClientFactory = Substitute.For<IMsalHttpClientFactory>();
            httpClientFactory.GetHttpClient().Returns(httpClient);

            var httpManager = new Client.Http.HttpManager(httpClientFactory, disableInternalRetries: true);

            // Act
            await httpManager.SendRequestAsync(
                new Uri("https://login.microsoftonline.com/common/discovery/instance?api-version=1.1&authorization_endpoint=https://login.microsoftonline.com/common/oauth2/v2.0/authorize"),
                null,
                null,
                HttpMethod.Get,
                Substitute.For<ILoggerAdapter>(),
                doNotThrow: true,
                bindingCertificate: null,
                validateServerCert: null,
                cancellationToken: CancellationToken.None,
                retryPolicy: Substitute.For<IRetryPolicy>()
            ).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(handler.CapturedRequest);
            Assert.AreEqual(HttpVersion.Version20, handler.CapturedRequest.Version);
            Assert.AreEqual(HttpVersionPolicy.RequestVersionOrLower, handler.CapturedRequest.VersionPolicy);
        }
#endif
    }
}
