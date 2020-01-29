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
        private readonly Xamarin.UITest.Platform _platform;

        public MobileTestHelper(Xamarin.UITest.Platform platform)
        {
            _platform = platform;
        }

        /// <summary>
        /// Runs through the standard acquire token flow, using the login prompt behavior. The ui behavior of "login" is used by default.
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void AcquireTokenTestHelper(
            ITestController controller,
            LabResponse labResponse,
            string testToRun)
        {
            PerformSignInFlow(
                controller,
                labResponse.User,
                testToRun);

            VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token flow with local account
        /// </summary>
        public void B2CLocalAccountAcquireTokenInteractiveTestHelper(
            ITestController controller,
            LabResponse labResponse,
            string testToRun)
        {
            PerformB2CSignInFlow(
                controller,
                labResponse.User,
                B2CIdentityProvider.Local,
                testToRun);
        }

        /// <summary>
        /// Runs through the B2C acquire token flow with Facebook Provider
        /// </summary>
        public void B2CFacebookAcquireTokenInteractiveTestHelper(
            ITestController controller,
            LabResponse labResponse,
            string testToRun)
        {
            PerformB2CSignInFlow(
                controller,
                labResponse.User,
                B2CIdentityProvider.Facebook,
                testToRun);

            VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token flow with Facebook Provider
        /// and Edit Policy authority
        /// </summary>
        public void B2CFacebookEditPolicyAcquireTokenInteractiveTestHelper(
            ITestController controller,
            LabResponse labResponse,
            string testToRun)
        {
            PerformB2CSignInEditProfileFlow(
                controller);

            VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token flow with Google Provider
        /// </summary>
        public void B2CGoogleAcquireTokenInteractiveTestHelper(
            ITestController controller,
            LabResponse labResponse,
            string testToRun)
        {
            PerformB2CSignInFlow(
                controller,
                labResponse.User,
                B2CIdentityProvider.Google,
                testToRun);

            VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token silent flow with local account
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void B2CLocalAccountAcquireTokenSilentTest(
            ITestController controller,
            LabResponse labResponse,
            string testToRun)
        {
            B2CLocalAccountAcquireTokenInteractiveTestHelper(
                controller,
                labResponse,
                testToRun);

            VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token silent flow with Facebook identity provider
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void B2CFacebookAcquireTokenSilentTest(
            ITestController controller,
            LabResponse labResponse,
            string testToRun)
        {
            PerformB2CSignInFlow(
                controller,
                labResponse.User,
                B2CIdentityProvider.Facebook,
                testToRun);

            VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the B2C acquire token silent flow with Google identity provider
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void B2CGoogleAcquireTokenSilentTest(
            ITestController controller,
            LabResponse labResponse,
            string testToRun)
        {
            PerformB2CSignInFlow(
                controller,
                labResponse.User,
                B2CIdentityProvider.Google,
                testToRun);

            VerifyResult(controller);
        }

        public void PerformB2CLocalAccountSignInFlow(ITestController controller, LabUser user, UserInformationFieldIds userInformationFieldIds)
        {
            controller.EnterText(CoreUiTestConstants.WebUpnB2CLocalInputId, 20, user.Upn, XamarinSelector.ByHtmlIdAttribute);

            controller.EnterText(userInformationFieldIds.GetPasswordInputId(), user.GetOrFetchPassword(), XamarinSelector.ByHtmlIdAttribute);

            controller.Tap(userInformationFieldIds.GetPasswordSignInButtonId(), XamarinSelector.ByHtmlIdAttribute);
        }

        public void PerformB2CFacebookProviderSignInFlow(ITestController controller, LabUser user, UserInformationFieldIds userInformationFieldIds)
        {
            controller.WaitForWebElementByCssId(CoreUiTestConstants.FacebookAccountId);

            controller.Tap(CoreUiTestConstants.FacebookAccountId, XamarinSelector.ByHtmlIdAttribute);

            controller.WaitForWebElementByCssId(CoreUiTestConstants.WebUpnB2CFacebookInputId);

            controller.EnterText(CoreUiTestConstants.WebUpnB2CFacebookInputId, 20, "jcufzuyznp_1559677030@tfbnw.net", XamarinSelector.ByHtmlIdAttribute);

            controller.EnterText(userInformationFieldIds.GetPasswordInputId(), user.GetOrFetchPassword(), XamarinSelector.ByHtmlIdAttribute);

            controller.WaitForWebElementByCssId(userInformationFieldIds.GetPasswordSignInButtonId());

            controller.Tap(userInformationFieldIds.GetPasswordSignInButtonId(), XamarinSelector.ByHtmlIdAttribute);
        }

        public void PerformB2CGoogleProviderSignInFlow(ITestController controller, LabUser user, UserInformationFieldIds userInformationFieldIds)
        {
            controller.Tap(CoreUiTestConstants.GoogleAccountId, XamarinSelector.ByHtmlIdAttribute);

            controller.EnterText(CoreUiTestConstants.WebUpnB2CGoogleInputId, 20, user.Upn, XamarinSelector.ByHtmlIdAttribute);

            controller.Tap(CoreUiTestConstants.B2CGoogleNextId, XamarinSelector.ByHtmlIdAttribute);

            controller.EnterText(userInformationFieldIds.GetPasswordInputId(), user.GetOrFetchPassword(), XamarinSelector.ByHtmlIdAttribute);

            controller.Tap(userInformationFieldIds.GetPasswordSignInButtonId(), XamarinSelector.ByHtmlIdAttribute);
        }

        public void PerformB2CSignInFlow(
            ITestController controller,
            LabUser user,
            B2CIdentityProvider b2CIdentityProvider,
            string testToRun)
        {
            UserInformationFieldIds userInformationFieldIds = DetermineUserInformationFieldIds(user);

            controller.Tap(CoreUiTestConstants.TestsToRunPicker);
            controller.Tap(testToRun);

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
        }

        public void PerformB2CSignInEditProfileFlow(
            ITestController controller)
        {
            controller.Tap(CoreUiTestConstants.B2CEditProfileContinueId, XamarinSelector.ByHtmlIdAttribute);

            controller.WaitForWebElementByCssId(CoreUiTestConstants.B2CEditProfileContinueId);

            VerifyResult(controller);
        }

        public void PerformSignInFlow(
            ITestController controller,
            LabUser user,
            string testToRun)
        {
            UserInformationFieldIds userInformationFieldIds = DetermineUserInformationFieldIds(user);

            controller.Tap(CoreUiTestConstants.TestsToRunPicker);
            controller.Tap(testToRun);

            try
            {
                controller.WaitForWebElementByCssId(CoreUiTestConstants.WebUPNInputId);
                //i0116 = UPN text field on AAD sign in endpoint
                controller.EnterText(CoreUiTestConstants.WebUPNInputId, 20, user.Upn, XamarinSelector.ByHtmlIdAttribute);
                //idSIButton9 = Sign in button
                controller.Tap(CoreUiTestConstants.WebSubmitId, XamarinSelector.ByHtmlIdAttribute);
                //i0118 = password text field
                controller.WaitForWebElementByCssId(userInformationFieldIds.GetPasswordInputId());
                controller.EnterText(userInformationFieldIds.GetPasswordInputId(), user.GetOrFetchPassword(), XamarinSelector.ByHtmlIdAttribute);
                controller.Tap(userInformationFieldIds.GetPasswordSignInButtonId(), XamarinSelector.ByHtmlIdAttribute);
            }
            catch
            {
                Console.WriteLine("Failed to find UPN input. Attempting to click on UPN from select account screen");
                controller.Tap(user.Upn, XamarinSelector.ByHtmlValue);
            }
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
    }
}
