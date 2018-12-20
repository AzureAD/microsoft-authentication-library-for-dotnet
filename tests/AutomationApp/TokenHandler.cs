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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Config;

namespace AutomationApp
{
    public class TokenHandler
    {
        #region Properties
        public IAccount CurrentUser { get; set; }

        private PublicClientApplication _publicClientApplication;
        #endregion

        private void EnsurePublicClientApplication(Dictionary<string, string> input)
        {
            if (_publicClientApplication != null)
            {
                return;
            }

            if (!input.ContainsKey("client_id"))
            {
                return;
            }

            _publicClientApplication = input.ContainsKey("authority")
                ? PublicClientApplicationBuilder.Create(input["client_id"]).WithAuthority(input["authority"], true, true).BuildConcrete()
                : PublicClientApplicationBuilder.Create(input["client_id"]).BuildConcrete();
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            AuthenticationResult result =
                await
                    _publicClientApplication.AcquireTokenAsync(scope)
                        .ConfigureAwait(false);
            CurrentUser = result.Account;
            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenSilentAsync(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            AuthenticationResult result = await
                _publicClientApplication.AcquireTokenSilentAsync(scope, CurrentUser)
                .ConfigureAwait(false);

            return result;
        }

        public void ExpireAccessToken(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            _publicClientApplication.RemoveAsync(CurrentUser);
        }
    }
}
