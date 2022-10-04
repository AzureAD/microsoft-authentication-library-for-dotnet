// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET_CORE

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Integration.Broker
{
    [TestClass]
    public class RuntimeBrokerTests
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [RunOn(TargetFrameworks.NetCore)]
        public async Task WamSilentAuthUserInteractionRequiredAsync()
        {
            string[] scopes = new[]
                {
                    "https://management.core.windows.net//.default"
                };

            PublicClientApplicationBuilder pcaBuilder = PublicClientApplicationBuilder
               .Create("04f0c124-f2bc-4f59-8241-bf6df9866bbd")
               .WithAuthority("https://login.microsoftonline.com/organizations");

            IPublicClientApplication pca = pcaBuilder.WithBrokerPreview().Build();

            // Act
            try
            {
                var result = await pca.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false);

            }
            catch (MsalUiRequiredException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Need user interaction to continue"));
            }

        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task WamSilentAuthLoginHintNoAccontInCacheAsync()
        {
            string[] scopes = new[]
                {
                    "https://management.core.windows.net//.default"
                };

            PublicClientApplicationBuilder pcaBuilder = PublicClientApplicationBuilder
               .Create("04f0c124-f2bc-4f59-8241-bf6df9866bbd")
               .WithAuthority("https://login.microsoftonline.com/organizations");

            IPublicClientApplication pca = pcaBuilder.WithBrokerPreview().Build();

            // Act
            try
            {
                var result = await pca.AcquireTokenSilent(scopes, "idlab@").ExecuteAsync().ConfigureAwait(false);

            }
            catch (MsalUiRequiredException ex)
            {
                Assert.IsTrue(ex.Message.Contains("You are trying to acquire a token silently using a login hint. " +
                    "No account was found in the token cache having this login hint"));
            }
        }

        [RunOn(TargetFrameworks.NetStandard | TargetFrameworks.NetCore)]
        public async Task WamUsernamePasswordRequestAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };
            string[] expectedScopes = { "email", "offline_access", "openid", "profile", "User.Read" };

            IntPtr intPtr = GetForegroundWindow();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.Lab.Authority, "organizations")
               .WithBrokerPreview().Build();

            // Acquire token using username password
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword()).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId, expectedScopes);

            // Get Accounts
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);

            var account = accounts.FirstOrDefault();
            Assert.IsNotNull(account);

            // Acquire token silently
            result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Cache, labResponse.Lab.TenantId, expectedScopes);

            // Acquire token interactively WithAccount
            // Commented out because of flakiness.
            //result = await pca.AcquireTokenInteractive(scopes).WithAccount(account).ExecuteAsync().ConfigureAwait(false);

            //MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId, expectedScopes);

            // Remove Account
            await pca.RemoveAsync(account).ConfigureAwait(false);

            // Assert the account is removed
            accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Assert.IsNotNull(accounts);
            Assert.AreEqual(0, accounts.Count());
        }
    }
}
#endif
