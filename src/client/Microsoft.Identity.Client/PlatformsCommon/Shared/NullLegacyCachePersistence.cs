// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
