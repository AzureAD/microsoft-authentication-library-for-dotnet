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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.Interfaces;

namespace Microsoft.Identity.Client
{
    internal class TokenCachePlugin : ITokenCachePlugin
    {
        internal readonly IDictionary<string, string> TokenCacheDictionary =
            new ConcurrentDictionary<string, string>();

        public void BeforeAccess(TokenCacheNotificationArgs args)
        {
        }

        public void AfterAccess(TokenCacheNotificationArgs args)
        {
        }

        public ICollection<string> AllAccessAndIdTokens()
        {
            return
                new ReadOnlyCollection<string>(
                    TokenCacheDictionary.Values.Where(
                        v => (JsonHelper.DeserializeFromJson<TokenCacheItem>(v).Scope.Count > 0)).ToList());
        }

        public ICollection<string> AllRefreshTokens()
        {
            return
                new ReadOnlyCollection<string>(
                    TokenCacheDictionary.Values.Where(
                        v => !string.IsNullOrEmpty(JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(v).RefreshToken)).ToList());
        }

        public void SaveToken(TokenCacheItem tokenItem)
        {
            TokenCacheDictionary[tokenItem.GetTokenCacheKey().ToString()] = JsonHelper.SerializeToJson(tokenItem);
        }

        public void SaveRefreshToken(RefreshTokenCacheItem refreshTokenItem)
        {
            TokenCacheDictionary[refreshTokenItem.GetTokenCacheKey().ToString()] = JsonHelper.SerializeToJson(refreshTokenItem);
        }

        public void DeleteToken(TokenCacheKey key)
        {
            TokenCacheDictionary.Remove(key.ToString());
        }

        public void DeleteRefreshToken(TokenCacheKey key)
        {
            TokenCacheDictionary.Remove(key.ToString());
        }

        public void DeleteAll(string clientId)
        {
            TokenCacheDictionary.Clear();
            TokenCacheDictionary.Clear();
        }
    }
}