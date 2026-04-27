// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal class InMemoryLegacyCachePersistance : ILegacyCachePersistence
    {
        private byte[] _data;

        public byte[] LoadCache()
        {
            return _data;
        }

        public void WriteCache(byte[] serializedCache)
        {
            _data = serializedCache;
        }
    }
}
