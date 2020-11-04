using Microsoft.Identity.Client.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal class NullLegacyCachePersistence : ILegacyCachePersistence
    {
        public byte[] LoadCache()
        {
            return null;
        }

        public void WriteCache(byte[] serializedCache)
        {
            // no-op
        }
    }
}
