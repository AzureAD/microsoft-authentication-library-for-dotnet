// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal interface INetworkCacheMetadataProvider
    {
        void AddMetadata(string environment, InstanceDiscoveryMetadataEntry entry);

        /// <summary>
        /// Caches <paramref name="entry"/> under each host in <paramref name="entry"/>.Aliases.
        /// If the entry has no aliases, falls back to <paramref name="fallbackEnvironment"/>.
        /// </summary>
        void AddMetadataWithAliases(InstanceDiscoveryMetadataEntry entry, string fallbackEnvironment);

        InstanceDiscoveryMetadataEntry GetMetadata(string environment, ILoggerAdapter logger);
    }
}
