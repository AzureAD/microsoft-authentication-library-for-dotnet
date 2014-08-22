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
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using Test.ADAL.Common;
using Test.ADAL.WinRT.Unit;

namespace Test.ADAL.WinPhone.Unit
{
    [TestClass]
    public class AdalWinPhoneTests : AdalTestsBase
    {
        [AssemblyInitialize]
        public static void AssemlyInitialize(TestContext testContext)
        {
            AdalTests.TestType = TestType.WinRT;
            ReplayerBase.InitializeAsync().Wait();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
        }

        [TestInitialize]
        public void TestMethodSetup()
        {
            AdalTests.InitializeTest();

            SetReplayerNetworkPlugin();
        }


        [TestMethod]
        //[Description("Positive Test for AcquireToken")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenPositiveTest(string stsType)
        {
            AdalTests.AcquireTokenPositiveTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken with Refresh Token")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveByRefreshTokenTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveByRefreshTokenTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for autority validation to AuthenticationContext")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AuthenticationContextAuthorityValidationTest(string stsType)
        {
            AdalTests.AuthenticationContextAuthorityValidationTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid authority")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithInvalidAuthorityTest(string stsType)
        {
            AdalTests.AcquireTokenWithInvalidAuthorityTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid resource")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithInvalidResourceTest(string stsType)
        {
            AdalTests.AcquireTokenWithInvalidResourceTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid client id")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithInvalidClientIdTest(string stsType)
        {
            AdalTests.AcquireTokenWithInvalidClientIdTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with incorrect user credential")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithIncorrectUserCredentialTest(string stsType)
        {
            AdalTests.AcquireTokenWithIncorrectUserCredentialTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void ExtraQueryParametersTest(string stsType)
        {
            AdalTests.ExtraQueryParametersTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with user canceling authentication")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithAuthenticationCanceledTest(string stsType)
        {
            // ADFS security dialog hang up
            AdalTests.AcquireTokenWithAuthenticationCanceledTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenPositiveWithDefaultCacheTest(string stsType)
        {
            AdalTests.AcquireTokenPositiveWithDefaultCacheTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing custom in memory token cache")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenPositiveWithInMemoryCacheTest(string stsType)
        {
            AdalTests.AcquireTokenPositiveWithInMemoryCacheTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        [Ignore]    // Enable once the test bug is fixed.
        public void AcquireTokenPositiveWithNullCacheTest(string stsType)
        {
            AdalTests.AcquireTokenPositiveWithNullCacheTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for UserInfo")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void UserInfoTest(string stsType)
        {
            AdalTests.UserInfoTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for multi resource refresh token")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task MultiResourceRefreshTokenTest(string stsType)
        {
            await AdalTests.MultiResourceRefreshTokenTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for acquring token using tenantless endpoint")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        public void TenantlessTest(string stsType)
        {
            AdalTests.TenantlessTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for STS Instance Discovery")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        public async Task InstanceDiscoveryTest(string stsType)
        {
            await AdalTests.InstanceDiscoveryTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for AuthenticationContextDelegate")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        public void AcquireTokenWithCallbackTest(string stsType)
        {
            AdalTests.AcquireTokenWithCallbackTest(SetupStsService(GetStsType(stsType)));
        }

        private static void SetReplayerNetworkPlugin()
        {
            NetworkPlugin.WebUIFactory = new ReplayerWebUIFactory();
            NetworkPlugin.HttpWebRequestFactory = new ReplayerHttpWebRequestFactory();
            NetworkPlugin.RequestCreationHelper = new ReplayerRequestCreationHelper();
        }
    }
}
