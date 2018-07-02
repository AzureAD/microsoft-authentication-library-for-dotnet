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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.Identity.Core;
using Microsoft.Identity.Client;
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
        [TestMethod]
        [Description("WS-Trust Request Xml Format Test")]
        public void WsTrustRequestXmlFormatTest()
        {
            // Arrange
            var cred = new UserCred("user");

            // Act
            StringBuilder sb = WsTrustRequest.BuildMessage("https://appliesto",
                new WsTrustAddress { Uri = new Uri("some://resource") }, cred);

            // Assert
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.XmlResolver = null;
            readerSettings.IgnoreWhitespace = true;
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            readerSettings.DtdProcessing = DtdProcessing.Ignore;

            // Load the fragment, validating it against the XSDs
            List<string> validationIssues = new List<string>();

            readerSettings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
            readerSettings.ValidationType = ValidationType.Schema;
            readerSettings.Schemas = CreateWsTrustEnvelopeSchemaSet();

            readerSettings.ValidationEventHandler += (s, e) =>
            {
                validationIssues.Add(e.Severity + " " + e.Message);
            };


            XmlDocument document = new XmlDocument();
            using (var xmlReader = XmlTextReader.Create(new StringReader(sb.ToString()), readerSettings))
            {
                document.Load(xmlReader);
            }


            Debug.WriteLine("All validation issues:");
            Debug.WriteLine(string.Join("\r\n", validationIssues.ToArray()));

            // Filter out "expected" schema-validation messages.
            // The real ws-trust XML namespace is http://docs.oasis-open.org/ws-sx/ws-trust/200512/ i.e. with a trailing slash. However, we use
            // the namespace without a trailing slash as this is what the server expects, so we expect validation messages about missing elements
            const string invalidTrustNamespaceMessageContent = "Could not find schema information for the element 'http://docs.oasis-open.org/ws-sx/ws-trust/200512:";
            List<string> unexpectedValidationIssues = validationIssues.Where(i => !i.Contains(invalidTrustNamespaceMessageContent)).ToList();

            Assert.AreEqual(0, unexpectedValidationIssues.Count, "Not expecting any XML schema validation errors. See the test output for the validation errors.");
        }

        private static XmlSchemaSet CreateWsTrustEnvelopeSchemaSet()
        {
            // Creates and returns a schema set that contains all of the schema required to
            // validate the XML Envelope.
            // Note: this schema are loaded dynamically from the web so this method can take several seconds.
            // However, before this validation was added the XML that was produced contained several schema
            // errors, so it's worth a few seconds to check that the XML is standards-compliant.
            XmlSchemaSet schemas = new XmlSchemaSet();
            try
            {
                schemas.XmlResolver = null;
                schemas.Add("http://www.w3.org/XML/1998/namespace", "http://www.w3.org/2001/xml.xsd");
                schemas.Add("http://www.w3.org/2003/05/soap-envelope", "http://www.w3.org/2003/05/soap-envelope");
                schemas.Add("http://www.w3.org/2005/08/addressing", "http://www.w3.org/2006/03/addressing/ws-addr.xsd");
                schemas.Add("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
                schemas.Add("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
                schemas.Add("http://schemas.xmlsoap.org/ws/2004/09/policy", "http://schemas.xmlsoap.org/ws/2004/09/policy/ws-policy.xsd");
                schemas.Add("http://docs.oasis-open.org/ws-sx/ws-trust/200512/", "http://docs.oasis-open.org/ws-sx/ws-trust/200512/ws-trust-1.3.xsd");
            }
            catch (Exception ex)
            {
                Assert.Inconclusive("Test error - failed to load the XML soap schema. Error: " + ex.ToString());
            }

            return schemas;
        }

        [TestMethod]
        [Description("WS-Trust Request Test")]
        public async Task WsTrustRequestTest()
        {
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();

            string URI = "https://some/address/usernamemixed";
            WsTrustAddress address = new WsTrustAddress()
            {
                Uri = new Uri(URI),
                Version = WsTrustVersion.WsTrust13
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Url = URI,
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse13.xml"))
                }
            });

            WsTrustResponse wstResponse = await WsTrustRequest.SendRequestAsync(address, new UserCred("username"), null, "urn:federation:SomeAudience");
            Assert.IsNotNull(wstResponse.Token);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [Description("WsTrustRequest encounters HTTP 404")]
        public async Task WsTrustRequestFailureTestAsync()
        {
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();

            string URI = "https://some/address/usernamemixed";
            WsTrustAddress address = new WsTrustAddress()
            {
                Uri = new Uri(URI),
                Version = WsTrustVersion.WsTrust13
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Url = URI,
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("Not found")
                }
            });

            var requestContext = new RequestContext(new TestLogger(Guid.NewGuid(), null));
            try
            {
                await WsTrustRequest.SendRequestAsync(address, new UserCred("username"), requestContext, "urn:federation:SomeAudience");
                Assert.Fail("We expect an exception to be thrown here");
            }
            catch (MsalException ex)
            {
                Assert.AreEqual(MsalError.FederatedServiceReturnedError, ex.ErrorCode);
            }
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }
    }
}
