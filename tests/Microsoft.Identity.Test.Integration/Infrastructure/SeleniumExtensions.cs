using OpenQA.Selenium;
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Core.UIAutomation;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public static class SeleniumExtensions
    {
        public static IWebDriver CreateDefaultWebDriver()
        {
            ChromeOptions options = new ChromeOptions();

            // ~2x faster, no visual rendering
            // remove when debugging to see the UI automation
             options.AddArguments("headless");

            var driver = new ChromeDriver(options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

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
            WebDriverWait webDriverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
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


        public static void PerformLogin(this IWebDriver driver, LabUser user)
        {
            UserInformationFieldIds fields = new UserInformationFieldIds(user);

            Trace.WriteLine("Logging in ... Entering username");
            driver.FindElement(By.Id(CoreUiTestConstants.WebUPNInputID)).SendKeys(user.Upn);

            Trace.WriteLine("Logging in ... Clicking next after username");
            driver.FindElement(By.Id(fields.SignInButtonId)).Click();

            Trace.WriteLine("Logging in ... Entering password");
            driver.WaitForElementToBeVisibleAndEnabled(By.Id(fields.PasswordInputId)).SendKeys(user.Password);

            Trace.WriteLine("Logging in ... Clicking next after password");
            driver.WaitForElementToBeVisibleAndEnabled(By.Id(fields.SignInButtonId)).Click();
        }
    }
}
