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
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.SSHCertificates;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.Utils;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Integration.Broker
{
    [TestClass]
    public class RuntimeBrokerTests
    {
        //This client id is for Azure CLI which is one of the only 2 clients that have PreAuth to use ssh cert feature
        string _SSH_ClientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
        //SSH User impersonation scope required for this test
        private string[] _SSH_scopes = new[] { "https://pas.windows.net/CheckMyAccess/Linux/user_impersonation" };

        private BrokerOptions _brokerOptions = TestUtils.GetPlatformBroker();

        private string CreateJwk()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            RSAParameters rsaKeyInfo = rsa.ExportParameters(false);

            string modulus = Base64UrlHelpers.Encode(rsaKeyInfo.Modulus);
            string exp = Base64UrlHelpers.Encode(rsaKeyInfo.Exponent);
            string jwk = $"{{\"kty\":\"RSA\", \"n\":\"{modulus}\", \"e\":\"{exp}\"}}";

            return jwk;
        }
        
        // This test should fail locally but succeed in a CI build.
        [IgnoreOnOneBranch]
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

            IPublicClientApplication pca = pcaBuilder
                .WithBroker(_brokerOptions)
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

        [DoNotRunOnLinux] // POP is not supported on Linux
        [IgnoreOnOneBranch]
        [Ignore("Tracking here: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5305")]
        [TestMethod]
        public async Task ExtractNonceWithAuthParserAndValidateShrAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };

            //Arrange & Act
            //Test for nonce in WWW-Authenticate header
            var parsedHeaders = await AuthenticationHeaderParser.ParseAuthenticationHeadersAsync("https://testingsts.azurewebsites.net/servernonce/invalidsignature").ConfigureAwait(false);

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithAuthority(labResponse.App.Authority, "organizations")
               .WithBroker(_brokerOptions)
               .Build();

            Assert.IsTrue(pca.IsProofOfPossessionSupportedByClient(), "Either the broker is not configured or it does not support POP.");

            Uri requestUri = new Uri("https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b");

            #pragma warning disable CS0618 // Type or member is obsolete
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
            #pragma warning restore CS0618

            MsalAssert.AssertAuthResult(
                result, 
                TokenSource.Broker, 
                labResponse.User.TenantId,
                scopes, true);

            PoPValidator.VerifyPoPToken(
                labResponse.App.AppId,
                requestUri.AbsoluteUri,
                HttpMethod.Get,
                result);
        }
    
        [DoNotRunOnLinux] // Linux broker return different error code
        [IgnoreOnOneBranch]
        [TestMethod]
        public async Task WamInvalidROPC_ThrowsException_TestAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };
            WamLoggerValidator wastestLogger = new WamLoggerValidator();

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithAuthority(labResponse.App.Authority, "organizations")
               .WithLogging(wastestLogger, enablePiiLogging: true) // it's important that the PII is turned on, otherwise context is 'pii'
               .WithBroker(_brokerOptions)
               .Build();

            #pragma warning disable CS0618 // Type or member is obsolete
            MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                pca.AcquireTokenByUsernamePassword(
                    scopes,
                    "noUser",
                    "badPassword")
                .ExecuteAsync())
                .ConfigureAwait(false);
            #pragma warning restore CS0618

            Assert.AreEqual("0x2142008A", ex.AdditionalExceptionData[MsalException.BrokerErrorTag]);
            Assert.AreEqual("User name is malformed.", ex.AdditionalExceptionData[MsalException.BrokerErrorContext]); // message might change. not a big deal
            Assert.AreEqual("ApiContractViolation", ex.AdditionalExceptionData[MsalException.BrokerErrorStatus]);
            Assert.AreEqual("3399811229", ex.AdditionalExceptionData[MsalException.BrokerErrorCode]);
            Assert.IsNotNull(ex.AdditionalExceptionData[MsalException.BrokerTelemetry]);
            
        }

        [IgnoreOnOneBranch]
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

            IPublicClientApplication pca = pcaBuilder
               .WithBroker(_brokerOptions)
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
        [TestMethod]
        public async Task WamUsernamePasswordRequestAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };            

            IntPtr intPtr = TestUtils.GetWindowHandle();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.App.Authority, "organizations")
               .WithLogging((x, y, z) => Debug.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
               .WithBroker(_brokerOptions)
               .Build();

            // Get Accounts
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            // Acquire token using username password
            #pragma warning disable CS0618 // Type or member is obsolete
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword()).ExecuteAsync().ConfigureAwait(false);
            #pragma warning restore CS0618
            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.User.TenantId, scopes);
            Assert.IsNotNull(result.AuthenticationResultMetadata.Telemetry);

            // Get Accounts
            accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);

            var account = accounts.FirstOrDefault();
            Assert.IsNotNull(account);

            // Acquire token silently
            result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.User.TenantId, scopes);
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

        [DoNotRunOnLinux] // SSH Certs are not supported on Linux
        [IgnoreOnOneBranch]
        [TestMethod]
        public async Task WamWithSSHCertificateAuthenticationSchemeAsync()
        {
            IntPtr intPtr = TestUtils.GetWindowHandle();
            Func<IntPtr> windowHandleProvider = () => intPtr;
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            IPublicClientApplication pca = PublicClientApplicationBuilder
            .Create(_SSH_ClientId)
            .WithTestLogging()
            .WithAuthority(labResponse.App.Authority, "organizations")
            .WithParentActivityOrWindow(windowHandleProvider)
            .WithBroker(_brokerOptions)
            .Build();

            string jwk = CreateJwk();
            //Do a login with username password
            #pragma warning disable CS0618 // Type or member is obsolete
            AuthenticationResult result = await pca
            .AcquireTokenByUsernamePassword(_SSH_scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword())
            .ExecuteAsync()
            .ConfigureAwait(false);
            #pragma warning restore CS0618

            //Assert successful login
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);
            var account = accounts.FirstOrDefault();
            Assert.IsNotNull(account);

            //Acquire token with SSH cert
            result = await pca
                .AcquireTokenSilent(_SSH_scopes, account)
                .WithSSHCertificateAuthenticationScheme(jwk, "key1")
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual("SshCert", result.TokenType);
        }

        [IgnoreOnOneBranch]
        [TestMethod]
        public async Task WamUsernamePasswordWithForceRefreshAsync()
        {
            var labResponse = await LabUserHelper.MergeKVLabDataAsync("MSAL-User-Default-JSON", "MSAL-APP-AzureADMultipleOrgsPC-JSON").ConfigureAwait(false);
            string[] scopes = { "User.Read" };

            IntPtr intPtr = TestUtils.GetWindowHandle();
            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.App.Authority, "organizations")
               .WithLogging((x, y, z) => Debug.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
               .WithBroker(_brokerOptions)
               .Build();

            // Acquire token using username password
            #pragma warning disable CS0618 // Type or member is obsolete
            var result = await pca.AcquireTokenByUsernamePassword(
                scopes, 
                labResponse.User.Upn, 
                labResponse.User.GetOrFetchPassword())
                .ExecuteAsync()
                .ConfigureAwait(false);
            #pragma warning restore CS0618

            string ropcToken = result.AccessToken;

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.User.TenantId, scopes);
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
        [TestMethod]
        [ExpectedException(typeof(MsalUiRequiredException))]
        public async Task WamUsernamePasswordRequestAsync_WithPiiAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };

            IntPtr intPtr = TestUtils.GetWindowHandle();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            WamLoggerValidator testLogger = new WamLoggerValidator();

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.App.Authority, "organizations")
               .WithLogging(testLogger, enablePiiLogging: true)
               .WithBroker(_brokerOptions)
               .Build();

            // Acquire token using username password
            #pragma warning disable CS0618 // Type or member is obsolete
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword()).ExecuteAsync().ConfigureAwait(false);
            #pragma warning restore CS0618

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.User.TenantId, scopes);
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

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.User.TenantId, scopes);
            Assert.IsNotNull(result.AuthenticationResultMetadata.Telemetry);

            await pca.RemoveAsync(account).ConfigureAwait(false);
            // Assert the account is removed
            accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsNotNull(accounts);

            // this should throw MsalUiRequiredException
            result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);
        }

        [DoNotRunOnLinux] // List Windows Work and School accounts is not supported on Linux
        [IgnoreOnOneBranch]
        [TestMethod]
        public async Task WamListWindowsWorkAndSchoolAccountsAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            string[] scopes = { "User.Read" };

            IntPtr intPtr = TestUtils.GetWindowHandle();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.App.Authority, "organizations")
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
               {
                   ListOperatingSystemAccounts = true,
               })
               .Build();

            // Acquire token using username password
            #pragma warning disable CS0618 // Type or member is obsolete
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword()).ExecuteAsync().ConfigureAwait(false);
            #pragma warning restore CS0618

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, labResponse.User.TenantId, scopes);
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
        [TestMethod]
        public async Task WamAddDefaultScopesWhenNoScopesArePassedAsync(string scopes)
        {
            IntPtr intPtr = TestUtils.GetWindowHandle();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create("43dfbb29-3683-4673-a66f-baba91798bd2")
               .WithAuthority("https://login.microsoftonline.com/organizations")
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithBroker(_brokerOptions)
               .Build();
            // Act
            if (SharedUtilities.IsLinuxPlatform()) {
                var exLinux = await AssertException.TaskThrowsAsync<MsalServiceException>(
                 () => pca.AcquireTokenSilent(new string[] { scopes }, PublicClientApplication.OperatingSystemAccount)
                        .ExecuteAsync())
                        .ConfigureAwait(false);
                StringAssert.Contains(exLinux.AdditionalExceptionData[MsalException.BrokerErrorContext], "requestedScopes is NULL or EMPTY");
            } else {
                var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                 () => pca.AcquireTokenSilent(new string[] { scopes }, PublicClientApplication.OperatingSystemAccount)
                        .ExecuteAsync())
                        .ConfigureAwait(false);
                Assert.IsTrue(!string.IsNullOrEmpty(ex.ErrorCode));
            }
        }

        [DoNotRunOnLinux] // POP is not supported on Linux     
        [IgnoreOnOneBranch]
        [TestMethod]
        public async Task WamUsernamePasswordPopTokenEnforcedWithCaOnValidResourceAsync()
        {
            //Arrange
            var labResponse = await LabUserHelper.MergeKVLabDataAsync("MSAL-User-POP-JSON", "MSAL-APP-AzureADMultipleOrgsPC-JSON").ConfigureAwait(false);

            string[] scopes = { "https://id4slab1.sharepoint.com/user.read" };

            IntPtr intPtr = TestUtils.GetWindowHandle();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.App.Authority, \"organizations\")
               .WithBroker(_brokerOptions)
               .Build();

            // Acquire token using username password with POP on a valid resource
            // CA policy enforces token issuance to popUser only for SPO
            // https://learn.microsoft.com/azure/active-directory/conditional-access/concept-token-protection
            #pragma warning disable CS0618 // Type or member is obsolete
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword())
                .WithProofOfPossession("some_nonce", System.Net.Http.HttpMethod.Get, new Uri(pca.Authority))
                .ExecuteAsync()
                .ConfigureAwait(false);
            #pragma warning restore CS0618

            //Act
            Assert.AreEqual(labResponse.User.Upn, result.Account.Username);
        }

        [DoNotRunOnLinux] // POP are not supported on Linux  
        [IgnoreOnOneBranch]
        [TestMethod]
        [ExpectedException(typeof(MsalUiRequiredException))]
        public async Task WamUsernamePasswordPopTokenEnforcedWithCaOnInValidResourceAsync()
        {
            //Arrange
            var labResponse = await LabUserHelper.MergeKVLabDataAsync("MSAL-User-POP-JSON", "MSAL-APP-AzureADMultipleOrgsPC-JSON").ConfigureAwait(false);

            string[] scopes = { "https://outlook.office365.com/Mail.Read" };

            IntPtr intPtr = TestUtils.GetWindowHandle();

            Func<IntPtr> windowHandleProvider = () => intPtr;

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithParentActivityOrWindow(windowHandleProvider)
               .WithAuthority(labResponse.App.Authority, \"organizations\")
               .WithBroker(_brokerOptions)
               .Build();

            // Acquire token using username password with POP on a resource not in the CA policy
            // CA policy enforces token issuance to popUser only for Exchange Online this call will fail with UI Required Exception
            // https://learn.microsoft.com/azure/active-directory/conditional-access/concept-token-protection
            #pragma warning disable CS0618 // Type or member is obsolete
            var result = await pca.AcquireTokenByUsernamePassword(scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword())
                .WithProofOfPossession("some_nonce", System.Net.Http.HttpMethod.Get, new Uri(pca.Authority))
                .ExecuteAsync()
                .ConfigureAwait(false);
            #pragma warning restore CS0618
        }
    }
}
#endif
