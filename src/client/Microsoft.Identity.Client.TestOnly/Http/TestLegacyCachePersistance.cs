// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    public class TestLegacyCachePersistance : ILegacyCachePersistence
    {
        private byte[] data;
        public byte[] LoadCache()
        {
            return data;
        }

        public void WriteCache(byte[] serializedCache)
        {
            data = serializedCache;
        }
    }
}
