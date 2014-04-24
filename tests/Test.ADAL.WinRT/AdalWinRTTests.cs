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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;

namespace Test.ADAL.WinRT
{
    [TestClass]
    [DeploymentItem("TestMetadata.xml")]
    public class AdalWinRTTests : AdalTestsBase
    {
        [AssemblyInitialize]
        public static void AssemlyInitialize(TestContext testContext)
        {
            AdalTests.TestType = TestType.WinRT;
        }

        [TestInitialize]
        public void TestMethodSetup()
        {
            AdalTests.InitializeTest();
            StsType stsType = this.GetStsTypeFromContext();
            Sts = SetupStsService(stsType);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveTest()
        {
            AdalTests.AcquireTokenPositive(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken with missing redirectUri and/or userId")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithoutRedirectUriOrUserIdAsync()
        {
            AdalTests.AcquireTokenPositiveWithoutRedirectUriOrUserId(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken with Refresh Token")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenPositiveByRefreshTokenTest()
        {
            await AdalTests.AcquireTokenPositiveByRefreshTokenTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Test for autority validation to AuthenticationContext")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AuthenticationContextAuthorityValidationTest()
        {
            AdalTests.AuthenticationContextAuthorityValidationTest(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Test for AcquireToken with redirectUri")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithRedirectUriTest()
        {
            AdalTests.AcquireTokenWithRedirectUriTest(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Negative Test for AcquireToken with invalid authority")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithInvalidAuthorityTest()
        {
            AdalTests.AcquireTokenWithInvalidAuthority(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Negative Test for AcquireToken with invalid resource")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithInvalidResourceTest()
        {
            AdalTests.AcquireTokenWithInvalidResource(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Negative Test for AcquireToken with invalid client id")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithInvalidClientIdTest()
        {
            AdalTests.AcquireTokenWithInvalidClientId(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Negative Test for AcquireToken with incorrect user credential")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithIncorrectUserCredentialTest()
        {
            AdalTests.AcquireTokenWithIncorrectUserCredentialTest(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void ExtraQueryParametersTest()
        {
            AdalTests.ExtraQueryParametersTest(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Negative Test for AcquireToken with user canceling authentication")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithAuthenticationCanceledTest()
        {
            // ADFS security dialog hang up
            AdalTests.AcquireTokenWithAuthenticationCanceledTest(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken testing default token cache")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithDefaultCacheTest()
        {
            AdalTests.AcquireTokenPositiveWithDefaultCacheTest(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken testing custom in memory token cache")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithInMemoryCacheTest()
        {
            AdalTests.AcquireTokenPositiveWithInMemoryCache(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken testing default token cache")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithNullCacheTest()
        {
            AdalTests.AcquireTokenPositiveWithNullCache(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken testing custom short lived token cache")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        [Ignore]    // This test does not work with the new feature of refreshing near expiry tokens and need to be either removed of updated.
        public void AcquireTokenPositiveWithShortLivedCacheTest()
        {
            AdalTests.AcquireTokenPositiveWithShortLivedCache(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Test for UserInfo")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void UserInfoTest()
        {
            AdalTests.UserInfoTest(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Test for multi resource refresh token")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public async Task MultiResourceRefreshTokenTest()
        {
            await AdalTests.MultiResourceRefreshTokenTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Test for acquring token using tenantless endpoint")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public void TenantlessTest()
        {
            AdalTests.TenantlessTest(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Test for STS Instance Discovery")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public async Task InstanceDiscoveryTest()
        {
            await AdalTests.InstanceDiscoveryTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Test for Force Prompt")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void ForcePromptTest()
        {
            AdalTests.ForcePromptTest(Sts);
        }

// Disabled Non-Interactive Feature
#if false
        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken non-interactive")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AADFederatedWithADFS3", DataAccessMethod.Sequential)]
        public async Task AcquireTokenNonInteractivePositiveTest()
        {
            await AdalTests.AcquireTokenNonInteractivePositiveTestAsync(Sts);
        }
#endif

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken using federated tenant")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AADFederatedWithADFS3", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithFederatedTenantTest()
        {
            AdalTests.AcquireTokenPositiveWithFederatedTenant(Sts, false);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Correlation Id test")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public async Task CorrelationIdTest()
        {
            await AdalTests.CorrelationIdTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for discovery of authentication parameters")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public async Task AuthenticationParametersDiscoveryTest()
        {
            await AdalTests.AuthenticationParametersDiscoveryTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Test SSO Mode")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public void SSOModeTest()
        {
            AuthenticationContextProxy.SetCredentials(null, Sts.ValidPassword);
            var context = new AuthenticationContextProxy(Sts.Authority, Sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(Sts.ValidResource, Sts.ValidClientId, Sts.ValidDefaultRedirectUri, Sts.ValidUserId);
            AdalTests.VerifySuccessResult(Sts, result);

            AuthenticationContextProxy.ClearDefaultCache();

            AuthenticationContextProxy.SetCredentials(Sts.ValidUserId, Sts.ValidPassword);
            result = context.AcquireToken(Sts.ValidResource, Sts.ValidClientId);
            AdalTests.VerifySuccessResult(Sts, result);

            AuthenticationContextProxy.ClearDefaultCache();

            result = context.AcquireToken(Sts.ValidResource, Sts.ValidClientId, (Uri)null);
            AdalTests.VerifySuccessResult(Sts, result);

            AuthenticationContextProxy.ClearDefaultCache();

            result = context.AcquireToken(Sts.ValidResource, Sts.ValidClientId,
                new Uri("ms-app://s-1-15-2-2097830667-3131301884-2920402518-3338703368-1480782779-4157212157-3811015497/"));
            AdalTests.VerifyErrorResult(result, Sts.InvalidArgumentError, "return URI");
        }
    }
}
