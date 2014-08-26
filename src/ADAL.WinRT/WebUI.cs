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
        private readonly PromptBehavior promptBehavior;
        private readonly bool useCorporateNetwork;

        public WebUI(PromptBehavior promptBehavior, bool useCorporateNetwork)
        {
            this.promptBehavior = promptBehavior;
            this.useCorporateNetwork = useCorporateNetwork;
        }

        public async Task<AuthorizationResult> AuthenticateAsync(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            bool ssoMode = ReferenceEquals(redirectUri, Constant.SsoPlaceHolderUri);
            if (this.promptBehavior == PromptBehavior.Never && !ssoMode && redirectUri.Scheme != Constant.MsAppScheme)
            {
                throw new ArgumentException(AdalErrorMessage.RedirectUriUnsupportedWithPromptBehaviorNever, "redirectUri");
            }
            
            WebAuthenticationResult webAuthenticationResult;

            WebAuthenticationOptions options = (this.useCorporateNetwork && (ssoMode || redirectUri.Scheme == Constant.MsAppScheme)) ? WebAuthenticationOptions.UseCorporateNetwork : WebAuthenticationOptions.None;

            if (this.promptBehavior == PromptBehavior.Never)
            {
                options |= WebAuthenticationOptions.SilentMode;
            }

            try
            {
                if (ssoMode)
                {
                    webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(options, authorizationUri);
                }
                else
                { 
                    webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(options, authorizationUri, redirectUri);
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new AdalException(AdalError.AuthenticationUiFailed, ex);
            }
            catch (Exception ex)
            {
                if (this.promptBehavior == PromptBehavior.Never)
                {
                    throw new AdalException(AdalError.UserInteractionRequired, ex);
                }

                throw new AdalException(AdalError.AuthenticationUiFailed, ex);
            }

            return ProcessAuthorizationResult(webAuthenticationResult, callState);
        }

        private static AuthorizationResult ProcessAuthorizationResult(WebAuthenticationResult webAuthenticationResult, CallState callState)
        {
            AuthorizationResult result;
            switch (webAuthenticationResult.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    result = OAuth2Response.ParseAuthorizeResponse(webAuthenticationResult.ResponseData, callState);
                    break;
                case WebAuthenticationStatus.ErrorHttp:
                    result = new AuthorizationResult(AdalError.AuthenticationFailed, webAuthenticationResult.ResponseErrorDetail.ToString());
                    break;
                case WebAuthenticationStatus.UserCancel:
                    result = new AuthorizationResult(AdalError.AuthenticationCanceled, AdalErrorMessage.AuthenticationCanceled);
                    break;
                default:
                    result = new AuthorizationResult(AdalError.AuthenticationFailed, AdalErrorMessage.AuthorizationServerInvalidResponse);
                    break;
            }

            return result;
        }
    }
}
