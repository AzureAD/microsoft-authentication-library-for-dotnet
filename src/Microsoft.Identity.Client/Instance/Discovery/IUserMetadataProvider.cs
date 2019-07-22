// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal interface IUserMetadataProvider
    {
        InstanceDiscoveryMetadataEntry GetMetadataOrThrow(string environment, ICoreLogger logger);
    }
}
