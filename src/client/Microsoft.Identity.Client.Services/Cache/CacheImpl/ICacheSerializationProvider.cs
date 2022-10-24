// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Cache.CacheImpl
{
    internal interface ICacheSerializationProvider
    {
        // Important - do not use SetBefore / SetAfter methods, as these are reserved for app developers
        // Instead, use AfterAccess = x, BeforeAccess = y
        // See UapTokenCacheBlobStorage for an example
        void Initialize(TokenCache tokenCache);
    }
}
