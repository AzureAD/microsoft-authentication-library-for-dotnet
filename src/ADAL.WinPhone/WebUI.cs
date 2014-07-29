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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;

using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WebUI : IWebUI
    {
        public WebUI()
        {
        }

        public PromptBehavior PromptBehavior
        {
            get
            {
                return PromptBehavior.Always; 
            }            
        }

        public void Authenticate(Uri authorizationUri, Uri redirectUri, IDictionary<string, object> headersMap, CallState callState)
        {
            ValueSet set = new ValueSet();
            foreach (string key in headersMap.Keys)
            {
                set[key] = headersMap[key];
            }

            if (redirectUri.AbsoluteUri == WebAuthenticationBroker.GetCurrentApplicationCallbackUri().AbsoluteUri)
            {
                // SSO Mode

                try
                {
                    WebAuthenticationBroker.AuthenticateAndContinue(authorizationUri, null, set, WebAuthenticationOptions.None);
                }
                catch (FileNotFoundException ex)
                {
                    throw new AdalException(AdalError.AuthenticationUiFailed, ex);
                }
            }
            else if (redirectUri.Scheme == "ms-app")
            {
                throw new ArgumentException(AdalErrorMessage.RedirectUriAppIdMismatch, "redirectUri");
            }
            else
            {
                try
                {
                    // Non-SSO Mode
                    WebAuthenticationBroker.AuthenticateAndContinue(authorizationUri, redirectUri, set, WebAuthenticationOptions.None);
                }
                catch (FileNotFoundException ex)
                {
                    throw new AdalException(AdalError.AuthenticationUiFailed, ex);
                }
            }
        }

        public AuthorizationResult ProcessAuthorizationResult(IWebAuthenticationBrokerContinuationEventArgs args, CallState callState)
        {
            AuthorizationResult result;
            switch (args.WebAuthenticationResult.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    result = OAuth2Response.ParseAuthorizeResponse(args.WebAuthenticationResult.ResponseData, callState);
                    break;
                case WebAuthenticationStatus.ErrorHttp:
                    result = new AuthorizationResult(AdalError.AuthenticationFailed, args.WebAuthenticationResult.ResponseErrorDetail.ToString());
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
