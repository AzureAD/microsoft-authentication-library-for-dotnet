// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.ManagedIdentity.V2.Cache.Abstractions
{
    internal interface IMtlsCertCache
    {
        bool TryGetLatest(MiCacheKey key, DateTimeOffset nowUtc, out MtlsCertCacheEntry entry);
        void Put(MiCacheKey key, MtlsCertCacheEntry entry);
        int Prune(DateTimeOffset nowUtc);
    }
}
