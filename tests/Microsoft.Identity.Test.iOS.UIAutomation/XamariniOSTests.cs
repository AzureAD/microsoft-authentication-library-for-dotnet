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

//NOTICE! Inorder to run UI automation tests for xamarin locally, you may need to upgrade nunit to 3.0 and above for this project and the core ui Automation project.
//It is set to 2.6.4 because that is the maximum version that appcenter can support.
//There is an error in visual studio that can prevent the NUnit test framework from loading the test dll properly.
//Remember to return the version back to 2.6.4 before commiting to prevent appcenter from failing

namespace Test.Microsoft.Identity.Msal.iOS.UIAutomation
{
    /// <summary>
    /// Configures environment for core/iOS tests to run
    /// </summary>
    [TestFixture(Platform.iOS)]
    public class XamarinIOSMsalTests
    {
        IApp app;
        Platform platform;
        ITestController xamarinController = new IOSXamarinUITestController();
        MobileTestHelper _mobileTestHelper = new MobileTestHelper();

        /// <summary>
        /// Initializes Xamarin UI tests
        /// </summary>
        /// <param name="platform">The platform where the tests will be performed</param>
        public XamarinIOSMsalTests(Platform platform)
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
        }

        ///// <summary>
        ///// Runs through the standard acquire token flow, using the default app configured UiBehavior = Login
        ///// </summary>
        //[Test]
        //public void AcquireTokenTest()
        //{
        //    _mobileTestHelper.AcquireTokenInteractiveTestHelper(xamarinController, LabUserHelper.GetDefaultUser());
        //}

        ///// <summary>
        ///// Runs through the standard acquire token silent flow
        ///// </summary>
        //[Test]
        //public void AcquireTokenSilentTest()
        //{
        //    _mobileTestHelper.AcquireTokenSilentTestHelper(xamarinController, LabUserHelper.GetDefaultUser());
        //}

        /// <summary>
        /// Runs through the standard acquire token flow
        /// </summary>
        [Test]
        //[Ignore("Current web element search implementation is unable to properly wait for select account elements on login page. Will be addressed in future updates.")]
        public void PromptBehaviorConsentSelectAccount()
        {
            var labResponse = LabUserHelper.GetDefaultUser();

            _mobileTestHelper.PromptBehaviorTestHelperWithConsent(xamarinController, labResponse);
        }

        ///// <summary>
        ///// B2C acquire token with Facebook provider
        ///// b2clogin.com authority
        ///// with subsequent silent call
        ///// </summary>
        //[Test]
        //[Ignore("Current web element search implementation is unable to properly locate certain B2C elements. Will be addressed in future updates")]
        //public void B2CFacebookProviderWithB2CLoginAuthorityAcquireTokenTest()
        //{
        //    _mobileTestHelper.B2CFacebookProviderAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CFacebookAccount(), true);
        //}

        ///// <summary>
        ///// B2C acquire token with Facebook provider 
        ///// login.microsoftonline.com authority
        ///// with subsequent silent call
        ///// </summary>
        //[Test]
        //[Ignore("Current web element search implementation is unable to properly locate certain B2C elements. Will be addressed in future updates")]
        //public void B2CFacebookProviderWithMicrosoftAuthorityAcquireTokenTest()
        //{
        //    _mobileTestHelper.B2CFacebookProviderAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CFacebookAccount(), false);
        //}

        ///// <summary>
        ///// B2C acquire token with Facebook provider
        ///// b2clogin.com authority
        ///// call to edit profile authority with
        /////  UIBehavior none
        ///// </summary>
        //[Test]
        //[Ignore("Current web element search implementation is unable to properly locate certain B2C elements. Will be addressed in future updates")]
        //public void B2CFacebookProviderEditPolicyAcquireTokenTest()
        //{
        //    _mobileTestHelper.B2CFacebookProviderAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CFacebookAccount(), true);
        //    _mobileTestHelper.B2CFacebookProviderEditPolicyAcquireTokenInteractiveTestHelper(xamarinController);
        //}

        ///// <summary>
        ///// B2C acquire token with Google provider
        ///// b2clogin.com authority
        ///// with subsequent silent call
        ///// </summary>
        //[Test]
        //[Ignore("Google Auth does not support embedded webview from b2clogin.com authority. " +
        //    "App Center cannot run system browser tests yet, so this test can only be run in " +
        //    "system browser locally.")]
        //public void B2CGoogleProviderWithB2CLoginAuthorityAcquireTokenTest()
        //{
        //    _mobileTestHelper.B2CGoogleProviderAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CGoogleAccount(), true);
        //}

        ///// <summary>
        ///// B2C acquire token with Google provider 
        ///// login.microsoftonline.com authority
        ///// with subsequent silent call
        ///// </summary>
        //[Test]
        //[Ignore("UI is different in AppCenter compared w/local.")]
        //public void B2CGoogleProviderWithMicrosoftAuthorityAcquireTokenTest()
        //{
        //    _mobileTestHelper.B2CGoogleProviderAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CGoogleAccount(), false);
        //}

        ///// <summary>
        ///// B2C acquire token with local account 
        ///// b2clogin.com authority
        ///// and subsequent silent call
        ///// </summary>
        //[Test]
        //[Ignore("Current web element search implementation is unable to properly locate certain B2C elements. Will be addressed in future updates")]
        //public void B2CLocalAccountAcquireTokenTest()
        //{
        //    _mobileTestHelper.B2CLocalAccountAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CLocalAccount(), true);
        //}

        ///// <summary>
        ///// B2C acquire token with local account 
        ///// login.microsoftonline.com authority
        ///// with subsequent silent call
        ///// </summary>
        //[Test]
        //[Ignore("Current web element search implementation is unable to properly locate certain B2C elements. Will be addressed in future updates")]
        //public void B2CLocalAccountAcquireTokenWithMicrosoftAuthorityTest()
        //{
        //    _mobileTestHelper.B2CLocalAccountAcquireTokenSilentTest(xamarinController, LabUserHelper.GetB2CLocalAccount(), false);
        //}

        ///// <summary>
        ///// Runs through the standard acquire token ADFSV4 Federated flow
        ///// </summary>
        //[Test]
        //public void AcquireTokenADFSV4InteractiveFederatedTest()
        //{
        //    _mobileTestHelper.AcquireTokenInteractiveTestHelper(
        //        xamarinController,
        //        LabUserHelper.GetAdfsUser(FederationProvider.AdfsV4));
        //}

        ///// <summary>
        ///// Runs through the standard acquire token ADFSV3 Federated flow
        ///// </summary>
        //[Test]
        //public void AcquireTokenADFSV3InteractiveFederatedTest()
        //{
        //    _mobileTestHelper.AcquireTokenInteractiveTestHelper(xamarinController, LabUserHelper.GetAdfsUser(FederationProvider.AdfsV3));
        //}

        ///// <summary>
        ///// Runs through the standard acquire token ADFSV4 Non-Federated flow
        ///// </summary>
        //[Test]
        //public void AcquireTokenADFSV4InteractiveNonFederatedTest()
        //{
        //    _mobileTestHelper.AcquireTokenInteractiveTestHelper(xamarinController, LabUserHelper.GetAdfsUser(FederationProvider.AdfsV4, false));
        //}

        ///// <summary>
        ///// Runs through the standard acquire token ADFSV3 Non-Federated flow
        ///// </summary>
        //[Test]
        //public void AcquireTokenADFSV3InteractiveNonFederatedTest()
        //{
        //    _mobileTestHelper.AcquireTokenInteractiveTestHelper(xamarinController, LabUserHelper.GetAdfsUser(FederationProvider.AdfsV4, false));
        //}
    }
}
