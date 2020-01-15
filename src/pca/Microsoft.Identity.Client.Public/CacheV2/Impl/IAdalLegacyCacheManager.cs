// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    /// <summary>
    /// Interface to handle transforming unified schema types to/from the ADAL Legacy cache format
    /// and storing/retrieving them to/from the adal cache persistence.
    /// </summary>
    internal interface IAdalLegacyCacheManager
    {
        ILegacyCachePersistence LegacyCachePersistence { get; }

        void WriteAdalRefreshToken();
        Credential GetAdalRefreshToken();
        IEnumerable<Microsoft.Identity.Client.CacheV2.Schema.Account> GetAllAdalUsers();
        void RemoveAdalUser();
    }
}
