using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
