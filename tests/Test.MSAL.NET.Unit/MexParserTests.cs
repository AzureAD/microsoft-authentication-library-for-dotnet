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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.Common;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\TestMex.xml")]
    [DeploymentItem(@"Resources\TestMex2005.xml")]
    public class MexParserTests
    {
        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
        [TestCategory("MexParserTests")]
        public void FetchWsTrustAddressFromMexTest()
        {
            using (Stream stream = new FileStream("TestMex2005.xml", FileMode.Open))
            {
            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method =  HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(stream)
                }
            };

            Task<WsTrustAddress> task = MexParser.FetchWsTrustAddressFromMexAsync("https://someUrl", UserAuthType.IntegratedAuth, null);

            task.Wait();
            WsTrustAddress address = task.Result;
            Assert.IsNotNull(address);
            Assert.AreEqual("https://sts.usystech.net/adfs/services/trust/2005/windowstransport", address.Uri.AbsoluteUri);
            Assert.AreEqual(WsTrustVersion.WsTrust2005, address.Version);

            }
        }

        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
        [TestCategory("MexParserTests")]
        public void MexParseExceptionTest()
        {
            string content = null;
            using (Stream stream = new FileStream("TestMex2005.xml", FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }
            }
            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content.Replace("<", "<<"))
                }
            };

            try
            {
                Task<WsTrustAddress> task = MexParser.FetchWsTrustAddressFromMexAsync("https://someUrl",
                    UserAuthType.IntegratedAuth, null);
                task.Wait();
            }
            catch (AggregateException ae)
            {
                Assert.IsTrue(ae.InnerException is MsalException);
                Assert.AreEqual(MsalError.ParsingWsMetadataExchangeFailed, ((MsalException)ae.InnerException).ErrorCode);
            }
        }
    }
}