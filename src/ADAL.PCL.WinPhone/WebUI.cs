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
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WebUI : IWebUI
    {
        private static SemaphoreSlim returnedUriReady;
        private static AuthorizationResult authorizationResult;

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, callState);
            await returnedUriReady.WaitAsync();

            return authorizationResult;
        }

        public static void SetAuthorizationResultUri(WebAuthenticationResult webAuthenticationResult)
        {
            switch (webAuthenticationResult.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    authorizationResult = new AuthorizationResult(AuthorizationStatus.Success, webAuthenticationResult.ResponseData);
                    break;
                case WebAuthenticationStatus.ErrorHttp:
                    authorizationResult = new AuthorizationResult(AuthorizationStatus.ErrorHttp, null);
                    break;
                case WebAuthenticationStatus.UserCancel:
                    authorizationResult = new AuthorizationResult(AuthorizationStatus.UserCancel, null);
                    break;
                default:
                    authorizationResult = new AuthorizationResult(AuthorizationStatus.UnknownError, null);
                    break;
            }

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
                throw new AdalException(AdalError.AuthenticationUiFailed, ex);
            }
        }
    }
}
