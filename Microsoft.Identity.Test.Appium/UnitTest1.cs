
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.MultiTouch;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Service.Options;
using OpenQA.Selenium.Support.PageObjects;
using OpenQA.Selenium.Support.UI;

namespace Microsoft.Identity.Test.Appium
{
    [TestClass]
    public class UnitTest1
    {
        private const string ApkPath =
@"C:\Users\bogavril\AppData\Local\Xamarin\Mono for Android\Archives\2020-03-04\AppiumAutomation.Android 3-04-20 3.03 PM.apkarchive\com.companyname.appiumautomation.apk";
        private const string ChromeDriverPath = @"c:\g\tools\chromedriver.exe";


        private static AndroidDriver<AppiumWebElement> _driver;
        private static AppiumLocalService _appiumLocalService;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var options = new OptionCollector().AddArguments(AndroidOptionList.ChromeDriverExecutable(ChromeDriverPath));
            _appiumLocalService = new AppiumServiceBuilder()
                //.WithArguments((new OptionCollector()).AddArguments()
                .WithArguments(options)
                .UsingAnyFreePort()
                .Build();
            _appiumLocalService.Start();
            var appiumOptions = new AppiumOptions();
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.DeviceName, "Android_Accelerated_x86_Oreo");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.PlatformName, "Android");
            //appiumOptions.AddAdditionalCapability(MobileCapabilityType.PlatformVersion, "7.1");
            //appiumOptions.AddAdditionalCapability(MobileCapabilityType.BrowserName, "Chrome");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.App, ApkPath);
            //appiumOptions.AddAdditionalCapability("appActivity", "MainActivity");
            //appiumOptions.AddAdditionalCapability("appPackage", "AppiumAutomation.Android");
            //appiumOptions.AddAdditionalCapability("appWaitDuration", "40000");
            //appiumOptions.AddAdditionalCapability("appActivity", "MainActivity");
            //appiumOptions.AddAdditionalCapability("fullReset", "true");

            _driver = new AndroidDriver<AppiumWebElement>(_appiumLocalService, appiumOptions);
        }
      

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _appiumLocalService.Dispose();
        }

        [TestMethod]
        public async Task Android_AAD_SystemWebView_Async()
        {
            AppiumWebElement testPicker = _driver.FindElementByAccessibilityId("uiTestPicker");
            testPicker.Click();
            //var test = _driver.FindElementByAccessibilityId("Acquire Token Interactive");
            //test.Click();

            //var runButton = _driver.FindElementByAccessibilityId("runTestButton"); //runTestButton ?
            //runButton.Click();

            // TODO figure out how to select test


            var webContext = GetWebContextName();
            SwitchContext(webContext);

            try
            {
                var x = _driver.FindElementsByXPath("//input").Where(y => y.Enabled && y.Displayed);

                AppiumWebElement usernameBox = _driver.FindElementByXPath("//input[@id='i0116']"); // username
                usernameBox.Click();
                usernameBox.SendKeys("liu.kang@bogavrilltd.onmicrosoft.com");  

                var nextBtn = _driver.FindElementByXPath("//input[@id='idSIButton9']");
                nextBtn.Click();

                var passwordBox = _driver.FindElementByXPath("//input[@id='i0118']"); // password
                passwordBox.Click();
                passwordBox.SendKeys("todo");

                nextBtn = _driver.FindElementByXPath("//input[@id='idSIButton9']");
                nextBtn.Click();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Switches the context
        /// </summary>
        public void SwitchContext(string contextName)
        {
            Console.WriteLine($"DeviceSession: Switching driver context to {contextName}");

            try
            {
                _driver.Context = contextName;
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeviceSession: Context switch failed", ex);
                Console.WriteLine("DeviceSession: Contexts available: " + string.Join(",", _driver.Contexts));
                throw;
            }

            Console.WriteLine("DeviceSession: Context switch successful");
        }

        private string GetWebContextName()
        {
            Console.WriteLine("DeviceSession: Searching for web contexts...");

            // Either due to poor device/host performance, or a slow network, it can sometimes take
            // some time for the webview context to be fully available.
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));

            var webContexts = wait.Until(d =>
            {
                var ctxs = _driver.Contexts.Where(c => c.StartsWith("WEBVIEW_")).ToArray();
                return ctxs.Any() ? ctxs : null;
            });

            Console.WriteLine($"DeviceSession: Available web contexts: {string.Join(", ", webContexts)}");

            return webContexts.Single();
        }

    }
}
