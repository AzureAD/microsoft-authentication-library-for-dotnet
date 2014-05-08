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
        [Description("Positive Test for AcquireToken with missing redirectUri and/or userId")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithoutRedirectUriOrUserIdAsync()
        {
            AdalTests.AcquireTokenPositiveWithoutRedirectUriOrUserId(Sts);
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
        [Description("Test for Force Prompt")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void ForcePromptTest()
        {
            AdalTests.ForcePromptTest(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken non-interactive")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AADFederatedWithADFS3", DataAccessMethod.Sequential)]
        public async Task AcquireTokenNonInteractivePositiveTest()
        {
            await AdalTests.AcquireTokenNonInteractivePositiveTestAsync(Sts);
        }

        [TestMethod]
        [TestCategory("AdalWinRT")]
        [Description("Positive Test for AcquireToken using federated tenant")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "AADFederatedWithADFS3", DataAccessMethod.Sequential)]
        public void AcquireTokenPositiveWithFederatedTenantTest()
        {
            AdalTests.AcquireTokenPositiveWithFederatedTenant(Sts);
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
        [TestCategory("AdalWinRTDomainJoined")]
        [Description("Negative Test for AcquireToken with PromptBehavior.Never")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"TestMetadata.xml", "Sts", DataAccessMethod.Sequential)]
        public void AcquireTokenWithPromptBehaviorNeverTestAsync()
        {
            // TODO: Not fully working at this point due to session cookies being deleted between WAB calls.

            Sts sts = Sts;

            // Should not be able to get a token silently passing redirectUri.
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Never);
            AdalTests.VerifyErrorResult(result, Sts.InvalidArgumentError, "SSO");

            AuthenticationContextProxy.SetCredentials(sts.ValidUserId, sts.ValidPassword);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri);
            AdalTests.VerifySuccessResult(sts, result);

            AuthenticationContextProxy.ClearDefaultCache();
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri);
            AdalTests.VerifySuccessResult(sts, result);

            // Should not be able to get a token silently on first try.
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, PromptBehaviorProxy.Never);
            AdalTests.VerifyErrorResult(result, Sts.UserInteractionRequired, null);

            AuthenticationContextProxy.SetCredentials(sts.ValidUserId, sts.ValidPassword);
            // Obtain a token interactively.
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId);
            AdalTests.VerifySuccessResult(sts, result);

            // Obtain a token interactively.
            AuthenticationContextProxy.ClearDefaultCache();
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId);
            AdalTests.VerifySuccessResult(sts, result);

            AuthenticationContextProxy.SetCredentials(null, null);
            // Now there should be a token available in the cache so token should be available silently.
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, PromptBehaviorProxy.Never);
            AdalTests.VerifySuccessResult(sts, result);

            // Clear the cache and silent auth should work via session cookies.
            AuthenticationContextProxy.ClearDefaultCache();
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, PromptBehaviorProxy.Never);
            AdalTests.VerifySuccessResult(sts, result);

            // Clear the cache and cookies and silent auth should fail.
            AuthenticationContextProxy.ClearDefaultCache();
            AdalTests.EndBrowserDialogSession();
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, PromptBehaviorProxy.Never);
            AdalTests.VerifyErrorResult(result, Sts.UserInteractionRequired, null);                
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
