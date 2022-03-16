// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal interface IUserMetadataProvider
    {
        InstanceDiscoveryMetadataEntry GetMetadataOrThrow(string environment, IMsalLogger logger);
    }
}
