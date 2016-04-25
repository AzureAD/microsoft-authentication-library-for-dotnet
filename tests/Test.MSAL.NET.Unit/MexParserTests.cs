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
                var wsTrustAddress = task.Result;
            }
            catch (AggregateException ae)
            {
                Assert.IsTrue(ae.InnerException is MsalException);
                Assert.AreEqual(MsalError.ParsingWsMetadataExchangeFailed, ((MsalException)ae.InnerException).ErrorCode);
            }
        }
    }
}
