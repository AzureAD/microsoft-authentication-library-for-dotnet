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
            PlatformPlugin.PlatformInformation = new TestPlatformInformation();
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

            AdalTests.AuthorizationParameters = new AuthorizationParameters();
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveTestAsync(SetupStsService(GetStsType(stsType)));
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
        public async Task AuthenticationContextAuthorityValidationTest(string stsType)
        {
            await AdalTests.AuthenticationContextAuthorityValidationTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid authority")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenWithInvalidAuthorityTest(string stsType)
        {
            await AdalTests.AcquireTokenWithInvalidAuthorityTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid resource")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenWithInvalidResourceTest(string stsType)
        {
            await AdalTests.AcquireTokenWithInvalidResourceTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with invalid client id")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenWithInvalidClientIdTest(string stsType)
        {
            await AdalTests.AcquireTokenWithInvalidClientIdTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with incorrect user credential")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenWithIncorrectUserCredentialTest(string stsType)
        {
            await AdalTests.AcquireTokenWithIncorrectUserCredentialTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task ExtraQueryParametersTest(string stsType)
        {
            await AdalTests.ExtraQueryParametersTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Negative Test for AcquireToken with user canceling authentication")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenWithAuthenticationCanceledTest(string stsType)
        {
            // ADFS security dialog hang up
            await AdalTests.AcquireTokenWithAuthenticationCanceledTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveWithDefaultCacheTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveWithDefaultCacheTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing custom in memory token cache")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task AcquireTokenPositiveWithInMemoryCacheTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveWithInMemoryCacheTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        [Ignore]    // Enable once the test bug is fixed.
        public async Task AcquireTokenPositiveWithNullCacheTest(string stsType)
        {
            await AdalTests.AcquireTokenPositiveWithNullCacheTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for UserInfo")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        [DataRow("ADFS")]
        public async Task UserInfoTest(string stsType)
        {
            await AdalTests.UserInfoTestAsync(SetupStsService(GetStsType(stsType)));
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
        public async Task TenantlessTest(string stsType)
        {
            await AdalTests.TenantlessTestAsync(SetupStsService(GetStsType(stsType)));
        }

        [TestMethod]
        //[Description("Test for STS Instance Discovery")]
        [TestCategory("AdalWinPhoneMock")]
        [DataRow("AAD")]
        public async Task InstanceDiscoveryTest(string stsType)
        {
            await AdalTests.InstanceDiscoveryTestAsync(SetupStsService(GetStsType(stsType)));
        }

        private static void SetReplayerNetworkPlugin()
        {
            PlatformPlugin.WebUIFactory = new ReplayerWebUIFactory();
            PlatformPlugin.HttpWebRequestFactory = new ReplayerHttpWebRequestFactory();
            PlatformPlugin.RequestCreationHelper = new ReplayerRequestCreationHelper();
        }

        class TestPlatformInformation : PlatformInformation
        {
            public override void AddPromptBehaviorQueryParameter(IAuthorizationParameters parameters, RequestParameters authorizationRequestParameters)
            {
                // Do not add prompt=login to the query to be able to use the mock dictionary created by Test.ADAL.NET.
            }            
        }
    }
}
