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
