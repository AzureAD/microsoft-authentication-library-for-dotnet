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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.WsTrust;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Microsoft.Identity.Core.Unit;
using Test.Microsoft.Identity.Core.Unit.Mocks;

namespace Test.Microsoft.Identity.Unit.WsTrustTests
{
    [TestClass]
    [DeploymentItem(@"Resources\WsTrustResponse13.xml")]
    public class WsTrustTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            CoreExceptionFactory.Instance = new TestExceptionFactory();
        }

        [TestMethod]
        [Description("WS-Trust Request Test")]
        public async Task WsTrustRequestTestAsync()
        {
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();

            string wsTrustAddress = "https://some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(wsTrustAddress), WsTrustVersion.WsTrust13);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Url = wsTrustAddress,
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse13.xml"))
                }
            });

            var requestContext = new RequestContext(new TestLogger(Guid.NewGuid(), null));
            var wsTrustRequest = endpoint.BuildTokenRequestMessageWindowsIntegratedAuth("urn:federation:SomeAudience");
            var manager = new WsTrustWebRequestManager();
            var wsTrustResponse = await manager.GetWsTrustResponseAsync(endpoint, wsTrustRequest, requestContext).ConfigureAwait(false);

            Assert.IsNotNull(wsTrustResponse.Token);
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [Description("WsTrustRequest encounters HTTP 404")]
        public async Task WsTrustRequestFailureTestAsync()
        {
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();

            string uri = "https://some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(uri), WsTrustVersion.WsTrust13);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Url = uri,
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("Not found")
                }
            });

            var requestContext = new RequestContext(new TestLogger(Guid.NewGuid(), null));
            try
            {
                var message = endpoint.BuildTokenRequestMessageWindowsIntegratedAuth("urn:federation:SomeAudience");
                var manager = new WsTrustWebRequestManager();

                WsTrustResponse wstResponse = await manager.GetWsTrustResponseAsync(endpoint, message, requestContext).ConfigureAwait(false);
                Assert.Fail("We expect an exception to be thrown here");
            }
            catch (TestException ex)
            {
                Assert.AreEqual(CoreErrorCodes.FederatedServiceReturnedError, ex.ErrorCode);
            }
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }
    }
}
