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
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AcquireTokenOnBehalfHandler : AcquireTokenHandlerBase
    {
        private readonly UserAssertion userAssertion;
        private readonly string assertionHash;

        public AcquireTokenOnBehalfHandler(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, UserAssertion userAssertion)
            : base(authenticator, tokenCache, resource, clientKey, TokenSubjectType.UserPlusClient)
        {
            if (userAssertion == null)
            {
                throw new ArgumentNullException("userAssertion");
            }

            this.userAssertion = userAssertion;
            this.DisplayableId = userAssertion.UserName;
            this.assertionHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(userAssertion.Assertion);

            this.SupportADFS = true;
        }

        protected override void ValidateResult()
        {
            // cache lookup returned a token. no username provided in the assertion. 
            // cannot deterministicly identify the user. fallback to compare hash. 
            if (this.ResultEx != null && string.IsNullOrEmpty(userAssertion.UserName))
            {
                //if cache result does not contain hash then return null
                if (!string.IsNullOrEmpty(this.ResultEx.UserAssertionHash))
                {
                    //if user assertion hash does not match then return null
                    if (!this.ResultEx.UserAssertionHash.Equals(assertionHash))
                    {
                        this.ResultEx = null;
                    }
                }
                else
                {
                    this.ResultEx = null;
                }
            }

            //return as is if it is null or provided userAssertion contains username
           
        }


        protected override async Task<AuthenticationResultEx> SendTokenRequestAsync()
        {
            AuthenticationResultEx resultEx = await base.SendTokenRequestAsync();
            if (resultEx != null)
            {
                resultEx.UserAssertionHash = this.assertionHash;
            }

            return resultEx;
        }

        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.JwtBearer;
            requestParameters[OAuthParameter.Assertion] = this.userAssertion.Assertion;
            requestParameters[OAuthParameter.RequestedTokenUse] = OAuthRequestedTokenUse.OnBehalfOf;

            // To request id_token in response
            requestParameters[OAuthParameter.Scope] = OAuthValue.ScopeOpenId;
        }
    }
}
