// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Test.LabInfrastructure;
using NUnit.Framework;
using Xamarin.UITest.Queries;

namespace Microsoft.Identity.Test.UIAutomation.Infrastructure
{
    /// <summary>
    /// Contains the core test functionality that will be used by Android and iOS tests
    /// </summary>
    public class MobileTestHelper
    {
        private string _acquirePageId;
        private string _cachePageId;
        private string _settingsPageId;

        public MobileTestHelper(Xamarin.UITest.Platform platform)
        {
            SetPageNavigationIds(platform);
        }

        /// <summary>
        /// Runs through the standard acquire token flow, using the login prompt behavior. The ui behavior of "login" is used by default.
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void AcquireTokenInteractiveTestHelper(
            ITestController controller,
            LabResponse labResponse,
            string promptBehavior = CoreUiTestConstants.UiBehaviorLogin)
        {
            AcquireTokenInteractiveHelper(controller, labResponse, promptBehavior);
            VerifyResult(controller);
        }

        public void AcquireTokenInteractiveWithConsentTest(
            ITestController controller,
            LabResponse labResponse,
            string promptBehavior = CoreUiTestConstants.UiBehaviorLogin)
        {
            PrepareForAuthentication(controller);
            SetInputData(controller, labResponse.AppId, CoreUiTestConstants.DefaultScope, promptBehavior);

            //Acquire token flow
            controller.Tap(CoreUiTestConstants.AcquireTokenButtonId);

            //i0116 = UPN text field on AAD sign in endpoint
            controller.Tap(labResponse.User.Upn, XamarinSelector.ByHtmlValue);

            // on consent, also hit the accept button
            if (promptBehavior == CoreUiTestConstants.UiBehaviorConsent)
            {
                AppWebResult consentHeader = controller.WaitForWebElementByCssId("consentHeader").FirstOrDefault();
                Assert.IsNotNull(consentHeader);
                Assert.IsTrue(consentHeader.TextContent.Contains("Permissions requested"));

                controller.Tap(CoreUiTestConstants.WebSubmitId, XamarinSelector.ByHtmlIdAttribute);
            }

            VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the standard acquire token silent flow
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void AcquireTokenSilentTestHelper(ITestController controller, LabResponse labResponse)
        {
            //acquire token for 1st resource
            AcquireTokenInteractiveHelper(controller, labResponse, CoreUiTestConstants.UiBehaviorLogin);
            VerifyResult(controller);

            //acquire token for 2nd resource with refresh token
            SetInputData(controller, labResponse.AppId, CoreUiTestConstants.DefaultScope, CoreUiTestConstants.UiBehaviorLogin);
            controller.Tap(CoreUiTestConstants.AcquireTokenSilentButtonId);
            VerifyResult(controller);
        }

        private void AcquireTokenInteractiveHelper(
            ITestController controller,
            LabResponse labResponse,
            string promptBehavior)
        {
            PrepareForAuthentication(controller);
            SetInputData(controller, labResponse.AppId, CoreUiTestConstants.DefaultScope, promptBehavior);
            PerformSignInFlow(controller, labResponse.User);

            // on consent, also hit the accept button
            if (promptBehavior == CoreUiTestConstants.UiBehaviorConsent)
            {
                AppWebResult consentHeader = controller.WaitForWebElementByCssId("consentHeader").FirstOrDefault();
                Assert.IsNotNull(consentHeader);
                Assert.IsTrue(consentHeader.TextContent.Contains("Permissions requested"));

                controller.Tap(CoreUiTestConstants.WebSubmitId, XamarinSelector.ByHtmlIdAttribute);
            }
        }

        private void PrepareForAuthentication(ITestController controller)
        {
            controller.Tap(_cachePageId);
            controller.Tap(CoreUiTestConstants.ClearCacheId);
            controller.Tap(_settingsPageId);
            controller.Tap(CoreUiTestConstants.ClearAllCacheId);
        }

        private void SetInputData(
            ITestController controller,
            string clientID,
            string scopes,
            string uiBehavior)
        {
            controller.Tap(_settingsPageId);

            //Enter ClientID
            controller.EnterText(CoreUiTestConstants.ClientIdEntryId, clientID, XamarinSelector.ByAutomationId);
            controller.Tap(CoreUiTestConstants.SaveID);

            //Enter Scopes
            controller.Tap(_acquirePageId);
            controller.EnterText(CoreUiTestConstants.ScopesEntryId, scopes, XamarinSelector.ByAutomationId);

            SetUiBehavior(controller, uiBehavior);
        }

        public void SetUiBehavior(ITestController controller, string promptBehavior)
        {
            // Enter Prompt Behavior
            controller.Tap(CoreUiTestConstants.UiBehaviorPickerId);
            controller.Tap(promptBehavior);
            controller.Tap(_acquirePageId);
        }

        /// <summary>
        /// Runs through the B2C acquire token flow with local account
        /// </summary>
        public void B2CLocalAccountAcquireTokenInteractiveTestHelper(ITestController controller, LabResponse labResponse, bool isB2CLoginAuthority)
        {
            PerformB2CSignInFlow(controller, labResponse.User, B2CIdentityProvider.Local, isB2CLoginAuthority);
        }

        /// <summary>
        /// Runs through the B2C acquire token flow with Facebook Provider
        /// </summary>
        public void B2CFacebookAcquireTokenInteractiveTestHelper(ITestController controller, LabResponse labResponse, bool isB2CLoginAuthority)
        {
            PerformB2CSignInFlow(controller, labResponse.User, B2CIdentityProvider.Facebook, isB2CLoginAuthority);
        }

        /// <summary>
        /// Runs through the B2C acquire token flow with Facebook Provider
        /// and Edit Policy authority
        /// </summary>
        public void B2CFacebookEditPolicyAcquireTokenInteractiveTestHelper(ITestController controller)
        {
            PerformB2CSignInEditProfileFlow(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token flow with Google Provider
        /// </summary>
        public void B2CGoogleAcquireTokenInteractiveTestHelper(ITestController controller, LabResponse labResponse, bool isB2CLoginAuthority)
        {
            PerformB2CSignInFlow(controller, labResponse.User, B2CIdentityProvider.Google, isB2CLoginAuthority);
        }

        private void SetB2CAuthority(ITestController controller, bool isB2CLoginAuthority)
        {
            PrepareForAuthentication(controller);
            controller.Tap(_settingsPageId);

            if (isB2CLoginAuthority)
            {
                SetB2CInputDataForB2CloginAuthority(controller);
            }
            else
            {
                SetB2CInputData(controller);
            }
        }

        /// <summary>
        /// Runs through the B2C acquire token silent flow with local account
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void B2CLocalAccountAcquireTokenSilentTest(ITestController controller, LabResponse labResponse, bool isB2CLoginAuthority)
        {
            //acquire token for 1st resource
            B2CLocalAccountAcquireTokenInteractiveTestHelper(controller, labResponse, isB2CLoginAuthority);

            B2CSilentFlowHelper(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token ROPC flow with local acount
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void B2CAcquireTokenROPCTest(ITestController controller, LabResponse labResponse)
        {
            SetB2CInputDataForROPC(controller);
            
            controller.Tap(_acquirePageId);

            controller.Tap(CoreUiTestConstants.ROPCUsernameId, XamarinSelector.ByAutomationId);
            controller.EnterText(CoreUiTestConstants.ROPCUsernameId, labResponse.User.Upn, XamarinSelector.ByAutomationId);
            controller.Tap(CoreUiTestConstants.ROPCPasswordId, XamarinSelector.ByAutomationId);
            controller.EnterText(CoreUiTestConstants.ROPCPasswordId, labResponse.User.GetOrFetchPassword(), XamarinSelector.ByAutomationId);

            VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token silent flow with Facebook identity provider
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void B2CFacebookAcquireTokenSilentTest(ITestController controller, LabResponse labResponse, bool isB2CLoginAuthority)
        {
            //acquire token for 1st resource
            B2CFacebookAcquireTokenInteractiveTestHelper(controller, labResponse, isB2CLoginAuthority);

            B2CSilentFlowHelper(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token silent flow with Google identity provider
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void B2CGoogleAcquireTokenSilentTest(ITestController controller, LabResponse labResponse, bool isB2CLoginAuthority)
        {
            //acquire token for 1st resource
            B2CGoogleAcquireTokenInteractiveTestHelper(controller, labResponse, isB2CLoginAuthority);

            B2CSilentFlowHelper(controller);
        }

        public void B2CSilentFlowHelper(ITestController controller)
        {
            //verify results of AT call
            VerifyResult(controller);

            //select user
            controller.Tap(CoreUiTestConstants.SelectUser);
            //b2c does not return userinfo in token response
            controller.Tap(CoreUiTestConstants.UserMissingFromResponse);
            //acquire token silent with selected user
            controller.Tap(CoreUiTestConstants.AcquireTokenSilentButtonId);
            VerifyResult(controller);
        }

        private void SetB2CInputData(ITestController controller)
        {
            // Select login.microsoftonline.com for authority
            SetAuthority(controller, CoreUiTestConstants.MicrosoftOnlineAuthority);
        }

        private void SetB2CInputDataForB2CloginAuthority(ITestController controller)
        {
            // Select b2clogin.com for authority
            SetAuthority(controller, CoreUiTestConstants.B2CLoginAuthority);
        }

        private void SetB2CInputDataForEditProfileAuthority(ITestController controller)
        {
            controller.Tap(_settingsPageId);
            // Select Edit Profile for Authority
            SetAuthority(controller, CoreUiTestConstants.B2CEditProfileAuthority);
        }

        private void SetB2CInputDataForROPC(ITestController controller)
        {
            controller.Tap(_settingsPageId);
            // Select ROPC for authority
            SetAuthority(controller, CoreUiTestConstants.ROPC);
        }

        public void SetAuthority(ITestController controller, string authority)
        {
            // Select authority
            controller.Tap(CoreUiTestConstants.AuthorityPickerId);
            controller.Tap(authority);
        }

        public void PerformB2CLocalAccountSignInFlow(ITestController controller, LabUser user, UserInformationFieldIds userInformationFieldIds)
        {
            controller.EnterText(CoreUiTestConstants.WebUpnB2CLocalInputId, 20, user.Upn, XamarinSelector.ByHtmlIdAttribute);

            controller.EnterText(userInformationFieldIds.PasswordInputId, user.GetOrFetchPassword(), XamarinSelector.ByHtmlIdAttribute);

            controller.Tap(userInformationFieldIds.PasswordSignInButtonId, XamarinSelector.ByHtmlIdAttribute);
        }

        public void PerformB2CFacebookProviderSignInFlow(ITestController controller, LabUser user, UserInformationFieldIds userInformationFieldIds)
        {
            controller.WaitForWebElementByCssId(CoreUiTestConstants.FacebookAccountId);

            controller.Tap(CoreUiTestConstants.FacebookAccountId, XamarinSelector.ByHtmlIdAttribute);

            controller.WaitForWebElementByCssId(CoreUiTestConstants.WebUpnB2CFacebookInputId);

            controller.EnterText(CoreUiTestConstants.WebUpnB2CFacebookInputId, 20, user.Upn, XamarinSelector.ByHtmlIdAttribute);

            controller.EnterText(userInformationFieldIds.PasswordInputId, user.GetOrFetchPassword(), XamarinSelector.ByHtmlIdAttribute);

            controller.WaitForWebElementByCssId(userInformationFieldIds.PasswordSignInButtonId);

            controller.Tap(userInformationFieldIds.PasswordSignInButtonId, XamarinSelector.ByHtmlIdAttribute);
        }

        public void PerformB2CGoogleProviderSignInFlow(ITestController controller, LabUser user, UserInformationFieldIds userInformationFieldIds)
        {
            controller.Tap(CoreUiTestConstants.GoogleAccountId, XamarinSelector.ByHtmlIdAttribute);

            controller.EnterText(CoreUiTestConstants.WebUpnB2CGoogleInputId, 20, user.Upn, XamarinSelector.ByHtmlIdAttribute);

            controller.Tap(CoreUiTestConstants.B2CGoogleNextId, XamarinSelector.ByHtmlIdAttribute);

            controller.EnterText(userInformationFieldIds.PasswordInputId, user.GetOrFetchPassword(), XamarinSelector.ByHtmlIdAttribute);

            controller.Tap(userInformationFieldIds.PasswordSignInButtonId, XamarinSelector.ByHtmlIdAttribute);
        }

        public void PerformB2CSignInFlow(ITestController controller, LabUser user, B2CIdentityProvider b2CIdentityProvider, bool isB2CLoginAuthority)
        {
            SetB2CAuthority(controller, true);

            UserInformationFieldIds userInformationFieldIds = DetermineUserInformationFieldIds(user);

            controller.Tap(_acquirePageId);

            //Acquire token flow
            controller.Tap(CoreUiTestConstants.AcquireTokenButtonId);

            switch (b2CIdentityProvider)
            {
            case B2CIdentityProvider.Local:
                PerformB2CLocalAccountSignInFlow(controller, user, userInformationFieldIds);
                break;
            case B2CIdentityProvider.Google:
                PerformB2CGoogleProviderSignInFlow(controller, user, userInformationFieldIds);
                break;

            case B2CIdentityProvider.Facebook:
                PerformB2CFacebookProviderSignInFlow(controller, user, userInformationFieldIds);
                break;
            default:
                throw new InvalidOperationException("B2CIdentityProvider unknown");
            }
            VerifyResult(controller);
        }

        public void PerformB2CSelectProviderOnlyFlow(ITestController controller, LabUser user, B2CIdentityProvider b2CIdentityProvider, bool isB2CLoginAuthority)
        {
            SetB2CAuthority(controller, isB2CLoginAuthority);

            controller.Tap(_acquirePageId);

            //Acquire token flow
            controller.Tap(CoreUiTestConstants.AcquireTokenButtonId);

            switch (b2CIdentityProvider)
            {
            case B2CIdentityProvider.Facebook:
                controller.Tap(CoreUiTestConstants.FacebookAccountId, XamarinSelector.ByHtmlIdAttribute);
                break;
            default:
                throw new InvalidOperationException("B2CIdentityProvider unknown");
            }
            VerifyResult(controller);
        }

        public void PerformB2CSignInEditProfileFlow(ITestController controller)
        {
            SetB2CInputDataForEditProfileAuthority(controller);

            controller.Tap(_acquirePageId);

            SetUiBehavior(controller, CoreUiTestConstants.UiBehaviorNoPrompt);

            //Acquire token flow
            controller.Tap(CoreUiTestConstants.AcquireTokenButtonId);

            controller.Tap(CoreUiTestConstants.B2CEditProfileContinueId, XamarinSelector.ByHtmlIdAttribute);

            controller.WaitForWebElementByCssId(CoreUiTestConstants.B2CEditProfileContinueId);

            VerifyResult(controller);
        }

        public void PromptBehaviorTestHelperWithConsent(ITestController controller, LabResponse labResponse)
        {
            // 1. Acquire token with uiBehavior set to consent
            AcquireTokenInteractiveTestHelper(
                controller,
                labResponse,
                CoreUiTestConstants.UiBehaviorConsent);

            // 2. Switch ui behavior to "select account"
            SetUiBehavior(controller, CoreUiTestConstants.UiBehaviorSelectAccount);

            // 3. Hit Acquire Token directly since we are not changing any other setting
            controller.Tap(CoreUiTestConstants.AcquireTokenButtonId);

            // 4. The web UI should display all users, so click on the current user
            controller.Tap(labResponse.User.Upn, XamarinSelector.ByHtmlValue);

            // 5. Validate token again
            VerifyResult(controller);
        }

        public void PerformSignInFlow(ITestController controller, LabUser user)
        {
            UserInformationFieldIds userInformationFieldIds = DetermineUserInformationFieldIds(user);

            //Acquire token flow
            controller.Tap(CoreUiTestConstants.AcquireTokenButtonId);
            try
            {
                //i0116 = UPN text field on AAD sign in endpoint
                controller.EnterText(CoreUiTestConstants.WebUPNInputId, 20, user.Upn, XamarinSelector.ByHtmlIdAttribute);
                //idSIButton9 = Sign in button
                controller.Tap(CoreUiTestConstants.WebSubmitId, XamarinSelector.ByHtmlIdAttribute);
                //i0118 = password text field
                controller.EnterText(userInformationFieldIds.PasswordInputId, user.GetOrFetchPassword(), XamarinSelector.ByHtmlIdAttribute);
                controller.Tap(userInformationFieldIds.PasswordSignInButtonId, XamarinSelector.ByHtmlIdAttribute);
            }
            catch
            {
                Console.WriteLine("Failed to find UPN input. Attempting to click on UPN from select account screen");
                controller.Tap(user.Upn, XamarinSelector.ByHtmlValue);
            }
        }

        public static void PerformSignInFlowWithoutUI(ITestController controller)
        {
            //Acquire token flow
            controller.Tap(CoreUiTestConstants.AcquireTokenButtonId);
        }

        public static UserInformationFieldIds DetermineUserInformationFieldIds(LabUser user)
        {
            UserInformationFieldIds userInformationFieldIds = new UserInformationFieldIds(user);
            return userInformationFieldIds;
        }

        public void VerifyResult(ITestController controller)
        {
            RetryVerificationHelper(() =>
            {
                //Test results are put into a label that is checked for messages
                var result = controller.GetText(CoreUiTestConstants.TestResultId);
                if (result.Contains(CoreUiTestConstants.TestResultSuccessfulMessage))
                {
                    return;
                }
                else if (result.Contains(CoreUiTestConstants.TestResultFailureMessage))
                {
                    throw new ResultVerificationFailureException(VerificationError.ResultIndicatesFailure);
                }
                else
                {
                    throw new ResultVerificationFailureException(VerificationError.ResultNotFound);
                }
            });
        }

        private static void RetryVerificationHelper(Action verification)
        {
            //There may be a delay in the amount of time it takes for an authentication request to complete.
            //Thus this method will check the result once a second for 20 seconds.
            var attempts = 0;
            do
            {
                try
                {
                    attempts++;
                    verification();
                    break;
                }
                catch (ResultVerificationFailureException ex)
                {
                    if (attempts == CoreUiTestConstants.MaximumResultCheckRetryAttempts)
                    {
                        Assert.Fail("Could not Verify test result");
                    }

                    switch (ex.Error)
                    {
                    case VerificationError.ResultIndicatesFailure:
                        Assert.Fail("Test result indicates failure");
                        break;
                    case VerificationError.ResultNotFound:
                        Task.Delay(CoreUiTestConstants.ResultCheckPolliInterval).Wait();
                        break;
                    default:
                        throw;
                    }
                }
            } while (true);
        }

        private void SetPageNavigationIds(Xamarin.UITest.Platform platform)
        {
            switch (platform)
            {
            case Xamarin.UITest.Platform.Android:
                _cachePageId = CoreUiTestConstants.CachePageAndroidID;
                _acquirePageId = CoreUiTestConstants.AcquirePageAndroidId;
                _settingsPageId = CoreUiTestConstants.SettingsPageAndroidId;
                break;
            case Xamarin.UITest.Platform.iOS:
                _cachePageId = CoreUiTestConstants.CachePageID;
                _acquirePageId = CoreUiTestConstants.AcquirePageId;
                _settingsPageId = CoreUiTestConstants.SettingsPageId;
                break;
            }
        }
    }
}
