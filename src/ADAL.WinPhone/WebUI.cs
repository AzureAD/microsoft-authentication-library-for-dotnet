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
using System.Threading;
using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WebUI : IWebUI
    {
        private static SemaphoreSlim returnedUriReady;
        private static string authorizationResultUri;

        public async Task<string> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, callState);
            await returnedUriReady.WaitAsync();
            return authorizationResultUri;
        }

        public static void SetAuthorizationResultUri(string authorizationResultUriInput)
        {
            authorizationResultUri = authorizationResultUriInput;
            returnedUriReady.Release();
        }

        public void Authenticate(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            try
            {
                WebAuthenticationBroker.AuthenticateAndContinue(authorizationUri, ReferenceEquals(redirectUri, Constant.SsoPlaceHolderUri) ? null : redirectUri, null, WebAuthenticationOptions.None);
            }
            catch (Exception ex)
            {
                var adalEx = new AdalException(AdalError.AuthenticationUiFailed, ex);
                PlatformPlugin.Logger.LogException(callState, ex);
                throw adalEx;
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
