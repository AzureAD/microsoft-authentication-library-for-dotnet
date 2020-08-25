// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    /// <summary>
    /// This factory ensures key rotation every 8h
    /// </summary>
    internal class PoPProviderFactory
    {
        private static InMemoryCryptoProvider s_currentProvider;
        private static DateTime s_providerExpiration;
        private static readonly TimeSpan s_expirationTimespan = TimeSpan.FromHours(8);
        private static object s_lock = new object();

        public static InMemoryCryptoProvider GetOrCreateProvider(/* for testing */ ITimeService timeService = null)
        {
            if (timeService == null)
            {
                timeService = new TimeService();
            }

            lock (s_lock)
            {
                if (s_currentProvider != null && s_providerExpiration > timeService.GetUtcNow())
                {
                    return s_currentProvider;
                }

                s_currentProvider = new InMemoryCryptoProvider();
                s_providerExpiration = timeService.GetUtcNow() + s_expirationTimespan;
                return s_currentProvider;
            }
        }
    }
}
