// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.ServiceEssentials
{
    internal class DistributedCacheEntry<T> 
    {
        public DistributedCacheEntry() { }

        public T Value { get; set; }

        /// <summary>
        /// </summary>
        public DateTimeOffset ExpirationTimeUTC { get; set; }

        /// <summary>
        /// </summary>
        public DateTimeOffset RefreshTimeUTC { get; set; }

        public int MaxCategoryCount { get; set; }

        public int JitterInSeconds { get; set; }

        public void Deserialize(string serializedValue)
        {
            _ = serializedValue;
            _ = MaxCategoryCount;
            // todo
        }

        public string Serialize()
        {
            _ = MaxCategoryCount;
            // todo
            return string.Empty;
        }
    }
}
