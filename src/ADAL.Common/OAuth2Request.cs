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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static partial class OAuth2Request
    {
        public static async Task<AuthenticationResult> SendTokenRequestAsync(string uri, string code, Uri redirectUri, string resource, string clientId, CallState callState)
        {
            RequestParameters requestParameters = OAuth2MessageHelper.CreateTokenRequest(code, redirectUri, resource, clientId);
            AuthenticationResult result = await SendHttpMessageAsync(uri, requestParameters, callState);
            return result;
        }

        public static async Task<AuthenticationResult> SendTokenRequestWithUserAssertionAsync(string uri, string resource, string clientId, UserAssertion credential, CallState callState)
        {
            RequestParameters requestParameters = OAuth2MessageHelper.CreateTokenRequest(resource, clientId, credential);

            AuthenticationResult result = await SendHttpMessageAsync(uri, requestParameters, callState);
            return result;
        }

        public static async Task<AuthenticationResult> SendTokenRequestByRefreshTokenAsync(string uri, string resource, string refreshToken, string clientId, CallState callState)
        {
            RequestParameters requestParameters = OAuth2MessageHelper.CreateTokenRequest(resource, refreshToken, clientId);
            AuthenticationResult result = await SendHttpMessageAsync(uri, requestParameters, callState);

            if (result.RefreshToken == null)
            {
                result.RefreshToken = refreshToken;
            }

            return result;
        }

        public static async Task<AuthenticationResult> SendTokenRequestWithUserCredentialAsync(string uri, string resource, string clientId, UserCredential credential, CallState callState)
        {
            RequestParameters requestParameters = OAuth2MessageHelper.CreateTokenRequest(resource, clientId, credential);
            AuthenticationResult result = await SendHttpMessageAsync(uri, requestParameters, callState);
            return result;
        }

        private static async Task<AuthenticationResult> SendHttpMessageAsync(string uri, RequestParameters requestParameters, CallState callState)
        {
            uri = HttpHelper.CheckForExtraQueryParameter(uri);

            TokenResponse tokenResponse = await HttpHelper.SendPostRequestAndDeserializeJsonResponseAsync<TokenResponse>(uri, requestParameters, callState);

            return OAuth2Response.ParseTokenResponse(tokenResponse);
        }

        private static Uri CreateAuthorizationUri(Authenticator authenticator, string resource, Uri redirectUri, string clientId, string userId, PromptBehavior promptBehavior, string extraQueryParameters, CallState callState)
        {
            RequestParameters requestParameters = OAuth2MessageHelper.CreateAuthorizationRequest(resource, clientId, redirectUri, userId, promptBehavior, extraQueryParameters, callState);
 
            var authorizationUri = new Uri(new Uri(authenticator.AuthorizationUri), "?" + requestParameters);
            authorizationUri = new Uri(HttpHelper.CheckForExtraQueryParameter(authorizationUri.AbsoluteUri));
 
            return authorizationUri;
        }
    }
}
