using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    [TestClass]
    public class InteractiveFlowTests
    {
        private readonly TimeSpan _seleniumTimeout = TimeSpan.FromMinutes(2);

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
            TestCommon.ResetStateAndInitMsal();
        }

        #endregion

        [TestMethod]
        public async Task InteractiveAuth_DefaultUserAsync()
        {
            // Arrange
            LabResponse labResponse = LabUserHelper.GetDefaultUser();
            LabUser user = labResponse.User;

            Action<IWebDriver> seleniumLogic = (driver) =>
             {
                 Trace.WriteLine("Starting Selenium automation");
                 driver.PerformLogin(user);
             };

            SeleniumWebUIFactory webUIFactory = new SeleniumWebUIFactory(seleniumLogic, _seleniumTimeout);

            // TODO: use the lab app once localhost is setup as a redirect uri
            PublicClientApplication pca = PublicClientApplicationBuilder.Create("1d18b3b0-251b-4714-a02a-9956cec86c2d").BuildConcrete();
            pca.ServiceBundle.PlatformProxy.SetWebUiFactory(webUIFactory);

            // tests need to use http://localhost:port so that we can capture the AT
            pca.RedirectUri = SeleniumWebUIFactory.FindFreeLocalhostRedirectUri();
            AuthenticationResult result = null;

            // Act
            result = await pca.AcquireTokenAsync(new[] { "user.read" }).ConfigureAwait(false);

            // Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AccessToken));
        }
    }

}
