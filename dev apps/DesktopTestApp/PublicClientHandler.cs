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

        public PublicClientHandler(string clientId)
        {
            ApplicationId = clientId;
            PublicClientApplication = new PublicClientApplication(ApplicationId)
            {
                UserTokenCache = TokenCacheHelper.GetUserCache()
            };
        }

        #region Properties
        public string ApplicationId { get; set; }

        public string AuthorityOverride { get; set; }

        public string[] Scopes { get; set; }

        public string ExtraQueryParams { get; set; }

        public string LoginHint { get; set; }

        public IUser CurrentUser { get; set; }

        public PublicClientApplication PublicClientApplication { get; set; }

        #endregion

        public async Task<AuthenticationResult> AcquireTokenInteractive(string overriddenAuthority, string[] scopes, IUser currentUser,
            UIBehavior uiBehavior, string extraQueryParams, UIParent uiParent, string loginHint)
        {
            PublicClientApplication = CreatePublicClientApplication(overriddenAuthority, ApplicationId);

            AuthenticationResult result;
            if (currentUser != null)
            {
                result = await PublicClientApplication.AcquireTokenAsync(scopes, CurrentUser, uiBehavior,
                    extraQueryParams,
                    uiParent);
            }
            else
            {
                result =
                    await PublicClientApplication.AcquireTokenAsync(scopes, loginHint, uiBehavior,
                        extraQueryParams);
            }

            CurrentUser = result.User;
            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenSilent(string[] scopes, IUser currentUser)
        {
            AuthenticationResult result = await PublicClientApplication.AcquireTokenSilentAsync(scopes, currentUser);

            return result;
        }

        private PublicClientApplication CreatePublicClientApplication(string overrriddenAuthority, string applicationId)
        {
            if (string.IsNullOrEmpty(overrriddenAuthority))
            {
                // Use default authority
                PublicClientApplication = new PublicClientApplication(applicationId)
                {
                    UserTokenCache = TokenCacheHelper.UsertokenCache
                };
            }
            else
            {
                // Use the override authority provided
                PublicClientApplication = new PublicClientApplication(applicationId, overrriddenAuthority)
                {
                    UserTokenCache = TokenCacheHelper.UsertokenCache
                };
            }
            return PublicClientApplication;
        }
    }
}
