// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Cache
{
    internal class CacheSessionManager : ICacheSessionManager
    {
        private readonly AuthenticationRequestParameters _requestParams;

        public CacheSessionManager(ITokenCacheInternal tokenCacheInternal, AuthenticationRequestParameters requestParams)
        {
            TokenCacheInternal = tokenCacheInternal;
            _requestParams = requestParams;
        }

        public ITokenCacheInternal TokenCacheInternal { get; }
        public bool HasCache => TokenCacheInternal != null;

        public Task<MsalAccessTokenCacheItem> FindAccessTokenAsync()
        {
            return TokenCacheInternal.FindAccessTokenAsync(_requestParams);
        }

        public Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem> SaveAccessAndRefreshToken(MsalTokenResponse tokenResponse)
        {
            return TokenCacheInternal.SaveAccessAndRefreshToken(_requestParams, tokenResponse);
        }

        public MsalIdTokenCacheItem GetIdTokenCacheItem(MsalIdTokenCacheKey idTokenCacheKey)
        {
            return TokenCacheInternal.GetIdTokenCacheItem(idTokenCacheKey, _requestParams.RequestContext);
        }

        public Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync()
        {
            return TokenCacheInternal.FindRefreshTokenAsync(_requestParams);
        }
    }
}