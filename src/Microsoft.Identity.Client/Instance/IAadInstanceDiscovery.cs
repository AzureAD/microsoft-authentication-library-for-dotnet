// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal interface IAadInstanceDiscovery
    {
        Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryAsync(
            Uri authority,
            RequestContext requestContext);

        bool TryAddValue(string host, InstanceDiscoveryMetadataEntry instanceDiscoveryMetadataEntry);
        bool TryGetValue(string host, out InstanceDiscoveryMetadataEntry instanceDiscoveryMetadataEntry);
    }
}
