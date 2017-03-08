using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                Dictionary<string, ICollection<string>> cacheDict = JsonHelper
                    .DeserializeFromJson<Dictionary<string, ICollection<string>>>(state);
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
                Dictionary<string, ICollection<string>> cacheDict = new Dictionary<string, ICollection<string>>();
                cacheDict["access_tokens"] = tokenCache.GetAllAccessTokenCacheItems();
                cacheDict["refresh_tokens"] = tokenCache.GetAllRefreshTokenCacheItems();
                return JsonHelper.SerializeToJson(cacheDict).ToByteArray();
            }
        }
    }
}
