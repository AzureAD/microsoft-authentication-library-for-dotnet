// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Extensions.FileCache;

namespace Microsoft.Identity.Client.MtlsPop.Attestation
{
    internal static class CachedAttestationProviderFactory
    {
        // Single internal place that knows how to create the file cache safely.
        public static IAttestationProvider CreateDefault()
        {
            // Plain-file cache; platform default directory; per-key file name
            ISecureTokenCache cache =
                new SecureTokenFileCache(baseDirectory: string.Empty,
                                         fileNameTemplate: "maa_attestation_{keyId}.jwt");

            return new CachedAttestationProvider(cache, new NativeAttestationProvider());
        }
    }
}
