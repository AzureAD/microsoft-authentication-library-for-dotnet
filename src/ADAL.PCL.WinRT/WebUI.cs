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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WebUI : IWebUI
    {
        private readonly PromptBehavior promptBehavior;
        private readonly bool useCorporateNetwork;

        public WebUI(IAuthorizationParameters parameters)
        {
            if (!(parameters is AuthorizationParameters))
            {
                throw new ArgumentException("parameters should be of type AuthorizationParameters", "parameters");
            }

            this.promptBehavior = ((AuthorizationParameters)parameters).PromptBehavior;
            this.useCorporateNetwork = ((AuthorizationParameters)parameters).UseCorporateNetwork;
        }

        public string AuthorizationResultUri { get; private set; }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            bool ssoMode = ReferenceEquals(redirectUri, Constant.SsoPlaceHolderUri);
            if (this.promptBehavior == PromptBehavior.Never && !ssoMode && redirectUri.Scheme != Constant.MsAppScheme)
            {
                var ex = new ArgumentException(AdalErrorMessageEx.RedirectUriUnsupportedWithPromptBehaviorNever, "redirectUri");
                PlatformPlugin.Logger.LogException(callState, ex);
                throw ex;
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
                var adalEx = new AdalException(AdalError.AuthenticationUiFailed, ex);
                PlatformPlugin.Logger.LogException(callState, adalEx);
                throw adalEx;
            }
            catch (Exception ex)
            {
                if (this.promptBehavior == PromptBehavior.Never)
                {
                    var adalEx = new AdalException(AdalError.UserInteractionRequired, ex);
                    PlatformPlugin.Logger.LogException(callState, adalEx);
                    throw adalEx;
                }

                var uiFailedEx = new AdalException(AdalError.AuthenticationUiFailed, ex);
                PlatformPlugin.Logger.LogException(callState, uiFailedEx);
                throw uiFailedEx;
            }

            AuthorizationResult result = ProcessAuthorizationResult(webAuthenticationResult, callState);

            return result;
        }

        private static AuthorizationResult ProcessAuthorizationResult(WebAuthenticationResult webAuthenticationResult, CallState callState)
        {
            AuthorizationResult result;
            switch (webAuthenticationResult.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    result = new AuthorizationResult(AuthorizationStatus.Success, webAuthenticationResult.ResponseData);
                    break;
                case WebAuthenticationStatus.ErrorHttp:
                    result = new AuthorizationResult(AuthorizationStatus.ErrorHttp, webAuthenticationResult.ResponseErrorDetail.ToString());
                    break;
                case WebAuthenticationStatus.UserCancel:
                    result = new AuthorizationResult(AuthorizationStatus.UserCancel, null);
                    break;
                default:
                    result = new AuthorizationResult(AuthorizationStatus.UnknownError, null);
                    break;
            }

            return result;
        }
    }
}
