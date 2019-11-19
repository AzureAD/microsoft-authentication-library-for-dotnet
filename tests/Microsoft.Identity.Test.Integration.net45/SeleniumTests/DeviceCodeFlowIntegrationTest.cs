// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    // Important: do not install a NuGet package with the Chrome driver as it is a security risk.
    // Instead, install the Chrome driver on the test machine

    // Note: these tests require permission to a KeyVault Microsoft account;
    // Please ignore them if you are not a Microsoft FTE, they will run as part of the CI build
    [TestClass]
    [TestCategory(TestCategories.Selenium)]
    [TestCategory(TestCategories.LabAccess)]
    public class DeviceCodeFlow
    {
        private static readonly string[] s_scopes = { "User.Read" };

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

        #endregion MSTest Hooks

        [TestMethod]
        [Timeout(2 * 60 * 1000)] // 2 min timeout
        public async Task DeviceCodeFlowTestAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            Trace.WriteLine("Calling AcquireTokenWithDeviceCodeAsync");
            var pca = PublicClientApplicationBuilder.Create(labResponse.App.AppId).Build();
            var userCacheAccess = pca.UserTokenCache.RecordAccess();

            var result = await pca.AcquireTokenWithDeviceCode(s_scopes, deviceCodeResult =>
            {
                SeleniumExtensions.PerformDeviceCodeLogin(deviceCodeResult, labResponse.User, TestContext, false);
                return Task.FromResult(0);
            }).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Trace.WriteLine("Running asserts");

            userCacheAccess.AssertAccessCounts(0, 1);
            Assert.IsFalse(userCacheAccess.LastNotificationArgs.IsApplicationCache);

            Assert.IsNotNull(result);
            Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)] // 2 min timeout
        public async Task DeviceCodeFlowAdfsTestAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, true).ConfigureAwait(false);

            Trace.WriteLine("Calling AcquireTokenWithDeviceCodeAsync");
            PublicClientApplication pca = PublicClientApplicationBuilder.Create(Adfs2019LabConstants.PublicClientId)
                                                                        .WithRedirectUri(Adfs2019LabConstants.ClientRedirectUri)
                                                                        .WithAdfsAuthority(Adfs2019LabConstants.Authority)
                                                                        .BuildConcrete();
            var result = await pca.AcquireTokenWithDeviceCode(s_scopes, deviceCodeResult =>
            {
                SeleniumExtensions.PerformDeviceCodeLogin(deviceCodeResult, labResponse.User, TestContext, true);
                return Task.FromResult(0);
            }).ExecuteAsync().ConfigureAwait(false);

            Trace.WriteLine("Running asserts");

            Assert.IsNotNull(result);
            Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));
        }
    }
}
