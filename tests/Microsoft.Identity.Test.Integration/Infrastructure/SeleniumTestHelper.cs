//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.LabInfrastructure.CloudInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public static class SeleniumTestHelper
    {
        private static readonly TimeSpan _interactiveAuthTimeout = TimeSpan.FromMinutes(1);
        private const string _authority = "https://login.microsoftonline.com/organizations/";

        private static string[] _scopes
        {
            get
            {
                return new[] { CloudConfigurationProvider.Scopes };
            }
        }

        public static async Task RunInteractiveTestForUserAsync(LabResponse labResponse)
        {
            PublicClientApplication pca = PublicClientApplicationBuilder.Create(labResponse.AppId)
                                                                        .WithAuthority(CloudConfigurationProvider.Authority, false)
                                                                        .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
                                                                        .BuildConcrete();

            Trace.WriteLine("Part 1 - Acquire a token interactively, no login hint");
            AuthenticationResult result = await pca
                .AcquireTokenInteractive(_scopes, null)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, false))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);
            IAccount account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

            Trace.WriteLine("Part 2 - Clear the cache");
            await pca.RemoveAsync(account).ConfigureAwait(false);
            Assert.IsFalse((await pca.GetAccountsAsync().ConfigureAwait(false)).Any());

            Trace.WriteLine("Part 3 - Acquire a token interactively again, with login hint");
            result = await pca
                .AcquireTokenInteractive(_scopes, null)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, true))
                .WithLoginHint(labResponse.User.HomeUPN)
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);
            account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

            Trace.WriteLine("Part 4 - Acquire a token silently");
            result = await pca.AcquireTokenSilentAsync(_scopes, account).ConfigureAwait(false);
            await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
        }

        static SeleniumWebUI CreateSeleniumCustomWebUI(LabUser user, bool withLoginHint)
        {
            return new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                driver.PerformLogin(user, withLoginHint);
            });
        }

        public static async Task AuthorityMigrationTestAsync()
        {
            var labResponse = LabUserHelper.GetDefaultUser();
            var user = labResponse.User;

            IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(labResponse.AppId)
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            AuthenticationResult authResult = await pca.AcquireTokenByUsernamePassword(
               _scopes,
                user.Upn,
                new NetworkCredential("", user.Password).SecurePassword)
                .WithAuthority("https://sts.windows.net/" + user.CurrentTenantId + "/")
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult.AccessToken);

            Trace.WriteLine("Acquire a token silently using the common authority alias");

            authResult = await pca.AcquireTokenSilent(_scopes, (await pca.GetAccountsAsync().ConfigureAwait(false)).First())
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult.AccessToken);
        }

        public static async Task AcquireTokenWithManagedUsernameIncorrectPasswordTestAsync()
        {
            var labResponse = LabUserHelper.GetDefaultUser();
            var user = labResponse.User;

            SecureString incorrectSecurePassword = new SecureString();
            incorrectSecurePassword.AppendChar('x');
            incorrectSecurePassword.MakeReadOnly();

            PublicClientApplication msalPublicClient = new PublicClientApplication(labResponse.AppId, _authority);

            try
            {
                var result =
                     await msalPublicClient.AcquireTokenByUsernamePasswordAsync(_scopes, user.Upn, incorrectSecurePassword)
                     .ConfigureAwait(false);
            }
            catch (MsalServiceException ex)
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(ex.CorrelationId));
                Assert.AreEqual(400, ex.StatusCode);
                Assert.AreEqual("invalid_grant", ex.ErrorCode);
                Assert.IsTrue(ex.Message.StartsWith("AADSTS50126: Invalid username or password"));

                return;
            }

            Assert.Fail("Bad exception or no exception thrown");
        }

        public static async Task RunHappyPathTestAsync(LabResponse labResponse)
        {
            var user = labResponse.User;

            SecureString securePassword = new NetworkCredential("", user.Password).SecurePassword;

            PublicClientApplication msalPublicClient = new PublicClientApplication(labResponse.AppId, _authority);

            //AuthenticationResult authResult = await msalPublicClient.AcquireTokenByUsernamePasswordAsync(Scopes, user.Upn, securePassword).ConfigureAwait(false);
            AuthenticationResult authResult = await msalPublicClient.AcquireTokenByUsernamePasswordAsync(_scopes, user.Upn, securePassword).ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.AreEqual(user.Upn, authResult.Account.Username);
            // If test fails with "user needs to consent to the application, do an interactive request" error,
            // Do the following: 
            // 1) Add in code to pull the user's password before creating the SecureString, and put a breakpoint there.
            // string password = ((LabUser)user).GetPassword();
            // 2) Using the MSAL Desktop app, make sure the ClientId matches the one used in integration testing.
            // 3) Do the interactive sign-in with the MSAL Desktop app with the username and password from step 1.
            // 4) After successful log-in, remove the password line you added in with step 1, and run the integration test again.
        }

        public static async Task SilentAuth_ForceRefresh_Async()
        {
            var labResponse = LabUserHelper.GetDefaultUser();
            var user = labResponse.User;

            var pca = new PublicClientApplication(labResponse.AppId, "https://login.microsoftonline.com/organizations");

            Trace.WriteLine("Part 1 - Acquire a token with U/P");
            AuthenticationResult authResult = await pca
                .AcquireTokenByUsernamePassword(_scopes, user.Upn, new NetworkCredential("", user.Password).SecurePassword)
                .ExecuteAsync(new CancellationTokenSource().Token)
                .ConfigureAwait(false);
            MsalAssert.AssertAuthResult(authResult, user);
            var at1 = authResult.AccessToken;
            // If test fails with "user needs to consent to the application, do an interactive request" error - see UsernamePassword tests

            Trace.WriteLine("Part 2 - Acquire a token silently, with forceRefresh = true");
            IAccount account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, authResult).ConfigureAwait(false);

            authResult = await pca.AcquireTokenSilent(_scopes, account)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);
            MsalAssert.AssertAuthResult(authResult, user);
            var at2 = authResult.AccessToken;

            Trace.WriteLine("Part 3 - Acquire a token silently with a login hint, with forceRefresh = true");
            authResult = await pca.AcquireTokenSilent(_scopes, user.Upn)
               .WithForceRefresh(true)
               .ExecuteAsync()
               .ConfigureAwait(false);
            MsalAssert.AssertAuthResult(authResult, user);
            var at3 = authResult.AccessToken;

            Assert.IsFalse(at1.Equals(at2, System.StringComparison.InvariantCultureIgnoreCase));
            Assert.IsFalse(at1.Equals(at3, System.StringComparison.InvariantCultureIgnoreCase));
            Assert.IsFalse(at2.Equals(at3, System.StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
