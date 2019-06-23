// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal interface IKnownMetadataProvider
    {
        InstanceDiscoveryMetadataEntry GetMetadata(string environment, IEnumerable<string> existingEnviromentsInCache);
    }
}
