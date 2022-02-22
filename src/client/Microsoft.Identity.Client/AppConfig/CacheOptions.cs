// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Options for MSAL token caches. 
    /// </summary>
    /// <remarks>
    /// Detailed cache guidance for each application type and platform, including L2 options:
    /// https://aka.ms/msal-net-token-cache-serialization
    /// </remarks>
    public class CacheOptions
    {
        /// <summary>
        /// Recommended options for using a static cache. 
        /// </summary>
        public static CacheOptions EnableSharedCacheOptions
        {
            get
            {
                return new CacheOptions(true);
            }
        }

        /// <summary>
        /// Constructor for the options with default values.
        /// </summary>
        public CacheOptions()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="useSharedCache">Set to true to share the cache between all ClientApplication objects. The cache becomes static. <see cref="UseSharedCache"/> for a detailed description. </param>
        public CacheOptions(bool useSharedCache)
        {
            UseSharedCache = useSharedCache;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="useSharedCache">Set to true to share the cache between all ClientApplication objects. The cache becomes static. <see cref="UseSharedCache"/> for a detailed description. </param>
        /// <param name="maximumItems">Token cache items limit. <see cref="MaximumItems"/> for a detailed description.</param>
        public CacheOptions(bool useSharedCache, int maximumItems)
        {
            UseSharedCache = useSharedCache;
            MaximumItems = maximumItems;
        }

        /// <summary>
        /// Share the cache between all ClientApplication objects. The cache becomes static. Defaults to false.
        /// </summary>
        /// <remarks>
        /// Recommended only for client credentials flow (service to service communication).
        /// Web apps and Web APIs should use external token caching (Redis, Cosmos etc.) for scaling purposes.
        /// Desktop apps should encrypt and persist their token cache to disk, to avoid losing tokens when app restarts. 
        /// ADAL used a static cache by default.
        /// </remarks>
        public bool UseSharedCache { get; set; }

        private int? _maximumItems;

        /// <summary>
        /// Total token cache items limit for both app and user token caches.
        /// </summary>
        /// <remarks>
        /// Once the limit is reached, either the app or user cache will be compacted, depending on which was most recently used.
        /// Using a MemoryCache via Microsoft.Identity.Web.TokenCache is more accurate but slower.
        /// This size limit applies only to internal memory cache and is not a concern when distributed caching is used.
        /// IMPORTANT: Monitor app health metrics (including memory usage) and cache performance (<see href="https://aka.ms/msal-net-token-cache-serialization"/>)
        /// and adjust size limit accordingly.
        /// </remarks>
        public int? MaximumItems
        {
            get => _maximumItems;
            set
            {
                ValidateSizeLimit(value);
                _maximumItems = value;
            }
        }

        private void ValidateSizeLimit(int? maximumItems)
        {
            if (maximumItems < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumItems), $"{nameof(maximumItems)} must be a positive number.");
            }
        }
    }
}
