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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.WsTrust;
using Test.ADAL.Common;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using UserCredential = Microsoft.IdentityModel.Clients.ActiveDirectory.UserCredential;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    [DeploymentItem("WsTrustResponse13.xml")]
    [DeploymentItem("WsTrustResponse.xml")]
    [DeploymentItem("TestMex.xml")]
    [DeploymentItem("TestMex2005.xml")]
    public class NonInteractiveTests
    {
        [TestInitialize]
        public void Initialize()
        {
            AdalHttpMessageHandlerFactory.InitializeMockProvider();
            InstanceDiscovery.InstanceCache.Clear();
            AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.GetDiscoveryEndpoint(TestConstants.DefaultAuthorityCommonTenant)));
        }

        [TestMethod]
        [Description("Get WsTrust Address from mex")]
        public async Task MexParserGetWsTrustAddressTestAsync()
        {
            AdalHttpMessageHandlerFactory.InitializeMockProvider();

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.DefaultAuthorityCommonTenant)
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("TestMex2005.xml"))
                }
            });

            WsTrustAddress address = await MexParser.FetchWsTrustAddressFromMexAsync(TestConstants.DefaultAuthorityCommonTenant, UserAuthType.IntegratedAuth, null);
            Assert.IsNotNull(address);

            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("User Realm Discovery Test")]
        public async Task UserRealmDiscoveryTestAsync()
        {
            AuthenticationContext context = new AuthenticationContext(TestConstants.GetUserRealmEndpoint(TestConstants.DefaultAuthorityCommonTenant) + "/" 
                + TestConstants.DefaultDisplayableId, new TokenCache());
            await context.Authenticator.UpdateFromTemplateAsync(null);

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetUserRealmEndpoint(TestConstants.DefaultAuthorityCommonTenant) + "/" 
                + TestConstants.DefaultDisplayableId)
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Federated\",\"domain_name\":\"microsoft.com\"," +
                                                "\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":" +
                                                "\"https://msft.sts.microsoft.com/adfs/services/trust/mex\"," +
                                                "\"federation_active_auth_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed\"" +
                                                ",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });

            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, TestConstants.DefaultDisplayableId, 
                new RequestContext(new AdalLogger(new Guid())));
            VerifyUserRealmResponse(userRealmResponse, "Federated");

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetUserRealmEndpoint(TestConstants.DefaultAuthorityCommonTenant) + "/"
                + TestConstants.DefaultDisplayableId)
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Unknown\",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });
            userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, TestConstants.DefaultDisplayableId, new RequestContext(new AdalLogger(new Guid())));
            VerifyUserRealmResponse(userRealmResponse, "Unknown");

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetUserRealmEndpoint(TestConstants.DefaultAuthorityCommonTenant) + "/"
                + null)
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateFailureResponseMessage("unknown_user")
            });

            AdalException ex = AssertException.TaskThrows<AdalException>(() =>
                UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, null, new RequestContext(new AdalLogger(new Guid()))));

            Assert.AreEqual(AdalError.UnknownUser, ex.Message);

            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Cloud Audience Urn Test")]
        public async Task CloudAudienceUrnTestAsync()
        {
            AuthenticationContext context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant);
            await context.Authenticator.UpdateFromTemplateAsync(null);

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetUserRealmEndpoint(TestConstants.DefaultAuthorityCommonTenant) + "/"
                + TestConstants.DefaultDisplayableId)
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Federated\",\"domain_name\":\"microsoft.com\"," +
                                    "\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":" +
                                    "\"https://msft.sts.microsoft.com/adfs/services/trust/mex\"," +
                                    "\"federation_active_auth_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed\"" +
                                    ",\"cloud_audience_urn\":\"urn:federation:Blackforest\"" +
                                    ",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0" }
                }
            });

            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, TestConstants.DefaultDisplayableId,
                new RequestContext(new AdalLogger(new Guid())));

            WsTrustAddress address = new WsTrustAddress()
            {
                Uri = new Uri("https://some/address/usernamemixed"),
                Version = WsTrustVersion.WsTrust13
            };

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler("https://some/address/usernamemixed")
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse13.xml"))
                }
            });

            WsTrustResponse wsTrustResponse = await WsTrustRequest.SendRequestAsync(address, new UserCredential(TestConstants.DefaultDisplayableId), null, userRealmResponse.CloudAudienceUrn);

            VerifyCloudInstanceUrnResponse("urn:federation:Blackforest", userRealmResponse.CloudAudienceUrn);

            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Cloud Audience Urn Null Test")]
        public async Task CloudAudienceUrnNullTestAsync()
        {
            AuthenticationContext context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant);
            await context.Authenticator.UpdateFromTemplateAsync(null);

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetUserRealmEndpoint(TestConstants.DefaultAuthorityCommonTenant) + "/" + TestConstants.DefaultDisplayableId)
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Federated\",\"domain_name\":\"microsoft.com\"," +
                                    "\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":" +
                                    "\"https://msft.sts.microsoft.com/adfs/services/trust/mex\"," +
                                    "\"federation_active_auth_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed\"" +
                                    ",\"cloud_audience_urn\":\"urn:federation:MicrosoftOnline\"" +
                                    ",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0" }
                }
            });

            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, TestConstants.DefaultDisplayableId, new RequestContext(new AdalLogger(new Guid())));

            WsTrustAddress address = new WsTrustAddress()
            {
                Uri = new Uri("https://some/address/usernamemixed"),
                Version = WsTrustVersion.WsTrust13
            };

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler("https://some/address/usernamemixed")
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse13.xml"))
                }
            });

            WsTrustResponse wsTrustResponse = await WsTrustRequest.SendRequestAsync(address, new UserCredential(TestConstants.DefaultDisplayableId), null, null);

            VerifyCloudInstanceUrnResponse("urn:federation:MicrosoftOnline", userRealmResponse.CloudAudienceUrn);

            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
        public async Task WsTrust2005AddressExtractionTestAsync()
        {
            await Task.Factory.StartNew(() =>
            {
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
        public async Task WsTrustRequestTestAsync()
        {
            AdalHttpMessageHandlerFactory.InitializeMockProvider();

            string URI = "https://some/address/usernamemixed";
            WsTrustAddress address = new WsTrustAddress()
            {
                Uri = new Uri(URI),
                Version = WsTrustVersion.WsTrust13
            };

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(URI)
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse13.xml"))
                }
            });

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(URI)
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse13.xml"))
                }
            });
            
            WsTrustResponse wstResponse = await WsTrustRequest.SendRequestAsync(address, new UserPasswordCredential(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword), null, TestConstants.CloudAudienceUrnMicrosoft);
            Assert.IsNotNull(wstResponse.Token);

            wstResponse = await WsTrustRequest.SendRequestAsync(address, new UserCredential(TestConstants.DefaultDisplayableId), null, TestConstants.CloudAudienceUrnMicrosoft);
            Assert.IsNotNull(wstResponse.Token);

            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("WS-Trust Request Generic Cloud Urn Test")]
        public async Task WsTrustRequestGenericCloudUrnTestAsync()
        {
            AdalHttpMessageHandlerFactory.InitializeMockProvider();

            string URI = "https://some/address/usernamemixed";

            WsTrustAddress address = new WsTrustAddress()
            {
                Uri = new Uri(URI),
                Version = WsTrustVersion.WsTrust13
            };

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(URI)
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse13.xml"))
                }
            });

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(URI)
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("WsTrustResponse13.xml"))
                }
            });

            WsTrustResponse wstResponse = await WsTrustRequest.SendRequestAsync(address, new UserPasswordCredential(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword), null, TestConstants.CloudAudienceUrn);
            Assert.IsNotNull(wstResponse.Token);

            wstResponse = await WsTrustRequest.SendRequestAsync(address, new UserCredential(TestConstants.DefaultDisplayableId), null, TestConstants.CloudAudienceUrn);
            Assert.IsNotNull(wstResponse.Token);

            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("WS-Trust Request Xml Format Test")]
        public void WsTrustRequestXmlFormatTest()
        {
            // Arrange
            UserCredential cred = new UserPasswordCredential("user", "pass&<>\"'");

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

        private static void VerifyCloudInstanceUrnResponse(string cloudAudienceUrn, string expectedCloudAudienceUrn)
        {
            Assert.AreEqual(cloudAudienceUrn, expectedCloudAudienceUrn);
        }
    }
}