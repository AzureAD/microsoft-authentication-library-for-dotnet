// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.WsTrust;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.WsTrustTests
{
    [TestClass]
    [DeploymentItem(@"Resources\TestMex2005.xml")]
    public class MexParserTests : TestBase
    {
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
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Get);

                try
                {
                    await harness.ServiceBundle.WsTrustWebRequestManager.GetMexDocumentAsync("http://somehost",
                                            new RequestContext(harness.ServiceBundle, Guid.NewGuid())

                        ).ConfigureAwait(false);
                    Assert.Fail("We expect an exception to be thrown here");
                }
                catch (MsalException ex)
                {
                    Assert.AreEqual(MsalError.AccessingWsMetadataExchangeFailed, ex.ErrorCode);
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
