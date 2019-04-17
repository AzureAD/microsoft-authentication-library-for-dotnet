// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class AdalLegacyCacheManager : IAdalLegacyCacheManager
    {
        public AdalLegacyCacheManager(ILegacyCachePersistence legacyCachePersistence)
        {
            LegacyCachePersistence = legacyCachePersistence;
        }

        public ILegacyCachePersistence LegacyCachePersistence { get; }

        /// <inheritdoc />
        public void WriteAdalRefreshToken()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Credential GetAdalRefreshToken()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<Microsoft.Identity.Client.CacheV2.Schema.Account> GetAllAdalUsers()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveAdalUser()
        {
            throw new NotImplementedException();
        }
    }
}
