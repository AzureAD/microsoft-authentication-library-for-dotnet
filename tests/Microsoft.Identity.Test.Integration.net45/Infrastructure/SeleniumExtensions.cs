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
using Microsoft.Identity.Test.UIAutomation;
using System.Linq;
using Microsoft.Identity.Client;

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
        private static int s_picNumber = 1;

        public static void SaveScreenshot(this IWebDriver driver, TestContext testContext, string name = "failure")
        {
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            string picName = name + s_picNumber++ + ".png";
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
            Trace.WriteLine($"[Selenium UI] Waiting for {by.ToString()} to be visible and enabled");
            var webDriverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(ExplicitTimeoutSeconds));

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
            Trace.WriteLine("Finding first elements by id: " + string.Join(" ", ids));
            string xPathSelector = string.Join(
                " or ",
                ids.Select(id => $"@id='{id}'"));

            return By.XPath($".//*[{xPathSelector}]");
        }

        public static IWebElement FindElementById(this IWebDriver driver, string id)
        {
            Trace.WriteLine("Finding element by id: " + id);

            return driver.FindElement(By.Id(id));
        }

        public static void PerformLogin(this IWebDriver driver, LabUser user, Prompt prompt, bool withLoginHint = false, bool adfsOnly = false)
        {
            UserInformationFieldIds fields = new UserInformationFieldIds(user);

            if (adfsOnly && !withLoginHint)
            {
                Trace.WriteLine("Logging in ... Entering username");
                driver.FindElement(By.Id(UITestConstants.AdfsV4UsernameInputdId)).SendKeys(user.Upn);
            }
            else
            {
                if (!withLoginHint)
                {
                    Trace.WriteLine("Logging in ... Entering username");
                    driver.FindElementById(fields.AADUsernameInputId).SendKeys(user.Upn.Contains("EXT") ? user.HomeUPN : user.Upn);

                    Trace.WriteLine("Logging in ... Clicking <Next> after username");
                    driver.FindElementById(fields.AADSignInButtonId).Click();
                }

                if (user.FederationProvider == FederationProvider.AdfsV2 && user.IsFederated)
                {
                    Trace.WriteLine("Logging in ... AFDSv2 - Entering the username again, this time in the ADFSv2 form");
                    driver.FindElementById(UITestConstants.AdfsV2WebUsernameInputId).SendKeys(user.Upn);
                }
            }

            Trace.WriteLine("Logging in ... Entering password");
            driver.WaitForElementToBeVisibleAndEnabled(By.Id(fields.GetPasswordInputId())).SendKeys(user.GetOrFetchPassword());

            Trace.WriteLine("Logging in ... Clicking next after password");
            driver.WaitForElementToBeVisibleAndEnabled(By.Id(fields.GetPasswordSignInButtonId())).Click();

            if (user.HomeUPN.Contains("outlook.com"))
            {
                Trace.WriteLine("Logging in ... clicking accept prompts for outlook.com MSA user");
                driver.WaitForElementToBeVisibleAndEnabled(By.Id(UITestConstants.ConsentAcceptId)).Click();
            }

            if (prompt == Prompt.Consent)
            {
                Trace.WriteLine("Consenting...");
                driver.WaitForElementToBeVisibleAndEnabled(By.Id(fields.AADSignInButtonId)).Click();
            }
        }
    }
}
