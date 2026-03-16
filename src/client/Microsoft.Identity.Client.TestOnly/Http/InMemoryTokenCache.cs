// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    public class InMemoryTokenCache
    {
        private byte[] _cacheData;
        private bool _shouldClearExistingCache;
        private bool _withOperationDelay;
        private const int OperationDelayInMs = 100;

        public InMemoryTokenCache(bool withOperationDelay = false, bool shouldClearExistingCache = true)
        {
            // Helps when testing cache operation metrics
            _withOperationDelay = withOperationDelay;
            _shouldClearExistingCache = shouldClearExistingCache;
        }

        public void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            if (_withOperationDelay)
            {
                Task.Delay(OperationDelayInMs).GetAwaiter().GetResult();
            }
            args.TokenCache.DeserializeMsalV3(_cacheData, _shouldClearExistingCache);
        }

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

        public void Bind(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }
    }
}
