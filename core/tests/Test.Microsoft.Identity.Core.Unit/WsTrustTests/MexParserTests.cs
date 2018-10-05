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

using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net.Http;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.WsTrust;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using Test.Microsoft.Identity.Core.Unit.Mocks;
using Test.Microsoft.Identity.Core.Unit;
using System;
using System.Xml;

namespace Test.Microsoft.Identity.Unit.WsTrustTests
{
    [TestClass]
    [DeploymentItem(@"Resources\TestMex2005.xml")]
    public class MexParserTests
    {
        RequestContext requestContext;

        [TestInitialize]
        public void TestInitialize()
        {
            CoreExceptionFactory.Instance = new TestExceptionFactory();
            requestContext = new RequestContext(new TestLogger(Guid.NewGuid()));
        }

        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
        public void WsTrust2005AddressExtractionTest()
        {
            // Arrange

            string responseBody = File.ReadAllText("TestMex2005.xml");
            Assert.IsFalse(string.IsNullOrWhiteSpace(responseBody));

            // Act
            var mexDocument = new MexDocument(responseBody);
            var wsTrustEndpoint = mexDocument.GetWsTrustWindowsTransportEndpoint();

            // Assert
            Assert.AreEqual("https://sts.usystech.net/adfs/services/trust/2005/windowstransport", wsTrustEndpoint.Uri.AbsoluteUri);
            Assert.AreEqual(wsTrustEndpoint.Version, WsTrustVersion.WsTrust2005);

            // Act
            wsTrustEndpoint = mexDocument.GetWsTrustUsernamePasswordEndpoint();

            // Assert
            Assert.AreEqual("https://sts.usystech.net/adfs/services/trust/2005/usernamemixed", wsTrustEndpoint.Uri.AbsoluteUri);
            Assert.AreEqual(wsTrustEndpoint.Version, WsTrustVersion.WsTrust2005);
        }

        [TestMethod]
        [Description("Mex endpoint fails to resolve")]
        public async Task MexEndpointFailsToResolveTestAsync()
        {
            // TODO: should we move this into a separate test class for WsTrustWebRequestManager?
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("Not found")
                }
            });

            try
            {
                var wsTrustWebRequestHandler = new WsTrustWebRequestManager();
                await wsTrustWebRequestHandler.GetMexDocumentAsync("http://somehost", requestContext);
                Assert.Fail("We expect an exception to be thrown here");
            }
            catch (TestException ex)
            {
                Assert.AreEqual(CoreErrorCodes.AccessingWsMetadataExchangeFailed, ex.ErrorCode);
            }
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [Description("Mex endpoint fails to parse")]
        [ExpectedException(typeof(XmlException))]
        public void MexEndpointFailsToParseTest()
        {
            MexDocument mexDocument = new MexDocument("malformed, non-xml content");
        }
    }
}
