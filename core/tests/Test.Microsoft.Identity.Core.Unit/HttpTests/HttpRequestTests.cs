//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Microsoft.Identity.Core.Unit;
using Test.Microsoft.Identity.Core.Unit.Mocks;

namespace Test.Microsoft.Identity.Unit.HttpTests
{
    [TestClass]
    public class HttpRequestTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
            CoreExceptionFactory.Instance = new TestExceptionFactory();
        }

        [TestMethod]
        [TestCategory("HttpRequestTests")]
        public void TestSendPostNullHeaderNullBody()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            HttpResponse response =
                HttpRequest.SendPostAsync(new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                    null, (HttpContent)null, null).Result;

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(MockHelpers.DefaultTokenResponse, response.Body);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("HttpRequestTests")]
        public void TestSendPostNoFailure()
        {
            Dictionary<string, string> bodyParameters = new Dictionary<string, string> { ["key1"] = "some value1", ["key2"] = "some value2" };
            Dictionary<string, string> queryParams = new Dictionary<string, string> { ["key1"] = "qp1", ["key2"] = "qp2" };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                PostData = bodyParameters,
                QueryParams = queryParams,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });


            HttpResponse response =
                HttpRequest.SendPostAsync(new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token?key1=qp1&key2=qp2"),
                    queryParams, bodyParameters, null).Result;

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(MockHelpers.DefaultTokenResponse, response.Body);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("HttpRequestTests")]
        public void TestSendGetNoFailure()
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string> { ["key1"] = "qp1", ["key2"] = "qp2" };
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                QueryParams = queryParams,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            HttpResponse response =
                HttpRequest.SendGetAsync(new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token?key1=qp1&key2=qp2"), queryParams, null).Result;

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(MockHelpers.DefaultTokenResponse, response.Body);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("HttpRequestTests")]
        public async Task TestSendGetWithHttp500TypeFailure()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.GatewayTimeout)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.InternalServerError),
            });

            try
            {
                var msalHttpResponse = await HttpRequest.SendGetAsync(new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                    new Dictionary<string, string>(), new RequestContext(new TestLogger(Guid.NewGuid(), null))).ConfigureAwait(false);
                Assert.Fail("request should have failed");
            }
            catch (TestServiceException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual(CoreErrorCodes.ServiceNotAvailable, exc.ErrorCode);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("HttpRequestTests")]
        public async Task TestSendPostWithHttp500TypeFailure()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.GatewayTimeout)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.ServiceUnavailable),
            });

            try
            {
                var msalHttpResponse = await HttpRequest.SendPostAsync(new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                    new Dictionary<string, string>(), (HttpContent)null, new RequestContext(new TestLogger(Guid.NewGuid(), null))).ConfigureAwait(false);
                Assert.Fail("request should have failed");
            }
            catch (TestServiceException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual(CoreErrorCodes.ServiceNotAvailable, exc.ErrorCode);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("HttpRequestTests")]
        public async Task TestSendGetWithRetryOnTimeoutFailure()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateRequestTimeoutResponseMessage(),
                ExceptionToThrow = new TaskCanceledException("request timed out")
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateRequestTimeoutResponseMessage(),
                ExceptionToThrow = new TaskCanceledException("request timed out")
            });

            try
            {
                var msalHttpResponse = await HttpRequest.SendGetAsync(new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                    new Dictionary<string, string>(), new RequestContext(new TestLogger(Guid.NewGuid(), null))).ConfigureAwait(false);
                Assert.Fail("request should have failed");
            }
            catch (TestServiceException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual(CoreErrorCodes.RequestTimeout, exc.ErrorCode);
                Assert.IsTrue(exc.InnerException is TaskCanceledException);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("HttpRequestTests")]
        public async Task TestSendPostWithRetryOnTimeoutFailure()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateRequestTimeoutResponseMessage(),
                ExceptionToThrow = new TaskCanceledException("request timed out")
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateRequestTimeoutResponseMessage(),
                ExceptionToThrow = new TaskCanceledException("request timed out")
            });

            try
            {
                var msalHttpResponse = await HttpRequest.SendPostAsync(new Uri(TestConstants.AuthorityHomeTenant + "oauth2/token"),
                    new Dictionary<string, string>(), new Dictionary<string, string>(), new RequestContext(new TestLogger(Guid.NewGuid(), null))).ConfigureAwait(false);
                Assert.Fail("request should have failed");
            }
            catch (TestServiceException exc)
            {
                Assert.IsNotNull(exc);
                Assert.AreEqual(CoreErrorCodes.RequestTimeout, exc.ErrorCode);
                Assert.IsTrue(exc.InnerException is TaskCanceledException);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }
    }
}
