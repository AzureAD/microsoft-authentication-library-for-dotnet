// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Cache.Keys
{
    internal struct MsalAccountCacheKeyData
    {
        public string HomeAccountId { get; set; }

        public string CacheKey { get; set; }

        public IiOSKey iOSKey { get; set; }

        public MsalAccountCacheKeyData(string homeAccountId, string cacheKey, IiOSKey iosKey)
        {
            HomeAccountId = homeAccountId;
            CacheKey = cacheKey;
            iOSKey = iosKey;
        }
    }
}
