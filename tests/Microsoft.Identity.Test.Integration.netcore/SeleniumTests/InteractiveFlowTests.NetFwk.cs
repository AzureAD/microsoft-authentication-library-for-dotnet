// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    [TestClass]
    public class InteractiveFlowTests
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
            ApplicationBase.ResetStateForTest();
        }

        #endregion MSTest Hooks

        [RunOn(TargetFrameworks.NetFx)]
        public async Task Interactive_AADAsync()
        {
            // Arrange - Use pure public client multi-tenant app to avoid AADSTS7000218 credential requirement
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.MsalAppAzureAdMultipleOrgsPublicClient).ConfigureAwait(false);
            var result = await RunTestForUserAsync(user, app).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.Arlington)]
        public async Task Arlington_Interactive_AADAsync()
        {
            // Arrange
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserArlington).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.ArlAppIdLabsApp).ConfigureAwait(false);
            user.AzureEnvironment = LabConstants.AzureEnvironmentUsGovernment;
            await RunTestForUserAsync(user, app, false).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task InteractiveConsentPromptAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.MsalAppAzureAdMultipleOrgsPublicClient).ConfigureAwait(false);

            await RunPromptTestForUserAsync(user, app, Prompt.Consent, true).ConfigureAwait(false);
            await RunPromptTestForUserAsync(user, app, Prompt.Consent, false).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
#if IGNORE_FEDERATED
        [Ignore]
#endif
        public async Task Interactive_Adfs_FederatedAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserFederated).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);
            await RunTestForUserAsync(user, app).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task Interactive_Arlington_MultiCloudSupport_AADAsync()
        {
            // Arrange
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserArlington).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.ArlAppIdLabsApp).ConfigureAwait(false);
            user.AzureEnvironment = LabConstants.AzureEnvironmentUsGovernment;
            
            IPublicClientApplication pca = PublicClientApplicationBuilder
                    .Create(app.AppId)
                    .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
                    .WithAuthority("https://login.microsoftonline.com/common")
                    .WithMultiCloudSupport(true)
                    .WithTestLogging()
                    .Build();

            Trace.WriteLine("Part 1 - Acquire a token interactively");
            AuthenticationResult result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(user, Prompt.SelectAccount, false))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Account);
            Assert.IsNotNull(result.Account.GetTenantProfiles());
            Assert.IsTrue(result.Account.GetTenantProfiles().Any());
            Assert.AreEqual(user.Upn, result.Account.Username);
            Assert.IsTrue(app.Authority.Contains(result.Account.Environment));

            Trace.WriteLine("Part 2 - Get Accounts");
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Assert.IsNotNull(accounts);
            Assert.IsNotNull(accounts.Single());

            var account = accounts.Single();

            Assert.IsNotNull(account.GetTenantProfiles());
            Assert.IsTrue(account.GetTenantProfiles().Any());
            Assert.AreEqual(user.Upn, account.Username);
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
            Assert.IsTrue(app.Authority.Contains(result.Account.Environment));
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.ADFS)]
#if IGNORE_FEDERATED
        [Ignore]
#endif
        public async Task Interactive_Adfs_DirectAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserFederated).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);
            await RunTestForUserAsync(user, app, true).ConfigureAwait(false);
        }      

        [RunOn(TargetFrameworks.NetCore)]
        public async Task ValidateCcsHeadersForInteractiveAuthCodeFlowAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.MsalAppAzureAdMultipleOrgsPublicClient).ConfigureAwait(false);

            var pca = PublicClientApplicationBuilder
               .Create(app.AppId)
               .WithDefaultRedirectUri()
               .WithRedirectUri("http://localhost:52073")
               .WithTestLogging(out HttpSnifferClientFactory factory)
               .Build();

            AuthenticationResult authResult = await pca
               .AcquireTokenInteractive(s_scopes)
               .WithPrompt(Prompt.SelectAccount)
               .WithCustomWebUi(CreateSeleniumCustomWebUI(user, Prompt.SelectAccount))
               .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
               .ConfigureAwait(false);

            var CcsHeader = TestCommon.GetCcsHeaderFromSnifferFactory(factory);
            var userObjectId = user.ObjectId;
            var userTenantID = user.TenantId;
            Assert.AreEqual($"x-anchormailbox:oid:{userObjectId}@{userTenantID}", $"{CcsHeader.Key}:{CcsHeader.Value.FirstOrDefault()}");

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
        }

        /// Based on the publicly available https://demo.duendesoftware.com/
        [RunOn(TargetFrameworks.NetCore)]
        public async Task Interactive_GenericAuthority_DuendeDemoInstanceAsync()
        {
            string[] scopes = new[] { "openid profile email api offline_access" };
            const string username = "bob", password = "bob";
            const string demoDuendeSoftwareDotCom = "https://demo.duendesoftware.com";

            var pca = PublicClientApplicationBuilder
                .Create("interactive.public")
                .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
                .WithTestLogging()
                .WithExperimentalFeatures()
                .WithOidcAuthority(demoDuendeSoftwareDotCom)
                .Build();

            AuthenticationResult authResult = await pca
                .AcquireTokenInteractive(scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUIForDuende(username, password))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.Scopes);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.AreEqual(5, authResult.Scopes.Count());
            Assert.AreEqual("Bearer", authResult.TokenType);
        }

        private async Task<AuthenticationResult> RunTestForUserAsync(UserConfig user, AppConfig app, bool directToAdfs = false)
        {
            HttpSnifferClientFactory factory = null;
            IPublicClientApplication pca;
            if (directToAdfs)
            {
                pca = PublicClientApplicationBuilder
                    .Create(app.AppId)
                    .WithRedirectUri("http://localhost:52073")
                    .WithAdfsAuthority("https://fs.id4slab1.com/adfs", validateAuthority: false)
                    .WithTestLogging()
                    .Build();
            }
            else
            {
                pca = PublicClientApplicationBuilder
                    .Create(app.AppId)
                    .WithRedirectUri("http://localhost:52073")
                    .WithAuthority(app.Authority + "common")
                    .WithTestLogging(out factory)
                    .Build();
            }

            var userCacheAccess = pca.UserTokenCache.RecordAccess();

            Trace.WriteLine("Part 1 - Acquire a token interactively, no login hint");
            AuthenticationResult result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(user, Prompt.SelectAccount, false, directToAdfs))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            Assert.IsTrue(result.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.IsTrue(result.AuthenticationResultMetadata.DurationInHttpInMs > 0);

            userCacheAccess.AssertAccessCounts(0, 1);
            IAccount account = await MsalAssert.AssertSingleAccountAsync(user, pca, result).ConfigureAwait(false);

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
                .WithCustomWebUi(CreateSeleniumCustomWebUI(user, Prompt.ForceLogin, true, directToAdfs))
                .WithPrompt(Prompt.ForceLogin)
                .WithLoginHint(user.Upn)
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(2, 3);
            AssertCcsRoutingInformationIsSent(factory, user);

            account = await MsalAssert.AssertSingleAccountAsync(user, pca, result).ConfigureAwait(false);
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
            
            Trace.WriteLine("Part 5 - Acquire a token silently with force refresh");
            result = await pca
                .AcquireTokenSilent(s_scopes, account)
                .WithForceRefresh(true)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            await MsalAssert.AssertSingleAccountAsync(user, pca, result).ConfigureAwait(false);
            Assert.IsFalse(userCacheAccess.LastAfterAccessNotificationArgs.IsApplicationCache);
            AssertCcsRoutingInformationIsSent(factory, user);

            return result;
        }

        private void AssertCcsRoutingInformationIsSent(HttpSnifferClientFactory factory, UserConfig user)
        {
            if (user.FederationProvider != LabConstants.FederationProviderNone)
            {
                return;
            }

            var CcsHeader = TestCommon.GetCcsHeaderFromSnifferFactory(factory);

            if (!String.IsNullOrEmpty(CcsHeader.Value?.FirstOrDefault()))
            {
                ValidateCcsHeader(CcsHeader, user);
            }
        }

        private void ValidateCcsHeader(KeyValuePair<string, IEnumerable<string>> CcsHeader, UserConfig user)
        {
            var ccsHeaderValue = CcsHeader.Value.FirstOrDefault();
            if (ccsHeaderValue.Contains("upn"))
            {
                Assert.AreEqual($"X-AnchorMailbox:UPN:{user.Upn}", $"{CcsHeader.Key}:{ccsHeaderValue}");
            }
            else
            {
                var userObjectId = user.ObjectId;
                var userTenantID = user.TenantId;
                Assert.AreEqual($"X-AnchorMailbox:Oid:{userObjectId}@{userTenantID}", $"{CcsHeader.Key}:{ccsHeaderValue}");
            }
        }

        private async Task RunPromptTestForUserAsync(UserConfig user, AppConfig app, Prompt prompt, bool useLoginHint)
        {
            var pca = PublicClientApplicationBuilder
               .Create(app.AppId)
               .WithDefaultRedirectUri()
               .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
               .WithTestLogging()
               .Build();

            AcquireTokenInteractiveParameterBuilder builder = pca
               .AcquireTokenInteractive(s_scopes)
               .WithPrompt(prompt)
               .WithCustomWebUi(CreateSeleniumCustomWebUI(user, prompt, useLoginHint));

            if (useLoginHint)
            {
                builder = builder.WithLoginHint(user.Upn);
            }

            AuthenticationResult result = await builder
               .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
               .ConfigureAwait(false);

            await MsalAssert.AssertSingleAccountAsync(user, pca, result).ConfigureAwait(false);
        }

        private SeleniumWebUI CreateSeleniumCustomWebUI(UserConfig user, Prompt prompt, bool withLoginHint = false, bool adfsOnly = false)
        {
            return new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                driver.PerformLogin(user, prompt, withLoginHint, adfsOnly);
            }, TestContext);
        }

        private SeleniumWebUI CreateSeleniumCustomWebUIForDuende(string username, string password)
        {
            return new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");

                driver.FindElementById("Input_Username").SendKeys(username);
                driver.FindElementById("Input_Password").SendKeys(password);

                var loginBtn = driver.WaitForElementToBeVisibleAndEnabled(OpenQA.Selenium.By.Name("Input.Button"));
                loginBtn?.Click();
            }, TestContext);
        }
    }
}
