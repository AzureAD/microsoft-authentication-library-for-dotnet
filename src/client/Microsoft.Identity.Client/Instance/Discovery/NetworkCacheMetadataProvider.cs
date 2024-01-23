// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal class NetworkCacheMetadataProvider : INetworkCacheMetadataProvider
    {
        private static readonly ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry> s_cache =
             new ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry>();

        public InstanceDiscoveryMetadataEntry GetMetadata(string environment, ILoggerAdapter logger)
        {
            s_cache.TryGetValue(environment, out InstanceDiscoveryMetadataEntry entry);
            logger.Verbose(() => $"[Instance Discovery] Tried to use network cache provider for {environment}. Success? {entry != null}. ");

            return entry;
        }

        public void AddMetadata(string environment, InstanceDiscoveryMetadataEntry entry)
        {
            // Always take the most recent value
            s_cache.AddOrUpdate(environment, entry, (_, _) => entry);
        }

        public void Clear()
        {
            s_cache.Clear();
        }
    }
}
