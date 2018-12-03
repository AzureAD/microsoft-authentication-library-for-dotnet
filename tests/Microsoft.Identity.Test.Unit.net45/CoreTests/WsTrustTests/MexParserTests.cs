// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.WsTrust;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Core.Mocks.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.WsTrustTests
{
    [TestClass]
    [DeploymentItem(@"Resources\TestMex2005.xml")]
    public class MexParserTests
    {
        private RequestContext _requestContext;
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            CoreExceptionFactory.Instance = new TestExceptionFactory();
            _requestContext = new RequestContext(null, new TestLogger(Guid.NewGuid()));
        }

        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
        public void WsTrust2005AddressExtractionTest()
        {
            // Arrange
            string responseBody = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex2005.xml"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(responseBody));

            // Act
            var mexDocument = new MexDocument(responseBody);
            var wsTrustEndpoint = mexDocument.GetWsTrustWindowsTransportEndpoint();

            // Assert
            Assert.AreEqual(
                "https://sts.usystech.net/adfs/services/trust/2005/windowstransport",
                wsTrustEndpoint.Uri.AbsoluteUri);
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
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Get);

                try
                {
                    await serviceBundle.WsTrustWebRequestManager.GetMexDocumentAsync("http://somehost", _requestContext).ConfigureAwait(false);
                    Assert.Fail("We expect an exception to be thrown here");
                }
                catch (TestException ex)
                {
                    Assert.AreEqual(CoreErrorCodes.AccessingWsMetadataExchangeFailed, ex.ErrorCode);
                }
            }
        }

        [TestMethod]
        [Description("Mex endpoint fails to parse")]
        [ExpectedException(typeof(XmlException))]
        public void MexEndpointFailsToParseTest()
        {
            var mexDocument = new MexDocument("malformed, non-xml content");
        }
    }
}