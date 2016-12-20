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
        private readonly IDictionary<string, string> _tokenCacheDictionary =
            new ConcurrentDictionary<string, string>();

        private readonly IDictionary<string, string> _refreshTokenCacheDictionary =
            new ConcurrentDictionary<string, string>();

        public void BeforeAccess(TokenCacheNotificationArgs args)
        {
        }

        public void AfterAccess(TokenCacheNotificationArgs args)
        {
        }

        public ICollection<string> AllAccessAndIdTokens()
        {
            return new ReadOnlyCollection<string>(_tokenCacheDictionary.Values.ToList());
        }

        public ICollection<string> AllRefreshTokens()
        {
            return new ReadOnlyCollection<string>(_refreshTokenCacheDictionary.Values.ToList());
        }

        public void SaveToken(TokenCacheItem tokenItem)
        {
            _tokenCacheDictionary[tokenItem.GetTokenCacheKey().ToString()] = JsonHelper.SerializeToJson(tokenItem);
        }

        public void SaveRefreshToken(RefreshTokenCacheItem refreshTokenItem)
        {
            _refreshTokenCacheDictionary[refreshTokenItem.GetTokenCacheKey().ToString()] = JsonHelper.SerializeToJson(refreshTokenItem);
        }

        public void DeleteToken(TokenCacheKey key)
        {
            _tokenCacheDictionary.Remove(key.ToString());
        }

        public void DeleteRefreshToken(TokenCacheKey key)
        {
            _refreshTokenCacheDictionary.Remove(key.ToString());
        }

        public void DeleteAll(string clientId)
        {
            _tokenCacheDictionary.Clear();
            _tokenCacheDictionary.Clear();
        }
    }
}