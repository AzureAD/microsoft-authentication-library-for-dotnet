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
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, new NetworkCredential("", user.Password).SecurePassword)
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
