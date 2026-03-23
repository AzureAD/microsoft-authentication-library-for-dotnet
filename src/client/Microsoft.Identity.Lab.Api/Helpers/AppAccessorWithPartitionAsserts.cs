// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    internal class AppAccessorWithPartitionAsserts : InMemoryPartitionedAppTokenCacheAccessor
    {
        public AppAccessorWithPartitionAsserts(
            ILoggerAdapter logger, 
            CacheOptions tokenCacheAccessorOptions) : base(logger, tokenCacheAccessorOptions)
        {

        }

        public override List<MsalAccessTokenCacheItem> GetAllAccessTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            ValidationHelpers.AssertIsNotNull(partitionKey);
            return base.GetAllAccessTokens(partitionKey, requestlogger);
        }

        public override List<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            ValidationHelpers.AssertIsNotNull(partitionKey);
            ValidationHelpers.AssertFail("App token cache - do not call GetAllAccounts");
            throw new InvalidOperationException();
        }

        public override List<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            ValidationHelpers.AssertIsNotNull(partitionKey);

            ValidationHelpers.AssertFail("App token cache - do not call GetAllIdTokens");
            throw new InvalidOperationException();
        }

        public override List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            ValidationHelpers.AssertIsNotNull(partitionKey);

            ValidationHelpers.AssertFail("App token cache - do not call GetAllRefreshTokens");
            throw new InvalidOperationException();
        }

        public override bool HasAccessOrRefreshTokens()
        {
            ValidationHelpers.AssertFail("HasAccessOrRefreshTokens was called. It should not be called unless the token cache serialization hooks");
            throw new InvalidOperationException();
        }
    }
}
