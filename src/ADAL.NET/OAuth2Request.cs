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
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{

    internal static partial class OAuth2Request
    {
        public static AuthorizationResult SendAuthorizeRequest(Authenticator authenticator, string resource, Uri redirectUri, string clientId, UserIdentifier userId, PromptBehavior promptBehavior, string extraQueryParameters, IWebUI webUi, CallState callState)
        {
            if (!string.IsNullOrWhiteSpace(redirectUri.Fragment))
            {
                throw new ArgumentException(AdalErrorMessage.RedirectUriContainsFragment, "redirectUri");
            }

            Uri authorizationUri = CreateAuthorizationUri(authenticator, resource, redirectUri, clientId, userId, promptBehavior, extraQueryParameters, IncludeFormsAuthParams(), callState);
            string resultUri = webUi.Authenticate(authorizationUri, redirectUri);
            return OAuth2Response.ParseAuthorizeResponse(resultUri, callState);
        }

        public static async Task<AuthenticationResult> SendTokenRequestAsync(string uri, string code, Uri redirectUri, string resource, ClientKey clientKey, string audience, CallState callState)
        {
            RequestParameters requestParameters = OAuth2MessageHelper.CreateTokenRequest(code, redirectUri, resource, clientKey);

            AuthenticationResult result = await SendHttpMessageAsync(uri, requestParameters, callState);
            return result;
        }

        public static async Task<AuthenticationResult> SendTokenRequestAsync(string uri, string resource, ClientKey clientKey, CallState callState)
        {
            RequestParameters requestParameters = OAuth2MessageHelper.CreateTokenRequest(resource, clientKey);

            AuthenticationResult result = await SendHttpMessageAsync(uri, requestParameters, callState);
            return result;
        }

        internal static async Task<AuthenticationResult> SendTokenRequestByRefreshTokenAsync(string uri, string resource, string refreshToken, string clientId, ClientKey clientKey, CallState callState)
        {
            RequestParameters requestParameters = OAuth2MessageHelper.CreateTokenRequest(resource, refreshToken, clientId, clientKey);
            AuthenticationResult result = await SendHttpMessageAsync(uri, requestParameters, callState);

            if (result.RefreshToken == null)
            {
                result.RefreshToken = refreshToken;
            }

            return result;
        }

        internal static async Task<AuthenticationResult> SendTokenRequestOnBehalfAsync(string uri, string resource, UserAssertion userCredential, ClientKey clientKey, CallState callState)
        {
            RequestParameters requestParameters = OAuth2MessageHelper.CreateTokenRequest(resource, userCredential, clientKey);
            AuthenticationResult result = await SendHttpMessageAsync(uri, requestParameters, callState);
            return result;
        }

        public static bool IncludeFormsAuthParams()
        {
            return PlatformSpecificHelper.IsDomainJoined() && PlatformSpecificHelper.IsUserLocal();
        }
    }
}