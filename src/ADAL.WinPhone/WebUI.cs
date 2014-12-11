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
                // In lack of PromptBehavior in WinPhone, we always pass prompt=login.
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

            try
            {
                WebAuthenticationBroker.AuthenticateAndContinue(authorizationUri, ReferenceEquals(redirectUri, Constant.SsoPlaceHolderUri) ? null : redirectUri, set, WebAuthenticationOptions.None);
            }
            catch (Exception ex)
            {
                throw new AdalException(AdalError.AuthenticationUiFailed, ex);
            }
        }

        public AuthorizationResult ProcessAuthorizationResult(IWebAuthenticationBrokerContinuationEventArgs args, CallState callState)
        {
            AuthorizationResult result;
            switch (args.WebAuthenticationResult.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
					// Issue #129 - Windows Phone cannot handle ms-app URI's so use the placeholder URI for SSO
					var responseData = args.WebAuthenticationResult.ResponseData;
					if(responseData.StartsWith(Constant.MsAppScheme, StringComparison.OrdinalIgnoreCase))
					{
						responseData = Constant.SsoPlaceHolderUri + responseData.Substring(responseData.IndexOf('?'));
					}

					result = OAuth2Response.ParseAuthorizeResponse(responseData, callState);
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
