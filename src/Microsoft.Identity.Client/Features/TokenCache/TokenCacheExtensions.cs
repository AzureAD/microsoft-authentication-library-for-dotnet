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
using Microsoft.Identity.Client.Internal.Cache;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// 
    /// </summary>
    public static class TokenCacheExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokencache"></param>
        /// <param name="beforeAccess"></param>
        public static void SetBeforeAccess(this TokenCache tokencache, TokenCache.TokenCacheNotification beforeAccess)
        {
            tokencache.BeforeAccess = beforeAccess;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokencache"></param>
        /// <param name="afterAccess"></param>
        public static void SetAfterAccess(this TokenCache tokencache, TokenCache.TokenCacheNotification afterAccess)
        {
            tokencache.AfterAccess = afterAccess;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokencache"></param>
        /// <param name="beforeWrite"></param>
        public static void SetBeforeWrite(this TokenCache tokencache, TokenCache.TokenCacheNotification beforeWrite)
        {
            tokencache.BeforeWrite = beforeWrite;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenCache"></param>
        /// <param name="state"></param>
        public static void Deserialize(this TokenCache tokenCache, byte[] state)
        {
            lock (tokenCache.LockObject)
            {
                RequestContext requestContext = new RequestContext(Guid.Empty, null);
                Dictionary<string, IEnumerable<string>> cacheDict = JsonHelper
                    .DeserializeFromJson<Dictionary<string, IEnumerable<string>>>(state);
                if (cacheDict == null || cacheDict.Count == 0)
                {
                    //TODO log about empty cache
                    return;
                }

                if (cacheDict.ContainsKey("access_tokens"))
                {
                    foreach (var atItem in cacheDict["access_tokens"])
                    {
                        tokenCache.AddAccessTokenCacheItem(JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(atItem));
                    }
                }

                if (cacheDict.ContainsKey("refresh_tokens"))
                {
                    foreach (var rtItem in cacheDict["refresh_tokens"])
                    {
                        tokenCache.AddRefreshTokenCacheItem(JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(rtItem));
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenCache"></param>
        /// <returns></returns>
        public static byte[] Serialize(this TokenCache tokenCache)
        {
            // reads the underlying in-memory dictionary and dumps out the content as a JSON
            lock (tokenCache.LockObject)
            {   
                RequestContext requestContext = new RequestContext(Guid.Empty, null);
                Dictionary<string, IEnumerable<string>> cacheDict = new Dictionary<string, IEnumerable<string>>();
                cacheDict["access_tokens"] = tokenCache.GetAllAccessTokenCacheItems(requestContext);
                cacheDict["refresh_tokens"] = tokenCache.GetAllRefreshTokenCacheItems(requestContext);
                return JsonHelper.SerializeToJson(cacheDict).ToByteArray();
            }
        }
    }
}
