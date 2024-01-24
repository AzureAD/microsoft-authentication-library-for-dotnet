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
using Microsoft.Identity.Client;
using System.Threading;
using OpenQA.Selenium.Edge;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public static class SeleniumExtensions
    {
        private static readonly TimeSpan ImplicitTimespan = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ExplicitTimespan = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan ShortExplicitTimespan = TimeSpan.FromSeconds(5);

        public static IWebDriver CreateDefaultWebDriver()
        {
            EdgeOptions options = new EdgeOptions();
            EdgeDriver driver;

            // ~2x faster, no visual rendering
            // remove when debugging to see the UI automation
            //options.AddArguments("headless");

            var env = Environment.GetEnvironmentVariable("EDGEWEBDRIVER");
            if (string.IsNullOrEmpty(env))
            {
                driver = new EdgeDriver(options);
            }
            else
            {
                driver = new EdgeDriver(env, options);
            }

            driver.Manage().Timeouts().ImplicitWait = ImplicitTimespan;
            driver.Manage().Window.Maximize();

            return driver;
        }

        #region ScreenShot support
        private static int s_picNumber = 1;

        public static void SaveScreenshot(this IWebDriver driver, TestContext testContext, string name = "failure")
        {
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            string picName = name + s_picNumber++ + ".png";
#if NETFRAMEWORK // Can't attach a file on netcore because mstest doesn't support it
            string failurePicturePath = Path.Combine(testContext.TestResultsDirectory, picName);
#else
            string failurePicturePath = Path.Combine(Directory.GetCurrentDirectory(), picName);
#endif

            Trace.WriteLine($"Saving picture to {failurePicturePath}");
            ss.SaveAsFile(failurePicturePath, ScreenshotImageFormat.Png);

#if NETFRAMEWORK // Can't attach a file to the logs on netcore because mstest doesn't support it
            testContext.AddResultFile(failurePicturePath);
#endif
        }

        public static void LogAllUiElements(this IWebDriver driver)
        {
            Trace.WriteLine("===== HTML elements Begin =====");

            driver.FindElements(By.XPath("//*[@id]")).ToList().ForEach(el =>
            {
                try
                {
                    Trace.WriteLine($"Element " +
                        $"id: {el.GetAttribute("id")} " +
                        $"type: {el.TagName} " +
                        $"text: {el.Text} " +
                        $"displayed: { el.Displayed} " +
                        $"enabled: { el.Enabled} ");
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Failed to get details about an element." +
                        " This can happen if an element becomes stale. " + e.Message);
                }
            });

            Trace.WriteLine("===== HTML elements End =====");
        }

        #endregion

        public static IWebElement WaitForElementToBeVisibleAndEnabled(
            this IWebDriver driver,
            By by,
            TimeSpan waitTime = default,
            bool ignoreFailures = false)
        {
            Trace.WriteLine($"[Selenium UI] Waiting for {by.ToString()} to be visible and enabled");
            var webDriverWait = new WebDriverWait(
                driver,
                waitTime != default ? waitTime : ExplicitTimespan);

            try
            {
                IWebElement element = webDriverWait.Until(dr =>
                {
                    try
                    {
                        var elementToBeDisplayed = driver.FindElement(by);

                        if (elementToBeDisplayed.Displayed && elementToBeDisplayed.Enabled)
                        {
                            Trace.WriteLine($"[Selenium UI][DEBUG] Element {by.ToString()} found and is visible");
                            // If element is not view, Selenium can't click on it
                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                            js.ExecuteScript("arguments[0].scrollIntoView(true);", elementToBeDisplayed);
                            return elementToBeDisplayed;
                        }

                        Trace.WriteLine($"[Selenium UI][DEBUG] Element {by.ToString()} found but Displayed={elementToBeDisplayed.Displayed} Enabled={elementToBeDisplayed.Enabled}");

                        return null;
                    }
                    catch (StaleElementReferenceException)
                    {
                        Trace.WriteLine($"[Selenium UI][DEBUG] {by.ToString()} is stale");
                        return null;
                    }
                    catch (NoSuchElementException)
                    {
                        Trace.WriteLine($"[Selenium UI][DEBUG] {by.ToString()} not found");
                        return null;
                    }
                });

                return element;
            }
            catch (WebDriverTimeoutException)
            {
                Trace.WriteLine($"[Selenium UI] Element {by.ToString()} has not been found");
                if (ignoreFailures)
                {
                    return null;
                }

                driver.LogAllUiElements();
                throw;
            }
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

            EnterUsername(driver, user, withLoginHint, adfsOnly, fields);
            EnterPassword(driver, user, fields);

            HandleConsent(driver, user, fields, prompt);
            HandleStaySignedIn(driver);
        }

        private static void HandleStaySignedIn(IWebDriver driver)
        {
            try
            {
                Trace.WriteLine("Finding the Stay Signed In - Yes button");
                var yesBtn = driver.WaitForElementToBeVisibleAndEnabled(
                    By.Id(CoreUiTestConstants.WebSubmitId));
                yesBtn?.Click();
            }
            catch
            {
                Trace.WriteLine("Stay Signed In button not found");
            }
        }

        private static void HandleConsent(IWebDriver driver, LabUser user, UserInformationFieldIds fields, Prompt prompt)
        {
            // For MSA, a special consent screen seems to come up every now and then
            if (user.Upn.Contains("outlook.com"))
            {
                try
                {
                    Trace.WriteLine("Finding accept prompt");
                    var acceptBtn = driver.WaitForElementToBeVisibleAndEnabled(
                        ByIds(CoreUiTestConstants.ConsentAcceptId, fields.AADSignInButtonId),
                        waitTime: ShortExplicitTimespan,
                        ignoreFailures: true);
                    acceptBtn?.Click();
                }
                catch
                {
                    Trace.WriteLine("No accept prompt found accept prompt");
                }
            }

            if (prompt == Prompt.Consent)
            {
                Trace.WriteLine("Consenting...");
                driver.WaitForElementToBeVisibleAndEnabled(By.Id(fields.AADSignInButtonId)).Click();
            }
        }

        private static void EnterPassword(IWebDriver driver, LabUser user, UserInformationFieldIds fields)
        {
            Trace.WriteLine("Logging in ... Entering password");
            string password = user.GetOrFetchPassword();
            string passwordField = fields.GetPasswordInputId();
            driver.WaitForElementToBeVisibleAndEnabled(By.Id(passwordField)).SendKeys(password);

            Trace.WriteLine("Logging in ... Clicking next after password");
            driver.WaitForElementToBeVisibleAndEnabled(By.Id(fields.GetPasswordSignInButtonId())).Click();
        }

        private static void EnterUsername(IWebDriver driver, LabUser user, bool withLoginHint, bool adfsOnly, UserInformationFieldIds fields)
        {
            if (adfsOnly && !withLoginHint)
            {
                Trace.WriteLine("Logging in ... Entering username");
                driver.FindElement(By.Id(CoreUiTestConstants.AdfsV4UsernameInputdId)).SendKeys(user.Upn);
            }
            else
            {
                if (!withLoginHint)
                {
                    Trace.WriteLine("Logging in ... Entering username");
                    driver.FindElementById(fields.AADUsernameInputId).SendKeys(user.Upn.Contains("EXT") ? user.HomeUPN : user.Upn);

                    Trace.WriteLine("Logging in ... Clicking <Next> after username");
                    driver.WaitForElementToBeVisibleAndEnabled(By.Id(fields.AADSignInButtonId)).Click();

                    try
                    {
                        driver.FindElementById(fields.AADSignInButtonId).Click();
                        Trace.WriteLine("Yes, workaround ok");
                    }
                    catch
                    {
                        Trace.WriteLine("No, workaround failed");
                    }
                }               
            }
        }

        public static void PerformDeviceCodeLogin(
            DeviceCodeResult deviceCodeResult,
            LabUser user,
            TestContext testContext,
            bool isAdfs = false)
        {
            using (var seleniumDriver = CreateDefaultWebDriver())
            {
                try
                {
                    var fields = new UserInformationFieldIds(user);

                    Trace.WriteLine("Browser is open. Navigating to the Device Code url and entering the code");

                    string codeId = isAdfs ? "userCodeInput" : "code";
                    string continueId = isAdfs ? "confirmationButton" : "continueBtn";

                    seleniumDriver.Navigate().GoToUrl(deviceCodeResult.VerificationUrl);
                    seleniumDriver
                        // Device Code Flow web ui is undergoing A/B testing and is sometimes different - use 2 IDs
                        .FindElement(SeleniumExtensions.ByIds("otc", codeId))
                        .SendKeys(deviceCodeResult.UserCode);

                    IWebElement continueBtn = seleniumDriver.WaitForElementToBeVisibleAndEnabled(
                        SeleniumExtensions.ByIds(fields.AADSignInButtonId, continueId));
                    continueBtn?.Click();

                    seleniumDriver.PerformLogin(user, Prompt.SelectAccount, false, isAdfs);
                    Thread.Sleep(1000); // allow the browser to redirect

                    seleniumDriver?.SaveScreenshot(testContext, "device_code_end");

                    Trace.WriteLine("Authentication complete");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Browser automation failed " + ex);
                    seleniumDriver?.SaveScreenshot(testContext);
                    throw;
                }
            }

        }
    }
}
