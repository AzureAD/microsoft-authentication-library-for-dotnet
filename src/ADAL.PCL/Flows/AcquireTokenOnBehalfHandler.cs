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
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AcquireTokenOnBehalfHandler : AcquireTokenHandlerBase
    {
        private readonly UserAssertion userAssertion;

        public AcquireTokenOnBehalfHandler(RequestData requestData, UserAssertion userAssertion)
            : base(requestData)
        {
            if (userAssertion == null)
            {
                throw new ArgumentNullException("userAssertion");
            }

            this.userAssertion = userAssertion;
            this.DisplayableId = userAssertion.UserName;
            CacheQueryData.AssertionHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(userAssertion.Assertion);

            PlatformPlugin.Logger.Verbose(CallState,
                string.Format(CultureInfo.InvariantCulture,
                    "Username provided in user assertion - " + string.IsNullOrEmpty(this.DisplayableId)));
            this.SupportADFS = true;
        }

        protected override async Task<AuthenticationResultEx> SendTokenRequestAsync()
        {
            AuthenticationResultEx resultEx = await base.SendTokenRequestAsync().ConfigureAwait(false);
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
