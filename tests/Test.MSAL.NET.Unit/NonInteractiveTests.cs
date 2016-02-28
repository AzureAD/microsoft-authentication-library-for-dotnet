//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.Common;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    [DeploymentItem("TestMex.xml")]
    [DeploymentItem("TestMex2005.xml")]
    public class NonInteractiveTests : MsalTestsBase
    {

        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
        [TestCategory("AdalDotNet")]
        public async Task WsTrust2005AddressExtractionTest()
        {
            await Task.Factory.StartNew(() => {
                XDocument mexDocument = null;
                using (Stream stream = new FileStream("TestMex2005.xml", FileMode.Open))
                {
                    mexDocument = XDocument.Load(stream);
                }

                Assert.IsNotNull(mexDocument);
                WsTrustAddress wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.IntegratedAuth, null);
                Assert.IsNotNull(wsTrustAddress);
                Assert.AreEqual(wsTrustAddress.Version, WsTrustVersion.WsTrust13);
                wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.UsernamePassword, null);
                Assert.IsNotNull(wsTrustAddress);
                Assert.AreEqual(wsTrustAddress.Version, WsTrustVersion.WsTrust2005);
            });
        }
        
        [TestMethod]
        [Description("WS-Trust Request Xml Format Test")]
        [TestCategory("AdalDotNet")]
        public async Task WsTrustRequestXmlFormatTest()
        {
            await Task.Factory.StartNew(() =>
            {
                UserCredential cred = new UserCredential("user");
                StringBuilder sb = WsTrustRequest.BuildMessage("https://appliesto",
                    new WsTrustAddress {Uri = new Uri("resource")}, cred);
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml("<?xml version=\"1.0\"?>" + sb.ToString());
                }
                catch (Exception ex)
                {
                    Assert.Fail("Not expected -- " + ex.Message);
                }
            });
        }
    }
}