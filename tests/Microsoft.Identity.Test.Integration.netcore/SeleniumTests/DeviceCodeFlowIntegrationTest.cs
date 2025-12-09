// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.Utils;
using Microsoft.Identity.Test.LabInfrastructure;
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

        #endregion MSTest Hooks

        [TestMethod]
        [Timeout(2 * 60 * 1000)] // 2 min timeout
        public async Task DeviceCodeFlowTestAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserWithMultiTenantAppAsync().ConfigureAwait(false);
            await AcquireTokenWithDeviceCodeFlowAsync(labResponse, "aad user").ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)] // 2 min timeout
        public async Task SilentTokenAfterDeviceCodeFlowWithBrokerTestAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserWithMultiTenantAppAsync().ConfigureAwait(false);
            await AcquireTokenSilentAfterDeviceCodeFlowWithBrokerAsync(labResponse, "aad user").ConfigureAwait(false);
        }

        private async Task AcquireTokenWithDeviceCodeFlowAsync(LabResponse labResponse, string userType)
        {
            Trace.WriteLine($"Calling AcquireTokenWithDeviceCodeAsync with {0}", userType);
            var builder = PublicClientApplicationBuilder.Create(labResponse.App.AppId).WithTestLogging();

            switch (labResponse.User.AzureEnvironment)
            {
                case AzureEnvironment.azureusgovernment:
                    builder.WithAuthority(labResponse.Lab.Authority + labResponse.Lab.TenantId);
                    break;
                default:
                    break;
            }

            var pca = builder.Build();
            var userCacheAccess = pca.UserTokenCache.RecordAccess();

            var result = await pca.AcquireTokenWithDeviceCode(s_scopes, deviceCodeResult =>
            {
                SeleniumExtensions.PerformDeviceCodeLogin(deviceCodeResult, labResponse.User, TestContext, false);
                return Task.FromResult(0);
            }).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Trace.WriteLine("Running asserts");

            userCacheAccess.AssertAccessCounts(0, 1);
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);

            Assert.IsNotNull(result);
            Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));
        }

        private async Task AcquireTokenSilentAfterDeviceCodeFlowWithBrokerAsync(LabResponse labResponse, string userType)
        {
            Trace.WriteLine($"Calling AcquireTokenSilentAfterDeviceCodeFlowWithBrokerAsync with {0}", userType);
            BrokerOptions options = TestUtils.GetPlatformBroker();
            var builder = PublicClientApplicationBuilder.Create(labResponse.App.AppId).WithTestLogging().WithBroker(options);

            switch (labResponse.User.AzureEnvironment)
            {
                case AzureEnvironment.azureusgovernment:
                    builder.WithAuthority(labResponse.Lab.Authority + labResponse.Lab.TenantId);
                    break;
                default:
                    break;
            }

            var pca = builder.Build();
            var userCacheAccess = pca.UserTokenCache.RecordAccess();

            var result = await pca.AcquireTokenWithDeviceCode(s_scopes, deviceCodeResult =>
            {
                SeleniumExtensions.PerformDeviceCodeLogin(deviceCodeResult, labResponse.User, TestContext, false);
                return Task.FromResult(0);
            }).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Trace.WriteLine("Running asserts");

            userCacheAccess.AssertAccessCounts(0, 1);
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);

            Assert.IsNotNull(result);
            var account = result.Account as Account;
            Assert.AreEqual("device_code_flow", account.AccountSource);
            Assert.IsTrue(!string.IsNullOrEmpty(result.AccessToken));

            var silentTokenResult = await pca.AcquireTokenSilent(s_scopes, result.Account).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(silentTokenResult);
            Assert.IsTrue(!string.IsNullOrEmpty(silentTokenResult.AccessToken));
        }
    }
}
