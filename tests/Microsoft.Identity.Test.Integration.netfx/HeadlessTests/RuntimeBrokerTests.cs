// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET_CORE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.Infrastructure;
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

        // This test should fail locally but succeed in a CI build.
        [IgnoreOnOneBranch]
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

            IPublicClientApplication pca = pcaBuilder
                .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
                .Build();

            // Act
            try
            {
                var result = await pca.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false);
                Assert.Fail(" AcquireTokenSilent was successful. MsalUiRequiredException should have been thrown.");

            }
            catch (MsalUiRequiredException ex)
            {
                Assert.IsTrue(!string.IsNullOrEmpty(ex.ErrorCode));
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [IgnoreOnOneBranch]
        [RunOn(TargetFrameworks.NetCore)]
        public async Task ExtractNonceWithAuthParserAndValidateShrAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };
            string[] expectedScopes = { "email", "offline_access", "openid", "profile", "User.Read" };

            //Arrange & Act
            //Test for nonce in WWW-Authenticate header
            var parsedHeaders = await AuthenticationHeaderParser.ParseAuthenticationHeadersAsync("https://testingsts.azurewebsites.net/servernonce/invalidsignature").ConfigureAwait(false);

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithAuthority(labResponse.Lab.Authority, "organizations")
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
               .Build();

            Assert.IsTrue(pca.IsProofOfPossessionSupportedByClient(), "Either the broker is not configured or it does not support POP.");

            Uri requestUri = new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b");

            var result = await pca
                .AcquireTokenByUsernamePassword(
                    scopes,
                    labResponse.User.Upn,
                    labResponse.User.GetOrFetchPassword())
                .WithProofOfPossession(
                    parsedHeaders.PopNonce, 
                    HttpMethod.Get,
                    requestUri)
                .ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId, expectedScopes, true);

            await PoPValidator.VerifyPoPTokenAsync(
                labResponse.App.AppId,
                requestUri.AbsoluteUri,
                HttpMethod.Get,
                result).ConfigureAwait(false);
        }

        [IgnoreOnOneBranch]
        [RunOn(TargetFrameworks.NetCore)]
        public async Task WamInvalidROPC_ThrowsException_TestAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };
            WamLoggerValidator wastestLogger = new WamLoggerValidator();

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithAuthority(labResponse.Lab.Authority, "organizations")
               .WithLogging(wastestLogger, enablePiiLogging: true) // it's important that the PII is turned on, otherwise context is 'pii'
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
               .Build();

            MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                pca.AcquireTokenByUsernamePassword(
                    scopes,
                    "noUser",
                    "badPassword")
                .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual("0x2142008A", ex.AdditionalExceptionData[MsalException.BrokerErrorTag]);
            Assert.AreEqual("User name is malformed.", ex.AdditionalExceptionData[MsalException.BrokerErrorContext]); // message might change. not a big deal
            Assert.AreEqual("ApiContractViolation", ex.AdditionalExceptionData[MsalException.BrokerErrorStatus]);
            Assert.AreEqual("3399811229", ex.AdditionalExceptionData[MsalException.BrokerErrorCode]);
            Assert.IsNotNull(ex.AdditionalExceptionData[MsalException.BrokerTelemetry]);
        }

        [IgnoreOnOneBranch]
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

            IPublicClientApplication pca = pcaBuilder
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
               .Build();

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

        [IgnoreOnOneBranch]
        [RunOn(TargetFrameworks.NetCore)]
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
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
               .Build();

            // Acquire token using username password
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword()).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId, expectedScopes);
            Assert.IsNotNull(result.AuthenticationResultMetadata.Telemetry);

            // Get Accounts
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);

            var account = accounts.FirstOrDefault();
            Assert.IsNotNull(account);

            // Acquire token silently
            result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId, expectedScopes);
            Assert.IsNotNull(result.AuthenticationResultMetadata.Telemetry);

            // Remove Account
            await pca.RemoveAsync(account).ConfigureAwait(false);

            // Assert the account is removed
            accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Assert.IsNotNull(accounts);

            await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
               () => pca.AcquireTokenSilent(scopes, account).ExecuteAsync())
                .ConfigureAwait(false);
        }

        [IgnoreOnOneBranch]
        [TestMethod]
        public async Task WamUsernamePasswordWithForceRefreshAsync()
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
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
               .Build();

            // Acquire token using username password
            var result = await pca.AcquireTokenByUsernamePassword(
                scopes, 
                labResponse.User.Upn, 
                labResponse.User.GetOrFetchPassword())
                .ExecuteAsync()
                .ConfigureAwait(false);

            DateTimeOffset ropcTokenExpiration = result.ExpiresOn;
            string ropcToken = result.AccessToken;

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId, expectedScopes);
            Assert.IsNotNull(result.AuthenticationResultMetadata.Telemetry);

            // Get Accounts
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);            
            var account = accounts.FirstOrDefault();
            Assert.IsNotNull(account);

            result = await pca.AcquireTokenSilent(scopes, account)
                .ExecuteAsync().ConfigureAwait(false);

            // This proves the token is from the cache
            Assert.AreEqual(ropcToken, result.AccessToken);

            result = await pca.AcquireTokenSilent(scopes, account)
                .WithForceRefresh(true)
               .ExecuteAsync().ConfigureAwait(false);

            // This proves the token is not from the cache
            Assert.AreNotEqual(ropcToken, result.AccessToken);
        }

        [IgnoreOnOneBranch]
        [RunOn(TargetFrameworks.NetCore)]
        [ExpectedException(typeof(MsalUiRequiredException))]
        public async Task WamUsernamePasswordRequestAsync_WithPiiAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };
            string[] expectedScopes = { "email", "offline_access", "openid", "profile", "User.Read" };

            IntPtr intPtr = GetForegroundWindow();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            WamLoggerValidator testLogger = new WamLoggerValidator();

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.Lab.Authority, "organizations")
               .WithLogging(testLogger, enablePiiLogging: true)
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
               .Build();

            // Acquire token using username password
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword()).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId, expectedScopes);
            Assert.IsNotNull(result.AuthenticationResultMetadata.Telemetry);

            // Get Accounts
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);

            var account = accounts.FirstOrDefault();
            Assert.IsNotNull(account);

            Assert.IsTrue(testLogger.HasLogged);
            Assert.IsTrue(testLogger.HasPiiLogged);

            // Acquire token silently
            result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId, expectedScopes);
            Assert.IsNotNull(result.AuthenticationResultMetadata.Telemetry);

            await pca.RemoveAsync(account).ConfigureAwait(false);
            // Assert the account is removed
            accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);

            // this should throw MsalUiRequiredException
            result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);
        }

        [IgnoreOnOneBranch]
        [RunOn(TargetFrameworks.NetCore)]
        public async Task WamListWindowsWorkAndSchoolAccountsAsync()
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
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
               {
                   ListOperatingSystemAccounts = true,
               })
               .Build();

            // Acquire token using username password
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword()).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.Lab.TenantId, expectedScopes);
            Assert.IsNotNull(result.AuthenticationResultMetadata.Telemetry);

            // Get Accounts
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);

            //This test does not actually get a work or school account
            //it simply validates that the GetAccounts merging works and accounts are returned
            var account = accounts.FirstOrDefault();
            Assert.AreEqual(labResponse.User.Upn, account.Username);
        }

        [IgnoreOnOneBranch]
        [DataTestMethod]
        [DataRow(null)]
        [RunOn(TargetFrameworks.NetCore)]
        public async Task WamAddDefaultScopesWhenNoScopesArePassedAsync(string scopes)
        {
            IntPtr intPtr = GetForegroundWindow();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create("43dfbb29-3683-4673-a66f-baba91798bd2")
               .WithAuthority("https://login.microsoftonline.com/organizations")
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
               .Build();

            // Act
            var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                 () => pca.AcquireTokenSilent(new string[] { scopes }, PublicClientApplication.OperatingSystemAccount)
                        .ExecuteAsync())
                        .ConfigureAwait(false);

            Assert.IsTrue(!string.IsNullOrEmpty(ex.ErrorCode));
        }

        [IgnoreOnOneBranch]
        [RunOn(TargetFrameworks.NetCore)]
        public async Task WamUsernamePasswordPopTokenEnforcedWithCaOnValidResourceAsync()
        {
            //Arrange
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            
            string popUser = "popUser@msidlab4.onmicrosoft.com";

            string[] scopes = { "https://msidlab4.sharepoint.com/user.read" };

            IntPtr intPtr = GetForegroundWindow();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.Lab.Authority, "organizations")
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
               .Build();

            // Acquire token using username password with POP on a valid resource
            // CA policy enforces token issuance to popUser only for SPO
            // https://learn.microsoft.com/en-us/azure/active-directory/conditional-access/concept-token-protection
            var result = await pca.AcquireTokenByUsernamePassword(scopes, popUser, labResponse.User.GetOrFetchPassword())
                .WithProofOfPossession("some_nonce", System.Net.Http.HttpMethod.Get, new Uri(pca.Authority))
                .ExecuteAsync()
                .ConfigureAwait(false);

            //Act
            Assert.AreEqual(popUser, result.Account.Username);
        }

        [IgnoreOnOneBranch]
        [RunOn(TargetFrameworks.NetCore)]
        [ExpectedException(typeof(MsalUiRequiredException))]
        public async Task WamUsernamePasswordPopTokenEnforcedWithCaOnInValidResourceAsync()
        {
            //Arrange
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            string popUser = "popUser@msidlab4.onmicrosoft.com";

            string[] scopes = { "user.read" };

            IntPtr intPtr = GetForegroundWindow();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.Lab.Authority, "organizations")
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
               .Build();

            // Acquire token using username password with POP on a resource not in the CA policy
            // CA policy enforces token issuance to popUser only for SPO this call will fail with UI Required Exception
            // https://learn.microsoft.com/en-us/azure/active-directory/conditional-access/concept-token-protection
            var result = await pca.AcquireTokenByUsernamePassword(scopes, popUser, labResponse.User.GetOrFetchPassword())
                .WithProofOfPossession("some_nonce", System.Net.Http.HttpMethod.Get, new Uri(pca.Authority))
                .ExecuteAsync()
                .ConfigureAwait(false);
        }
    }
}
#endif
