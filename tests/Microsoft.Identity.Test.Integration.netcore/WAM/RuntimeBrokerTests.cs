// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Client.OAuth2;
using System.Runtime.InteropServices;
using System;
using NSubstitute;
using System.Linq;

namespace Microsoft.Identity.Test.Integration.Broker
{
    
    [TestClass]
    public class RuntimeBrokerTests
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Initialized by MSTest (do not make private or readonly)
        /// </summary>
        //public TestContext TestContext { get; set; }
        private CoreUIParent _coreUIParent;
        private ILoggerAdapter _logger;
        private RuntimeBroker _wamBroker;
        IntPtr _parentHandle = GetForegroundWindow();

        [TestInitialize]
        public void Init()
        {
            _coreUIParent = new CoreUIParent() { OwnerWindow = _parentHandle };
            ApplicationConfiguration applicationConfiguration = new ApplicationConfiguration();
            _logger = Substitute.For<ILoggerAdapter>();
            _wamBroker = new RuntimeBroker(_coreUIParent, applicationConfiguration, _logger);
        }

        [TestMethod]
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
                var result = await pca.AcquireTokenSilent(scopes,PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false);

            }
            catch (MsalUiRequiredException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Need user interaction to continue"));
            }

        }

        [TestMethod]
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

        [TestMethod]
        public async Task WamInteractiveAuthNoWindowHandleAsync()
        {
            string[] scopes = new[]
                {
                    "https://management.core.windows.net//.default"
                };

            IAccount accountToLogin = PublicClientApplication.OperatingSystemAccount;

            PublicClientApplicationBuilder pcaBuilder = PublicClientApplicationBuilder
               .Create("04f0c124-f2bc-4f59-8241-bf6df9866bbd")
               .WithAuthority("https://login.microsoftonline.com/organizations");

            IPublicClientApplication pca = pcaBuilder.WithBrokerPreview().Build();

            // Act
            try
            {
                var result = await pca.AcquireTokenInteractive(scopes)
                    .WithAccount(accountToLogin)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

            }
            catch (MsalClientException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Public Client applications wanting to use WAM need to provide their window handle. " +
                    "Console applications can use GetConsoleWindow Windows API for this"));
            }

        }

        [TestMethod]
        public async Task WamUsernamePasswordRequestAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };

            IntPtr intPtr = GetForegroundWindow();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.Lab.Authority, "organizations")
               .WithBrokerPreview().Build();

            // Acquire token using username password
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, new NetworkCredential("", labResponse.User.GetOrFetchPassword()).SecurePassword).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId);

            // Get Accounts
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);

            var account = accounts.FirstOrDefault();
            Assert.IsNotNull(account);

            // Acquire token silently
            result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Cache, labResponse.Lab.TenantId);

            // Acquire token interactively WithAccount
            result = await pca.AcquireTokenInteractive(scopes).WithAccount(account).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId);

            // Acquire token interactively WithLoginHint
            result = await pca.AcquireTokenInteractive(scopes).WithLoginHint(account.Username).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Cache, labResponse.Lab.TenantId);

            // Remove Account
            await pca.RemoveAsync(account).ConfigureAwait(false);

            // Assert the account is removed
            accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Assert.IsNotNull(accounts);
            Assert.AreEqual(0, accounts.Count());
        }
    }
}
