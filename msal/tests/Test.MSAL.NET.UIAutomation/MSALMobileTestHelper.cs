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

namespace Test.MSAL.UIAutomation
{
    /// <summary>
    /// Contains the core test functionality that will be used by Android and iOS tests
    /// </summary>
    public class MSALMobileTestHelper
    {
        CoreMobileTestHelper _coreMobileTestHelper = new CoreMobileTestHelper();

        /// <summary>
        /// Runs through the standard acquire token flow
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void AcquireTokenInteractiveTestHelper(ITestController controller, LabResponse labResponse)
        {
            AcquireTokenInteractiveHelper(controller, labResponse);
            _coreMobileTestHelper.VerifyResult(controller);
        }

        /// <summary>
        /// Runs through the standard acquire token silent flow
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public void AcquireTokenSilentTestHelper(ITestController controller, LabResponse labResponse)
        {
            //acquire token for 1st resource
            AcquireTokenInteractiveHelper(controller, labResponse);
            _coreMobileTestHelper.VerifyResult(controller);

            //acquire token for 2nd resource with refresh token
            SetInputData(controller, labResponse.AppId, CoreUiTestConstants.DefaultScope);
            controller.Tap(CoreUiTestConstants.AcquireTokenSilentID);
            _coreMobileTestHelper.VerifyResult(controller);
        }

        private void AcquireTokenInteractiveHelper(ITestController controller, LabResponse labResponse)
        {
            PrepareForAuthentication(controller);
            SetInputData(controller, labResponse.AppId, CoreUiTestConstants.DefaultScope);
            _coreMobileTestHelper.PerformSignInFlow(controller, labResponse.User);
        }

        private void PrepareForAuthentication(ITestController controller)
        {
            //Clear Cache
            controller.Tap(CoreUiTestConstants.CachePageID);
            controller.Tap(CoreUiTestConstants.ClearCacheID);
        }

        private void SetInputData(ITestController controller, string ClientID, string scopes)
        {
            controller.Tap(CoreUiTestConstants.SettingsPageID);

            //Enter ClientID
            controller.EnterText(CoreUiTestConstants.ClientIdEntryID, ClientID, false);
            controller.Tap(CoreUiTestConstants.SaveID);

            //Enter Scopes
            controller.Tap(CoreUiTestConstants.AcquirePageID);
            controller.EnterText(CoreUiTestConstants.ScopesEntryID, scopes, false);
        }
    }
}
