// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal class StaticMetadataProvider : IStaticMetadataProvider
    {
        private static readonly ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry> s_cache =
             new ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry>();

        public InstanceDiscoveryMetadataEntry GetMetadata(string environment)
        {
            s_cache.TryGetValue(environment, out InstanceDiscoveryMetadataEntry entry);
            return entry;
        }

        public void AddMetadata(string environment, InstanceDiscoveryMetadataEntry entry)
        {
            // Always take the most recent value
            s_cache.AddOrUpdate(environment, entry, (key, oldValue) => entry);
        }

        public void Clear()
        {
            s_cache.Clear();
        }
    }
}
