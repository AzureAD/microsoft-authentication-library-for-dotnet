// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Identity.Client.OAuth2.Throttling;

namespace Microsoft.Identity.Test.Unit.Throttling
{
    internal static class ThrottlingManagerExtensions
    {
        public static void SimulateTimePassing(
            this IThrottlingProvider throttlingManager, 
            TimeSpan delay)
        {
            var (retryAfterProvider, httpStatusProvider, uiRequiredProvider) = GetTypedThrottlingProviders(throttlingManager);
            MoveToPast(delay, retryAfterProvider.ThrottlingCache.CacheForTest);
            MoveToPast(delay, httpStatusProvider.ThrottlingCache.CacheForTest);
            MoveToPast(delay, uiRequiredProvider.ThrottlingCache.CacheForTest);
        }

        public static (RetryAfterProvider, HttpStatusProvider, UiRequiredProvider) GetTypedThrottlingProviders(
          this IThrottlingProvider throttlingManager)
        {
            var manager = throttlingManager as SingletonThrottlingManager;
            return (
                manager.ThrottlingProviders.Single(p => p is RetryAfterProvider) as RetryAfterProvider,
                manager.ThrottlingProviders.Single(p => p is HttpStatusProvider) as HttpStatusProvider,
                manager.ThrottlingProviders.Single(p => p is UiRequiredProvider) as UiRequiredProvider);
        }

        private static void MoveToPast(TimeSpan delay, ConcurrentDictionary<string, ThrottlingCacheEntry> cacheDictionary)
        {
            foreach (var kvp in cacheDictionary)
            {
                // move time forward by moving creation and expiration time back
                cacheDictionary[kvp.Key] = new ThrottlingCacheEntry(
                    kvp.Value.Exception,
                    kvp.Value.CreationTime - delay,
                    kvp.Value.ExpirationTime - delay);
            }
        }
    }
}
