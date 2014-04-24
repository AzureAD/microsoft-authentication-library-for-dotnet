//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;

using Windows.Security.Authentication.Web;

using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WebUI : IWebUI
    {
        private readonly bool useCorporateNetwork;

        public WebUI(bool useCorporateNetwork)
        {
            this.useCorporateNetwork = useCorporateNetwork;
        }

        public async Task<AuthorizationResult> AuthenticateAsync(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            WebAuthenticationResult webAuthenticationResult;

            try
            {
                if (redirectUri.AbsoluteUri == WebAuthenticationBroker.GetCurrentApplicationCallbackUri().AbsoluteUri)
                {
                    if (this.useCorporateNetwork)
                    {
                        // SSO Mode with CorporateNetwork
                        webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.UseCorporateNetwork, authorizationUri);
                    }
                    else
                    {
                        // SSO Mode
                        webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, authorizationUri);
                    }
                }
                else if (redirectUri.Scheme == "ms-app")
                {
                    throw new ArgumentException(ActiveDirectoryAuthenticationErrorMessage.RedirectUriAppIdMismatch, "redirectUri");
                }
                else
                {
                    // Non-SSO Mode
                    webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, authorizationUri, redirectUri);
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new ActiveDirectoryAuthenticationException(ActiveDirectoryAuthenticationError.AuthenticationUiFailed, ex);
            }

            AuthorizationResult result;

            switch (webAuthenticationResult.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    result = OAuth2Response.ParseAuthorizeResponse(webAuthenticationResult.ResponseData, callState);
                    break;
                case WebAuthenticationStatus.ErrorHttp:
                    result = new AuthorizationResult(ActiveDirectoryAuthenticationError.AuthenticationFailed, webAuthenticationResult.ResponseErrorDetail.ToString());
                    break;
                case WebAuthenticationStatus.UserCancel:
                    result = new AuthorizationResult(ActiveDirectoryAuthenticationError.AuthenticationCanceled, ActiveDirectoryAuthenticationErrorMessage.AuthenticationCanceled);
                    break;
                default:
                    result = new AuthorizationResult(ActiveDirectoryAuthenticationError.AuthenticationFailed, ActiveDirectoryAuthenticationErrorMessage.AuthorizationServerInvalidResponse);
                    break;
            }

            return result;
        }
    }
}
