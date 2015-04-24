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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class AcquireTokenByAuthorizationCodeHandler : AcquireTokenHandlerBase
    {
        private readonly string authorizationCode;

        private readonly Uri redirectUri;

        public AcquireTokenByAuthorizationCodeHandler(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, string authorizationCode, Uri redirectUri)
            : base(authenticator, tokenCache, resource ?? NullResource, clientKey, TokenSubjectType.UserPlusClient)
        {
            if (string.IsNullOrWhiteSpace(authorizationCode))
            {
                throw new ArgumentNullException("authorizationCode");
            }

            this.authorizationCode = authorizationCode;

            if (redirectUri == null)
            {
                throw new ArgumentNullException("redirectUri");
            }

            this.redirectUri = redirectUri;

            this.LoadFromCache = false;

            this.SupportADFS = true;
        }

        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.AuthorizationCode;
            requestParameters[OAuthParameter.Code] = this.authorizationCode;
            requestParameters[OAuthParameter.RedirectUri] = this.redirectUri.OriginalString;
        }

        protected override void PostTokenRequest(AuthenticationResultEx resultEx)
        {
            base.PostTokenRequest(resultEx);
            UserInfo userInfo = resultEx.Result.UserInfo;
            this.UniqueId = (userInfo == null) ? null : userInfo.UniqueId;
            this.DisplayableId = (userInfo == null) ? null : userInfo.DisplayableId;
            if (resultEx.ResourceInResponse != null)
            {
                this.Resource = resultEx.ResourceInResponse;
                PlatformPlugin.Logger.Verbose(this.CallState, "Resource value in the token response was used for storing tokens in the cache");
            }

            // If resource is not passed as an argument and is not returned by STS either, 
            // we cannot store the token in the cache with null resource.
            // TODO: Store refresh token though if STS supports MRRT.
            this.StoreToCache = this.StoreToCache && (this.Resource != null);
        }
    }
}
