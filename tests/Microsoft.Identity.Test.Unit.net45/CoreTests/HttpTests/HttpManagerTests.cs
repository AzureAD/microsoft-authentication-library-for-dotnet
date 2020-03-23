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

namespace Microsoft.Identity.Test.Unit.CoreTests.HttpTests
{
    [TestClass]
    public class HttpManagerTests
    {
        readonly Dictionary<string, string> _httpTelemetryHeaders = new Dictionary<string, string>
        {
            {TelemetryConstants.XClientLastTelemetry, TelemetryConstants.HttpTelemetrySchemaVersion2},
            {TelemetryConstants.XClientCurrentTelemetry, TelemetryConstants.HttpTelemetrySchemaVersion2}
        };

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
                httpManager.AddResponseMockHandlerForPost(MockHelpers.CreateSuccessTokenResponseMessage());

                var response = httpManager.SendPostAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/v2.0/token"),
                    null,
                    (IDictionary<string, string>)null,
                    null).Result;

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(MockHelpers.DefaultTokenResponse, response.Body);
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
                httpManager.AddResponseMockHandlerForPost(MockHelpers.CreateSuccessTokenResponseMessage(), bodyParameters, queryParams);

                var response = httpManager.SendPostAsync(
                    new Uri(TestConstants.AuthorityHomeTenant + "oauth2/v2.0/token?key1=qp1&key2=qp2"),
                    queryParams,
                    bodyParameters,
                    null).Result;

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(MockHelpers.DefaultTokenResponse, response.Body);
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
                    null).Result;

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(MockHelpers.DefaultTokenResponse, response.Body);
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
                                                                _httpTelemetryHeaders,
                                                                Substitute.For<ICoreLogger>())
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
        public async Task TestSendGetWithHttp500TypeFailure2Async()
        {
            using (var httpManager = new MockHttpManager())
            {                
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.BadGateway);
                httpManager.AddResiliencyMessageMockHandler(HttpMethod.Post, HttpStatusCode.BadGateway);

                var msalHttpResponse = await httpManager.SendPostForceResponseAsync(
                                                            new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                                                            _httpTelemetryHeaders,
                                                            new StringContent("body"),
                                                            Substitute.For<ICoreLogger>())
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
                                                                _httpTelemetryHeaders,
                                                                (IDictionary<string, string>)null,
                                                               Substitute.For<ICoreLogger>())
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
                                                                Substitute.For<ICoreLogger>())
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
                                                                Substitute.For<ICoreLogger>())
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
    }
}
