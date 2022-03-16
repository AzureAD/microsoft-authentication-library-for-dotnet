using Microsoft.Identity.Client.Cache;

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
