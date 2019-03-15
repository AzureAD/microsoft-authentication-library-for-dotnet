//------------------------------------------------------------------------------
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

using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// Keeps the 4 token cache dictionaries in memory. Token Cache extensions 
    /// are responsible for persistance. 
    /// </summary>
    internal class InMemoryTokenCacheAccessor : ITokenCacheAccessor
    {
        private readonly IDictionary<string, MsalAccessTokenCacheItem> _accessTokenCacheDictionary =
            new Dictionary<string, MsalAccessTokenCacheItem>();

        private readonly IDictionary<string, MsalRefreshTokenCacheItem> _refreshTokenCacheDictionary =
            new Dictionary<string, MsalRefreshTokenCacheItem>();

        private readonly IDictionary<string, MsalIdTokenCacheItem> _idTokenCacheDictionary =
            new Dictionary<string, MsalIdTokenCacheItem>();

        private readonly IDictionary<string, MsalAccountCacheItem> _accountCacheDictionary =
            new Dictionary<string, MsalAccountCacheItem>();

        private readonly IDictionary<string, MsalAppMetadataCacheItem> _appMetadataDictionary =
           new Dictionary<string, MsalAppMetadataCacheItem>();

        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            _accessTokenCacheDictionary[item.GetKey().ToString()] = item;
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            _refreshTokenCacheDictionary[item.GetKey().ToString()] = item;
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            _idTokenCacheDictionary[item.GetKey().ToString()] = item;
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            _accountCacheDictionary[item.GetKey().ToString()] = item;
        }

        public void SaveAppMetadata(MsalAppMetadataCacheItem item)
        {
            _appMetadataDictionary[item.GetKey().ToString()] = item;
        }

        public MsalAccessTokenCacheItem GetAccessToken(MsalAccessTokenCacheKey accessTokenKey)
        {
            if (_accessTokenCacheDictionary.TryGetValue(accessTokenKey.ToString(), out var cacheItem))
            {
                return cacheItem;
            }

            return null;
        }

        public MsalRefreshTokenCacheItem GetRefreshToken(MsalRefreshTokenCacheKey refreshTokenKey)
        {
            if (_refreshTokenCacheDictionary.TryGetValue(refreshTokenKey.ToString(), out var cacheItem))
            {
                return cacheItem;
            }

            return null;
        }

        public MsalIdTokenCacheItem GetIdToken(MsalIdTokenCacheKey idTokenKey)
        {
            if (_idTokenCacheDictionary.TryGetValue(idTokenKey.ToString(), out var cacheItem))
            {
                return cacheItem;
            }
            return null;
        }

        public MsalAccountCacheItem GetAccount(MsalAccountCacheKey accountKey)
        {
            if (_accountCacheDictionary.TryGetValue(accountKey.ToString(), out var cacheItem))
            {
                return cacheItem;
            }

            return null;
        }

        public void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey)
        {
            _accessTokenCacheDictionary.Remove(cacheKey.ToString());
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey)
        {
            _refreshTokenCacheDictionary.Remove(cacheKey.ToString());
        }

        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey)
        {
            _idTokenCacheDictionary.Remove(cacheKey.ToString());
        }

        public void DeleteAccount(MsalAccountCacheKey cacheKey)
        {
            _accountCacheDictionary.Remove(cacheKey.ToString());
        }
        
        public IEnumerable<MsalAccessTokenCacheItem> GetAllAccessTokens()
        {
            return new ReadOnlyCollection<MsalAccessTokenCacheItem>(
                _accessTokenCacheDictionary.Values.ToList());
        }

        public IEnumerable<MsalRefreshTokenCacheItem> GetAllRefreshTokens()
        {
            return new ReadOnlyCollection<MsalRefreshTokenCacheItem>(
                _refreshTokenCacheDictionary.Values.ToList());

        }

        public IEnumerable<MsalIdTokenCacheItem> GetAllIdTokens()
        {
            return new ReadOnlyCollection<MsalIdTokenCacheItem>(
                _idTokenCacheDictionary.Values.ToList());
        }

        public IEnumerable<MsalAccountCacheItem> GetAllAccounts()
        {
            return new ReadOnlyCollection<MsalAccountCacheItem>(
                _accountCacheDictionary.Values.ToList());
        }

        public IEnumerable<MsalAppMetadataCacheItem> GetAllAppMetadata()
        {
            return new ReadOnlyCollection<MsalAppMetadataCacheItem>(
               _appMetadataDictionary.Values.ToList());
        }

        public void SetiOSKeychainSecurityGroup(string keychainSecurityGroup)
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            _accessTokenCacheDictionary.Clear();
            _refreshTokenCacheDictionary.Clear();
            _idTokenCacheDictionary.Clear();
            _accountCacheDictionary.Clear();
            // app metadata isn't removable
        }

        public MsalAppMetadataCacheItem GetAppMetadata(MsalAppMetadataCacheKey appMetadataKey)
        {
            if (_appMetadataDictionary.TryGetValue(appMetadataKey.ToString(), out var cacheItem))
            {
                return cacheItem;
            }
            return null;
        }
    }
}
