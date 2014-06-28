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
using Windows.Storage;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Test.ADAL.Common;
using Logger = Microsoft.IdentityModel.Clients.ActiveDirectory.Logger;

namespace Test.ADAL.WinPhone.UnitTest
{
    [TestClass]
    public partial class AdalWinPhoneTests : AdalTestsBase
    {
        [AssemblyInitialize]
        public static void AssemlyInitialize(TestContext testContext)
        {
            AdalTests.TestType = TestType.WinPhone;
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
        //[Description("Positive Test for ContinueAcquireToken")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveTest(string stsType)
        {
            AdalTests.AcquireTokenPositive(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for ContinueAcquireToken with Refresh Token")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveByRefreshTokenTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveByRefreshTokenTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for ContinueAcquireToken with invalid authority")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithInvalidAuthorityTest(string stsType)
        {//
           // AdalTests.AcquireTokenWithInvalidAuthority(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for ContinueAcquireToken with invalid resource")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithInvalidResourceTest(string stsType)
        {
            //AdalTests.AcquireTokenWithInvalidResource(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for ContinueAcquireToken with invalid client id")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithInvalidClientIdTest(string stsType)
        {
           // AdalTests.AcquireTokenWithInvalidClientId(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for ContinueAcquireToken with incorrect user credential")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithIncorrectUserCredentialTest(string stsType)
        {
            //AdalTests.AcquireTokenWithIncorrectUserCredentialTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for ContinueAcquireToken with user canceling authentication")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithAuthenticationCanceledTest(string stsType)
        {
            // ADFS security dialog hang up
            //AdalTests.AcquireTokenWithAuthenticationCanceledTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for ContinueAcquireToken testing default token cache")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenPositiveWithDefaultCacheTest(string stsType)
        {
            //AdalTests.AcquireTokenPositiveWithDefaultCacheTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for ContinueAcquireToken testing custom in memory token cache")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenPositiveWithInMemoryCacheTest(string stsType)
        {
            //AdalTests.AcquireTokenPositiveWithInMemoryCache(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for ContinueAcquireToken testing default token cache")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenPositiveWithNullCacheTest(string stsType)
        {
            AdalTests.AcquireTokenPositiveWithNullCache(SetupStsService(GetStsType(stsType)));
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
            //await AdalTests.MultiResourceRefreshTokenTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for acquring token using tenantless endpoint")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        public void TenantlessTest(string stsType)
        {
            //AdalTests.TenantlessTest(SetupStsService(GetStsType(stsType)));
        }

        //[TestMethod]
        ////[Description("Test for STS Instance Discovery")]
        //[TestCategory("AdalWinPhoneMock")]
        //[DataRow("AAD")]
        //public async Task InstanceDiscoveryTest(string stsType)
        //{
        //    await AdalTests.InstanceDiscoveryTestAsync(SetupStsService(GetStsType(stsType)));
        //}

        [TestMethod]
        //[Description("Test for STS Instance Discovery")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        public async Task InstanceDiscoveryErrorTest(string stsType)
        {
            await AdalTests.InstanceDiscoveryErrorTestAsync(SetupStsService(GetStsType(stsType)));
        }

        private static void SetReplayerNetworkPlugin()
        {
            NetworkPlugin.WebUIFactory = new ReplayerWebUIFactory();
            NetworkPlugin.HttpWebRequestFactory = new ReplayerHttpWebRequestFactory();
            NetworkPlugin.RequestCreationHelper = new ReplayerRequestCreationHelper();
        }
    }
}
