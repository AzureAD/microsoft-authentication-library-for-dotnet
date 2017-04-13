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

using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace DesktopTestApp
{
    class PublicClientHandler
    {
        public IUser CurrentUser;

        public PublicClientApplication PublicClientApplication;

        public async Task<AuthenticationResult> AcquireTokenInteractive(string overriddenAuthority, string applicationId, string[] scopes, IUser currentUser,
            UIBehavior uiBehavior, string extraQueryParams, UIParent uiParent, string loginHint)
        {
            PublicClientApplication publicClientApplication = CreateClientApplication(overriddenAuthority, applicationId);

            AuthenticationResult result;
            if (currentUser != null)
            {
                result = await publicClientApplication.AcquireTokenAsync(scopes, CurrentUser, uiBehavior,
                    extraQueryParams,
                    uiParent);
            }
            else
            {
                result =
                    await publicClientApplication.AcquireTokenAsync(scopes, loginHint, uiBehavior,
                        extraQueryParams);
            }

            CurrentUser = result.User;
            return (result);
        }

        private PublicClientApplication CreateClientApplication(string overrriddenAuthority, string applicationId)
        {
            if (string.IsNullOrEmpty(overrriddenAuthority))
            {
                // Use default authority
                PublicClientApplication = new PublicClientApplication(applicationId);
            }
            else
            {
                // Use the override authority provided
                PublicClientApplication = new PublicClientApplication(applicationId, overrriddenAuthority);
            }
            return PublicClientApplication;
        }
    }
}
