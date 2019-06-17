// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Provides instance metadata across all authority types. Deals with metadata caching.
    /// </summary>
    internal interface IInstanceDiscoveryManager
    {
        Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryAsync(
            Uri authority,
            RequestContext requestContext);
    }
}
