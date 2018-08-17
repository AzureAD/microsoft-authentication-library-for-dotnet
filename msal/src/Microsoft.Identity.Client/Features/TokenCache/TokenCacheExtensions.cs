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
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extension methods used to subscribe to cache serialization events, and to effectively serialize and deserialize the cache
    /// </summary>
    public static class TokenCacheExtensions
    {
        /// <summary>
        /// Sets a delegate to be notified before any library method accesses the cache. This gives an option to the
        /// delegate to deserialize a cache entry for the application and accounts specified in the <see cref="TokenCacheNotificationArgs"/>.
        /// See https://aka.ms/msal-net-token-cache-serialization
        /// </summary>
        /// <param name="tokencache">Token cache that will be accessed</param>
        /// <param name="beforeAccess">Delegate set in order to handle the cache deserialiation</param>
        /// <remarks>In the case where the delegate is used to deserialize the cache, it might
        /// want to call <see cref="Deserialize(TokenCache, byte[])"/></remarks>
        public static void SetBeforeAccess(this TokenCache tokencache, TokenCache.TokenCacheNotification beforeAccess)
        {
            tokencache.BeforeAccess = beforeAccess;
        }

        /// <summary>
        /// Sets a delegate to be notified after any library method accesses the cache. This gives an option to the
        /// delegate to serialize a cache entry for the application and accounts specified in the <see cref="TokenCacheNotificationArgs"/>.
        /// See https://aka.ms/msal-net-token-cache-serialization
        /// </summary>
        /// <param name="tokencache">Token cache that was accessed</param>
        /// <param name="afterAccess">Delegate set in order to handle the cache serialization in the case where the <see cref="TokenCache.HasStateChanged"/>
        /// member of the cache is <c>true</c></param>
        /// <remarks>In the case where the delegate is used to serialize the cache entierely (not just a row), it might
        /// want to call <see cref="Serialize(TokenCache)"/></remarks>
        public static void SetAfterAccess(this TokenCache tokencache, TokenCache.TokenCacheNotification afterAccess)
        {
            tokencache.AfterAccess = afterAccess;
        }

        /// <summary>
        /// Sets a delegate called before any library method writes to the cache. This gives an option to the delegate
        /// to reload the cache state from a row in database and lock that row. That database row can then be unlocked in the delegate
        /// registered with <see cref="SetAfterAccess(TokenCache, TokenCache.TokenCacheNotification)"/>
        /// </summary>
        /// <param name="tokencache">Token cache that will be accessed</param>
        /// <param name="beforeWrite">Delegate set in order to prepare the cache serialization</param>
        public static void SetBeforeWrite(this TokenCache tokencache, TokenCache.TokenCacheNotification beforeWrite)
        {
            tokencache.BeforeWrite = beforeWrite;
        }

        /// <summary>
        /// Deserializes the token cache from a serialization blob
        /// </summary>
        /// <param name="tokenCache">Token cache to deserialize (to fill-in from the state)</param>
        /// <param name="state">Array of bytes containing serialized cache data</param>
        /// <remarks>
        /// <paramref name="state"/>Is a Json blob containing access tokens, refresh tokens, id tokens and accounts information
        /// </remarks>
        public static void Deserialize(this TokenCache tokenCache, byte[] state)
        {
            lock (tokenCache.LockObject)
            {
                RequestContext requestContext = new RequestContext(new MsalLogger(Guid.Empty, null));

                Dictionary<string, IEnumerable<string>> cacheDict = JsonHelper
                    .DeserializeFromJson<Dictionary<string, IEnumerable<string>>>(state);
                if (cacheDict == null || cacheDict.Count == 0)
                {
                    string msg = "Cache is empty.";
                    CoreLoggerBase.Default.Info(msg);
                    CoreLoggerBase.Default.InfoPii(msg);
                    return;
                }

                if (cacheDict.ContainsKey("access_tokens"))
                {
                    foreach (var atItem in cacheDict["access_tokens"])
                    {
                        var msalAccessTokenCacheItem =  JsonHelper.TryToDeserializeFromJson<MsalAccessTokenCacheItem>(atItem, requestContext);
                        if (msalAccessTokenCacheItem != null)
                        {
                            tokenCache.AddAccessTokenCacheItem(msalAccessTokenCacheItem);
                        }
                    }
                }

                if (cacheDict.ContainsKey("refresh_tokens"))
                {
                    foreach (var rtItem in cacheDict["refresh_tokens"])
                    {
                        var msalRefreshTokenCacheItem = JsonHelper.TryToDeserializeFromJson<MsalRefreshTokenCacheItem>(rtItem, requestContext);
                        if (msalRefreshTokenCacheItem != null)
                        {
                            tokenCache.AddRefreshTokenCacheItem(msalRefreshTokenCacheItem);
                        }
                    }
                }

                if (cacheDict.ContainsKey("id_tokens"))
                {
                    foreach (var idItem in cacheDict["id_tokens"])
                    {
                        var msalIdTokenCacheItem = JsonHelper.TryToDeserializeFromJson<MsalIdTokenCacheItem>(idItem, requestContext);
                        if (msalIdTokenCacheItem != null)
                        {
                            tokenCache.AddIdTokenCacheItem(msalIdTokenCacheItem);
                        }
                    }
                }

                if (cacheDict.ContainsKey("accounts"))
                {
                    foreach (var account in cacheDict["accounts"])
                    {
                        var msalAccountCacheItem = JsonHelper.TryToDeserializeFromJson<MsalIdTokenCacheItem>(account, requestContext);

                        tokenCache.AddAccountCacheItem(JsonHelper.DeserializeFromJson<MsalAccountCacheItem>(account));
                    }
                }
            }
        }

        /// <summary>
        /// Serializes the entiere token cache
        /// </summary>
        /// <param name="tokenCache">Token cache to serialize</param>
        /// <returns>array of bytes containing the serialized cache</returns>
        public static byte[] Serialize(this TokenCache tokenCache)
        {
            // reads the underlying in-memory dictionary and dumps out the content as a JSON
            lock (tokenCache.LockObject)
            {   
                RequestContext requestContext = new RequestContext(new MsalLogger(Guid.Empty, null));
                Dictionary<string, IEnumerable<string>> cacheDict = new Dictionary<string, IEnumerable<string>>();
                cacheDict["access_tokens"] = tokenCache.GetAllAccessTokenCacheItems(requestContext);
                cacheDict["refresh_tokens"] = tokenCache.GetAllRefreshTokenCacheItems(requestContext);
                cacheDict["id_tokens"] = tokenCache.GetAllIdTokenCacheItems(requestContext);
                cacheDict["accounts"] = tokenCache.GetAllAccountCacheItems(requestContext);
                return JsonHelper.SerializeToJson(cacheDict).ToByteArray();
            }
        }
    }
}
