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
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CoreTests.HttpTests
{
    [TestClass]
    public class HttpManagerTests
    {
        private static int _originalStsRetryDelay;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            // Backup original retry delay values
            _originalStsRetryDelay = DefaultRetryPolicy.DefaultStsRetryDelayMs;

            // Speed up retry delays by 100x
            DefaultRetryPolicy.DefaultStsRetryDelayMs = (int)(_originalStsRetryDelay * TestConstants.ONE_HUNDRED_TIMES_FASTER);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Restore retry policy values after each test
            DefaultRetryPolicy.DefaultStsRetryDelayMs = _originalStsRetryDelay;
        }

        private DefaultRetryPolicy StsRetryPolicy;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();

            DefaultRetryPolicy.NumRetries = 0;
            StsRetryPolicy = new DefaultRetryPolicy(
                DefaultRetryPolicy.DefaultStsRetryDelayMs,
                DefaultRetryPolicy.DefaultStsMaxRetries,
                HttpRetryConditions.Sts);
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
                    retryPolicy: StsRetryPolicy)
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
                        retryPolicy: StsRetryPolicy))
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
                    retryPolicy: StsRetryPolicy)
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
                    retryPolicy: StsRetryPolicy)
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
                    retryPolicy: StsRetryPolicy)
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
                    retryPolicy: StsRetryPolicy)
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
                        retryPolicy: StsRetryPolicy))
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
                        retryPolicy: StsRetryPolicy))
                   .ConfigureAwait(false);

                Assert.AreEqual(MsalError.ServiceNotAvailable, ex.ErrorCode);
                Assert.AreEqual(DefaultRetryPolicy.NumRetries, 0);
            }
        }

        [TestMethod]
        public async Task TestSendGetWithHttp500TypeFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Simulate permanent errors (to trigger the maximum number of retries)
                const int NumErrors = DefaultRetryPolicy.DefaultStsMaxRetries + 1; // initial request + maximum number of retries (3)
                for (int i = 0; i < NumErrors; i++)
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
                        retryPolicy: StsRetryPolicy))
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.ServiceNotAvailable, ex.ErrorCode);
                Assert.AreEqual(httpManager.QueueSize, 0);
                Assert.AreEqual(DefaultRetryPolicy.NumRetries, DefaultRetryPolicy.DefaultStsMaxRetries);
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
                        retryPolicy: StsRetryPolicy))
                    .ConfigureAwait(false);

                Assert.AreEqual(0, httpManager.QueueSize, "HttpManager must not retry because a RetryAfter header is present");
                Assert.AreEqual(MsalError.ServiceNotAvailable, exc.ErrorCode);
                Assert.AreEqual(DefaultRetryPolicy.NumRetries, 0);
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
                    retryPolicy: StsRetryPolicy)
                .ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.BadRequest, msalHttpResponse.StatusCode);
                Assert.AreEqual(httpManager.QueueSize, 0);
                Assert.AreEqual(DefaultRetryPolicy.NumRetries, 0);
            }
        }

        [TestMethod]
        public async Task TestSendGetWithHttp500TypeFailure2Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Simulate permanent errors (to trigger the maximum number of retries)
                const int NumErrors = DefaultRetryPolicy.DefaultStsMaxRetries + 1; // initial request + maximum number of retries (3)
                for (int i = 0; i < NumErrors; i++)
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
                    retryPolicy: StsRetryPolicy)
                .ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.BadGateway, msalHttpResponse.StatusCode);
                Assert.AreEqual(httpManager.QueueSize, 0);
                Assert.AreEqual(DefaultRetryPolicy.NumRetries, DefaultRetryPolicy.DefaultStsMaxRetries);
            }
        }

        [TestMethod]
        public async Task TestSendPostWithHttp500TypeFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Simulate permanent errors (to trigger the maximum number of retries)
                const int NumErrors = DefaultRetryPolicy.DefaultStsMaxRetries + 1; // initial request + maximum number of retries (3)
                for (int i = 0; i < NumErrors; i++)
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
                        retryPolicy: StsRetryPolicy))
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.ServiceNotAvailable, exc.ErrorCode);
                Assert.AreEqual(httpManager.QueueSize, 0);
                Assert.AreEqual(DefaultRetryPolicy.NumRetries, DefaultRetryPolicy.DefaultStsMaxRetries);
            }
        }

        [TestMethod]
        public async Task TestSendGetWithRetryOnTimeoutFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Get);
                httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Get);

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
                        retryPolicy: StsRetryPolicy))
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.RequestTimeout, exc.ErrorCode);
                Assert.IsTrue(exc.InnerException is TaskCanceledException);
                Assert.AreEqual(DefaultRetryPolicy.NumRetries, 1);
            }
        }

        [TestMethod]
        public async Task TestSendPostWithRetryOnTimeoutFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Post);
                httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Post);

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
                        retryPolicy: StsRetryPolicy))
                    .ConfigureAwait(false);
                Assert.AreEqual(MsalError.RequestTimeout, exc.ErrorCode);
                Assert.IsTrue(exc.InnerException is TaskCanceledException);
                Assert.AreEqual(DefaultRetryPolicy.NumRetries, 1);
            }
        }
    }
}
