//------------------------------------------------------------------------------
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

using Microsoft.Identity.Test.LabInfrastructure;
using NUnit.Framework;
using Microsoft.Identity.Test.UIAutomation.Infrastructure;
using Xamarin.UITest;
using Xamarin.UITest.Queries;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

//NOTICE! Inorder to run UI automation tests for xamarin locally, you may need to upgrade nunit to 3.0 and above for this project and the core ui Automation project.
//It is set to 2.6.4 because that is the maximum version that appcenter can support.
//There is an error in visual studio that can prevent the NUnit test framework from loading the test dll properly.
//Remember to return the version back to 2.6.4 before commiting to prevent appcenter from failing

namespace Test.Microsoft.Identity.UIAutomation
{
    /// <summary>
    /// Configures environment for core/iOS tests to run
    /// </summary>
    [TestFixture(Platform.iOS)]
    public class IOSMsalTests
    {
        IApp app;
        Platform platform;
        ITestController xamarinController = new IOSXamarinUiTestController();
        MobileTestHelper _mobileTestHelper;

        /// <summary>
        /// Initializes Xamarin UI tests
        /// </summary>
        /// <param name="platform">The platform where the tests will be performed</param>
        public IOSMsalTests(Platform platform)
        {
            this.platform = platform;
        }

        /// <summary>
        /// Initializes app and test controller before each test
        /// </summary>
        [SetUp]
        public void InitializeBeforeTest()
        {
            app = AppFactory.StartApp(platform, "XForms.iOS");
            xamarinController.Application = app;
            _mobileTestHelper = new MobileTestHelper(platform);
        }

        /// <summary>
        /// Test runner to run all tests, as test initialization is expensive.
        /// </summary>
        [Test]
        public void RunAllTests()
        {
            var tests = new List<Action>()
            {
                AcquireTokenTest,
                AcquireTokenSilentTest,
                AcquireTokenADFSV3InteractiveFederatedTest,
                AcquireTokenADFSV3InteractiveNonFederatedTest,
                AcquireTokenADFSV4InteractiveFederatedTest,
                AcquireTokenADFSV4InteractiveNonFederatedTest,

                //B2CFacebookB2CLoginAuthorityAcquireTokenTest,
                //B2CFacebookMicrosoftAuthorityAcquireTokenTest,
                //B2CGoogleB2CLoginAuthorityAcquireTokenTest,
                //B2CGoogleMicrosoftAuthorityAcquireTokenTest,
                //B2CLocalAccountAcquireTokenTest,
                //B2CFacebookEditPolicyAcquireTokenTest
            };

            var hasFailed = false;
            var stringBuilderMessage = new StringBuilder();

            foreach (Action test in tests)
            {
                try
                {
                    LogMessage($"Running test: {test.Method.Name}", stringBuilderMessage);
                    test();
                }
                catch (Exception ex)
                {
                    LogMessage($"Fail: {test.Method.Name}, Error: {ex.Message}", stringBuilderMessage);
                    hasFailed = true;
                }
                finally
                {
                    LogMessage($"Complete test: {test.Method.Name}", stringBuilderMessage);
                }
            }

            Assert.IsFalse(hasFailed, $"Test Failed. {stringBuilderMessage}");
        }

        private static void LogMessage(string message, StringBuilder stringBuilderMessage)
        {
            Console.WriteLine(message);
            stringBuilderMessage.AppendLine(message);
        }

        /// <summary>
        /// Runs through the standard acquire token flow, using the default app configured UiBehavior = Login
        /// </summary>
        public void AcquireTokenTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(xamarinController, LabUserHelper.GetDefaultUser());
        }

        /// <summary>
        /// Runs through the standard acquire token silent flow
        /// </summary>
        public void AcquireTokenSilentTest()
        {
            _mobileTestHelper.AcquireTokenSilentTestHelper(xamarinController, LabUserHelper.GetDefaultUser());
        }

        /// <summary>
        /// Runs through the standard acquire token flow
        /// </summary>
        [Ignore("Current web element search implementation is unable to properly wait for select account elements on login page. Will be addressed in future updates.")]
        public void PromptBehaviorConsentSelectAccount()
        {
            var labResponse = LabUserHelper.GetDefaultUser();

            _mobileTestHelper.PromptBehaviorTestHelperWithConsent(xamarinController, labResponse);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// b2clogin.com authority
        /// with subsequent silent call
        /// </summary>
        [Ignore("Facebook updated to Graph v3 and app center tests are failing. Ignoring for the moment.")]
        public void B2CFacebookB2CLoginAuthorityAcquireTokenTest()
        {
            _mobileTestHelper.B2CFacebookAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CFacebookAccount(), true);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider 
        /// login.microsoftonline.com authority
        /// with subsequent silent call
        /// </summary>
        [Ignore("Facebook updated to Graph v3 and app center tests are failing. Ignoring for the moment.")]
        public void B2CFacebookMicrosoftAuthorityAcquireTokenTest()
        {
            _mobileTestHelper.PerformB2CSelectProviderOnlyFlow(xamarinController, LabUserHelper.GetB2CFacebookAccount().User, B2CIdentityProvider.Facebook, false);
            _mobileTestHelper.B2CSilentFlowHelper(xamarinController);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// b2clogin.com authority
        /// call to edit profile authority with
        ///  UIBehavior none
        /// </summary>
        [Ignore("Facebook updated to Graph v3 and app center tests are failing. Ignoring for the moment.")]
        public void B2CFacebookEditPolicyAcquireTokenTest()
        {
            _mobileTestHelper.PerformB2CSelectProviderOnlyFlow(xamarinController, LabUserHelper.GetB2CFacebookAccount().User, B2CIdentityProvider.Facebook, true);
            _mobileTestHelper.B2CSilentFlowHelper(xamarinController);
            _mobileTestHelper.B2CFacebookEditPolicyAcquireTokenInteractiveTestHelper(xamarinController);
        }

        /// <summary>
        /// B2C acquire token with Google provider
        /// b2clogin.com authority
        /// with subsequent silent call
        /// </summary>
        [Ignore("Google Auth does not support embedded webview from b2clogin.com authority. " +
            "App Center cannot run system browser tests yet, so this test can only be run in " +
            "system browser locally.")]
        public void B2CGoogleB2CLoginAuthorityAcquireTokenTest()
        {
            _mobileTestHelper.B2CGoogleAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CGoogleAccount(), true);
        }

        /// <summary>
        /// B2C acquire token with Google provider 
        /// login.microsoftonline.com authority
        /// with subsequent silent call
        /// </summary>
        [Ignore("UI is different in AppCenter compared w/local.")]
        public void B2CGoogleMicrosoftAuthorityAcquireTokenTest()
        {
            _mobileTestHelper.B2CGoogleAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CGoogleAccount(), false);
        }

        /// <summary>
        /// B2C acquire token with local account 
        /// b2clogin.com authority
        /// and subsequent silent call
        /// </summary>
        public void B2CLocalAccountAcquireTokenTest()
        {
            _mobileTestHelper.B2CLocalAccountAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CLocalAccount(), true);
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV4 Federated flow
        /// </summary>
        public void AcquireTokenADFSV4InteractiveFederatedTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(
                xamarinController,
                LabUserHelper.GetAdfsUser(FederationProvider.AdfsV4));
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV3 Federated flow
        /// </summary>
        public void AcquireTokenADFSV3InteractiveFederatedTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(xamarinController, LabUserHelper.GetAdfsUser(FederationProvider.AdfsV3));
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV4 Non-Federated flow
        /// </summary>
        public void AcquireTokenADFSV4InteractiveNonFederatedTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(xamarinController, LabUserHelper.GetAdfsUser(FederationProvider.AdfsV4, false));
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV3 Non-Federated flow
        /// </summary>
        public void AcquireTokenADFSV3InteractiveNonFederatedTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(xamarinController, LabUserHelper.GetAdfsUser(FederationProvider.AdfsV4, false));
        }
    }
}
