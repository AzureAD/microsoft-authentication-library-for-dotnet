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
using System.Linq;
using Microsoft.Identity.Client.CacheV2.Impl;
using Microsoft.Identity.Core.Cache;

namespace Microsoft.Identity.Client.CacheV2
{
    /// <summary>
    /// In the V2 cache, the ITokenCache interface will provide the existing infra used by external developers
    /// to serialize/deserialize if they're managing their own cache.
    /// But the cache implementation does not live within the TokenCacheV2 infra, it is simply a facade
    /// into the StorageManager and other pieces of the cache that are owned by the ClientApplicationBase.
    /// </summary>
    internal class TokenCacheV2 : ITokenCache
    {
        private readonly object _lock = new object();

        //private TokenCacheNotification _afterAccess;
        //private TokenCacheNotification _beforeAccess;
        //private TokenCacheNotification _beforeWrite;
        private CacheData _deserializedCacheDataHolding = new CacheData();
        private IStorageManager _storageManager;

        //public void SetBeforeAccess(TokenCacheNotification beforeAccess)
        //{
        //    lock (_lock)
        //    {
        //        _beforeAccess = beforeAccess;
        //    }
        //}

        //public void SetAfterAccess(TokenCacheNotification afterAccess)
        //{
        //    lock (_lock)
        //    {
        //        _afterAccess = afterAccess;
        //    }
        //}

        //public void SetBeforeWrite(TokenCacheNotification beforeWrite)
        //{
        //    lock (_lock)
        //    {
        //        _beforeWrite = beforeWrite;
        //    }
        //}        //public void SetBeforeAccess(TokenCacheNotification beforeAccess)
        //{//    lock (_lock)
        //    {
        //        _beforeAccess = beforeAccess;
        //    }
        //}

        //public void SetAfterAccess(TokenCacheNotification afterAccess)
        //{
        //    lock (_lock)
        //    {
        //        _afterAccess = afterAccess;
        //    }
        //}

        //public void SetBeforeWrite(TokenCacheNotification beforeWrite)
        //{
        //    lock (_lock)
        //    {
        //        _beforeWrite = beforeWrite;
        //    }
        //}

        public byte[] Serialize()
        {
            lock (_lock)
            {
                if (_storageManager == null)
                {
                    var output = new byte[_deserializedCacheDataHolding.UnifiedState.Length];
                    Array.Copy(
                        _deserializedCacheDataHolding.UnifiedState,
                        output,
                        _deserializedCacheDataHolding.UnifiedState.Length);
                    return output;
                }
                else
                {
                    // call into _storageManager and get the data to serialize...
                    return _storageManager.Serialize();
                }
            }
        }

        public void Deserialize(byte[] unifiedState)
        {
            lock (_lock)
            {
                if (_storageManager == null)
                {
                    _deserializedCacheDataHolding.UnifiedState = new byte[unifiedState.Length];
                    Array.Copy(unifiedState, _deserializedCacheDataHolding.UnifiedState, unifiedState.Length);
                }
                else
                {
                    // call into _storageManager and push the contents...
                    _storageManager.Deserialize(unifiedState);
                }
            }
        }

        private ILegacyCachePersistence LegacyCachePersistence => _storageManager?.AdalLegacyCacheManager?.LegacyCachePersistence;

        public CacheData SerializeUnifiedAndAdalCache()
        {
            lock (_lock)
            {
                if (_storageManager == null && LegacyCachePersistence == null)
                {
                    return _deserializedCacheDataHolding;
                }

                var cacheData = new CacheData();
                if (_storageManager != null)
                {
                    cacheData.UnifiedState = _storageManager.Serialize();
                }

                if (LegacyCachePersistence != null)
                {
                    cacheData.AdalV3State = LegacyCachePersistence.LoadCache();
                }

                return cacheData;
            }
        }

        public void DeserializeUnifiedAndAdalCache(CacheData cacheData)
        {
            lock (_lock)
            {
                if (_storageManager == null && LegacyCachePersistence == null)
                {
                    _deserializedCacheDataHolding = cacheData;
                }

                _storageManager?.Deserialize(cacheData.UnifiedState);
                LegacyCachePersistence?.WriteCache(cacheData.AdalV3State);
            }
        }

        /// <summary>
        ///     This is called when the TokenCache is set into the clientapplicationbase and we need to
        ///     hook it up since the storage manager is the actual source of truth for the cache...
        /// </summary>
        /// <param name="storageManager"></param>
        internal void BindToStorageManager(IStorageManager storageManager)
        {
            if (storageManager == null)
            {
                throw new ArgumentNullException(nameof(storageManager));
            }

            lock (_lock)
            {
                _storageManager = storageManager;

                // If the user has deserialized any data into the cache before binding...
                if (_deserializedCacheDataHolding.UnifiedState != null && _deserializedCacheDataHolding.UnifiedState.Any())
                {
                    // decode buffer and translate data into the storage manager
                    _storageManager.Deserialize(_deserializedCacheDataHolding.UnifiedState);
                }

                if (_deserializedCacheDataHolding.AdalV3State != null && _deserializedCacheDataHolding.AdalV3State.Any())
                {
                    // decode buffer and translate into the adal legacy cache manager
                    LegacyCachePersistence.WriteCache(_deserializedCacheDataHolding.AdalV3State);
                }

                // subscribe to IStorageManager beforeaccess/afteraccess/beforewrite events so we can forward them as needed to the caller...
                //_storageManager.BeforeAccess += StorageManagerOnBeforeAccess;
                //_storageManager.BeforeWrite += StorageManagerOnBeforeWrite;
                //_storageManager.AfterAccess += StorageManagerOnAfterAccess;
            }
        }

        //private void StorageManagerOnBeforeAccess(object sender, TokenCacheNotificationArgs e)
        //{
        //    lock (_lock)
        //    {
        //        _beforeAccess?.Invoke(new TokenCacheNotificationArgs(this, e.ClientId, e.Account));
        //    }
        //}

        //private void StorageManagerOnBeforeWrite(object sender, TokenCacheNotificationArgs e)
        //{
        //    lock (_lock)
        //    {
        //        _beforeWrite?.Invoke(new TokenCacheNotificationArgs(this, e.ClientId, e.Account));
        //    }
        //}

        //private void StorageManagerOnAfterAccess(object sender, TokenCacheNotificationArgs e)
        //{
        //    lock (_lock)
        //    {
        //        _afterAccess?.Invoke(new TokenCacheNotificationArgs(this, e.ClientId, e.Account));
        //    }
        //}
    }
}