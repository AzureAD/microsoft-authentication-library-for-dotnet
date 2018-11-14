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

using Test.Microsoft.Identity.LabInfrastructure;
using NUnit.Framework;
using Test.Microsoft.Identity.Core.UIAutomation;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Linq;
using Xamarin.UITest.Queries;

namespace Test.MSAL.UIAutomation
{
    /// <summary>
    /// Contains the core test functionality that will be used by Android and iOS tests
    /// </summary>
    public class MSALMobileTestHelper
    {
        public CoreMobileTestHelper CoreMobileTestHelper { get; set; } = new CoreMobileTestHelper();
        public bool isB2CloginAuthority;

        /// <summary>
        /// Runs through the standard acquire token flow, using the login prompt behavior
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void AcquireTokenInteractiveTestHelper(
            ITestController controller,
            LabResponse labResponse,
            string promptBehavior = CoreUiTestConstants.UIBehaviorLogin)
        {
            AcquireTokenInteractiveHelper(controller, labResponse, promptBehavior);
            CoreMobileTestHelper.VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the standard acquire token silent flow
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void AcquireTokenSilentTestHelper(ITestController controller, LabResponse labResponse)
        {
            //acquire token for 1st resource
            AcquireTokenInteractiveHelper(controller, labResponse, CoreUiTestConstants.UIBehaviorLogin);
            CoreMobileTestHelper.VerifyResult(controller);

            //acquire token for 2nd resource with refresh token
            SetInputData(controller, labResponse.AppId, CoreUiTestConstants.DefaultScope, CoreUiTestConstants.UIBehaviorLogin);
            controller.Tap(CoreUiTestConstants.AcquireTokenSilentID);
            CoreMobileTestHelper.VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token flow
        /// </summary>
        public void B2CLocalAccountAcquireTokenInteractiveTestHelper(ITestController controller, LabResponse labResponse)
        {
            PrepareForAuthentication(controller);
            if (isB2CloginAuthority)
            {
                SetB2CInputDataForB2CloginAuthority(controller);
            }
            else
            {
                SetB2CInputData(controller);
            }

            PerformB2CLocalAccountSignInFlow(controller, labResponse.User);
            CoreMobileTestHelper.VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token silent flow
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void B2CLocalAccountAcquireTokenSilentTestHelper(ITestController controller, LabResponse labResponse)
        {
            //acquire token for 1st resource   
            isB2CloginAuthority = false;
            B2CLocalAccountAcquireTokenInteractiveTestHelper(controller, labResponse);
            CoreMobileTestHelper.VerifyResult(controller);

            //select user
            controller.Tap(CoreUiTestConstants.SelectUser);
            //b2c does not return userinfo in token response
            controller.Tap(CoreUiTestConstants.UserMissingFromResponse);
            //acquire token silent with selected user
            controller.Tap(CoreUiTestConstants.AcquireTokenSilentID);
            CoreMobileTestHelper.VerifyResult(controller);
        }

        private void AcquireTokenInteractiveHelper(
            ITestController controller,
            LabResponse labResponse,
            string promptBehavior)
        {
            PrepareForAuthentication(controller);
            SetInputData(controller, labResponse.AppId, CoreUiTestConstants.DefaultScope, promptBehavior);
            CoreMobileTestHelper.PerformSignInFlow(controller, labResponse.User);


            // on consent, also hit the accept button
            if (promptBehavior == CoreUiTestConstants.UIBehaviorConsent)
            {
                AppWebResult consentHeader = controller.WaitForWebElementByCssId("consentHeader").FirstOrDefault();
                Assert.IsNotNull(consentHeader);
                Assert.IsTrue(consentHeader.TextContent.Contains("Permissions requested"));

                controller.Tap(CoreUiTestConstants.WebSubmitID, XamarinSelector.ByHtmlIdAttribute);
            }
        }

        private void PrepareForAuthentication(ITestController controller)
        {
            //Clear Cache
            controller.Tap(CoreUiTestConstants.CachePageID);
            controller.Tap(CoreUiTestConstants.ClearCacheID);
        }

        private void SetInputData(
            ITestController controller,
            string ClientID,
            string scopes,
            string uiBehavior)
        {
            controller.Tap(CoreUiTestConstants.SettingsPageID);

            //Enter ClientID
            controller.EnterText(CoreUiTestConstants.ClientIdEntryID, ClientID, XamarinSelector.ByAutomationId);
            controller.Tap(CoreUiTestConstants.SaveID);

            //Enter Scopes
            controller.Tap(CoreUiTestConstants.AcquirePageID);
            controller.EnterText(CoreUiTestConstants.ScopesEntryID, scopes, XamarinSelector.ByAutomationId);

            SetUiBehavior(controller, uiBehavior);
        }

        private void SetB2CInputData(ITestController controller)
        {
            controller.Tap(CoreUiTestConstants.SettingsPageID);

            // Select login.microsoftonline.com for authority
            SetAuthority(controller, CoreUiTestConstants.MicrosoftOnlineAuthority);
        }

        private void SetB2CInputDataForB2CloginAuthority(ITestController controller)
        {
            controller.Tap(CoreUiTestConstants.SettingsPageID);

            // Select b2clogin.com for authority
            SetAuthority(controller, CoreUiTestConstants.B2CLoginAuthority);
        }

        public void SetUiBehavior(ITestController controller, string promptBehavior)
        {
            // Enter Prompt Behavior
            controller.Tap(CoreUiTestConstants.UiBehaviorPickerID);
            controller.Tap(promptBehavior);
        }

        private void ValidateUiBehaviorString(string uiBehavior)
        {
            var okList = new[] {
                CoreUiTestConstants.UIBehaviorConsent,
                CoreUiTestConstants.UIBehaviorLogin,
                CoreUiTestConstants.UIBehaviorSelectAccount };

            bool isInList = okList.Any(item => string.Equals(item, uiBehavior, StringComparison.InvariantCulture));

            if (!isInList)
            {
                throw new InvalidOperationException("Test Setup Error: invalid uiBehavior " + uiBehavior);
            }
        }

        public void SetAuthority(ITestController controller, string authority)
        {
            // Select authority
            controller.Tap(CoreUiTestConstants.AuthorityPickerID);
            controller.Tap(authority);
        }

        public void PerformB2CLocalAccountSignInFlow(ITestController controller, LabUser user)
        {
            UserInformationFieldIds userInformationFieldIds = CoreMobileTestHelper.DetermineUserInformationFieldIds(user);

            controller.Tap(CoreUiTestConstants.AcquirePageID);

            //Acquire token flow
            controller.Tap(CoreUiTestConstants.AcquireTokenID);

            controller.EnterText(CoreUiTestConstants.WebUPNB2CLocalInputID, 20, user.Upn, XamarinSelector.ByHtmlIdAttribute);

            controller.EnterText(userInformationFieldIds.PasswordInputId, LabUserHelper.GetUserPassword(user), XamarinSelector.ByHtmlIdAttribute);

            controller.Tap(userInformationFieldIds.SignInButtonId, XamarinSelector.ByHtmlIdAttribute);
        }
    }
}
