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

        public string InteractiveAuthority { get; set; }

        public string AuthorityOverride { get; set; }

        public string ExtraQueryParams { get; set; }

        public string LoginHint { get; set; }

        public IUser CurrentUser { get; set; }

        public PublicClientApplication PublicClientApplication { get; set; }

        #endregion

        public async Task<AuthenticationResult> AcquireTokenInteractive(string[] scopes, UIBehavior uiBehavior, string extraQueryParams, UIParent uiParent)
        {
            CreatePublicClientApplication(InteractiveAuthority, ApplicationId);

            AuthenticationResult result;
            if (CurrentUser != null)
            {
                result = await PublicClientApplication.AcquireTokenAsync(scopes, CurrentUser, uiBehavior,
                    extraQueryParams,
                    uiParent);
            }
            else
            {
                result =
                    await PublicClientApplication.AcquireTokenAsync(scopes, LoginHint, uiBehavior,
                        extraQueryParams,
                        uiParent);
            }

            CurrentUser = result.User;
            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenInteractiveWithAuthority(string[] scopes, UIBehavior uiBehavior, string extraQueryParams, UIParent uiParent)
        {
            CreatePublicClientApplication(InteractiveAuthority, ApplicationId);

            AuthenticationResult result;
            if (CurrentUser != null)
            {
                result = await PublicClientApplication.AcquireTokenAsync(scopes, CurrentUser, uiBehavior,
                    extraQueryParams, null, AuthorityOverride,
                    uiParent);
            }
            else
            {
                result =
                    await PublicClientApplication.AcquireTokenAsync(scopes, LoginHint, uiBehavior,
                        extraQueryParams, null, AuthorityOverride,
                        uiParent);
            }

            CurrentUser = result.User;
            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenSilent(string[] scopes)
        {
            return await PublicClientApplication.AcquireTokenSilentAsync(scopes, CurrentUser, AuthorityOverride,
                        false);
        }

        private void CreatePublicClientApplication(string interactiveAuthority, string applicationId)
        {
            if (string.IsNullOrEmpty(interactiveAuthority))
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
                PublicClientApplication = new PublicClientApplication(applicationId, interactiveAuthority)
                {
                    UserTokenCache = TokenCacheHelper.UsertokenCache
                };
            }
        }
    }
}
