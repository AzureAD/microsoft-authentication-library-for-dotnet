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

using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Config;

namespace SampleApp
{
    class MsalAuthHelper
    {
        private readonly string _clientId;
        private readonly string user = ""; //can be empty for IWA and U/P

        public PublicClientApplication Application { get; private set; }

        public MsalAuthHelper(string clientId)
        {
            _clientId = clientId;
            Application = PublicClientApplicationBuilder
                          .Create(_clientId).WithAuthority("https://login.microsoftonline.com/organizations/", true, true)
                          .WithUserTokenCache(CachePersistence.GetUserCache())
                          .BuildConcrete();
        }

        public async Task<string> GetTokenForCurrentAccountAsync(IEnumerable<string> scopes, IAccount account)
        {
            AuthenticationResult result = null;
            try
            {
                result = await Application.AcquireTokenSilentAsync(scopes, account).ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                result = await Application.AcquireTokenAsync(scopes).ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to get token", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }

        public async Task<string> GetTokenWithUsernamePasswordAsync(IEnumerable<string> scopes, string password)
        {
            AuthenticationResult result = null;

            try
            {
                result = await Application.AcquireTokenByUsernamePasswordAsync(scopes, user, password).ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to get token", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }

        internal async Task<string> GetTokenWithIWAAsync(string[] scopes, string user)
        {
            AuthenticationResult result = null;

            try
            {
                result = await Application.AcquireTokenByIntegratedWindowsAuthAsync(scopes, user).ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to get token", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }
    }
}
