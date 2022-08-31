// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;
using NSubstitute;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Helpers;
using System.Threading;

namespace Microsoft.Identity.Test.Unit.CoreTests.HttpTests
{
    [TestClass]
    public class HttpManagerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void TestSendPostNullHeaderNullBody()
        {
            using (var httpManager = new MockHttpManager())
            {
                var mock = MockHelpers.CreateSuccessTokenResponseMessage();
                string actualResponseBody = mock.Content.ReadAsStringAsync().Result;
                httpManager.AddResponseMockHandlerForPost(mock);

                var response = httpManager.SendPostAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/v2.0/token"),
                    null,
                    (IDictionary<string, string>)null,
                    Substitute.For<ILoggerAdapter>()).Result;

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(
                    actualResponseBody,
                    response.Body);
            }
        }

        [TestMethod]
        public void TestSendPostNoFailure()
        {
            var bodyParameters = new Dictionary<string, string>
            {
                ["key1"] = "some value1",
                ["key2"] = "some value2"
            };
            var queryParams = new Dictionary<string, string>
            {
                ["key1"] = "qp1",
                ["key2"] = "qp2"
            };

            using (var httpManager = new MockHttpManager())
            {
                var mock = MockHelpers.CreateSuccessTokenResponseMessage();
                string actualResponseBody = mock.Content.ReadAsStringAsync().Result;

                httpManager.AddResponseMockHandlerForPost(mock, bodyParameters, queryParams);

                var response = httpManager.SendPostAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/v2.0/token?key1=qp1&key2=qp2"),
                    queryParams,
                    bodyParameters,
                    Substitute.For<ILoggerAdapter>()).Result;

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(actualResponseBody, response.Body);
            }
        }

        [TestMethod]
        public void TestSendGetNoFailure()
        {
            var queryParams = new Dictionary<string, string>
            {
                ["key1"] = "qp1",
                ["key2"] = "qp2"
            };

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddSuccessTokenResponseMockHandlerForGet(queryParameters: queryParams);

                var response = httpManager.SendGetAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token?key1=qp1&key2=qp2"),
                    queryParams,
                    Substitute.For<ILoggerAdapter>()).Result;

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

                await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => httpManager.SendGetAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token?key1=qp1&key2=qp2"),
                        queryParams,
                        Substitute.For<ILoggerAdapter>(),
                        cancellationToken: cts.Token)).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task TestSendGetWithRetryFalseHttp500TypeFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Get, HttpStatusCode.GatewayTimeout);

                try
                {
                    var msalHttpResponse = await httpManager.SendGetAsync(
                                                                new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                                                                null,
                                                                Substitute.For<ILoggerAdapter>(),
                                                                retry: false)
                                                            .ConfigureAwait(false);
                    Assert.Fail("request should have failed");
                }
                catch (MsalServiceException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.ServiceNotAvailable, exc.ErrorCode);
                }
            }
        }

        [TestMethod]
        public async Task TestSendGetWithHttp500TypeFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Get, HttpStatusCode.GatewayTimeout);
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Get, HttpStatusCode.InternalServerError);

                try
                {
                    var msalHttpResponse = await httpManager.SendGetAsync(
                                                                new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                                                                null,
                                                                Substitute.For<ILoggerAdapter>())
                                                            .ConfigureAwait(false);
                    Assert.Fail("request should have failed");
                }
                catch (MsalServiceException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.ServiceNotAvailable, exc.ErrorCode);
                }
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

                var exc = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    () => httpManager.SendGetAsync(
                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"), null, Substitute.For<ILoggerAdapter>()))
                    .ConfigureAwait(false);

                Assert.AreEqual(0, httpManager.QueueSize, "HttpManager must not retry because a RetryAfter header is present");
                Assert.AreEqual(MsalError.ServiceNotAvailable, exc.ErrorCode);
            }
        }

        [TestMethod]
        public async Task TestSendGetWithHttp500TypeFailure2Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.BadGateway);
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.BadGateway);

                var msalHttpResponse = await httpManager.SendPostForceResponseAsync(
                                                            new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                                                            null,
                                                            new StringContent("body"),
                                                            Substitute.For<ILoggerAdapter>())
                                                        .ConfigureAwait(false);

                Assert.AreEqual(HttpStatusCode.BadGateway, msalHttpResponse.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestSendPostWithHttp500TypeFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.GatewayTimeout);
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.ServiceUnavailable);

                try
                {
                    var msalHttpResponse = await httpManager.SendPostAsync(
                                                                new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                                                                null,
                                                                (IDictionary<string, string>)null,
                                                               Substitute.For<ILoggerAdapter>())
                                                            .ConfigureAwait(false);
                    Assert.Fail("request should have failed");
                }
                catch (MsalServiceException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.ServiceNotAvailable, exc.ErrorCode);
                }
            }
        }

        [TestMethod]
        public async Task TestSendGetWithRetryOnTimeoutFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Get);
                httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Get);

                try
                {
                    var msalHttpResponse = await httpManager.SendGetAsync(
                                                                new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                                                                new Dictionary<string, string>(),
                                                                Substitute.For<ILoggerAdapter>())
                                                            .ConfigureAwait(false);
                    Assert.Fail("request should have failed");
                }
                catch (MsalServiceException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.RequestTimeout, exc.ErrorCode);
                    Assert.IsTrue(exc.InnerException is TaskCanceledException);
                }
            }
        }

        [TestMethod]
        public async Task TestSendPostWithRetryOnTimeoutFailureAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Post);
                httpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Post);

                try
                {
                    var msalHttpResponse = await httpManager.SendPostAsync(
                                                                new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                                                                new Dictionary<string, string>(),
                                                                new Dictionary<string, string>(),
                                                                Substitute.For<ILoggerAdapter>())
                                                            .ConfigureAwait(false);
                    Assert.Fail("request should have failed");
                }
                catch (MsalServiceException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalError.RequestTimeout, exc.ErrorCode);
                    Assert.IsTrue(exc.InnerException is TaskCanceledException);
                }
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestRetryConfigWithHttp500TypeFailureAsync(bool retry)
        {
            using (var httpManager = new MockHttpManager(retry, null))
            {
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.ServiceUnavailable);

                if (retry)
                {
                    //Adding second response for retry
                    httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.ServiceUnavailable);
                }
                var msalHttpResponse = await httpManager.SendPostForceResponseAsync(
                                                        new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                                                        null,
                                                        new StringContent("body"),
                                                        Substitute.For<ILoggerAdapter>())
                                                        .ConfigureAwait(false);

                Assert.IsNotNull(msalHttpResponse);
                Assert.AreEqual(HttpStatusCode.ServiceUnavailable, msalHttpResponse.StatusCode);
                //If a second request is sent when retry is configured to false, the test will fail since
                //the MockHttpManager will not be able to serve another response.
                //The MockHttpManager will also check for unused responses which will check if the retry did not occur when it should have.

            }
        }
    }
}
