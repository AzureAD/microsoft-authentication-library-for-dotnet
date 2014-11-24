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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AcquireTokenByRefreshTokenHandler : AcquireTokenHandlerBase
    {
        private readonly string refreshToken;

        public AcquireTokenByRefreshTokenHandler(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, string refreshToken, bool callSync)
            : base(authenticator, tokenCache, resource ?? NullResource, clientKey, TokenSubjectType.UserPlusClient, callSync)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentNullException("refreshToken");
            }

            if (!string.IsNullOrWhiteSpace(resource) && this.Authenticator.AuthorityType != AuthorityType.AAD)
            {
                throw new ArgumentException(AdalErrorMessage.UnsupportedMultiRefreshToken, "resource");
            }

            this.refreshToken = refreshToken;
            this.LoadFromCache = false;
            this.StoreToCache = false;

            this.SupportADFS = true;
        }

        protected async override Task<AuthenticationResult> SendTokenRequestAsync()
        {
            return await this.SendTokenRequestByRefreshTokenAsync(this.refreshToken);
        }

        protected override void AddAditionalRequestParameters(RequestParameters requestParameters)
        {                
        }
    }
}
