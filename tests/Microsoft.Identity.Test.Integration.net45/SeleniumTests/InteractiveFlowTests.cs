// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
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

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        #endregion MSTest Hooks

        [TestMethod]
        public async Task Interactive_AADAsync()
        {
            // Arrange
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

#if DESKTOP // no point in running these tests on NetCore - the code path is similar

        [TestMethod]
        public async Task Interactive_AdfsV3_FederatedAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV3, true).ConfigureAwait(false);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV2_FederatedAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV2, true).ConfigureAwait(false);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV4_FederatedAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, true).ConfigureAwait(false);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task InteractiveConsentPromptAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            await RunPromptTestForUserAsync(labResponse, Prompt.Consent, true).ConfigureAwait(false);
            await RunPromptTestForUserAsync(labResponse, Prompt.Consent, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV2019_FederatedAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, true).ConfigureAwait(false);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

#endif

        [TestMethod]
        public async Task Interactive_AdfsV2019_DirectAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, true).ConfigureAwait(false);
            await RunTestForUserAsync(labResponse, true).ConfigureAwait(false);
        }

        [TestMethod]
        [Ignore("Lab needs a way to provide multiple account types(AAD, ADFS, MSA) that can sign into the same client id")]
        public async Task MultiUserCacheCompatabilityTestAsync()
        {
            // Arrange

            //Acquire AT for default lab account
            LabResponse labResponseDefault = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            AuthenticationResult defaultAccountResult = await RunTestForUserAsync(labResponseDefault).ConfigureAwait(false);

            //Acquire AT for ADFS 2019 account
            LabResponse labResponseFederated = await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, true).ConfigureAwait(false);
            var federatedAccountResult = await RunTestForUserAsync(labResponseFederated, false).ConfigureAwait(false);

            //Acquire AT for MSA account
            LabResponse labResponseMsa = await LabUserHelper.GetB2CMSAAccountAsync().ConfigureAwait(false);
            labResponseMsa.App.AppId = LabApiConstants.MSAOutlookAccountClientID;
            var msaAccountResult = await RunTestForUserAsync(labResponseMsa).ConfigureAwait(false);

            PublicClientApplication pca = PublicClientApplicationBuilder.Create(labResponseDefault.App.AppId).BuildConcrete();

            AuthenticationResult authResult = await pca.AcquireTokenSilent(new[] { CoreUiTestConstants.DefaultScope }, defaultAccountResult.Account)
                                                       .ExecuteAsync()
                                                       .ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);

            pca = PublicClientApplicationBuilder.Create(labResponseFederated.App.AppId).BuildConcrete();

            authResult = await pca.AcquireTokenSilent(new[] { CoreUiTestConstants.DefaultScope }, federatedAccountResult.Account)
                                  .ExecuteAsync()
                                  .ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNull(authResult.IdToken);

            pca = PublicClientApplicationBuilder.Create(LabApiConstants.MSAOutlookAccountClientID).BuildConcrete();

            authResult = await pca.AcquireTokenSilent(new[] { CoreUiTestConstants.DefaultScope }, msaAccountResult.Account)
                                  .ExecuteAsync()
                                  .ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNull(authResult.IdToken);
        }

        private async Task<AuthenticationResult> RunTestForUserAsync(LabResponse labResponse, bool directToAdfs = false)
        {
            IPublicClientApplication pca;
            if (directToAdfs)
            {
                pca = PublicClientApplicationBuilder
                    .Create(Adfs2019LabConstants.PublicClientId)
                    .WithRedirectUri(Adfs2019LabConstants.ClientRedirectUri)
                    .WithAdfsAuthority(Adfs2019LabConstants.Authority)
                    .Build();
            }
            else
            {
                pca = PublicClientApplicationBuilder
                    .Create(labResponse.App.AppId)
                    .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
                    .Build();
            }

            var userCacheAccess = pca.UserTokenCache.RecordAccess();

            Trace.WriteLine("Part 1 - Acquire a token interactively, no login hint");
            AuthenticationResult result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.SelectAccount, false, directToAdfs))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            userCacheAccess.AssertAccessCounts(0, 1);
            IAccount account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(1, 1); // the assert calls GetAccounts
            Assert.IsFalse(userCacheAccess.LastNotificationArgs.IsApplicationCache);

            Trace.WriteLine("Part 2 - Clear the cache");
            await pca.RemoveAsync(account).ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(1, 2);
            Assert.IsFalse((await pca.GetAccountsAsync().ConfigureAwait(false)).Any());
            userCacheAccess.AssertAccessCounts(2, 2);
            Assert.IsFalse(userCacheAccess.LastNotificationArgs.IsApplicationCache);

            Trace.WriteLine("Part 3 - Acquire a token interactively again, with login hint");
            result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.ForceLogin, true, directToAdfs))
                .WithPrompt(Prompt.ForceLogin)
                .WithLoginHint(labResponse.User.Upn)
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(2, 3);

            account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(3, 3);
            Assert.IsFalse(userCacheAccess.LastNotificationArgs.IsApplicationCache);

            Trace.WriteLine("Part 4 - Acquire a token silently");
            result = await pca
                .AcquireTokenSilent(s_scopes, account)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            Assert.IsFalse(userCacheAccess.LastNotificationArgs.IsApplicationCache);

            return result;
        }

        private async Task RunPromptTestForUserAsync(LabResponse labResponse, Prompt prompt, bool useLoginHint)
        {
            var pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
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
        }

        private SeleniumWebUI CreateSeleniumCustomWebUI(LabUser user, Prompt prompt, bool withLoginHint = false, bool adfsOnly = false)
        {
            return new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                driver.PerformLogin(user, prompt, withLoginHint, adfsOnly);
            }, TestContext);
        }
    }
}
