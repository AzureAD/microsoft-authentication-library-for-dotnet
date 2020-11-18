// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.;

namespace Microsoft.Identity.Client.Region
{
    // Enum to be used only for telemetry, to log the source of region discovery.
    internal enum RegionSource
    {
        None = 0,
        EnvVariable = 1,
        Imds = 2,
        Cache = 3
    }
}
