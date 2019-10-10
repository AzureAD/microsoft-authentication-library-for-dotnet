// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License.

using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
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

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestMethod]
        public async Task ROPC_B2C_Async()
        {
            var labResponse = await LabUserHelper.GetB2CLocalAccountAsync().ConfigureAwait(false);
            await RunB2CHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        private async Task RunB2CHappyPathTestAsync(LabResponse labResponse)
        {
            var user = labResponse.User;

            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithB2CAuthority(_b2CROPCAuthority)
                .Build();

            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_b2cScopes, user.Upn, securePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            // If test fails with "user needs to consent to the application, do an interactive request" error,
            // Do the following: 
            // 1) Add in code to pull the user's password before creating the SecureString, and put a breakpoint there.
            // string password = ((LabUser)user).GetPassword();
            // 2) Using the MSAL Desktop app, make sure the ClientId matches the one used in integration testing.
            // 3) Do the interactive sign-in with the MSAL Desktop app with the username and password from step 1.
            // 4) After successful log-in, remove the password line you added in with step 1, and run the integration test again.
        }
    }
}
