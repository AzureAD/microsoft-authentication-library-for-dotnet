// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal static class ManagedIdentitySourceExtensions
    {
        private static readonly HashSet<ManagedIdentitySource> s_supportsClaimsAndCaps =
            [
            // add other sources here as they light up
            ManagedIdentitySource.ServiceFabric,
            ];

        internal static bool SupportsClaimsAndCapabilities(
            this ManagedIdentitySource source) => s_supportsClaimsAndCaps.Contains(source);
    }
}
