// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License.

using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // Note: these tests require permission to a KeyVault Microsoft account; 
    // Please ignore them if you are not a Microsoft FTE, they will run as part of the CI build
    [TestClass]
    public class B2CUsernamePasswordIntegrationTests
    {
        private const string _b2CROPCAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_ROPC_Auth";

        private static readonly string[] s_b2cScopes = { "https://msidlabb2c.onmicrosoft.com/msidlabb2capi/read" };

        // If test fails with "user needs to consent to the application, do an interactive request" error,
        // Do the following: 
        // 1) Add in code to pull the user's password, and put a breakpoint there.
        // string password = ((LabUser)user).GetPassword();
        // 2) Using the MSAL Desktop app, make sure the ClientId matches the one used in integration testing.
        // 3) Do the interactive sign-in with the MSAL Desktop app with the username and password from step 1.
        // 4) After successful log-in, remove the password line you added in with step 1, and run the integration test again.
        [RunOn(TargetFrameworks.NetCore)]
        public async Task ROPC_B2C_Async()
        {
            var labResponse = await LabUserHelper.GetB2CLocalAccountAsync().ConfigureAwait(false);
            var user = labResponse.User;

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithB2CAuthority(_b2CROPCAuthority)
                .WithTestLogging()
                .Build();

            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_b2cScopes, user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            TestCommon.ValidateNoKerberosTicketFromAuthenticationResult(authResult);

            var acc = (await msalPublicClient.GetAccountsAsync().ConfigureAwait(false)).Single();
            var claimsPrincipal = acc.GetTenantProfiles().Single().ClaimsPrincipal;

            Assert.AreNotEqual(TokenResponseHelper.NullPreferredUsernameDisplayLabel, acc.Username);
            Assert.IsNotNull(claimsPrincipal.FindFirst("Name"));
            Assert.IsNotNull(claimsPrincipal.FindFirst("nbf"));
            Assert.IsNotNull(claimsPrincipal.FindFirst("exp"));

        }
    }
}
