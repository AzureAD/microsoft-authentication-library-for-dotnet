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

            AdalTests.PlatformParameters = new PlatformParameters(PromptBehavior.Auto, false);
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for autority validation to AuthenticationContext")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AuthenticationContextAuthorityValidationTest(string stsType)
        {
            await AdalTests.AuthenticationContextAuthorityValidationTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid authority")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenWithInvalidAuthorityTest(string stsType)
        {
            await AdalTests.AcquireTokenWithInvalidAuthorityTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid resource")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenWithInvalidResourceTest(string stsType)
        {
            await AdalTests.AcquireTokenWithInvalidResourceTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid client id")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenWithInvalidClientIdTest(string stsType)
        {
            await AdalTests.AcquireTokenWithInvalidClientIdTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with incorrect user credential")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenWithIncorrectUserCredentialTest(string stsType)
        {
            await AdalTests.AcquireTokenWithIncorrectUserCredentialTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task ExtraQueryParametersTest(string stsType)
        {
            await AdalTests.ExtraQueryParametersTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with user canceling authentication")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenWithAuthenticationCanceledTest(string stsType)
        {
            // ADFS security dialog hang up
            await AdalTests.AcquireTokenWithAuthenticationCanceledTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveWithDefaultCacheTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveWithDefaultCacheTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing custom in memory token cache")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveWithInMemoryCacheTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveWithInMemoryCacheTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        [Ignore]    // Enable once the test bug is fixed.
        public async Task AcquireTokenPositiveWithNullCacheTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveWithNullCacheTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for UserInfo")]
        [TestCategory("AdalWinRTMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task UserInfoTest(string stsType)
        {
            await AdalTests.UserInfoTestAsync(SetupStsService(GetStsType(stsType)));
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
        public async Task TenantlessTest(string stsType)
        {
            await AdalTests.TenantlessTestAsync(SetupStsService(GetStsType(stsType)));
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
            PlatformPlugin.WebUIFactory = new ReplayerWebUIFactory();
            PlatformPlugin.HttpClientFactory = new ReplayerHttpClientFactory();
        }
    }
}
