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
        private IPlatformParameters parameters;


        public AcquireTokenSilentHandler(Authenticator authenticator, TokenCache tokenCache, string[] scope, ClientKey clientKey, string userIdentifer, IPlatformParameters parameters, string policy) 
            : base(authenticator, tokenCache, scope, clientKey, policy, clientKey.HasCredential ? TokenSubjectType.UserPlusClient : TokenSubjectType.User)
        {
            //TODO look up userIdentifier in the cache and get a user object
        }

        public AcquireTokenSilentHandler(Authenticator authenticator, TokenCache tokenCache, string[] scope, ClientKey clientKey, User userId, IPlatformParameters parameters, string policy)
            : base(authenticator, tokenCache, scope, clientKey, policy, clientKey.HasCredential ? TokenSubjectType.UserPlusClient : TokenSubjectType.User)
        {
            if (userId == null)
            {
                throw new ArgumentNullException("userId", MsalErrorMessage.SpecifyAnyUser);
            }

            this.UniqueId = userId.UniqueId;
            this.DisplayableId = userId.DisplayableId;
            PlatformPlugin.BrokerHelper.PlatformParameters = parameters;    
            this.SupportADFS = false;
            
            this.brokerParameters["username"] = userId.DisplayableId;
            this.brokerParameters["silent_broker_flow"] = null; //add key
        }

        protected override Task<AuthenticationResultEx> SendTokenRequestAsync()
        {
            PlatformPlugin.Logger.Verbose(this.CallState, "No token matching arguments found in the cache");
            throw new MsalSilentTokenAcquisitionException();
        }

        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {            
        }
    }
}
