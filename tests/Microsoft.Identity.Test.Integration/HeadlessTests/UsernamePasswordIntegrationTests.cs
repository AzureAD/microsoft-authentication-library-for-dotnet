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
#if !WINDOWS_APP && !ANDROID && !iOS // U/P not available on UWP, Android and iOS
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // Note: these tests require permission to a KeyVault Microsoft account; 
    // Please ignore them if you are not a Microsoft FTE, they will run as part of the CI build
    [TestClass]
    public class UsernamePasswordIntegrationTests
    {
        public const string ClientId = "0615b6ca-88d4-4884-8729-b178178f7c27";
        public const string Authority = "https://login.microsoftonline.com/organizations/";
        public string[] Scopes = { "User.Read" };

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetState();
        }


        [TestMethod]
        [TestCategory("UsernamePasswordIntegrationTests")]
        public async Task AcquireTokenWithManagedUsernamePasswordAsync()
        {
            var user = LabUserHelper.GetDefaultUser().User;

            SecureString securePassword = new NetworkCredential("", LabUserHelper.GetUserPassword(user)).SecurePassword;

            PublicClientApplication msalPublicClient = new PublicClientApplication(ClientId, Authority);

            AuthenticationResult authResult = await msalPublicClient.AcquireTokenByUsernamePasswordAsync(Scopes, user.Upn, securePassword).ConfigureAwait(false);
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

        [TestMethod]
        [TestCategory("UsernamePasswordIntegrationTests")]
        public async Task AcquireTokenWithFederatedUsernamePasswordAsync()
        {
            var user = LabUserHelper.GetDefaultUser().User;

            SecureString securePassword = new NetworkCredential("", LabUserHelper.GetUserPassword(user)).SecurePassword;

            PublicClientApplication msalPublicClient = new PublicClientApplication(ClientId, Authority);
            AuthenticationResult authResult = await msalPublicClient.AcquireTokenByUsernamePasswordAsync(Scopes, user.Upn, securePassword).ConfigureAwait(false);
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

        [TestMethod]
        [TestCategory("UsernamePasswordIntegrationTests")]
        public void AcquireTokenWithManagedUsernameIncorrectPassword()
        {
            var user = LabUserHelper.GetDefaultUser().User;

            SecureString incorrectSecurePassword = new SecureString();
            incorrectSecurePassword.AppendChar('x');
            incorrectSecurePassword.MakeReadOnly();

            PublicClientApplication msalPublicClient = new PublicClientApplication(ClientId, Authority);

            var result = Assert.ThrowsExceptionAsync<MsalException>(async () =>
                 await msalPublicClient.AcquireTokenByUsernamePasswordAsync(Scopes, user.Upn, incorrectSecurePassword).ConfigureAwait(false));
        }

        [TestMethod]
        [TestCategory("UsernamePasswordIntegrationTests")]
        public void AcquireTokenWithFederatedUsernameIncorrectPassword()
        {
            var user = LabUserHelper.GetDefaultUser().User;

            SecureString incorrectSecurePassword = new SecureString();
            incorrectSecurePassword.AppendChar('x');
            incorrectSecurePassword.MakeReadOnly();

            PublicClientApplication msalPublicClient = new PublicClientApplication(ClientId, Authority);

            var result = Assert.ThrowsExceptionAsync<MsalException>(async () =>
                 await msalPublicClient.AcquireTokenByUsernamePasswordAsync(Scopes, user.Upn, incorrectSecurePassword).ConfigureAwait(false));
        }
    }
}
#endif
