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

using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Test.Microsoft.Identity.LabInfrastructure;

namespace Test.Microsoft.Identity.Core.UIAutomation
{
    public class CoreMobileTestHelper
    {   
        public void PerformSignInFlow(ITestController controller, IUser user)
        {
            UserInformationFieldIds userInformationFieldIds = new UserInformationFieldIds();
            userInformationFieldIds.DetermineUser(user);

            //Acquire token flow
            controller.Tap(CoreUiTestConstants.AcquireTokenID);
            //i0116 = UPN text field on AAD sign in endpoint
            controller.EnterText(CoreUiTestConstants.WebUPNInputID, 20, user.Upn, true);
            controller.DismissKeyboard();
            //idSIButton9 = Sign in button
            controller.Tap(CoreUiTestConstants.WebSubmitID, true);
            //i0118 = password text field
            controller.EnterText(userInformationFieldIds.PasswordInputId, ((LabUser)user).GetPassword(), true);
            controller.DismissKeyboard();
            controller.Tap(userInformationFieldIds.SignInButtonId, true);
        }

        public void PerformSignInFlowWithoutUI(ITestController controller)
        {
            //Acquire token flow
            controller.Tap(CoreUiTestConstants.AcquireTokenID);
        }

        public void VerifyResult(ITestController controller)
        {
            RetryVerificationHelper(() =>
            {
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

        private void RetryVerificationHelper(Action verification)
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
