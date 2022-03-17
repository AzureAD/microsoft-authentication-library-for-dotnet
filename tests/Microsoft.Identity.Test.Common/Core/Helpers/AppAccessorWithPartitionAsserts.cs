// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    internal class AppAccessorWithPartitionAsserts : InMemoryPartitionedAppTokenCacheAccessor
    {
        public AppAccessorWithPartitionAsserts(
            ICoreLogger logger,
            CacheOptions tokenCacheAccessorOptions) : base(logger, tokenCacheAccessorOptions)
        {

        }

        public override List<MsalAccessTokenCacheItem> GetAllAccessTokens(string partitionKey = null)
        {
            Assert.IsNotNull(partitionKey);
            return base.GetAllAccessTokens(partitionKey);
        }

        public override List<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null)
        {
            Assert.IsNotNull(partitionKey);
            Assert.Fail("App token cache - do not call GetAllAccounts");
            throw new InvalidOperationException();
        }

        public override List<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null)
        {
            Assert.IsNotNull(partitionKey);

            Assert.Fail("App token cache - do not call GetAllIdTokens");
            throw new InvalidOperationException();
        }

        public override List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null)
        {
            Assert.IsNotNull(partitionKey);

            Assert.Fail("App token cache - do not call GetAllRefreshTokens");
            throw new InvalidOperationException();
        }

        public override bool HasAccessOrRefreshTokens()
        {
            Assert.Fail("HasAccessOrRefreshTokens was called. It should not be called unless the token cache serialization hooks");
            throw new InvalidOperationException();
        }
    }
}
