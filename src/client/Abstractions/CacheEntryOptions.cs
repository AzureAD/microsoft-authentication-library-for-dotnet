// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.ServiceEssentials
{
    /// <summary>
    /// .
    /// </summary>
    public class CacheEntryOptions
    {
        /// <summary>
        /// </summary>
        public DateTimeOffset ExpirationTimeUTC { get; }

        /// <summary>
        /// </summary>
        public DateTimeOffset RefreshTimeUTC { get; }

        /// <summary>
        /// </summary>
        public bool StoreToLocalCacheOnly { get; set; }

        /// <summary>
        /// Value that can be used to randomize spans in <see cref="CacheEntryOptions"/>.
        /// </summary>
        /// <remarks>
        /// Negative value will be used to randomize spans to a maximum of -<see cref="JitterInSeconds"/>,
        /// while positive value will be used to randomize spans to a maximum of +-<see cref="JitterInSeconds"/>.
        /// </remarks>
        public int JitterInSeconds { get; set; }

        /// <summary>
        /// If default was not provided for a category.
        /// </summary>
        public int MaxCategoryCount { get; set; }

        /// <summary>
        /// </summary>
        public CacheEntryOptions(DateTimeOffset expirationTimeUTC, int maxCategoryCount) : this(expirationTimeUTC, DateTimeOffset.MaxValue, maxCategoryCount)
        {
        }

        /// <summary>
        /// </summary>
        public CacheEntryOptions(DateTimeOffset expirationTimeUTC, DateTimeOffset refreshTimeUTC, int maxCategoryCount)
        {
            ExpirationTimeUTC = expirationTimeUTC;
            MaxCategoryCount = maxCategoryCount;
            RefreshTimeUTC = refreshTimeUTC;
        }
    }
}
