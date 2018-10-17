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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class OnBehalfOfRequest : RequestBase
    {
        public OnBehalfOfRequest(
            IHttpManager httpManager, 
            ICryptographyManager cryptographyManager,
            AuthenticationRequestParameters authenticationRequestParameters)
            : base(httpManager, cryptographyManager, authenticationRequestParameters)
        {
            if (authenticationRequestParameters.UserAssertion == null)
            {
                throw new ArgumentNullException(nameof(authenticationRequestParameters.UserAssertion));
            }
        }

        internal override async Task PreTokenRequestAsync(CancellationToken cancellationToken)
        {
            await base.PreTokenRequestAsync(cancellationToken).ConfigureAwait(false);

            // look for access token in the cache first.
            // no access token is found, then it means token does not exist
            // or new assertion has been passed. We should not use Refresh Token
            // for the user because the new incoming token may have updated claims
            // like mfa etc.
            if (LoadFromCache)
            {
                MsalAccessTokenItem = 
                    await TokenCache.FindAccessTokenAsync(AuthenticationRequestParameters).ConfigureAwait(false);
            }
        }

        protected override async Task SendTokenRequestAsync(CancellationToken cancellationToken)
        {
            if (MsalAccessTokenItem == null)
            {
                await base.SendTokenRequestAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        protected override void SetAdditionalRequestParameters(OAuth2Client client)
        {
            client.AddBodyParameter(OAuth2Parameter.GrantType,
                AuthenticationRequestParameters.UserAssertion.AssertionType);
            client.AddBodyParameter(OAuth2Parameter.Assertion, AuthenticationRequestParameters.UserAssertion.Assertion);
            client.AddBodyParameter(OAuth2Parameter.RequestedTokenUse, OAuth2RequestedTokenUse.OnBehalfOf);
        }
    }
}