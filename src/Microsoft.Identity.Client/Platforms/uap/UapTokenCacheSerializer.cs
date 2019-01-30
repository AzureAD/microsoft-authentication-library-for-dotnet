using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Platforms.uap
{
    internal class UapTokenCacheBlobStorage : ITokenCacheBlobStorage
    {
        public void OnAfterAccess(TokenCacheNotificationArgs args)
        {

        }

        public void OnBeforeAccess(TokenCacheNotificationArgs args)
        {

        }
        public void OnBeforeWrite(TokenCacheNotificationArgs args)
        {

        }

    }
}
