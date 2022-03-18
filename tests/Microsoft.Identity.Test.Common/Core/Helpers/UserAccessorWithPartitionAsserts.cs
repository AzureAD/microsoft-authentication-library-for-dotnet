// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    internal class UserAccessorWithPartitionAsserts : InMemoryPartitionedUserTokenCacheAccessor
    {
        public UserAccessorWithPartitionAsserts(ICoreLogger logger, CacheOptions tokenCacheAccessorOptions) : base(logger, tokenCacheAccessorOptions)
        {

        }

        public override IReadOnlyList<MsalAccessTokenCacheItem> GetAllAccessTokens(string partitionKey = null, ICoreLogger requestlogger = null)
        {
            Assert.IsNotNull(partitionKey);
            return base.GetAllAccessTokens(partitionKey);
        }

        public override IReadOnlyList<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null, ICoreLogger requestlogger = null)
        {
            Assert.IsNotNull(partitionKey);
            return base.GetAllAccounts(partitionKey);
        }

        public override IReadOnlyList<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null, ICoreLogger requestlogger = null)
        {
            Assert.IsNotNull(partitionKey);
            return base.GetAllIdTokens(partitionKey);
        }

        public override IReadOnlyList<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null, ICoreLogger requestlogger = null)
        {
            Assert.IsNotNull(partitionKey);
            return base.GetAllRefreshTokens(partitionKey);
        }

        public override bool HasAccessOrRefreshTokens()
        {
            Assert.Fail("HasAccessOrRefreshTokens was called. It should not be called unless the token cache serialization hooks");
            throw new InvalidOperationException();
        }
    }
}
