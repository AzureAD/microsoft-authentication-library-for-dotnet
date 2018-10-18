using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Microsoft.Identity.LabInfrastructure;

namespace Test.Microsoft.Identity.Core.UIAutomation
{
    public class CoreMobileTestHelper
    {
        public static void PerformSignInFlow(ITestController controller, IUser user)
        {
            string passwordInputID = string.Empty;
            string signInButtonID = string.Empty;

            if (user.IsFederated)
            {
                switch (user.FederationProvider)
                {
                    case FederationProvider.AdfsV3:
                    case FederationProvider.AdfsV4:
                        passwordInputID = CoreUiTestConstants.AdfsV4WebPasswordID;
                        signInButtonID = CoreUiTestConstants.AdfsV4WebSubmitID;
                        break;
                    default:
                        passwordInputID = CoreUiTestConstants.WebPasswordID;
                        signInButtonID = CoreUiTestConstants.WebSubmitID;
                        break;
                }
            }
            else
            {
                passwordInputID = CoreUiTestConstants.WebPasswordID;
                signInButtonID = CoreUiTestConstants.WebSubmitID;
            }

            //Acquire token flow
            controller.Tap(CoreUiTestConstants.AcquireTokenID);
            //i0116 = UPN text field on AAD sign in endpoint
            controller.EnterText(CoreUiTestConstants.WebUPNInputID, 20, user.Upn, true);
            controller.DismissKeyboard();
            //idSIButton9 = Sign in button
            controller.Tap(CoreUiTestConstants.WebSubmitID, true);
            //i0118 = password text field
            controller.EnterText(passwordInputID, ((LabUser)user).GetPassword(), true);
            controller.DismissKeyboard();
            controller.Tap(signInButtonID, true);
        }

        public static void VerifyResult(ITestController controller)
        {
            RetryVerificationHelper(() => {
                //Test results are put into a label that is checked for messages
                var result = controller.GetText(CoreUiTestConstants.TestResultID);
                if (result.Contains(CoreUiTestConstants.TestResultSuccsesfulMessage))
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
                    if (attempts == CoreUiTestConstants.maximumResultCheckRetryAttempts)
                        Assert.Fail("Could not Verify test result");

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
