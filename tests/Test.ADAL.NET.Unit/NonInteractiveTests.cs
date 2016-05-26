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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;
using System.Xml;
using Test.ADAL.NET.Unit.Mocks;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    [DeploymentItem("WsTrustResponse.xml")]
    [DeploymentItem("TestMex.xml")]
    [DeploymentItem("TestMex2005.xml")]
    public class NonInteractiveTests
    {
        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.ClearMockHandlers();    
        }
    
        [TestMethod]
        [Description("User Realm Discovery Test")]
        public async Task UserRealmDiscoveryTest()
        {
            AuthenticationContext context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant);
            await context.Authenticator.UpdateFromTemplateAsync(null);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Federated\",\"domain_name\":\"microsoft.com\"," +
                                                "\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":" +
                                                "\"https://msft.sts.microsoft.com/adfs/services/trust/mex\"," +
                                                "\"federation_active_auth_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed\"" +
                                                ",\"cloudinstancename\":\"login.microsoftonline.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });

            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, TestConstants.DefaultDisplayableId, null);
            VerifyUserRealmResponse(userRealmResponse, "Federated");


            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Unknown\",\"cloudinstancename\":\"login.microsoftonline.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });
            userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, TestConstants.DefaultDisplayableId, null);
            VerifyUserRealmResponse(userRealmResponse, "Unknown");

            try
            {
                await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, null, null);
                Assert.Fail("Exception expected");
            }
            catch (AdalException ex)
            {
                Assert.IsNotNull(ex.ErrorCode, AdalError.UnknownUser);
            }
        }

        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
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
                Assert.AreEqual(wsTrustAddress.Version, WsTrustVersion.WsTrust2005);
                wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.UsernamePassword, null);
                Assert.IsNotNull(wsTrustAddress);
                Assert.AreEqual(wsTrustAddress.Version, WsTrustVersion.WsTrust2005);
            });
        }

        [TestMethod]
        [Description("WS-Trust Request Test")]
        public async Task WsTrustRequestTest()
        {
            WsTrustAddress address = new WsTrustAddress()
            {
                Uri = new Uri("https://some/address/usernamemixed"),
                Version = WsTrustVersion.WsTrust13
            };
            
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse.xml"))
                }
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse.xml"))
                }
            });

            WsTrustResponse wstResponse = await WsTrustRequest.SendRequestAsync(address, new UserPasswordCredential(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword), null);
            Assert.IsNotNull(wstResponse.Token);

            wstResponse = await WsTrustRequest.SendRequestAsync(address, new UserCredential(TestConstants.DefaultDisplayableId), null);
            Assert.IsNotNull(wstResponse.Token);
        }

        [TestMethod]
        [Description("WS-Trust Request Xml Format Test")]
        public async Task WsTrustRequestXmlFormatTest()
        {
            await Task.Factory.StartNew(() =>
            {
                UserCredential cred = new UserPasswordCredential("user", "pass&<>\"'");
                StringBuilder sb = WsTrustRequest.BuildMessage("https://appliesto",
                    new WsTrustAddress {Uri = new Uri("some://resource")}, cred);
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

        private static void VerifyUserRealmResponse(UserRealmDiscoveryResponse userRealmResponse, string expectedAccountType)
        {
            Assert.AreEqual("1.0", userRealmResponse.Version);
            Assert.AreEqual(userRealmResponse.AccountType, expectedAccountType);
            if (expectedAccountType == "Federated")
            {
                Assert.IsNotNull(userRealmResponse.FederationActiveAuthUrl);
                Assert.IsNotNull(userRealmResponse.FederationMetadataUrl);
                Assert.AreEqual("WSTrust", userRealmResponse.FederationProtocol);
            }
            else
            {
                Assert.IsNull(userRealmResponse.FederationActiveAuthUrl);
                Assert.IsNull(userRealmResponse.FederationMetadataUrl);
                Assert.IsNull(userRealmResponse.FederationProtocol);
            }
        }
    }
}
