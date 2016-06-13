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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class AcquireTokenByAuthorizationCodeHandler : AcquireTokenHandlerBase
    {
        private readonly string authorizationCode;

        private readonly Uri redirectUri;

        public AcquireTokenByAuthorizationCodeHandler(RequestData requestData, string authorizationCode, Uri redirectUri)
            : base(requestData)
        {
            if (requestData.Resource == null)
            {
                requestData.Resource = NullResource;
            }

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
