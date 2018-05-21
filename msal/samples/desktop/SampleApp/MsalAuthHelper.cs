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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;

namespace SampleApp
{
    class MsalAuthHelper
    {
        private string _clientId;

        public PublicClientApplication Application { get; private set; }

        public MsalAuthHelper(string clientId)
        {
            _clientId = clientId;
            Application = new PublicClientApplication(_clientId, "https://login.microsoftonline.com/common/",
                CachePersistence.GetUserCache());
        }

        public async Task<IUser> SignIn()
        {
            try
            {
                AuthenticationResult result = await Application.AcquireTokenAsync(new[] {"user.read", "calendars.read"}).ConfigureAwait(false);
                return result.User;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Sign in failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }

        public async Task<string> GetTokenForCurrentUser(IEnumerable<string> scopes, IUser user)
        {
            AuthenticationResult result = null;
            Exception exception = null;
            try
            {
                result = await Application.AcquireTokenAsync(scopes, user).ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    result = await Application.AcquireTokenAsync(scopes, user)
                        .ConfigureAwait(false);
                    return result.AccessToken;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                MessageBox.Show(exception.Message, "Failed to get token", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }

    }
}
