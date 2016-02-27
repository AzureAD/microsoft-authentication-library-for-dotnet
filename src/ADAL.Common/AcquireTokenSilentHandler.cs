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
    internal class AcquireTokenSilentHandler : AcquireTokenHandlerBase
    {
        public AcquireTokenSilentHandler(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, UserIdentifier userId, bool callSync)
            : base(authenticator, tokenCache, resource, clientKey, clientKey.HasCredential ? TokenSubjectType.UserPlusClient : TokenSubjectType.User, callSync)
        {
            if (userId == null)
            {
                throw new ArgumentNullException("userId", AdalErrorMessage.SpecifyAnyUser);
            }

            this.UniqueId = userId.UniqueId;
            this.DisplayableId = userId.DisplayableId;
            this.UserIdentifierType = userId.Type;

            this.SupportADFS = true;
        }

        protected override Task<AuthenticationResult> SendTokenRequestAsync()
        {
            if (this.RefreshException == null)
            {
                Logger.Verbose(this.CallState, "No token matching arguments found in the cache");
                throw new AdalSilentTokenAcquisitionException();
            }

            throw new AdalSilentTokenAcquisitionException(this.RefreshException);
        }

        protected override void AddAditionalRequestParameters(RequestParameters requestParameters)
        {            
        }
    }
}
