// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using OpenQA.Selenium;
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.UIAutomation.Infrastructure;
using System.Linq;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public static class SeleniumExtensions
    {
        private const int ImplicitTimeoutSeconds = 10;
        private const int ExplicitTimeoutSeconds = 15;

        public static IWebDriver CreateDefaultWebDriver()
        {
            ChromeOptions options = new ChromeOptions();
            ChromeDriver driver;

            // ~2x faster, no visual rendering
            // remove when debugging to see the UI automation
            options.AddArguments("headless");

            var env = Environment.GetEnvironmentVariable("ChromeWebDriver");
            if (String.IsNullOrEmpty(env))
            {
                driver = new ChromeDriver(options);
            }
            else
            {
                driver = new ChromeDriver(env, options);
            }

            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(ImplicitTimeoutSeconds);

            return driver;
        }

        #region ScreenShot support
        private static int _picNumber = 1;

        public static void SaveScreenshot(this IWebDriver driver, TestContext testContext, string name = "failure")
        {
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            string picName = name + _picNumber++ + ".png";
#if DESKTOP // Can't attach a file on netcore because mstest doesn't support it
            string failurePicturePath = Path.Combine(testContext.TestResultsDirectory, picName);
#else
            string failurePicturePath = Path.Combine(Directory.GetCurrentDirectory(), picName);
#endif

            Trace.WriteLine($"Saving picture to {failurePicturePath}");
            ss.SaveAsFile(failurePicturePath, ScreenshotImageFormat.Png);

#if DESKTOP // Can't attach a file to the logs on netcore because mstest doesn't support it
            testContext.AddResultFile(failurePicturePath);
#endif
        }

        #endregion

        public static IWebElement WaitForElementToBeVisibleAndEnabled(this IWebDriver driver, By by)
        {
            WebDriverWait webDriverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(ExplicitTimeoutSeconds));
            IWebElement continueBtn = webDriverWait.Until(dr =>
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

        /// <summary>
        /// Creates a filter for selecting elements from multiple IDs. Uses XPath, e.g.
        /// .//*[@id='otc' or @id='code']
        /// </summary>
        public static By ByIds(params string[] ids)
        {
            string xPathSelector = string.Join(
                " or ",
                ids.Select(id => $"@id='{id}'"));

            return By.XPath($".//*[{xPathSelector}]");
        }

        public static void PerformLogin(this IWebDriver driver, LabUser user, bool withLoginHint = false)
        {
            UserInformationFieldIds fields = new UserInformationFieldIds(user);

            if (!withLoginHint)
            {
                Trace.WriteLine("Logging in ... Entering username");
                driver.FindElement(By.Id(fields.AADUsernameInputId)).SendKeys(user.Upn);

                Trace.WriteLine("Logging in ... Clicking <Next> after username");
                driver.FindElement(By.Id(fields.AADSignInButtonId)).Click();
            }

            if (user.FederationProvider == FederationProvider.AdfsV2)
            {
                Trace.WriteLine("Logging in ... AFDSv2 - Entering the username again, this time in the ADFSv2 form");
                driver.FindElement(By.Id(CoreUiTestConstants.AdfsV2WebUsernameInputId)).SendKeys(user.Upn);
            }

            Trace.WriteLine("Logging in ... Entering password");
            driver.WaitForElementToBeVisibleAndEnabled(By.Id(fields.PasswordInputId)).SendKeys(user.GetOrFetchPassword());

            Trace.WriteLine("Logging in ... Clicking next after password");
            driver.WaitForElementToBeVisibleAndEnabled(By.Id(fields.PasswordSignInButtonId)).Click();
        }
    }
}
