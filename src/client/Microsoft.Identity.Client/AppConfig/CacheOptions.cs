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
        /// <param name="sizeLimit">Token cache size limit in bytes. <see cref="SizeLimit"/> for a detailed description.</param>
        public CacheOptions(bool useSharedCache, long sizeLimit)
        {
            ValidateSizeLimit(sizeLimit);

            UseSharedCache = useSharedCache;
            _sizeLimit = sizeLimit;
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

        private long? _sizeLimit;

        /// <summary>
        /// Total token cache size limit in bytes for both app and user token caches.
        /// </summary>
        /// <remarks>
        /// Once the limit is reached, either the app or user cache will be fully cleared, depending on which was most recently used.
        /// MSAL doesn't calculate the exact memory usage and uses approximations of the token sizes. 
        /// For instance, app token cache entry is approximately at least 4500 bytes; user access token entry - 6500 bytes,
        /// user refresh token entry - 3700 bytes.
        /// IMPORTANT: Monitor app health metrics (including memory usage) and cache performance (<see href="https://aka.ms/msal-net-token-cache-serialization"/>)
        /// and adjust size limit accordingly.
        /// </remarks>
        public long? SizeLimit
        {
            get => _sizeLimit;
            set
            {
                ValidateSizeLimit(value);
                _sizeLimit = value;
            }
        }

        private void ValidateSizeLimit(long? sizeLimit)
        {
            if (!sizeLimit.HasValue || sizeLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeLimit), $"{nameof(sizeLimit)} must be a positive number.");
            }
        }
    }
}
