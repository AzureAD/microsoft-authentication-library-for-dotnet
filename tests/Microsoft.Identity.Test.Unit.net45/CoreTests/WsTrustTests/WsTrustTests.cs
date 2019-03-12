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
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.WsTrust;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.WsTrustTests
{
    [TestClass]
    [DeploymentItem(@"Resources\WsTrustResponse13.xml")]
    public class WsTrustTests
    {
        [TestMethod]
        [Description("WS-Trust Request Test")]
        public async Task WsTrustRequestTestAsync()
        {
            string wsTrustAddress = "https://some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(wsTrustAddress), WsTrustVersion.WsTrust13);

            using (var harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedUrl = wsTrustAddress,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                               File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("WsTrustResponse13.xml")))
                        }
                    });

                var requestContext = RequestContext.CreateForTest(harness.ServiceBundle);
                var wsTrustRequest = endpoint.BuildTokenRequestMessageWindowsIntegratedAuth("urn:federation:SomeAudience");
                var wsTrustResponse = await harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(endpoint, wsTrustRequest, requestContext)
                                                   .ConfigureAwait(false);

                Assert.IsNotNull(wsTrustResponse.Token);
            }
        }

        [TestMethod]
        [Description("WsTrustRequest encounters HTTP 404")]
        public async Task WsTrustRequestFailureTestAsync()
        {
            string uri = "https://some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(uri), WsTrustVersion.WsTrust13);

            using (var harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Post, url: uri);

                var requestContext = RequestContext.CreateForTest(harness.ServiceBundle);
                try
                {
                    var message = endpoint.BuildTokenRequestMessageWindowsIntegratedAuth("urn:federation:SomeAudience");

                    WsTrustResponse wstResponse =
                        await harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(endpoint, message, requestContext).ConfigureAwait(false);
                    Assert.Fail("We expect an exception to be thrown here");
                }
                catch (MsalException ex)
                {
                    Assert.AreEqual(MsalError.FederatedServiceReturnedError, ex.ErrorCode);
                }
            }
        }
    }
}
