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
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    // Important: do not install a NuGet package with the Chrome driver as it is a security risk.
    // Instead, install the Chrome driver on the test machine

    // Note: these tests require permission to a KeyVault Microsoft account;
    // Please ignore them if you are not a Microsoft FTE, they will run as part of the CI build
    [TestClass]
    [TestCategory(TestCategories.Selenium)]
    [TestCategory(TestCategories.LabAccess)]
    public class DeviceCodeFlow
    {
        private static readonly string[] Scopes = { "User.Read" };
        private IWebDriver _seleniumDriver;

        #region MSTest Hooks
        /// <summary>
        /// Initialized by MSTest (do not make private or readonly)
        /// </summary>
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            //TODO: hook up the logger?
            _seleniumDriver = SeleniumExtensions.CreateDefaultWebDriver();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _seleniumDriver?.Dispose();
        }
        #endregion

        [TestMethod]
        [Timeout(1 * 60 * 1000)] // 1 min timeout
        public async Task DeviceCodeFlowTestAsync()
        {            
            LabResponse labResponse = LabUserHelper.GetDefaultUser();

            Trace.WriteLine("Calling AcquireTokenWithDeviceCodeAsync");
            PublicClientApplication pca = new PublicClientApplication(labResponse.AppId);
            var result = await pca.AcquireTokenWithDeviceCodeAsync(Scopes, deviceCodeResult =>
            {
                RunAutomatedDeviceCodeFlow(deviceCodeResult, labResponse.User);

                return Task.FromResult(0);
            }).ConfigureAwait(false);

            Trace.WriteLine("Running asserts");

            Assert.IsNotNull(result);
            Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));
        }


        [TestMethod]
        [Ignore("Adfs does not currently support device code flow")]
        [Timeout(1 * 60 * 1000)] // 1 min timeout
        public async Task DeviceCodeFlowAdfsTestAsync()
        {
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);

            Trace.WriteLine("Calling AcquireTokenWithDeviceCodeAsync");
            PublicClientApplication pca = PublicClientApplicationBuilder.Create(Adfs2019LabConstants.PublicClientId)
                                                                        .WithRedirectUri(Adfs2019LabConstants.ClientRedirectUri)
                                                                        .WithAdfsAuthority(Adfs2019LabConstants.Authority)
                                                                        .BuildConcrete();
            var result = await pca.AcquireTokenWithDeviceCodeAsync(Scopes, deviceCodeResult =>
            {
                RunAutomatedDeviceCodeFlow(deviceCodeResult, labResponse.User, true);

                return Task.FromResult(0);
            }).ConfigureAwait(false);

            Trace.WriteLine("Running asserts");

            Assert.IsNotNull(result);
            Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));
        }

        private void RunAutomatedDeviceCodeFlow(DeviceCodeResult deviceCodeResult, LabUser user, bool isAdfs = false)
        {
            try
            {
                Trace.WriteLine("Browser is open. Navigating to the Device Code url and entering the code");

                string codeId = isAdfs ? "userCodeInput" : "code";
                string continueId = isAdfs ? "confirmationButton" : "continueBtn";

                _seleniumDriver.Navigate().GoToUrl(deviceCodeResult.VerificationUrl);
                _seleniumDriver.FindElement(By.Id(codeId)).SendKeys(deviceCodeResult.UserCode);

                IWebElement continueBtn = _seleniumDriver.WaitForElementToBeVisibleAndEnabled(By.Id(continueId));
                continueBtn?.Click();

                _seleniumDriver.PerformLogin(user, isAdfs);

                Trace.WriteLine("Authentication complete");

            }
            catch (Exception ex)
            {
                Trace.WriteLine("Browser automation failed " + ex);
                _seleniumDriver.SaveScreenshot(TestContext);
                throw;
            }
        }

    }
}
