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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Internal.Interfaces;

namespace Microsoft.Identity.Client.Internal.Cache
{
    internal class TokenCacheAccessor
    {
        public ITokenCachePlugin TokenCachePlugin = PlatformPlugin.TokenCachePlugin;

        public void SaveAccessToken(TokenCacheItem accessTokenItem)
        {
            TokenCachePlugin.SaveToken(accessTokenItem);
        }

        public void SaveRefreshToken(RefreshTokenCacheItem refreshTokenItem)
        {
            TokenCachePlugin.SaveRefreshToken(refreshTokenItem);
        }

        public IList<TokenCacheItem> GetTokens(TokenCacheKey tokenCacheKey)
        {
            //TODO: check android implementation
            ICollection<string> allAccessTokens = TokenCachePlugin.AllAccessAndIdTokens();
            IList<TokenCacheItem> matchedTokens = new List<TokenCacheItem>();
            foreach (string accessTokenItemJson in allAccessTokens)
            {
                TokenCacheItem tokenCacheItem = JsonHelper.DeserializeFromJson<TokenCacheItem>(accessTokenItemJson);
                if (tokenCacheKey.Equals(tokenCacheItem.GetTokenCacheKey()))
                {
                    matchedTokens.Add(tokenCacheItem);
                }
            }

            return matchedTokens;
        }
        
        public IList<RefreshTokenCacheItem> GetRefreshTokens(TokenCacheKey tokenCacheKey)
        {
            ICollection<string> allRefreshTokens = TokenCachePlugin.AllRefreshTokens();
            IList<RefreshTokenCacheItem> matchedRefreshTokens = new List<RefreshTokenCacheItem>();
            foreach (string refreshTokenValue in allRefreshTokens)
            {
                RefreshTokenCacheItem refreshTokenCacheItem =
                    JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(refreshTokenValue);

                if (tokenCacheKey.Equals(refreshTokenCacheItem.GetTokenCacheKey()))
                {
                    matchedRefreshTokens.Add(refreshTokenCacheItem);
                }
            }

            return matchedRefreshTokens;
        }

        public void DeleteToken(TokenCacheItem token‪Item)
        {
            TokenCachePlugin.DeleteToken(token‪Item.GetTokenCacheKey());
        }

        public void DeleteRefreshToken(RefreshTokenCacheItem refreshToken‪Item)
        {
            TokenCachePlugin.DeleteRefreshToken(refreshToken‪Item.GetTokenCacheKey());
        }

        public IList<TokenCacheItem> GetAllAccessTokens()
        {
            ICollection<string> allTokensAsString = TokenCachePlugin.AllAccessAndIdTokens();
            IList<TokenCacheItem> returnList = new List<TokenCacheItem>();
            foreach (var token in allTokensAsString)
            {
                returnList.Add(JsonHelper.DeserializeFromJson<TokenCacheItem>(token));
            }

            return returnList;
        }
        
        public IList<RefreshTokenCacheItem> GetAllRefreshTokens()
        {
            ICollection<string> allTokensAsString = TokenCachePlugin.AllRefreshTokens();
            IList<RefreshTokenCacheItem> returnList = new List<RefreshTokenCacheItem>();
            foreach (var token in allTokensAsString)
            {
                returnList.Add(JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(token));
            }

            return returnList;
        }
        
        public IList<RefreshTokenCacheItem> GetAllRefreshTokensForGivenClientId(string clientId)
        {
            return this.GetAllRefreshTokens().Where(t => t.ClientId.Equals(clientId)).ToList();
        }
    }
}
