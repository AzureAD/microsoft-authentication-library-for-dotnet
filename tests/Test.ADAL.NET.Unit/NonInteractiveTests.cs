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
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class NonInteractiveTests : AdalTestsBase
    {
        [TestMethod]
        [Description("User Realm Discovery Test")]
        [TestCategory("AdalDotNetUnit")]
        public async Task UserRealmDiscoveryTest()
        {
            var federatedSts = SetupStsService(StsType.AADFederatedWithADFS3);
            AuthenticationContext context = new AuthenticationContext(federatedSts.Authority, federatedSts.ValidateAuthority);
            await context.CreateAuthenticatorAsync(null);
            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, federatedSts.ValidUserId, null);
            VerifyUserRealmResponse(userRealmResponse, "Federated");

            var managedSts = SetupStsService(StsType.AAD);
            context = new AuthenticationContext(managedSts.Authority, managedSts.ValidateAuthority);
            await context.CreateAuthenticatorAsync(null);
            userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, managedSts.ValidUserId, null);
            VerifyUserRealmResponse(userRealmResponse, "Managed");

            userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, managedSts.InvalidUserId, null);
            VerifyUserRealmResponse(userRealmResponse, "Unknown");

            try
            {
                await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, null, null);
                Verify.Fail("Exception expected");
            }
            catch (ActiveDirectoryAuthenticationException ex)
            {
                Verify.IsNotNull(ex.ErrorCode, ActiveDirectoryAuthenticationError.UnknownUser);
            }

            try
            {
                await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, "ab@cd@ef", null);
                Verify.Fail("Exception expected");
            }
            catch (ActiveDirectoryAuthenticationException ex)
            {
                Verify.IsNotNull(ex.ErrorCode, ActiveDirectoryAuthenticationError.UserRealmDiscoveryFailed);
                Verify.IsNotNull(ex.InnerException);
            }

            try
            {
                await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, "#$%@#$(%@#$&%@#$&jahgfk2!#@$%346", null);
                Verify.Fail("Exception expected");
            }
            catch (ActiveDirectoryAuthenticationException ex)
            {
                Verify.IsNotNull(ex.ErrorCode, ActiveDirectoryAuthenticationError.UserRealmDiscoveryFailed);
                Verify.IsNotNull(ex.InnerException);
            }
        }

        [TestMethod]
        [Description("Mex Fetching Test")]
        [TestCategory("AdalDotNetUnit")]
        public async Task MexFetchingTest()
        {
            var federatedSts = SetupStsService(StsType.AADFederatedWithADFS3);
            AuthenticationContext context = new AuthenticationContext(federatedSts.Authority, federatedSts.ValidateAuthority);
            await context.CreateAuthenticatorAsync(null);
            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, federatedSts.ValidUserId, null);
            XDocument mexDocument = await MexParser.FetchMexAsync(userRealmResponse.FederationMetadataUrl, null);
            Verify.IsNotNull(mexDocument);

            try
            {
                await MexParser.FetchMexAsync(userRealmResponse.FederationMetadataUrl + "x", null);
                Verify.Fail("Exception expected");
            }
            catch (ActiveDirectoryAuthenticationException ex)
            {
                Verify.AreEqual(ex.ErrorCode, ActiveDirectoryAuthenticationError.AccessingWsMetadataExchangeFailed);
            }
        }

        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
        [TestCategory("AdalDotNetUnit")]
        public async Task WsTrustAddressExtractionTest()
        {
            var federatedSts = SetupStsService(StsType.AADFederatedWithADFS3);
            AuthenticationContext context = new AuthenticationContext(federatedSts.Authority, federatedSts.ValidateAuthority);
            await context.CreateAuthenticatorAsync(null);
            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, federatedSts.ValidUserId, null);
            XDocument mexDocument = await MexParser.FetchMexAsync(userRealmResponse.FederationMetadataUrl, null);
            Verify.IsNotNull(mexDocument);
            Uri wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.IntegratedAuth);
            Verify.IsNotNull(wsTrustAddress);
            wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.UsernamePassword);
            Verify.IsNotNull(wsTrustAddress);

            string mexDocumentContent = mexDocument.ToString();

            try
            {
                string modifiedMexDocumentContent = mexDocumentContent.Replace("securitypolicy", string.Empty);
                XDocument modifiedMexDocument = ConvertStringToXDocument(modifiedMexDocumentContent);
                MexParser.ExtractWsTrustAddressFromMex(modifiedMexDocument, UserAuthType.UsernamePassword);
                Verify.Fail("Exception expected");
            }
            catch (ActiveDirectoryAuthenticationException ex)
            {
                Verify.AreEqual(ex.ErrorCode, ActiveDirectoryAuthenticationError.WsTrustEndpointNotFoundInMetadataDocument);
            }

            try
            {
                string modifiedMexDocumentContent = mexDocumentContent.Replace(wsTrustAddress.AbsoluteUri, string.Empty);
                XDocument modifiedMexDocument = ConvertStringToXDocument(modifiedMexDocumentContent);
                MexParser.ExtractWsTrustAddressFromMex(modifiedMexDocument, UserAuthType.UsernamePassword);
                Verify.Fail("Exception expected");
            }
            catch (ActiveDirectoryAuthenticationException ex)
            {
                Verify.AreEqual(ex.ErrorCode, ActiveDirectoryAuthenticationError.WsTrustEndpointNotFoundInMetadataDocument);
            }
        }


        [TestMethod]
        [Description("WS-Trust Request Test")]
        [TestCategory("AdalDotNetUnit")]
        public async Task WsTrustRequestTest()
        {
            var federatedSts = SetupStsService(StsType.AADFederatedWithADFS3);
            AuthenticationContext context = new AuthenticationContext(federatedSts.Authority, federatedSts.ValidateAuthority);
            await context.CreateAuthenticatorAsync(null);
            UserRealmDiscoveryResponse userRealmResponse = await UserRealmDiscoveryResponse.CreateByDiscoveryAsync(context.Authenticator.UserRealmUri, federatedSts.ValidUserId, null);
            XDocument mexDocument = await MexParser.FetchMexAsync(userRealmResponse.FederationMetadataUrl, null);
            Verify.IsNotNull(mexDocument);
            Uri wsTrustAddress = MexParser.ExtractWsTrustAddressFromMex(mexDocument, UserAuthType.UsernamePassword);
            Verify.IsNotNull(wsTrustAddress);

            WsTrustResponse wstResponse = await WsTrustRequest.SendRequestAsync(wsTrustAddress, new UserCredential(federatedSts.ValidUserId, federatedSts.ValidPassword), null);
            Verify.IsNotNull(wstResponse.Token);
            Verify.IsTrue(wstResponse.TokenType.Contains("SAML"));

            SecureString securePassword = new SecureString();
            foreach (var ch in federatedSts.ValidPassword)
            {
                securePassword.AppendChar(ch);
            }

            wstResponse = await WsTrustRequest.SendRequestAsync(wsTrustAddress, new UserCredential(federatedSts.ValidUserId, securePassword), null);
            Verify.IsNotNull(wstResponse.Token);
            Verify.IsTrue(wstResponse.TokenType.Contains("SAML"));

            try
            {
                await WsTrustRequest.SendRequestAsync(new Uri(wsTrustAddress.AbsoluteUri + "x"), new UserCredential(federatedSts.ValidUserId, federatedSts.ValidPassword), null);
            }
            catch (ActiveDirectoryAuthenticationException ex)
            {
                Verify.IsNotNull(ex.ErrorCode, ActiveDirectoryAuthenticationError.FederatedServiceReturnedError);
                Verify.IsNotNull(ex.InnerException);
            }

            try
            {
                await WsTrustRequest.SendRequestAsync(new Uri(wsTrustAddress.AbsoluteUri), new UserCredential(federatedSts.ValidUserId, "InvalidPassword"), null);
            }
            catch (ActiveDirectoryAuthenticationException ex)
            {
                Verify.IsNotNull(ex.ErrorCode, ActiveDirectoryAuthenticationError.FederatedServiceReturnedError);
                Verify.IsNotNull(ex.InnerException);
            }
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
    }
}
