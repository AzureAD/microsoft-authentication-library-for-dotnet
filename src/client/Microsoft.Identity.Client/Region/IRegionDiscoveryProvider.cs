// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Region
{
    internal interface IRegionDiscoveryProvider
    {
        Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext);
    }
}
