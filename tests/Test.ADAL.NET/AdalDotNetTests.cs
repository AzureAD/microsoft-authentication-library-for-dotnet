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
        public Exception TestException { get; set; }

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
            try
            {
                AuthenticationContextProxy.CallSync = bool.Parse((string)TestContext.DataRow["CallSync"]);
            }
            catch (ArgumentException)
            {
                AuthenticationContextProxy.CallSync = false;
            }

            Sts = SetupStsService(stsType);

            RecorderSettings.SetMockModeByTestContext(TestContext);
            AdalTests.EndBrowserDialogSession();
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void SmokeTest()
        {
            AdalTests.AcquireTokenPositive(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveTest()
        {
            AdalTests.AcquireTokenPositive(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Positive Test for AcquireToken with missing redirectUri and/or userId")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithoutRedirectUriOrUserIdAsync()
        {
            AdalTests.AcquireTokenPositiveWithoutRedirectUriOrUserId(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken by Refresh Token")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockStsSyncAsync", DataAccessMethod.Sequential)]
        public async Task AcquireTokenPositiveByRefreshTokenTest()
        {
            await AdalTests.AcquireTokenPositiveByRefreshTokenTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for autority validation to AuthenticationContext")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void AuthenticationContextAuthorityValidationTest()
        {
            AdalTests.AuthenticationContextAuthorityValidationTest(Sts);
        }

        [TestMethod]
        [Description("Test for AcquireToken with redirectUri")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithRedirectUriTest()
        {
            AdalTests.AcquireTokenWithRedirectUriTest(Sts);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid authority")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithInvalidAuthorityTest()
        {
            AdalTests.AcquireTokenWithInvalidAuthority(Sts);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid resource")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithInvalidResourceTest()
        {
            AdalTests.AcquireTokenWithInvalidResource(Sts);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid client id")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithInvalidClientIdTest()
        {
            AdalTests.AcquireTokenWithInvalidClientId(Sts);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with incorrect user credential")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithIncorrectUserCredentialTest()
        {
            AdalTests.AcquireTokenWithIncorrectUserCredentialTest(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void ExtraQueryParametersTest()
        {
            AdalTests.ExtraQueryParametersTest(Sts);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with user canceling authentication")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithAuthenticationCanceledTest()
        {
            // ADFS security dialog hang up
            AdalTests.AcquireTokenWithAuthenticationCanceledTest(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithDefaultCacheTest()
        {
            AdalTests.AcquireTokenPositiveWithDefaultCacheTest(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing custom in memory token cache")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithInMemoryCacheTest()
        {
            AdalTests.AcquireTokenPositiveWithInMemoryCache(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing default token cache")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        [Ignore]    // Enable once the test bug is fixed.
        public void AcquireTokenPositiveWithNullCacheTest()
        {
            AdalTests.AcquireTokenPositiveWithNullCache(Sts);
        }

        [TestMethod]
        [Description("Test for UserInfo")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void UserInfoTest()
        {
            AdalTests.UserInfoTest(Sts);
        }

        [TestMethod]
        [Description("Test for multi resource refresh token")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockStsSyncAsync", DataAccessMethod.Sequential)]
        public async Task MultiResourceRefreshTokenTest()
        {
            await AdalTests.MultiResourceRefreshTokenTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for acquring token using tenantless endpoint")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public void TenantlessTest()
        {
            AdalTests.TenantlessTest(Sts);
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
        public void ForcePromptTest()
        {
            AdalTests.ForcePromptTest(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken non-interactive for managed user")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAADSyncAsync", DataAccessMethod.Sequential)]
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
        public void AcquireTokenAndRefreshSessionTest()
        {
            AdalTests.AcquireTokenAndRefreshSession(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken using federated tenant")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AADFederatedWithADFS3", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithFederatedTenantTest()
        {
            AdalTests.AcquireTokenPositiveWithFederatedTenant(Sts);
        }


        [TestMethod]
        [Description("Correlation Id test")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "StsSyncAsync", DataAccessMethod.Sequential)]
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
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockStsSyncAsync", DataAccessMethod.Sequential)]
        public async Task ConfidentialClientWithX509Test()
        {
            await AdalTests.ConfidentialClientWithX509TestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for Client credential")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockStsSyncAsync", DataAccessMethod.Sequential)]
        public async Task ClientCredentialTestAsync()
        {
            await AdalTests.ClientCredentialTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for Client assertion with X509")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockStsSyncAsync", DataAccessMethod.Sequential)]
        public async Task ClientAssertionWithX509Test()
        {
            await AdalTests.ClientAssertionWithX509TestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for Confidential Client with self signed jwt")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task ConfidentialClientWithJwtTest()
        {
            await AdalTests.ConfidentialClientWithJwtTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for Client assertion with self signed Jwt")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockStsSyncAsync", DataAccessMethod.Sequential)]
        public async Task ClientAssertionWithSelfSignedJwtTest()
        {
            await AdalTests.ClientAssertionWithSelfSignedJwtTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for Confidential Client")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockStsSyncAsync", DataAccessMethod.Sequential)]
        public async Task ConfidentialClientTest()
        {
            await AdalTests.ConfidentialClientTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Negative Test for AcquireToken with PromptBehavior.Never")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithPromptBehaviorNeverTestAsync()
        {
            AdalTests.AcquireTokenWithPromptBehaviorNeverTest(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client credential")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAADSyncAsync", DataAccessMethod.Sequential)]
        public async Task AcquireTokenOnBehalfAndClientCredentialTest()
        {
            await AdalTests.AcquireTokenOnBehalfAndClientCredentialTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client certificate")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAADSyncAsync", DataAccessMethod.Sequential)]
        public async Task AcquireTokenOnBehalfAndClientCertificateTest()
        {
            await AdalTests.AcquireTokenOnBehalfAndClientCertificateTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client assertion")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAADSyncAsync", DataAccessMethod.Sequential)]
        public async Task AcquireTokenOnBehalfAndClientAssertionTest()
        {
            await AdalTests.AcquireTokenOnBehalfAndClientAssertionTestAsync(Sts);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken from cache only")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockStsSyncAsync", DataAccessMethod.Sequential)]
        public async Task AcquireTokenFromCacheTest()
        {
            await AdalTests.AcquireTokenFromCacheTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Test for cache in multi user scenario")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public void MultiUserCacheTest()
        {
            AdalTests.MultiUserCacheTest(Sts);
        }

        [TestMethod]
        [TestCategory("AdalDotNet")]
        [Description("Test for switching user in multi user scenario")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public void SwitchUserTest()
        {
            AdalTests.SwitchUserTest(Sts);
        }

        [TestMethod]
        [Description("Test for cache expiration margin")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public void CacheExpirationMarginTest()
        {
            AdalTests.CacheExpirationMarginTest(Sts);
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
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAADSyncAsync", DataAccessMethod.Sequential)]
        public async Task AcquireTokenByAuthorizationCodeWithCacheTest()
        {
            await AdalTests.AcquireTokenByAuthorizationCodeWithCacheTest(Sts);
        }
    }
}
