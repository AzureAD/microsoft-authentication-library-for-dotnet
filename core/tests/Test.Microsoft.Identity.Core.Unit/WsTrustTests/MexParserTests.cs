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
            
            XDocument mexDocument = null;
            using (Stream stream = new FileStream("TestMex2005.xml", FileMode.Open))
            {
                mexDocument = XDocument.Load(stream);
            }
            Assert.IsNotNull(mexDocument);

            // Act
            MexParser mexParser = new MexParser(UserAuthType.IntegratedAuth, requestContext);
            WsTrustAddress wsTrustAddress = mexParser.ExtractWsTrustAddressFromMex(mexDocument);

            // Assert
            Assert.AreEqual("https://sts.usystech.net/adfs/services/trust/2005/windowstransport", wsTrustAddress.Uri.AbsoluteUri);
            Assert.AreEqual(wsTrustAddress.Version, WsTrustVersion.WsTrust2005);

            // Act
            mexParser = new MexParser(UserAuthType.UsernamePassword, requestContext);
            wsTrustAddress = mexParser.ExtractWsTrustAddressFromMex(mexDocument);

            // Assert
            Assert.AreEqual("https://sts.usystech.net/adfs/services/trust/2005/usernamemixed", wsTrustAddress.Uri.AbsoluteUri);
            Assert.AreEqual(wsTrustAddress.Version, WsTrustVersion.WsTrust2005);
        }

        [TestMethod]
        [Description("Mex endpoint fails to resolve")]
        public async Task MexEndpointFailsToResolveTestAsync()
        {
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
                MexParser mexParser = new MexParser(UserAuthType.IntegratedAuth, requestContext);
                await mexParser.FetchWsTrustAddressFromMexAsync("http://somehost");
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
        public async Task MexEndpointFailsToParseTestAsync()
        {
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("malformed, non-xml content")
                }
            });
            var requestContext = new RequestContext(new TestLogger(Guid.NewGuid(), null));
            try
            {
                MexParser mexParser = new MexParser(UserAuthType.IntegratedAuth, requestContext);
                await mexParser.FetchWsTrustAddressFromMexAsync("http://somehost");
                Assert.Fail("We expect an exception to be thrown here");
            }
            catch (System.Xml.XmlException)
            {
            }
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }
    }
}
