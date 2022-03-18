// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal interface INetworkCacheMetadataProvider
    {
        void AddMetadata(string environment, InstanceDiscoveryMetadataEntry entry);
        InstanceDiscoveryMetadataEntry GetMetadata(string environment, ICoreLogger logger);
        void /* for test purposes */ Clear();
    }
}
