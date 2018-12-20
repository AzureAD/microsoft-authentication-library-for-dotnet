using OpenQA.Selenium;
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;

namespace Microsoft.Identity.Test.Unit
{
    public class SeleniumWrapper : IDisposable
    {
        public SeleniumWrapper()
        {
            ChromeOptions chromeOptions = new ChromeOptions();

            // ~2x faster, no visual rendering
            // remove when debugging to see the UI automation
            chromeOptions.AddArguments("headless");

            Driver = new ChromeDriver(chromeOptions);
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }

        public IWebDriver Driver { get; }

        private int _picNumber = 1;

        public void SaveScreenshot(TestContext testContext, string name = "failure")
        {
            Screenshot ss = ((ITakesScreenshot)Driver).GetScreenshot();
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

        public IWebElement WaitForElementToBeVisibleAndEnabled(By by)
        {
            WebDriverWait webDriverWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            IWebElement continueBtn = webDriverWait.Until(dr =>
            {
                try
                {
                    var elementToBeDisplayed = Driver.FindElement(by);
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

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    Driver?.Dispose();
                }

                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
