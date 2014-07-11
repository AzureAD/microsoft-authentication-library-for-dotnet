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
    class AcquireTokenByAuthorizationCodeHandler : AcquireTokenHandlerBase
    {
        private readonly string authorizationCode;

        private readonly Uri redirectUri;

        public AcquireTokenByAuthorizationCodeHandler(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, string authorizationCode, Uri redirectUri, bool callSync)
            : base(authenticator, tokenCache, resource ?? NullResource, clientKey, TokenSubjectType.UserPlusClient, callSync)
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
        }

        protected override async Task<AuthenticationResult> SendTokenRequestAsync()
        {
            RequestParameters requestParameters = OAuth2MessageHelper.CreateTokenRequest(this.authorizationCode, this.redirectUri, this.Resource, this.ClientKey, this.Authenticator.SelfSignedJwtAudience);
            AuthenticationResult result = await this.SendHttpMessageAsync(requestParameters);

            this.UniqueId = (result.UserInfo == null) ? null : result.UserInfo.UniqueId;
            this.DisplayableId = (result.UserInfo == null) ? null : result.UserInfo.DisplayableId;
            this.Resource = result.Resource;

            return result;
        }
    }
}
