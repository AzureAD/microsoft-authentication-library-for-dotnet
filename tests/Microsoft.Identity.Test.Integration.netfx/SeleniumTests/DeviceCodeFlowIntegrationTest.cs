// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Kerberos;
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

        #endregion MSTest Hooks

        [RunOn(TargetFrameworks.NetFx)]
        [Timeout(2 * 60 * 1000)] // 2 min timeout
        public async Task DeviceCodeFlowTestAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await AcquireTokenWithDeviceCodeFlowAsync(labResponse, "aad user").ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [Timeout(2 * 60 * 1000)] // 2 min timeout
        [TestCategory(TestCategories.Arlington)]
        public async Task ArlingtonDeviceCodeFlowTestAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetArlingtonUserAsync().ConfigureAwait(false);
            await AcquireTokenWithDeviceCodeFlowAsync(labResponse, "aad user").ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [Timeout(2 * 60 * 1000)] // 2 min timeout
        [TestCategory(TestCategories.ADFS)]
        public async Task DeviceCodeFlowAdfsTestAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, true).ConfigureAwait(false);

            await AcquireTokenWithDeviceCodeFlowAsync(labResponse, "adfs user").ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [Timeout(2 * 60 * 1000)] // 2 min timeout
        [TestCategory(TestCategories.MSA)]
        public async Task DeviceCodeFlowMsaTestAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetMsaUserAsync().ConfigureAwait(false);

            await AcquireTokenWithDeviceCodeFlowAsync(labResponse, "msa user").ConfigureAwait(false);
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
            TestCommon.ValidateNoKerberosTicketFromAuthenticationResult(result);
        }

        #region Azure AD Kerberos Feature Tests
        [IgnoreOnOneBranch]
        [RunOn(TargetFrameworks.NetCore)]
        [Timeout(2 * 60 * 1000)] // 2 min timeout
        public async Task KerberosDeviceCodeFlowTestAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await KerberosAcquireTokenWithDeviceCodeFlowAsync(labResponse, "aad user", KerberosTicketContainer.IdToken).ConfigureAwait(false);
            await KerberosAcquireTokenWithDeviceCodeFlowAsync(labResponse, "aad user", KerberosTicketContainer.AccessToken).ConfigureAwait(false);
        }

        private async Task KerberosAcquireTokenWithDeviceCodeFlowAsync(LabResponse labResponse, string userType, KerberosTicketContainer ticketContainer)
        {
            Trace.WriteLine($"Calling KerberosAcquireTokenWithDeviceCodeFlowAsync with {0}", userType);
            var builder = PublicClientApplicationBuilder.Create(labResponse.App.AppId)
                .WithTestLogging()
                .WithTenantId(labResponse.Lab.TenantId)
                .WithClientId(TestConstants.KerberosTestApplicationId)
                .WithKerberosTicketClaim(TestConstants.KerberosServicePrincipalName, ticketContainer);

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

            KerberosSupplementalTicket ticket = TestCommon.GetValidatedKerberosTicketFromAuthenticationResult(
                result,
                ticketContainer,
                labResponse.User.Upn);
            Assert.IsNotNull(ticket);
            TestCommon.ValidateKerberosWindowsTicketCacheOperation(ticket);
        }

        #endregion
    }
}
