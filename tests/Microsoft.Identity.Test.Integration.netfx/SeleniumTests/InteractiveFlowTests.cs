// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Kerberos;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.UIAutomation.Infrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    [TestClass]
    public partial class InteractiveFlowTests
    {
        private readonly TimeSpan _interactiveAuthTimeout = TimeSpan.FromMinutes(5);
        private static readonly string[] s_scopes = new[] { "user.read" };

        #region MSTest Hooks

        /// <summary>
        /// Initialized by MSTest (do not make private or readonly)
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        #endregion MSTest Hooks

        [RunOn(TargetFrameworks.NetFx)]
        public async Task Interactive_AADAsync()
        {
            // Arrange
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            var result = await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.Arlington)]
        public async Task Arlington_Interactive_AADAsync()
        {
            // Arrange
            LabResponse labResponse = await LabUserHelper.GetArlingtonUserAsync().ConfigureAwait(false);
            await RunTestForUserAsync(labResponse, false).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.MSA)]
        public async Task Interactive_MsaUser_Async()
        {
            // Arrange
            LabResponse labResponse = await LabUserHelper.GetMsaUserAsync().ConfigureAwait(false);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task Interactive_AdfsV4_FederatedAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, true).ConfigureAwait(false);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task InteractiveConsentPromptAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            await RunPromptTestForUserAsync(labResponse, Prompt.Consent, true).ConfigureAwait(false);
            await RunPromptTestForUserAsync(labResponse, Prompt.Consent, false).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task Interactive_AdfsV2019_FederatedAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, true).ConfigureAwait(false);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.Arlington)]
        public async Task Arlington_Interactive_AdfsV2019_FederatedAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetArlingtonADFSUserAsync().ConfigureAwait(false);
            await RunTestForUserAsync(labResponse, false).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task Interactive_Arlington_MultiCloudSupport_AADAsync()
        {
            // Arrange
            LabResponse labResponse = await LabUserHelper.GetArlingtonUserAsync().ConfigureAwait(false);
            IPublicClientApplication pca = PublicClientApplicationBuilder
                    .Create(labResponse.App.AppId)
                    .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
                    .WithAuthority("https://login.microsoftonline.com/common")
                    .WithMultiCloudSupport(true)
                    .WithTestLogging()
                    .Build();

            Trace.WriteLine("Part 1 - Acquire a token interactively");
            AuthenticationResult result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.SelectAccount, false))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
            Assert.IsNotNull(result.Account.GetTenantProfiles());
            Assert.IsTrue(result.Account.GetTenantProfiles().Any());
            Assert.AreEqual(labResponse.User.Upn, result.Account.Username);
            Assert.IsTrue(labResponse.Lab.Authority.Contains(result.Account.Environment));

            Trace.WriteLine("Part 2 - Get Accounts");
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Assert.IsNotNull(accounts);
            Assert.IsNotNull(accounts.Single());

            var account = accounts.Single();

            Assert.IsNotNull(account.GetTenantProfiles());
            Assert.IsTrue(account.GetTenantProfiles().Any());
            Assert.AreEqual(labResponse.User.Upn, account.Username);
            Assert.AreEqual("login.microsoftonline.us", account.Environment);

            Trace.WriteLine("Part 3 - Acquire a token silently");
            result = await pca
                .AcquireTokenSilent(s_scopes, result.Account)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
            Assert.IsNotNull(result.Account.GetTenantProfiles());
            Assert.IsTrue(result.Account.GetTenantProfiles().Any());
            Assert.IsTrue(labResponse.Lab.Authority.Contains(result.Account.Environment));
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.ADFS)]
        public async Task Interactive_AdfsV2019_DirectAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, true).ConfigureAwait(false);
            await RunTestForUserAsync(labResponse, true).ConfigureAwait(false);
        }      

        [RunOn(TargetFrameworks.NetCore)]
        public async Task ValidateCcsHeadersForInteractiveAuthCodeFlowAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            var pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithDefaultRedirectUri()
               .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
               .WithTestLogging(out HttpSnifferClientFactory factory)
               .Build();

            AuthenticationResult authResult = await pca
               .AcquireTokenInteractive(s_scopes)
               .WithPrompt(Prompt.SelectAccount)
               .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.SelectAccount))
               .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
               .ConfigureAwait(false);

            var CcsHeader = TestCommon.GetCcsHeaderFromSnifferFactory(factory);
            var userObjectId = labResponse.User.ObjectId;
            var userTenantID = labResponse.User.TenantId;
            Assert.AreEqual($"x-anchormailbox:oid:{userObjectId}@{userTenantID}", $"{CcsHeader.Key}:{CcsHeader.Value.FirstOrDefault()}");

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
        }

        private async Task<AuthenticationResult> RunTestForUserAsync(LabResponse labResponse, bool directToAdfs = false)
        {
            HttpSnifferClientFactory factory = null;
            IPublicClientApplication pca;
            if (directToAdfs)
            {
                pca = PublicClientApplicationBuilder
                    .Create(Adfs2019LabConstants.PublicClientId)
                    .WithRedirectUri(Adfs2019LabConstants.ClientRedirectUri)
                    .WithAdfsAuthority(Adfs2019LabConstants.Authority)
                    .WithTestLogging()
                    .Build();
            }
            else
            {
                pca = PublicClientApplicationBuilder
                    .Create(labResponse.App.AppId)
                    .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
                    .WithAuthority(labResponse.Lab.Authority + "common")
                    .WithTestLogging(out factory)
                    .Build();
            }

            var userCacheAccess = pca.UserTokenCache.RecordAccess();

            Trace.WriteLine("Part 1 - Acquire a token interactively, no login hint");
            AuthenticationResult result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.SelectAccount, false, directToAdfs))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.IsTrue(result.AuthenticationResultMetadata.DurationInHttpInMs > 0);
            TestCommon.ValidateNoKerberosTicketFromAuthenticationResult(result);

            userCacheAccess.AssertAccessCounts(0, 1);
            IAccount account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

            userCacheAccess.AssertAccessCounts(1, 1); // the assert calls GetAccounts
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);

            Trace.WriteLine("Part 2 - Clear the cache");
            await pca.RemoveAsync(account).ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(1, 2);
            Assert.IsFalse((await pca.GetAccountsAsync().ConfigureAwait(false)).Any());
            userCacheAccess.AssertAccessCounts(2, 2);
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);

            if (factory?.RequestsAndResponses != null)
            {
                factory.RequestsAndResponses.Clear();
            }

            Trace.WriteLine("Part 3 - Acquire a token interactively again, with login hint");
            result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.ForceLogin, true, directToAdfs))
                .WithPrompt(Prompt.ForceLogin)
                .WithLoginHint(labResponse.User.Upn)
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(2, 3);
            AssertCcsRoutingInformationIsSent(factory, labResponse);
            TestCommon.ValidateNoKerberosTicketFromAuthenticationResult(result);

            account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(3, 3);
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);

            if (factory?.RequestsAndResponses != null)
            {
                factory.RequestsAndResponses.Clear();
            }

            Trace.WriteLine("Part 4 - Acquire a token silently");
            result = await pca
                .AcquireTokenSilent(s_scopes, account)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            
            TestCommon.ValidateNoKerberosTicketFromAuthenticationResult(result);

            Trace.WriteLine("Part 5 - Acquire a token silently with force refresh");
            result = await pca
                .AcquireTokenSilent(s_scopes, account)
                .WithForceRefresh(true)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);
            AssertCcsRoutingInformationIsSent(factory, labResponse);
            TestCommon.ValidateNoKerberosTicketFromAuthenticationResult(result);

            return result;
        }

        private void AssertCcsRoutingInformationIsSent(HttpSnifferClientFactory factory, LabResponse labResponse)
        {
            if (labResponse.User.FederationProvider != FederationProvider.None)
            {
                return;
            }

            var CcsHeader = TestCommon.GetCcsHeaderFromSnifferFactory(factory);

            if (!String.IsNullOrEmpty(CcsHeader.Value?.FirstOrDefault()))
            {
                ValidateCcsHeader(CcsHeader, labResponse);
            }
        }

        private void ValidateCcsHeader(KeyValuePair<string, IEnumerable<string>> CcsHeader, LabResponse labResponse)
        {
            var ccsHeaderValue = CcsHeader.Value.FirstOrDefault();
            if (ccsHeaderValue.Contains("upn"))
            {
                Assert.AreEqual($"X-AnchorMailbox:UPN:{labResponse.User.Upn}", $"{CcsHeader.Key}:{ccsHeaderValue}");
            }
            else
            {
                var userObjectId = labResponse.User.ObjectId;
                var userTenantID = labResponse.User.TenantId;
                Assert.AreEqual($"X-AnchorMailbox:Oid:{userObjectId}@{userTenantID}", $"{CcsHeader.Key}:{ccsHeaderValue}");
            }
        }

        private async Task RunPromptTestForUserAsync(LabResponse labResponse, Prompt prompt, bool useLoginHint)
        {
            var pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithDefaultRedirectUri()
               .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
               .WithTestLogging()
               .Build();

            AcquireTokenInteractiveParameterBuilder builder = pca
               .AcquireTokenInteractive(s_scopes)
               .WithPrompt(prompt)
               .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, prompt, useLoginHint));

            if (useLoginHint)
            {
                builder = builder.WithLoginHint(labResponse.User.Upn);
            }

            AuthenticationResult result = await builder
               .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
               .ConfigureAwait(false);

            await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            TestCommon.ValidateNoKerberosTicketFromAuthenticationResult(result);
        }

        private SeleniumWebUI CreateSeleniumCustomWebUI(LabUser user, Prompt prompt, bool withLoginHint = false, bool adfsOnly = false)
        {
            return new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                driver.PerformLogin(user, prompt, withLoginHint, adfsOnly);
            }, TestContext);
        }

        #region Azure AD Kerberos Feature Tests
        [IgnoreOnOneBranch]
        public async Task Kerberos_Interactive_AADAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await KerberosRunTestForUserAsync(labResponse, KerberosTicketContainer.IdToken).ConfigureAwait(false);
            await KerberosRunTestForUserAsync(labResponse, KerberosTicketContainer.AccessToken).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> KerberosRunTestForUserAsync(
            LabResponse labResponse,
            KerberosTicketContainer ticketContainer)
        {
            IPublicClientApplication pca = PublicClientApplicationBuilder
                    .Create(labResponse.App.AppId)
                    .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
                    .WithAuthority(labResponse.Lab.Authority + "common")
                    .WithTestLogging(out HttpSnifferClientFactory factory)
                    .WithTenantId(labResponse.Lab.TenantId)
                    .WithClientId(TestConstants.KerberosTestApplicationId)
                    .WithKerberosTicketClaim(TestConstants.KerberosServicePrincipalName, ticketContainer)
                    .Build();

            var userCacheAccess = pca.UserTokenCache.RecordAccess();

            Trace.WriteLine("Part 1 - Acquire a token interactively, no login hint");
            AuthenticationResult result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.SelectAccount, false, false))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.IsTrue(result.AuthenticationResultMetadata.DurationInHttpInMs > 0);

            KerberosSupplementalTicket ticket = TestCommon.GetValidatedKerberosTicketFromAuthenticationResult(
                result,
                ticketContainer,
                labResponse.User.Upn);
            Assert.IsNotNull(ticket);

            userCacheAccess.AssertAccessCounts(0, 1);
            IAccount account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(1, 1); // the assert calls GetAccounts
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);

            Trace.WriteLine("Part 2 - Clear the cache");
            await pca.RemoveAsync(account).ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(1, 2);
            Assert.IsFalse((await pca.GetAccountsAsync().ConfigureAwait(false)).Any());
            userCacheAccess.AssertAccessCounts(2, 2);
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);

            if (factory?.RequestsAndResponses != null)
            {
                factory.RequestsAndResponses.Clear();
            }

            Trace.WriteLine("Part 3 - Acquire a token interactively again, with login hint");
            result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.ForceLogin, true, false))
                .WithPrompt(Prompt.ForceLogin)
                .WithLoginHint(labResponse.User.Upn)
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(2, 3);
            AssertCcsRoutingInformationIsSent(factory, labResponse);
            ticket = TestCommon.GetValidatedKerberosTicketFromAuthenticationResult(
                result,
                ticketContainer,
                labResponse.User.Upn);
            Assert.IsNotNull(ticket);

            account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(3, 3);
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);

            if (factory?.RequestsAndResponses != null)
            {
                factory.RequestsAndResponses.Clear();
            }

            Trace.WriteLine("Part 4 - Acquire a token silently");
            result = await pca
                .AcquireTokenSilent(s_scopes, account)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            ticket = TestCommon.GetValidatedKerberosTicketFromAuthenticationResult(
                result,
                ticketContainer,
                labResponse.User.Upn);
            Assert.IsNotNull(ticket);

            Trace.WriteLine("Part 5 - Acquire a token silently with force refresh");
            result = await pca
                .AcquireTokenSilent(s_scopes, account)
                .WithForceRefresh(true)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);
            AssertCcsRoutingInformationIsSent(factory, labResponse);

            ticket = TestCommon.GetValidatedKerberosTicketFromAuthenticationResult(
                result,
                ticketContainer,
                labResponse.User.Upn);
            Assert.IsNotNull(ticket);
            TestCommon.ValidateKerberosWindowsTicketCacheOperation(ticket);

            return result;
        }

        #endregion
    }
}
