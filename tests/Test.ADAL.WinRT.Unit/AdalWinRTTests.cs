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

namespace Test.ADAL.WinRT.Unit
{
    [TestClass]
    public partial class AdalWinRTTests : AdalTestsBase
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
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveTest(string stsType)
        {
            AdalTests.AcquireTokenPositive(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken with Refresh Token")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveByRefreshTokenTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveByRefreshTokenTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for autority validation to AuthenticationContext")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AuthenticationContextAuthorityValidationTest(string stsType)
        {
            AdalTests.AuthenticationContextAuthorityValidationTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid authority")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithInvalidAuthorityTest(string stsType)
        {
            AdalTests.AcquireTokenWithInvalidAuthority(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid resource")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithInvalidResourceTest(string stsType)
        {
            AdalTests.AcquireTokenWithInvalidResource(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid client id")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithInvalidClientIdTest(string stsType)
        {
            AdalTests.AcquireTokenWithInvalidClientId(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with incorrect user credential")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithIncorrectUserCredentialTest(string stsType)
        {
            AdalTests.AcquireTokenWithIncorrectUserCredentialTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void ExtraQueryParametersTest(string stsType)
        {
            AdalTests.ExtraQueryParametersTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with user canceling authentication")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenWithAuthenticationCanceledTest(string stsType)
        {
            // ADFS security dialog hang up
            AdalTests.AcquireTokenWithAuthenticationCanceledTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenPositiveWithDefaultCacheTest(string stsType)
        {
            AdalTests.AcquireTokenPositiveWithDefaultCacheTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing custom in memory token cache")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenPositiveWithInMemoryCacheTest(string stsType)
        {
            AdalTests.AcquireTokenPositiveWithInMemoryCache(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void AcquireTokenPositiveWithNullCacheTest(string stsType)
        {
            AdalTests.AcquireTokenPositiveWithNullCache(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for UserInfo")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public void UserInfoTest(string stsType)
        {
            AdalTests.UserInfoTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for multi resource refresh token")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task MultiResourceRefreshTokenTest(string stsType)
        {
            await AdalTests.MultiResourceRefreshTokenTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for acquring token using tenantless endpoint")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        public void TenantlessTest(string stsType)
        {
            AdalTests.TenantlessTest(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for STS Instance Discovery")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        public async Task InstanceDiscoveryTest(string stsType)
        {
            await AdalTests.InstanceDiscoveryTestAsync(SetupStsService(GetStsType(stsType)));
        }

        private static void SetReplayerNetworkPlugin()
        {
            NetworkPlugin.WebUIFactory = new ReplayerWebUIFactory();
            NetworkPlugin.HttpWebRequestFactory = new ReplayerHttpWebRequestFactory();
            NetworkPlugin.RequestCreationHelper = new ReplayerRequestCreationHelper();
        }
    }
}
