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
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    [TestClass]
    public class InteractiveFlowTests
    {
        private readonly TimeSpan _seleniumTimeout = TimeSpan.FromMinutes(2);
        private static readonly string[] _scopes = new[] { "user.read" };

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
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV3_NotFederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = false
            };


            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV3_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };


            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV2_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV2,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };


            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV4_NotFederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = false
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV4_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        private async Task RunTestForUserAsync(LabResponse labResponse)
        {
            InitTestInfra(labResponse);

            var pca = new PublicClientApplication(labResponse.AppId);

            // tests need to use http://localhost:port so that we can capture the AT
            pca.RedirectUri = SeleniumWebUIFactory.FindFreeLocalhostRedirectUri();

            Trace.WriteLine("Part 1 - Acquire a token interactively, no login hint");
            AuthenticationResult result = await pca.AcquireTokenAsync(_scopes).ConfigureAwait(false);
            IAccount account = await AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

            Trace.WriteLine("Part 2 - Clear the cache");
            await pca.RemoveAsync(account).ConfigureAwait(false);
            Assert.IsFalse((await pca.GetAccountsAsync().ConfigureAwait(false)).Any());

            Trace.WriteLine("Part 3 - Acquire a token interactively again, with login hint");
            InitTestInfra(labResponse, withLoginHint: true);
            result = await pca.AcquireTokenAsync(_scopes, labResponse.User.HomeUPN).ConfigureAwait(false);
            account = await AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

            Trace.WriteLine("Part 4 - Acquire a token silently");
            result = await pca.AcquireTokenSilentAsync(_scopes, account).ConfigureAwait(false);
            account = await AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            await AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
        }

        private static async Task<IAccount> AssertSingleAccountAsync(
            LabResponse labResponse, 
            PublicClientApplication pca, 
            AuthenticationResult result)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AccessToken));
            var account = (await pca.GetAccountsAsync().ConfigureAwait(false)).Single();
            Assert.AreEqual(labResponse.User.HomeUPN, account.Username);

            return account;
        }

        private void InitTestInfra(LabResponse labResponse, bool withLoginHint = false)
        {
            Action<IWebDriver> seleniumLogic = (driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                driver.PerformLogin(labResponse.User, withLoginHint);
            };

            SeleniumWebUIFactory webUIFactory = new SeleniumWebUIFactory(seleniumLogic, _seleniumTimeout);
            PlatformProxyFactory.GetPlatformProxy().SetWebUiFactory(webUIFactory);
        }
    }

}
