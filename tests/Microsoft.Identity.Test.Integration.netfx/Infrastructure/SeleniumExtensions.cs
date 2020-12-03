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

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public static class SeleniumExtensions
    {
        private static readonly TimeSpan ImplicitTimespan = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ExplicitTimespan = TimeSpan.FromSeconds(20);

        public static IWebDriver CreateDefaultWebDriver()
        {
            ChromeOptions options = new ChromeOptions();
            ChromeDriver driver;

            // ~2x faster, no visual rendering
            // remove when debugging to see the UI automation
            options.AddArguments("headless");

            var env = Environment.GetEnvironmentVariable("ChromeWebDriver");
            if (string.IsNullOrEmpty(env))
            {
                driver = new ChromeDriver(options);
            }
            else
            {
                driver = new ChromeDriver(env, options);
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

        public static void CheckElementNotPresent(this IWebDriver driver, By by, TestContext testContext, string failureMessage)
        {
            try
            {
                var el = driver.FindElement(by);
                if (el.Enabled && el.Displayed)
                {
                    driver.SaveScreenshot(testContext, failureMessage);
                    throw new InvalidOperationException(failureMessage);
                }
            }
            catch
            {
                // all good, move along
            }
        }

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

      
    }
}
