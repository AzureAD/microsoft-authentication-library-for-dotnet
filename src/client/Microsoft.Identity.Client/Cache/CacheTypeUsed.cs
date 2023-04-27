﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        /// Specifies that the cache level used is unknown.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Specifies if the L1 cache is used
        /// </summary>
        L1Cache = 1,
        /// <summary>
        /// Specifies if the L2 cache is used
        /// </summary>
        L2Cache = 2,
        /// <summary>
        /// Specifies if both the L1 and L2 caches are used
        /// </summary>
        L1AndL2Cache = 3
    }
}
