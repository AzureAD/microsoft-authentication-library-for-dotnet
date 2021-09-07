// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Options for the internal in-memory MSAL token caches.
    /// </summary>
    /// <remarks>
    /// Recommended only for client credentials flow (service to service communication).
    /// Web apps and Web APIs should use external token caching (Redis, Cosmos etc.) for scaling purposes.
    /// Desktop apps should encrypt and persist their token cache to disk, to avoid losing tokens when app restarts. 
    /// For detailed recommendations see: https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-net-token-cache-serialization?tabs=aspnetcore
    /// </remarks>
    public class InternalMemoryTokenCacheOptions
    {
        /// <summary>
        /// Constructor for the options with default values.
        /// </summary>
        public InternalMemoryTokenCacheOptions()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="useSharedCache">Set to true to share the cache between all ClientApplication objects. The cache becomes static. </param>
        public InternalMemoryTokenCacheOptions(bool useSharedCache)
        {
            UseSharedCache = useSharedCache;
        }

        /// <summary>
        /// Share the cache between all ClientApplication objects. The cache becomes static. Defaults to false.
        /// </summary>
        /// <remarks>ADAL used a static cache by default</remarks>
        public bool UseSharedCache { get; set; }

    }
}
