﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        private readonly string _popNonce = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6bnVsbH0.eyJ0cyI6MTY1MjI4NTAzNH0.Nh-mAJwRphv57IdpdIrYzmYp6vP_BmmYy4UrNKj5A2x4XKLbp_H3aH4J5_s9hP5MzoiHE2SgVaDG8YUbP4xOjFYmpNG884pWqI-z9RjFNKJgBTXUhwv8HsUnxUHq1KTvpLmd1K1gJZORdeUI2LDr07EEH3-aT0PkRt-wT1YNNh5gU_RHV5KvlsyDWCvCJpEbZmGUf8JX9tHO2ux7XAKD77lVb5m6lFq_8Wr5nhJDyREHrXKWQq-X4rTxnBCZ4KBAufImSVHAeVi7ihlGbcobU2CuyJscTZkyELWMG8rBD6QK57AzrM77mua9-QClKIHArL8_d2fgyksLLS89wxy25A";

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

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithAuthority(labResponse.Lab.Authority, "organizations")
               .WithBrokerPreview().Build();

            // Acquire token using username password
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, new NetworkCredential("", labResponse.User.GetOrFetchPassword()).SecurePassword).ExecuteAsync().ConfigureAwait(false);

            AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId);

            // Get Accounts
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);

            var account = accounts.FirstOrDefault();
            Assert.IsNotNull(account);

            // Acquire token silently
            result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);

            AssertAuthResult(result, TokenSource.Cache, labResponse.Lab.TenantId);

            // Remove Account
            await pca.RemoveAsync(account).ConfigureAwait(false);

            // Assert the account is removed
            accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Assert.IsNotNull(accounts);
            Assert.AreEqual(0, accounts.Count());
        }

        [TestMethod]
        public async Task WamUsernamePasswordRequestMsaPassthroughAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create("04f0c124-f2bc-4f59-8241-bf6df9866bbd")
               .WithAuthority(labResponse.Lab.Authority, "organizations")
               .WithWindowsBrokerOptions(new WindowsBrokerOptions()
               {
                   MsaPassthrough = true
               })
               .WithBrokerPreview().Build();

            // Acquire token using username password
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, new NetworkCredential("", labResponse.User.GetOrFetchPassword()).SecurePassword).ExecuteAsync().ConfigureAwait(false);

            AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId);

            // Get Accounts
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);

            var account = accounts.FirstOrDefault();
            Assert.IsNotNull(account);

            // Acquire token silently
            result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);

            AssertAuthResult(result, TokenSource.Cache, labResponse.Lab.TenantId);

            // Remove Account
            await pca.RemoveAsync(account).ConfigureAwait(false);

            // Assert the account is removed
            accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Assert.IsNotNull(accounts);
            Assert.AreEqual(0, accounts.Count());
        }

        [TestMethod]
        public async Task WamUsernamePasswordRequestWithPOPAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithAuthority(labResponse.Lab.Authority, "organizations")
               .WithExperimentalFeatures()
               .WithBrokerPreview().Build();

            var result = await pca
                .AcquireTokenByUsernamePassword(
                    scopes, 
                    labResponse.User.Upn, 
                    new NetworkCredential("", labResponse.User.GetOrFetchPassword()).SecurePassword)
                .WithProofOfPossession(_popNonce, System.Net.Http.HttpMethod.Get, new Uri(labResponse.Lab.Authority))
                .ExecuteAsync().ConfigureAwait(false);

            AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId, true);
        }

        private void AssertAuthResult(AuthenticationResult result, TokenSource tokenSource, string tenantId, bool isPop = false)
        {
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);
            Assert.IsNotNull(result.IdToken);
            Assert.IsNotNull(result.Account);
            Assert.IsNotNull(result.Account.Username);

            if (isPop)
            {
                Assert.AreEqual("PoP", result.TokenType);
            }
            else
            {
                Assert.AreEqual("Bearer", result.TokenType);
            } 

            Assert.AreEqual(tokenSource, result.AuthenticationResultMetadata.TokenSource);

            Assert.IsTrue(result.ExpiresOn > DateTimeOffset.UtcNow + TimeSpan.FromHours(1));

            Assert.AreEqual(tenantId, result.TenantId);
        }
    }
}
