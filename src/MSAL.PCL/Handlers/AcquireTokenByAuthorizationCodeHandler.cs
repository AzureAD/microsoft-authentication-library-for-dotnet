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
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Handlers
{
    class AcquireTokenByAuthorizationCodeHandler : AcquireTokenHandlerBase
    {
        private readonly string authorizationCode;

        private readonly Uri redirectUri;

        public AcquireTokenByAuthorizationCodeHandler(HandlerData handlerData, string authorizationCode, Uri redirectUri)
            : base(handlerData)
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
            this.SupportADFS = false;
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
            this.User = resultEx.Result.User;
        }
    }
}
