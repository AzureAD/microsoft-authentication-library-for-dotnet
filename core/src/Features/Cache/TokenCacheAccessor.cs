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
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Core
{
    internal class TokenCacheAccessor : ITokenCacheAccessor
    {
        internal readonly IDictionary<string, string> AccessTokenCacheDictionary =
            new ConcurrentDictionary<string, string>();

        internal readonly IDictionary<string, string> RefreshTokenCacheDictionary =
            new ConcurrentDictionary<string, string>();

        internal readonly IDictionary<string, string> IdTokenCacheDictionary =
            new ConcurrentDictionary<string, string>();

        internal readonly IDictionary<string, string> AccountCacheDictionary =
            new ConcurrentDictionary<string, string>();

        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            AccessTokenCacheDictionary[item.GetKey().ToString()] = JsonHelper.SerializeToJson(item);
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            RefreshTokenCacheDictionary[item.GetKey().ToString()] = JsonHelper.SerializeToJson(item);
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            IdTokenCacheDictionary[item.GetKey().ToString()] = JsonHelper.SerializeToJson(item);
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            AccountCacheDictionary[item.GetKey().ToString()] = JsonHelper.SerializeToJson(item);
        }

        public string GetAccessToken(MsalAccessTokenCacheKey accessTokenKey)
        {
            var strKey = accessTokenKey.ToString();
            if (!AccessTokenCacheDictionary.ContainsKey(strKey))
            {
                return null;
            }

            return AccessTokenCacheDictionary[strKey];
        }

        public string GetRefreshToken(MsalRefreshTokenCacheKey refreshTokenKey)
        {
            var strKey = refreshTokenKey.ToString();
            if (!RefreshTokenCacheDictionary.ContainsKey(strKey))
            {
                return null;
            }

            return RefreshTokenCacheDictionary[strKey];
        }

        public string GetIdToken(MsalIdTokenCacheKey idTokenKey)
        {
            var strKey = idTokenKey.ToString();
            if (!IdTokenCacheDictionary.ContainsKey(strKey))
            {
                return null;
            }

            return IdTokenCacheDictionary[strKey];
        }

        public string GetAccount(MsalAccountCacheKey accountKey)
        {
            var strKey = accountKey.ToString();
            if (!AccountCacheDictionary.ContainsKey(strKey))
            {
                return null;
            }

            return AccountCacheDictionary[strKey];
        }

        public void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey)
        {
            AccessTokenCacheDictionary.Remove(cacheKey.ToString());
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey)
        {
            RefreshTokenCacheDictionary.Remove(cacheKey.ToString());
        }

        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey)
        {
            IdTokenCacheDictionary.Remove(cacheKey.ToString());
        }

        public void DeleteAccount(MsalAccountCacheKey cacheKey)
        {
            AccountCacheDictionary.Remove(cacheKey.ToString());
        }
        
        public ICollection<string> GetAllAccessTokensAsString()
        {
            return
                new ReadOnlyCollection<string>(
                    AccessTokenCacheDictionary.Values.ToList());
        }

        public ICollection<string> GetAllRefreshTokensAsString()
        {
            return
                new ReadOnlyCollection<string>(
                    RefreshTokenCacheDictionary.Values.ToList());
        }

        public ICollection<string> GetAllIdTokensAsString()
        {
            return
                new ReadOnlyCollection<string>(
                   IdTokenCacheDictionary.Values.ToList());
        }

        public ICollection<string> GetAllAccountsAsString()
        {
            return
                new ReadOnlyCollection<string>(
                   AccountCacheDictionary.Values.ToList());
        }
        /*
        public ICollection<string> GetAllAccessTokenKeys()
        {
            return
                new ReadOnlyCollection<string>(
                    AccessTokenCacheDictionary.Keys.ToList());
        }

        public ICollection<string> GetAllRefreshTokenKeys()
        {
            return
                new ReadOnlyCollection<string>(
                    RefreshTokenCacheDictionary.Keys.ToList());
        }

        public ICollection<string> GetAllIdTokenKeys()
        {
            return
                new ReadOnlyCollection<string>(
                    IdTokenCacheDictionary.Keys.ToList());
        }

        public ICollection<string> GetAllAccountKeys()
        {
            return
                new ReadOnlyCollection<string>(
                    AccountCacheDictionary.Keys.ToList());
        }
        */

        public void Clear()
        {
            AccessTokenCacheDictionary.Clear();
            RefreshTokenCacheDictionary.Clear();
            IdTokenCacheDictionary.Clear();
            AccountCacheDictionary.Clear();
        }

        public void SetSecurityGroup(string securityGroup)
        {
            throw new System.NotImplementedException();
        }
    }
}
