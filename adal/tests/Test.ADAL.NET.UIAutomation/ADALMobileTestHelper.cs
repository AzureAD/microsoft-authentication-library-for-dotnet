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
using Xamarin.UITest;
using Xamarin.UITest.Queries;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace Test.ADAL.UIAutomation
{
    /// <summary>
    /// Contains the core test functionality that will be used by Android and iOS tests
    /// </summary>
	public class ADALMobileTestHelper
    {
        CoreMobileTestHelper CoreMobileTestHelper = new CoreMobileTestHelper();

        /// <summary>
        /// Runs through the standard acquire token interactive flow
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void AcquireTokenInteractiveTestHelper(ITestController controller, UserQueryParameters userParams)
        {
            AcquireTokenInteractiveHelper(controller, userParams);
            CoreMobileTestHelper.VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the standard acquire token silent flow
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void AcquireTokenSilentTestHelper(ITestController controller, UserQueryParameters userParams)
        {
            AcquireTokenInteractiveHelper(controller, userParams);
            CoreMobileTestHelper.VerifyResult(controller);

            //Enter 2nd Resource
            controller.EnterText(CoreUiTestConstants.ResourceEntryID, CoreUiTestConstants.Exchange, false);
            controller.DismissKeyboard();

            //Acquire token silently
            controller.Tap(CoreUiTestConstants.AcquireTokenSilentID);

            CoreMobileTestHelper.VerifyResult(controller);
        }

        public void AcquireTokenInteractiveHelper(ITestController controller, UserQueryParameters userParams)
        {
            var user = PrepareForAuthentication(controller, userParams);
            SetInputData(controller, CoreUiTestConstants.MSIDLAB4ClientId, CoreUiTestConstants.MSGraph);
            CoreMobileTestHelper.PerformSignInFlow(controller, user);
        }

        public void AcquireTokenWithPromptBehaviorAlwaysHelper(ITestController controller, UserQueryParameters userParams)
        {
            var user = PrepareForAuthentication(controller, userParams);
            SetInputData(controller, CoreUiTestConstants.MSIDLAB4ClientId, CoreUiTestConstants.MSGraph);

            // AcquireToken promptBehavior.Auto to get a token in the cache 
            SetPromptBehavior(controller, CoreUiTestConstants.PromptBehaviorAuto);
            CoreMobileTestHelper.PerformSignInFlow(controller, user);

            // AcquireToken promptBehavior.Always. Even with a token, the UI should be shown 
            SetPromptBehavior(controller, CoreUiTestConstants.PromptBehaviorAlways);
            CoreMobileTestHelper.PerformSignInFlow(controller, user);

            // AcquireToken promptBehavior.Auto. No UI should be shown. 
            SetPromptBehavior(controller, CoreUiTestConstants.PromptBehaviorAuto);
            CoreMobileTestHelper.PerformSignInFlowWithoutUI(controller);
            CoreMobileTestHelper.VerifyResult(controller);
        }

        private IUser PrepareForAuthentication(ITestController controller, UserQueryParameters userParams)
        {
            //Navigate to second page
            controller.Tap(CoreUiTestConstants.SecondPageID);

            //Clear Cache
            controller.Tap(CoreUiTestConstants.ClearCacheID);

            //Get User from Lab
            return controller.GetUser(userParams);
        }

        private void SetInputData(ITestController controller, string clientID, string resource)
        {
            //Enter ClientID
            controller.EnterText(CoreUiTestConstants.ClientIdEntryID, clientID, false);
            controller.DismissKeyboard();

            //Enter Resource
            controller.EnterText(CoreUiTestConstants.ResourceEntryID, resource, false);
            controller.DismissKeyboard();
        }

        private void SetPromptBehavior(ITestController controller, string promptBehavior)
        {
            //Select PromptBehavior 
            controller.EnterText(CoreUiTestConstants.PromptBehaviorEntryID, promptBehavior, false);
            controller.DismissKeyboard();
        }
    }
}
