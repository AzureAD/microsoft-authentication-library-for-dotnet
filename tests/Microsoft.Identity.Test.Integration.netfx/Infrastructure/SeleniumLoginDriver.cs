using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.UIAutomation.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public class SeleniumLoginDriver
    {
        private readonly IWebDriver _driver;
        private readonly LabUser _user;
        private readonly TestContext _testContext;
        private readonly CancellationToken _cancellationToken;
        private readonly UserInformationFieldIds _htmlFieldIds;
        private static readonly TimeSpan ShortExplicitTimespan = TimeSpan.FromSeconds(5);


        public SeleniumLoginDriver(
            IWebDriver driver,
            LabUser user,
            TestContext testContext,
            CancellationToken cancellationToken)
        {
            _driver = driver;
            _user = user;
            _testContext = testContext;
            _cancellationToken = cancellationToken;
            _htmlFieldIds = new UserInformationFieldIds(user);

        }

        public void PerformInteractiveLogin(
          Prompt prompt,
          bool withLoginHint = false,
          bool adfsOnly = false)
        {
            if (!_cancellationToken.IsCancellationRequested)
                EnterUsername(withLoginHint, adfsOnly);

            if (!_cancellationToken.IsCancellationRequested)
                EnterPassword();

            if (!_cancellationToken.IsCancellationRequested)
                HandleConsent(prompt);

            if (!_cancellationToken.IsCancellationRequested)
                HandleStaySignedIn();         
        }

        public void PerformDeviceCodeLogin(
          DeviceCodeResult deviceCodeResult,
          bool isAdfs = false)
        {
            try
            {
                Trace.WriteLine("Browser is open. Navigating to the Device Code url and entering the code");

                string codeId = isAdfs ? "userCodeInput" : "code";
                string continueId = isAdfs ? "confirmationButton" : "continueBtn";

                _driver.Navigate().GoToUrl(deviceCodeResult.VerificationUrl);
                _driver
                    // Device Code Flow web ui is undergoing A/B testing and is sometimes different - use 2 IDs
                    .FindElement(SeleniumExtensions.ByIds("otc", codeId))
                    .SendKeys(deviceCodeResult.UserCode);

                IWebElement continueBtn = _driver.WaitForElementToBeVisibleAndEnabled(
                    SeleniumExtensions.ByIds(_htmlFieldIds.AADSignInButtonId, continueId));
                continueBtn?.Click();

                PerformInteractiveLogin(Prompt.SelectAccount, false, isAdfs);
                Thread.Sleep(1000); // allow the browser to redirect

                _driver?.SaveScreenshot(_testContext, "device_code_end");

                Trace.WriteLine("Authentication complete");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Browser automation failed " + ex);
                _driver?.SaveScreenshot(_testContext);
                throw;
            }
        }

        private void EnterPassword()
        {
            Trace.WriteLine("Logging in ... Entering password");
            string password = _user.GetOrFetchPassword();
            string passwordField = _htmlFieldIds.GetPasswordInputId();
            _driver.WaitForElementToBeVisibleAndEnabled(By.Id(passwordField)).SendKeys(password);

            Trace.WriteLine("Logging in ... Clicking next after password");
            _driver.WaitForElementToBeVisibleAndEnabled(By.Id(_htmlFieldIds.GetPasswordSignInButtonId())).Click();

            Thread.Sleep(500); // allow the browser to redirect

            // the SeleniumWebUi should have stopped us by now...
            if (!_cancellationToken.IsCancellationRequested)
            {
                Trace.WriteLine("Checking that the password field is no longer present");
                _driver.CheckElementNotPresent(By.Id(passwordField), _testContext, "incorrect_password");
            }
        }

        private void EnterUsername(bool withLoginHint, bool adfsOnly)
        {
            if (adfsOnly && !withLoginHint)
            {
                Trace.WriteLine("Logging in ... Entering username");
                _driver.FindElement(By.Id(CoreUiTestConstants.AdfsV4UsernameInputdId)).SendKeys(_user.Upn);
            }
            else
            {
                if (!withLoginHint)
                {
                    Trace.WriteLine("Logging in ... Entering username");
                    _driver
                        .FindElementById(_htmlFieldIds.AADUsernameInputId)
                        .SendKeys(_user.Upn.Contains("EXT") ? _user.HomeUPN : _user.Upn);

                    Trace.WriteLine("Logging in ... Clicking <Next> after username");
                    _driver.WaitForElementToBeVisibleAndEnabled(By.Id(_htmlFieldIds.AADSignInButtonId)).Click();

                    try
                    {
                        _driver.FindElementById(_htmlFieldIds.AADSignInButtonId).Click();
                        Trace.WriteLine("Yes, workaround ok");
                    }
                    catch
                    {
                        Trace.WriteLine("No, workaround failed");
                    }
                }

                if (_user.FederationProvider == FederationProvider.AdfsV2 &&
                    (_user.UserType == UserType.Federated))
                {
                    Trace.WriteLine("Logging in ... AFDSv2 - Entering the username again, this time in the ADFSv2 form");
                    _driver.FindElementById(CoreUiTestConstants.AdfsV2WebUsernameInputId).SendKeys(_user.Upn);
                }
            }
        }

        private void HandleStaySignedIn()
        {
            try
            {
                Trace.WriteLine("Finding the Stay Signed In - Yes button");
                var yesBtn = _driver.WaitForElementToBeVisibleAndEnabled(
                    By.Id(CoreUiTestConstants.WebSubmitId));
                yesBtn?.Click();
            }
            catch
            {
                Trace.WriteLine("Stay Signed In button not found");
            }
        }

        private void HandleConsent(Prompt prompt)
        {
            // For MSA, a special consent screen seems to come up every now and then
            if (_user.Upn.Contains("outlook.com"))
            {
                try
                {
                    Trace.WriteLine("Finding accept prompt");
                    var acceptBtn = _driver.WaitForElementToBeVisibleAndEnabled(
                        SeleniumExtensions.ByIds(
                            CoreUiTestConstants.ConsentAcceptId,
                            _htmlFieldIds.AADSignInButtonId),
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
                _driver.WaitForElementToBeVisibleAndEnabled(By.Id(_htmlFieldIds.AADSignInButtonId)).Click();
            }
        }
    }
}
