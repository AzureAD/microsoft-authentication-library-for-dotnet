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
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Microsoft.Identity.Test.Unit.Integration
{
    // Important: do not install a NuGet package with the Chrome driver as it is a security risk.
    // Instead, install the Chrome driver on the test machine

    // Note: these tests require permission to a KeyVault Microsoft account; 
    // Please ignore them if you are not a Microsoft FTE, they will run as part of the CI build
    [TestClass]
    public class DeviceCodeFlow
    {
        private static readonly string[] Scopes = { "User.Read" };

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
        }


        [TestMethod]
        [Timeout(60 * 1000)] // 1 min timeout
        public async Task DeviceCodeFlowTestAsync()
        {
            Debug.WriteLine("Fetching user from lab");
            LabResponse labResponse = LabUserHelper.GetDefaultUser();

            Debug.WriteLine("Calling AcquireTokenWithDeviceCodeAsync");
            PublicClientApplication pca = new PublicClientApplication(labResponse.AppId);
            var result = await pca.AcquireTokenWithDeviceCodeAsync(Scopes, deviceCodeResult =>
            {
                RunAutomatedDeviceCodeFlow(deviceCodeResult, labResponse.User);

                return Task.FromResult(0);
            }).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));
        }

        private void RunAutomatedDeviceCodeFlow(DeviceCodeResult deviceCodeResult, LabUser user)
        {
            IWebDriver driver = null;
            try
            {
                driver = InitDriver();

                Debug.WriteLine("Navigating and enterign the code");

                driver.Navigate().GoToUrl(deviceCodeResult.VerificationUrl);
                driver.FindElement(By.Id("code")).SendKeys(deviceCodeResult.UserCode);

                IWebElement continueBtn = WaitForElementToBeEnabled(driver, By.Id("continueBtn"));
                continueBtn?.Click();

                Debug.WriteLine("Logging in");
                PerformLogin(driver, user);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Browser automation failed " + ex);
#if DESKTOP // Can't attach a file on netcore because mstest doesn't support it
                Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
                string failurePicturePath = Path.Combine(TestContext.ResultsDirectory, "failure.png");
                ss.SaveAsFile(failurePicturePath, ScreenshotImageFormat.Png);
                TestContext.AddResultFile(failurePicturePath);

                Debug.WriteLine("Failing because of " + ex);
#endif
                 throw;
            }
            finally
            {
                driver?.Close();
                driver?.Dispose();
            }
        }

        private static void PerformLogin(IWebDriver driver, LabUser user)
        {
            driver.FindElement(By.Id("i0116")).SendKeys(user.Upn); // username
            driver.FindElement(By.Id("idSIButton9")).Click(); //Next
            WaitForElementToBeEnabled(driver, By.Id("i0118")).SendKeys(user.Password); // password
            WaitForElementToBeEnabled(driver, By.Id("idSIButton9")).Click(); // Finish
        }

        private static ChromeDriver InitDriver()
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless"); // ~2x faster, no visual rendering

            var driver = new ChromeDriver(chromeOptions);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            return driver;
        }

        private static IWebElement WaitForElementToBeEnabled(IWebDriver driver, By by)
        {
            WebDriverWait webDriverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            IWebElement continueBtn = webDriverWait.Until<IWebElement>(dr =>
            {
                try
                {
                    var elementToBeDisplayed = driver.FindElement(by);
                    if (elementToBeDisplayed.Displayed && elementToBeDisplayed.Enabled)
                    {
                        return elementToBeDisplayed;
                    }
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            });
            return continueBtn;
        }
    }
}
