// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Lab.Api.Core.Mocks
{
    /// <summary>
    /// In-memory token cache implementation for testing purposes. This class provides a simple in-memory storage mechanism for MSAL token cache data, allowing tests to simulate caching behavior without relying on external storage or file systems.
    /// </summary>
    public class InMemoryTokenCache
    {
        private byte[] _cacheData;
        private bool _shouldClearExistingCache;
        private bool _withOperationDelay;
        private const int OperationDelayInMs = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryTokenCache"/> class.
        /// </summary>
        /// <param name="withOperationDelay">Indicates whether to introduce a delay in cache operations to simulate real-world latency.</param>
        /// <param name="shouldClearExistingCache">Indicates whether to clear the existing cache data before deserializing new data.</param>
        public InMemoryTokenCache(bool withOperationDelay = false, bool shouldClearExistingCache = true)
        {
            // Helps when testing cache operation metrics
            _withOperationDelay = withOperationDelay;
            _shouldClearExistingCache = shouldClearExistingCache;
        }

        /// <summary>
        /// Handles the before access notification for the token cache.
        /// </summary>
        /// <param name="args">The token cache notification arguments.</param>
        public void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            if (_withOperationDelay)
            {
                Task.Delay(OperationDelayInMs).GetAwaiter().GetResult();
            }
            args.TokenCache.DeserializeMsalV3(_cacheData, _shouldClearExistingCache);
        }

        /// <summary>
        /// Handles the after access notification for the token cache.
        /// </summary>
        /// <param name="args">The token cache notification arguments.</param>
        public void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (_withOperationDelay)
            {
                Task.Delay(OperationDelayInMs).GetAwaiter().GetResult();
            }

            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                _cacheData = args.TokenCache.SerializeMsalV3();
            }
        }

        /// <summary>
        /// Binds the in-memory token cache to the specified <see cref="ITokenCache"/> instance.
        /// </summary>
        /// <param name="tokenCache">The token cache instance to bind to.</param>
        public void Bind(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }
    }
}
