// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class AuthorityMigrationTests
    {
        private static readonly string[] s_scopes = { "User.Read" };

        [TestMethod]
        public async Task AuthorityMigrationAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            var user = labResponse.User;

            IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(labResponse.AppId)
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            AuthenticationResult authResult = await pca.AcquireTokenByUsernamePassword(
               s_scopes,
                user.Upn,
                new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword)
                .WithAuthority("https://sts.windows.net/" + user.CurrentTenantId + "/")
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult.AccessToken);

            Trace.WriteLine("Acquire a token silently using the common authority alias");

            authResult = await pca.AcquireTokenSilent(s_scopes, (await pca.GetAccountsAsync().ConfigureAwait(false)).First())
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult.AccessToken);
        }

        [TestMethod]
        public async Task FailedAuthorityValidationTestAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            var user = labResponse.User;

            IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(labResponse.AppId)
                .WithAuthority("https://bogus.microsoft.com/common")
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            MsalServiceException exception = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                 pca.AcquireTokenByUsernamePassword(
                    s_scopes,
                     user.Upn,
                     new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword)
                     .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.IsTrue(exception.Message.Contains("AADSTS50049"));
            Assert.AreEqual("invalid_instance", exception.ErrorCode);
        }
    }
}
