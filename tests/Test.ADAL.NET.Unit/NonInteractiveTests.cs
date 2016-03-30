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
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;
using System.Xml;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    [DeploymentItem("TestMex.xml")]
    [DeploymentItem("TestMex2005.xml")]
    public class NonInteractiveTests : AdalTestsBase
    {
        // Switch this to true to run test against actual service
        private const bool MockService = true;

        [TestMethod]
        [Description("User Realm Discovery Test")]
        [TestCategory("AdalDotNet")]
        public async Task UserRealmDiscoveryTest()
        {
            var federatedSts = SetupStsService(StsType.AADFederatedWithADFS3);
            AuthenticationContext context = new AuthenticationContext(federatedSts.Authority, federatedSts.ValidateAuthority);
            await context.Authenticator.UpdateFromTemplateAsync(null);
            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, federatedSts.ValidUserName, null);
            VerifyUserRealmResponse(userRealmResponse, "Federated");

            var managedSts = SetupStsService(StsType.AAD);
            context = new AuthenticationContext(managedSts.Authority, managedSts.ValidateAuthority);
            await context.Authenticator.UpdateFromTemplateAsync(null);
            userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, managedSts.ValidUserName, null);
            VerifyUserRealmResponse(userRealmResponse, "Managed");

            userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, managedSts.InvalidUserName, null);
            VerifyUserRealmResponse(userRealmResponse, "Unknown");

            try
            {
                await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, null, null);
                Verify.Fail("Exception expected");
            }
            catch (AdalException ex)
            {
                Verify.IsNotNull(ex.ErrorCode, AdalError.UnknownUser);
            }

            userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, "ab@cd@ef", null);
            Verify.AreEqual("Unknown", userRealmResponse.AccountType);

            try
            {
                await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, "#$%@#$(%@#$&%@#$&jahgfk2!#@$%346", null);
                Verify.Fail("Exception expected");
            }
            catch (AdalException ex)
            {
                Verify.IsNotNull(ex.ErrorCode, AdalError.UserRealmDiscoveryFailed);
                Verify.IsNotNull(ex.InnerException);
            }
        }

        [TestMethod]
        [Description("Mex Fetching Test")]
        [TestCategory("AdalDotNet")]
        public async Task MexFetchingTest()
        {
            var federatedSts = SetupStsService(StsType.AADFederatedWithADFS3);
            AuthenticationContext context = new AuthenticationContext(federatedSts.Authority, federatedSts.ValidateAuthority);
            await context.Authenticator.UpdateFromTemplateAsync(null);
            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, federatedSts.ValidUserName, null);
            XDocument mexDocument = await FecthMexAsync(userRealmResponse.FederationMetadataUrl);
            Verify.IsNotNull(mexDocument);

            try
            {
                await FecthMexAsync(userRealmResponse.FederationMetadataUrl + "x");
                Verify.Fail("Exception expected");
            }
            catch (AdalException ex)
            {
                Verify.AreEqual(ex.ErrorCode, AdalError.AccessingWsMetadataExchangeFailed);
            }
        }

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

                Verify.IsNotNull(mexDocument);
                WsTrustAddress wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.IntegratedAuth, null);
                Verify.IsNotNull(wsTrustAddress);
                Verify.AreEqual(wsTrustAddress.Version, WsTrustVersion.WsTrust13);
                wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.UsernamePassword, null);
                Verify.IsNotNull(wsTrustAddress);
                Verify.AreEqual(wsTrustAddress.Version, WsTrustVersion.WsTrust2005);
            });
        }

        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
        [TestCategory("AdalDotNet")]
        public async Task WsTrustAddressExtractionTest()
        {
            var federatedSts = SetupStsService(StsType.AADFederatedWithADFS3);
            AuthenticationContext context = new AuthenticationContext(federatedSts.Authority, federatedSts.ValidateAuthority);
            await context.Authenticator.UpdateFromTemplateAsync(null);
            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, federatedSts.ValidUserName, null);
            XDocument mexDocument = await FecthMexAsync(userRealmResponse.FederationMetadataUrl);
            Verify.IsNotNull(mexDocument);
            WsTrustAddress wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.IntegratedAuth, null);
            Verify.IsNotNull(wsTrustAddress);
            wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.UsernamePassword, null);
            Verify.IsNotNull(wsTrustAddress);

            string mexDocumentContent = mexDocument.ToString();
            string modifiedMexDocumentContent = null;
            XDocument modifiedMexDocument = null;

            try
            {
                modifiedMexDocumentContent = mexDocumentContent.Replace("securitypolicy", string.Empty);
                modifiedMexDocument = ConvertStringToXDocument(modifiedMexDocumentContent);
                MexParser.ExtractWsTrustAddressFromMex(modifiedMexDocument, UserAuthType.UsernamePassword, null);
                Verify.Fail("Exception expected");
            }
            catch (AdalException ex)
            {
                Verify.AreEqual(ex.ErrorCode, AdalError.WsTrustEndpointNotFoundInMetadataDocument);
            }
            
                modifiedMexDocumentContent = mexDocumentContent.Replace(wsTrustAddress.Uri.AbsoluteUri, string.Empty);
                modifiedMexDocument = ConvertStringToXDocument(modifiedMexDocumentContent);
                wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(modifiedMexDocument, UserAuthType.UsernamePassword, null);
                //falls back to WS-Trust 2005
                Assert.IsTrue(wsTrustAddress.Version == WsTrustVersion.WsTrust2005);
        }


        [TestMethod]
        [Description("WS-Trust Request Test")]
        [TestCategory("AdalDotNet")]
        public async Task WsTrustRequestTest()
        {
            var federatedSts = SetupStsService(StsType.AADFederatedWithADFS3);
            AuthenticationContext context = new AuthenticationContext(federatedSts.Authority, federatedSts.ValidateAuthority);
            await context.Authenticator.UpdateFromTemplateAsync(null);
            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, federatedSts.ValidUserName, null);
            XDocument mexDocument = await FecthMexAsync(userRealmResponse.FederationMetadataUrl);
            Verify.IsNotNull(mexDocument);
            WsTrustAddress wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.UsernamePassword, null);
            Verify.IsNotNull(wsTrustAddress);

            WsTrustResponse wstResponse = await WsTrustRequest.SendRequestAsync(wsTrustAddress, new UserPasswordCredential(federatedSts.ValidUserName, federatedSts.ValidPassword), null);
            Verify.IsNotNull(wstResponse.Token);
            Verify.IsTrue(wstResponse.TokenType.Contains("SAML"));

            Verify.IsNotNull(wstResponse.Token);
            Verify.IsTrue(wstResponse.TokenType.Contains("SAML"));

            try
            {
                await WsTrustRequest.SendRequestAsync(new WsTrustAddress { Uri = new Uri(wsTrustAddress.Uri.AbsoluteUri + "x") },
                    new UserPasswordCredential(federatedSts.ValidUserName, federatedSts.ValidPassword), null);
            }
            catch (AdalException ex)
            {
                Verify.IsNotNull(ex.ErrorCode, AdalError.FederatedServiceReturnedError);
                Verify.IsNotNull(ex.InnerException);
            }

            try
            {
                await WsTrustRequest.SendRequestAsync(new WsTrustAddress { Uri = new Uri(wsTrustAddress.Uri.AbsoluteUri) }, new UserPasswordCredential(federatedSts.ValidUserName, "InvalidPassword"), null);
            }
            catch (AdalException ex)
            {
                Verify.IsNotNull(ex.ErrorCode, AdalError.FederatedServiceReturnedError);
                Verify.IsNotNull(ex.InnerException);
            }
        }

        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
        [TestCategory("AdalDotNet")]
        public async Task WsTrustPolicyExtraction()
        {
            XDocument mexDocument = null;
            using (Stream stream = new FileStream("TestMex2005.xml", FileMode.Open))
            {
                mexDocument = XDocument.Load(stream);
            }

            Verify.IsNotNull(mexDocument);
            Dictionary<string, MexPolicy> policies = MexParser.ReadPolicies(mexDocument);
            Verify.IsNotNull(policies);
            Verify.IsTrue(policies.Count == 2);
            foreach (var policy in policies)
            {
                Verify.IsTrue(policy.Value.Version == WsTrustVersion.WsTrust2005);
            }
        }

        [TestMethod]
        [Description("WS-Trust Request Xml Format Test")]
        [TestCategory("AdalDotNet")]
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
                    Verify.Fail("Not expected -- " + ex.Message);
                }
            });
        }

        private static void VerifyUserRealmResponse(UserRealmDiscoveryResponse userRealmResponse, string expectedAccountType)
        {
            Verify.AreEqual("1.0", userRealmResponse.Version);
            Verify.AreEqual(userRealmResponse.AccountType, expectedAccountType);
            if (expectedAccountType == "Federated")
            {
                Verify.IsNotNull(userRealmResponse.FederationActiveAuthUrl);
                Verify.IsNotNull(userRealmResponse.FederationMetadataUrl);
                Verify.AreEqual("WSTrust", userRealmResponse.FederationProtocol);
            }
            else
            {
                Verify.IsNull(userRealmResponse.FederationActiveAuthUrl);
                Verify.IsNull(userRealmResponse.FederationMetadataUrl);
                Verify.IsNull(userRealmResponse.FederationProtocol);
            }
        }

        private static XDocument ConvertStringToXDocument(string mexDocumentContent)
        {
            byte[] serializedMexDocumentContent = Encoding.UTF8.GetBytes(mexDocumentContent);
            using (MemoryStream stream = new MemoryStream(serializedMexDocumentContent))
            {
                return XDocument.Load(stream);
            }
        }

        private async static Task<XDocument> FecthMexAsync(string metadataUrl)
        {
            return await Task.Factory.StartNew(() =>
            {
                if (metadataUrl.EndsWith("xx"))
                {
                    throw new AdalException(AdalError.AccessingWsMetadataExchangeFailed);
                }

                using (Stream stream = new FileStream("TestMex.xml", FileMode.Open))
                {
                    return XDocument.Load(stream);
                }
            });
        }
    }
}