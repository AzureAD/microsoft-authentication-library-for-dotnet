// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Provides instance metadata across all authority types. Deals with metadata caching.
    /// </summary>
    internal interface IInstanceDiscoveryManager
    {
        Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryTryAvoidNetworkAsync(
            AuthorityInfo authorityinfo,
            IEnumerable<string> existingEnvironmentsInCache,
            RequestContext requestContext);

        Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryAsync(
            AuthorityInfo authorityinfo,
            RequestContext requestContext, 
            bool forceValidation=false);

    }
}
