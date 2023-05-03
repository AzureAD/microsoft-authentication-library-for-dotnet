// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// Identifies the type of cache used when accessing the cache. Cache implementations must provide this.
    /// </summary>
    public enum CacheLevel
    {
        /// <summary>
        /// Specifies that the cache level used is None.
        /// Token was retrieved from ESTS
        /// </summary>
        None = 0,
        /// <summary>
        /// Specifies that the cache level used is unknown.
        /// The custom token cache which doesn't relay the info about which cache was hit back to MSAL.
        /// </summary>
        Unknown = 1,
        /// <summary>
        /// Specifies if the L1 cache is used.
        /// </summary>
        L1Cache = 2,
        /// <summary>
        /// Specifies if the L2 cache is used.
        L2Cache = 3
    }
}
