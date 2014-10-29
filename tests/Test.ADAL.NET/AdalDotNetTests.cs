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
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;
using Test.ADAL.NET.Friend;

namespace Test.ADAL.NET
{
    [TestClass]
    [DeploymentItem("TestMetadata.xml")]
    [DeploymentItem("recorded_data.dat")]
    [DeploymentItem("valid_cert.pfx")]
    [DeploymentItem("invalid_cert.pfx")]
    [DeploymentItem("Microsoft.Owin.Host.HttpListener.dll")]
    public class AdalDotNetTests : AdalTestsBase
    {
        [AssemblyInitialize]
        public static void AssemlyInitialize(TestContext testContext)
        {
            AdalTests.TestType = TestType.DotNet;

            // To record request/response with actual service, switch mode to Record
            RecorderSettings.Mode = RecorderMode.Replay;
            ConsoleTraceListener myWriter = new ConsoleTraceListener();
            Trace.Listeners.Add(myWriter);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            if (RecorderSettings.Mode == RecorderMode.Record)
            {
                RecorderSettings.WriteRecordersToFile();
            }
        }

        [TestInitialize]
        public void TestMethodSetup()
        {
            AdalTests.InitializeTest();
            StsType stsType = this.GetStsTypeFromContext();
            Sts = SetupStsService(stsType);

            RecorderSettings.SetMockModeByTestContext(TestContext);
            AdalTests.EndBrowserDialogSession();

            AdalTests.AuthorizationParameters = new AuthorizationParameters(PromptBehavior.Auto, null);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public async Task SmokeTest()
        {
            await AdalTests.AcquireTokenPositiveTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenPositiveTest()
        {
            await AdalTests.AcquireTokenPositiveTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Positive Test for AcquireToken with missing redirectUri and/or userId")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenPositiveWithoutRedirectUriOrUserIdAsync()
        {
            await AdalTests.AcquireTokenPositiveWithoutRedirectUriOrUserIdTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken by Refresh Token")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenPositiveByRefreshTokenTest()
        {
            await AdalTests.AcquireTokenPositiveByRefreshTokenTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for autority validation to AuthenticationContext")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AuthenticationContextAuthorityValidationTest()
        {
            await AdalTests.AuthenticationContextAuthorityValidationTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for AcquireToken with redirectUri")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenWithRedirectUriTest()
        {
            await AdalTests.AcquireTokenWithRedirectUriTestAsync(Sts);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid authority")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenWithInvalidAuthorityTest()
        {
            await AdalTests.AcquireTokenWithInvalidAuthorityTestAsync(Sts);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid resource")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenWithInvalidResourceTest()
        {
            await AdalTests.AcquireTokenWithInvalidResourceTestAsync(Sts);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid client id")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenWithInvalidClientIdTest()
        {
            await AdalTests.AcquireTokenWithInvalidClientIdTestAsync(Sts);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with incorrect user credential")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenWithIncorrectUserCredentialTest()
        {
            await AdalTests.AcquireTokenWithIncorrectUserCredentialTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task ExtraQueryParametersTest()
        {
            await AdalTests.ExtraQueryParametersTestAsync(Sts);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with user canceling authentication")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenWithAuthenticationCanceledTest()
        {
            // ADFS security dialog hang up
            await AdalTests.AcquireTokenWithAuthenticationCanceledTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenPositiveWithDefaultCacheTest()
        {
            await AdalTests.AcquireTokenPositiveWithDefaultCacheTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing custom in memory token cache")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenPositiveWithInMemoryCacheTest()
        {
            await AdalTests.AcquireTokenPositiveWithInMemoryCacheTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        [Ignore]    // Enable once the test bug is fixed.
        public async Task AcquireTokenPositiveWithNullCacheTest()
        {
            await AdalTests.AcquireTokenPositiveWithNullCacheTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for UserInfo")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task UserInfoTest()
        {
            await AdalTests.UserInfoTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for multi resource refresh token")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task MultiResourceRefreshTokenTest()
        {
            await AdalTests.MultiResourceRefreshTokenTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for acquring token using tenantless endpoint")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task TenantlessTest()
        {
            await AdalTests.TenantlessTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for STS Instance Discovery")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task InstanceDiscoveryTest()
        {
            await AdalTests.InstanceDiscoveryTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Test for Force Prompt")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public async Task ForcePromptTest()
        {
            await AdalTests.ForcePromptTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken non-interactive for managed user")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task AcquireTokenNonInteractiveManagedPositiveTest()
        {
            await AdalTests.AcquireTokenNonInteractivePositiveTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken non-interactive")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AADFederatedWithADFS3", DataAccessMethod.Sequential)]
        public async Task AcquireTokenNonInteractiveFederatedPositiveTest()
        {
            await AdalTests.AcquireTokenNonInteractivePositiveTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken using federated tenant and then refreshing the session")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AADFederatedWithADFS3", DataAccessMethod.Sequential)]
        public async Task AcquireTokenAndRefreshSessionTest()
        {
            await AdalTests.AcquireTokenAndRefreshSessionTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken using federated tenant")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AADFederatedWithADFS3", DataAccessMethod.Sequential)]
        public async Task AcquireTokenPositiveWithFederatedTenantTest()
        {
            await AdalTests.AcquireTokenPositiveWithFederatedTenantTest(Sts);
        }


        [TestMethod]
        [Description("Correlation Id test")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public async Task CorrelationIdTest()
        {
            await AdalTests.CorrelationIdTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for discovery of authentication parameters")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task AuthenticationParametersDiscoveryTest()
        {
            await AdalTests.AuthenticationParametersDiscoveryTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for reading WebException as inner exception")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task WebExceptionAccessTest()
        {
            await AdalTests.WebExceptionAccessTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for Confidential Client")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task ConfidentialClientWithX509Test()
        {
            await AdalTests.ConfidentialClientWithX509TestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for Client credential")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task ClientCredentialTestAsync()
        {
            await AdalTests.ClientCredentialTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for Client assertion with X509")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task ClientAssertionWithX509Test()
        {
            await AdalTests.ClientAssertionWithX509TestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for Confidential Client with self signed jwt")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task ConfidentialClientWithJwtTest()
        {
            await AdalTests.ConfidentialClientWithJwtTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for Client assertion with self signed Jwt")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task ClientAssertionWithSelfSignedJwtTest()
        {
            await AdalTests.ClientAssertionWithSelfSignedJwtTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for Confidential Client")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task ConfidentialClientTest()
        {
            await AdalTests.ConfidentialClientTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Negative Test for AcquireToken with PromptBehavior.Never")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenWithPromptBehaviorNeverTestAsync()
        {
            await AdalTests.AcquireTokenWithPromptBehaviorNeverTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client credential")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task AcquireTokenOnBehalfAndClientCredentialTest()
        {
            await AdalTests.AcquireTokenOnBehalfAndClientCredentialTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client certificate")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task AcquireTokenOnBehalfAndClientCertificateTest()
        {
            await AdalTests.AcquireTokenOnBehalfAndClientCertificateTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client assertion")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task AcquireTokenOnBehalfAndClientAssertionTest()
        {
            await AdalTests.AcquireTokenOnBehalfAndClientAssertionTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken from cache only")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task AcquireTokenFromCacheTest()
        {
            await AdalTests.AcquireTokenFromCacheTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Test for cache in multi user scenario")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public async Task MultiUserCacheTest()
        {
            await AdalTests.MultiUserCacheTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Test for switching user in multi user scenario")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public async Task SwitchUserTest()
        {
            await AdalTests.SwitchUserTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for cache expiration margin")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task CacheExpirationMarginTest()
        {
            await AdalTests.CacheExpirationMarginTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for client assertion in multi threaded scenario")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public async Task MultiThreadedClientAssertionWithX509Test()
        {
            await AdalTests.MultiThreadedClientAssertionWithX509Test(Sts);
        }

        [TestMethod]
        [Description("Test for token cache usage in AcquireTokenByAuthorizationCode")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task AcquireTokenByAuthorizationCodeWithCacheTest()
        {
            await AdalTests.AcquireTokenByAuthorizationCodeWithCacheTest(Sts);
        }

        [TestMethod]
        [Description("Test for token refresh for confidnetial client using Multi Resource Refresh Token (MRRT) in cache")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task ConfidentialClientTokenRefreshWithMrrtTest()
        {
            await AdalTests.ConfidentialClientTokenRefreshWithMRRTTest(Sts);
        }

        [TestMethod]
        [Description("Test for different token subject types (Client, User, ClientPlusUser)")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task TokenSubjectTypeTest()
        {
            await AdalTests.TokenSubjectTypeTest(Sts);
        }

        [TestMethod]
        [Description("Test for GetAuthorizationRequestURL")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task GetAuthorizationRequestUrlTest()
        {
            await AdalTests.GetAuthorizationRequestURLTestAsync(Sts);
        }
    }
}
