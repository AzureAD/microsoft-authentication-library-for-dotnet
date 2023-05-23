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
    /// Identifies the type of cache that the token was read from. Cache implementations must provide this.
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
        /// Token was retrieved from cache but the token cache implementation didn't specify which cache level was used.
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
