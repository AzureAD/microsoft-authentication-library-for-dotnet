// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.WsTrust;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Unit.CoreTests.WsTrustTests
{
    [TestClass]
    [DeploymentItem(@"Resources\WsTrustResponse13.xml")]
    public class WsTrustTests : TestBase
    {
        [TestMethod]
        [Description("WS-Trust Request Test")]
        public async Task WsTrustRequestTestAsync()
        {
            string wsTrustAddress = "https://some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(wsTrustAddress), WsTrustVersion.WsTrust13);

            using (var harness = CreateTestHarness())
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

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid());
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

            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Post, url: uri);

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid());
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
