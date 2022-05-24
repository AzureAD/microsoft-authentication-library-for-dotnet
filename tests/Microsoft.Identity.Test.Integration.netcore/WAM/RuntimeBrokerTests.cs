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
using Microsoft.Identity.Client.NativeInterop;
using System;
using NSubstitute;

namespace Microsoft.Identity.Test.Integration.Broker
{
    
    [TestClass]
    public class RuntimeBrokerTests
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        public static IntPtr hWnd;
        public static string CorrelationId = "b0435a5c-6d97-41d6-9372-812e7fac3c10";
        public static string VSApplicationId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        public const string MicrosoftCommonAuthority = "https://login.microsoftonline.com/common";
        public const string Scopes = "user.read";
        public const string RedirectUri = "http://localhost";

        /// <summary>
        /// Initialized by MSTest (do not make private or readonly)
        /// </summary>
        //public TestContext TestContext { get; set; }
        private CoreUIParent _coreUIParent;
        private ICoreLogger _logger;
        private RuntimeBroker _wamBroker;
        IntPtr _parentHandle = GetForegroundWindow();

        [TestInitialize]
        public void Init()
        {
            _coreUIParent = new CoreUIParent() { OwnerWindow = _parentHandle };
            ApplicationConfiguration applicationConfiguration = new ApplicationConfiguration();
            _logger = Substitute.For<ICoreLogger>();
            _wamBroker = new RuntimeBroker(_coreUIParent, applicationConfiguration, _logger);
            hWnd = GetForegroundWindow();
        }

        private static async Task<AuthResult> GetDefaultAccountAsync()
        {
            try
            {
                using (var core = new Core())
                using (var authParams = new AuthParameters(VSApplicationId, MicrosoftCommonAuthority))
                {
                    authParams.RequestedScopes = Scopes;
                    authParams.RedirectUri = RedirectUri;

                    using (AuthResult authResult = await core.SignInAsync(hWnd, authParams, CorrelationId).ConfigureAwait(false))
                    {

                        if (authResult.IsSuccess)
                        {
                            return authResult;
                        }
                        else
                        {
                            throw new Exception("Test failure - Unable to get default account to sign in.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
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

        private static void ValidateAuthResult(AuthResult authResult)
        {
            if (authResult.IsSuccess)
            {
                Console.WriteLine($"Account Id: {authResult.Account.Id}");
                Console.WriteLine($"Account Client Info: {authResult.Account.ClientInfo}");
                Console.WriteLine($"Access Token: {authResult.AccessToken}");
                Console.WriteLine($"Expires On: {authResult.ExpiresOn}");
                Console.WriteLine($"Raw Id Token: {authResult.RawIdToken}");
            }
            else
            {
                Console.WriteLine($"Error: {authResult.Error}");
                throw new MsalRuntimeException(authResult.Error);
            }
        }

        [TestMethod]
        public async Task ReadAccountAsync()
        {
            AuthResult defaultAccount = await GetDefaultAccountAsync().ConfigureAwait(false);

            using (var core = new Core())
            using (var authParams = new AuthParameters(VSApplicationId, MicrosoftCommonAuthority))
            {
                authParams.RequestedScopes = Scopes;
                authParams.RedirectUri = RedirectUri;
                using (Client.NativeInterop.Account accountId = await core.ReadAccountByIdAsync(defaultAccount.Account.Id, CorrelationId).ConfigureAwait(false))
                {
                    if (accountId == null)
                    {
                        Assert.Fail($"Account id: {accountId} is not found");
                    }
                    else
                    {
                        Assert.IsNotNull(accountId);
                    }
                }
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilentlyAsync()
        {
            AuthResult defaultAccount = await GetDefaultAccountAsync().ConfigureAwait(false);

            using (var core = new Core())
            using (var authParams = new AuthParameters(VSApplicationId, MicrosoftCommonAuthority))
            {
                authParams.RequestedScopes = Scopes;
                authParams.RedirectUri = RedirectUri;
                using (AuthResult authResult = await core.AcquireTokenSilentlyAsync(authParams, CorrelationId, defaultAccount.Account).ConfigureAwait(false))
                {
                    if (authResult == null)
                    {
                        Assert.Fail($"Unable to get Auth Result.");
                    }
                    else
                    {
                        Assert.IsNotNull(authResult.Account.Id);
                        Assert.IsNotNull(authResult.Account.ClientInfo);
                        Assert.IsNotNull(authResult.AccessToken);
                        Assert.IsNotNull(authResult.ExpiresOn);
                        Assert.IsNotNull(authResult.RawIdToken);
                    }
                }
            }
        }
    }
}
