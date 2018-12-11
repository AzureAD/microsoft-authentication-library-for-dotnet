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
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Microsoft.Identity.Test.Unit.Integration
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
        private const string UsernameHtmlId = "i0116";
        private const string NextButtonHtmlId = "idSIButton9";
        private const string PasswordHtmlId = "i0118";

        private static readonly string[] Scopes = { "User.Read" };
        private SeleniumWrapper _seleniumWrapper;

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
            TestCommon.ResetState();
            _seleniumWrapper = new SeleniumWrapper();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _seleniumWrapper?.Dispose();
        }
        #endregion

        [TestMethod]
        [Timeout(1 * 60 * 1000)] // 1 min timeout
        public async Task DeviceCodeFlowTestAsync()
        {
           Trace.WriteLine("Fetching user from lab");
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

        private void RunAutomatedDeviceCodeFlow(DeviceCodeResult deviceCodeResult, LabUser user)
        {
            IWebDriver driver = _seleniumWrapper.Driver;
            try
            {
               Trace.WriteLine("Browser is open. Navigating to the Device Code url and entering the code");

                driver.Navigate().GoToUrl(deviceCodeResult.VerificationUrl);
                driver.FindElement(By.Id("code")).SendKeys(deviceCodeResult.UserCode);

                IWebElement continueBtn = _seleniumWrapper.WaitForElementToBeVisibleAndEnabled(By.Id("continueBtn"));
                continueBtn?.Click();

                PerformLogin(user);

               Trace.WriteLine("Authentication complete");

            }
            catch (Exception ex)
            {
               Trace.WriteLine("Browser automation failed " + ex);
                _seleniumWrapper.SaveScreenshot(TestContext);
                throw;
            }
        }

        private void PerformLogin(LabUser user)
        {
            IWebDriver driver = _seleniumWrapper.Driver;

           Trace.WriteLine("Logging in ... Entering username");
            driver.FindElement(By.Id(UsernameHtmlId)).SendKeys(user.Upn); // username

           Trace.WriteLine("Logging in ... Clicking next after username");
            driver.FindElement(By.Id(NextButtonHtmlId)).Click(); //Next

           Trace.WriteLine("Logging in ... Entering password");
            _seleniumWrapper.WaitForElementToBeVisibleAndEnabled(By.Id(PasswordHtmlId)).SendKeys(user.Password); // password

           Trace.WriteLine("Logging in ... Clicking next after password");
            _seleniumWrapper.WaitForElementToBeVisibleAndEnabled(By.Id(NextButtonHtmlId)).Click(); // Finish
        }
    }
}
