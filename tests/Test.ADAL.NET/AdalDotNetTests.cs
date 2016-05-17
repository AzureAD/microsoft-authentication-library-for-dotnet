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

            AdalTests.PlatformParameters = new PlatformParameters(PromptBehavior.Auto, null);
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
        [Description("Negative Test for non https redirect")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public async Task NonHttpsRedirectTest()
        {
            await AdalTests.NonHttpsURLNegativeTest(Sts);
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
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
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
            await AdalTests.AcquireTokenPositiveWithFederatedTenantTestAsync(Sts);
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
        [Description("Test for reading WebException as inner exception")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task InnerExceptionAccessTest()
        {
            await AdalTests.InnerExceptionAccessTestAsync(Sts);
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
            await AdalTests.MultiThreadedClientAssertionWithX509TestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for token cache usage in AcquireTokenByAuthorizationCode")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task AcquireTokenByAuthorizationCodeWithCacheTest()
        {
            await AdalTests.AcquireTokenByAuthorizationCodeWithCacheTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for token refresh for confidnetial client using Multi Resource Refresh Token (MRRT) in cache")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task ConfidentialClientTokenRefreshWithMrrtTest()
        {
            await AdalTests.ConfidentialClientTokenRefreshWithMRRTTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for different token subject types (Client, User, ClientPlusUser)")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task TokenSubjectTypeTest()
        {
            await AdalTests.TokenSubjectTypeTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for GetAuthorizationRequestURL")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockSts", DataAccessMethod.Sequential)]
        public async Task GetAuthorizationRequestUrlTest()
        {
            await AdalTests.GetAuthorizationRequestURLTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for logging in ADAL")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task LoggerTest()
        {
            await AdalTests.LoggerTestAsync(Sts);
        }

        [TestMethod]
        [Description("Test for non-interactive federation with MSA")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AAD", DataAccessMethod.Sequential)]
        public async Task MsaTest()
        {
            await AdalTests.MsaTestAsync();
        }

        [TestMethod]
        [Description("Test for mixed case username and cache")]
        [TestCategory("AdalDotNetMock")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "MockAAD", DataAccessMethod.Sequential)]
        public async Task MixedCaseUserNameTest()
        {
            await AdalTests.MixedCaseUserNameTestAsync(Sts);
        }
        
        [TestMethod]
        [Description("Positive Test for AcquireToken with valid user credentials")]
        [TestCategory("AdalDotNet")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "ResourceOwnerCredentials", DataAccessMethod.Sequential)]
        public async Task ResourceOwnerCredentialsTest()
        {
            await AdalTests.ResourceOwnerCredentialsTestAsync(Sts);
        }
    }
}
