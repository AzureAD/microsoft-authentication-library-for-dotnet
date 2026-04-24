// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Lab.Api.Helpers;

namespace Microsoft.Identity.Lab.Api.Helpers
{
    internal class UserAccessorWithPartitionAsserts : InMemoryPartitionedUserTokenCacheAccessor
    {
        public UserAccessorWithPartitionAsserts(ILoggerAdapter logger, CacheOptions tokenCacheAccessorOptions) : base(logger, tokenCacheAccessorOptions)
        {

        }

        public override List<MsalAccessTokenCacheItem> GetAllAccessTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            Assert.IsNotNull(partitionKey);
            return base.GetAllAccessTokens(partitionKey, requestlogger);
        }

        public override List<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            Assert.IsNotNull(partitionKey);
            return base.GetAllAccounts(partitionKey, requestlogger);
        }

        public override List<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            Assert.IsNotNull(partitionKey);
            return base.GetAllIdTokens(partitionKey, requestlogger);
        }

        public override List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            Assert.IsNotNull(partitionKey);
            return base.GetAllRefreshTokens(partitionKey, requestlogger);
        }

        public override bool HasAccessOrRefreshTokens()
        {
            Assert.Fail("HasAccessOrRefreshTokens was called. It should not be called unless the token cache serialization hooks");
            throw new InvalidOperationException();
        }
    }
}
