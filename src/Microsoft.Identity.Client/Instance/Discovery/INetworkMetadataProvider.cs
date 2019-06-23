// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal interface INetworkMetadataProvider
    {
        Task<InstanceDiscoveryResponse> FetchAllDiscoveryMetadataAsync(Uri authority, RequestContext requestContext);
    }
}
