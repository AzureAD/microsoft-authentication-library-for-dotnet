// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class SilentAuthTests
    {
        private static readonly string[] s_scopes = { "User.Read" };


        [TestMethod]
        public async Task SilentAuth_ForceRefresh_Async()
        {
            var labResponse = LabUserHelper.GetDefaultUser();
            var user = labResponse.User;

            var pca = PublicClientApplicationBuilder
                .Create(labResponse.AppId).WithAuthority("https://login.microsoftonline.com/organizations")
                .Build();

            Trace.WriteLine("Part 1 - Acquire a token with U/P");
            AuthenticationResult authResult = await pca
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword)
                .ExecuteAsync(new CancellationTokenSource().Token)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult, user);
            var at1 = authResult.AccessToken;
            // If test fails with "user needs to consent to the application, do an interactive request" error - see UsernamePassword tests

            Trace.WriteLine("Part 2 - Acquire a token silently, with forceRefresh = true");
            IAccount account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, authResult).ConfigureAwait(false);

            authResult = await pca.AcquireTokenSilent(s_scopes, account)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);
            MsalAssert.AssertAuthResult(authResult, user);
            var at2 = authResult.AccessToken;

            Trace.WriteLine("Part 3 - Acquire a token silently with a login hint, with forceRefresh = true");
            authResult = await pca.AcquireTokenSilent(s_scopes, user.Upn)
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
