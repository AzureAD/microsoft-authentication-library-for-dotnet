//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AcquireTokenOnBehalfHandler : AcquireTokenHandlerBase
    {
        private readonly UserAssertion userAssertion;

        public AcquireTokenOnBehalfHandler(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, UserAssertion userAssertion)
            : base(authenticator, tokenCache, resource, clientKey, TokenSubjectType.UserPlusClient)
        {
            if (userAssertion == null)
            {
                throw new ArgumentNullException("userAssertion");
            }

            this.userAssertion = userAssertion;
            this.DisplayableId = userAssertion.UserName;
            CacheQueryData.AssertionHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(userAssertion.Assertion);

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
                    if (!this.ResultEx.UserAssertionHash.Equals(CacheQueryData.AssertionHash))
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
                resultEx.UserAssertionHash = CacheQueryData.AssertionHash;
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
