// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// Decorates a token cache with counting logic. Delegates must be configured before this.
    /// </summary>
    public class TokenCacheAccessRecorder
    {
        private readonly TokenCache _tokenCache;
        private int _beforeAccessCount = 0;
        private int _beforeWriteCount = 0;
        private int _afterAccessTotalCount = 0;
        private int _afterAccessWriteCount = 0;

        public TokenCacheNotificationArgs LastNotificationArgs { get; private set; }

        public TokenCacheAccessRecorder(TokenCache tokenCache)
        {
            _tokenCache = tokenCache;

            var existingBeforeAccessCallback = _tokenCache.BeforeAccess;
            _tokenCache.BeforeAccess = (args) =>
            {
                _beforeAccessCount++;
                LastNotificationArgs = args;
                existingBeforeAccessCallback?.Invoke(args);
            };

            var existingBeforeWriteCallback = _tokenCache.BeforeWrite;
            _tokenCache.BeforeWrite = (args) =>
            {
                _beforeWriteCount++;
                LastNotificationArgs = args;

                existingBeforeWriteCallback?.Invoke(args);
            };

            var existingAfterAccessCallback = _tokenCache.AfterAccess;
            _tokenCache.AfterAccess = (args) =>
            {
                _afterAccessTotalCount++;
                LastNotificationArgs = args;

                if (args.HasStateChanged)
                {
                    _afterAccessWriteCount++;
                }

                existingAfterAccessCallback?.Invoke(args);
            };

        }

        public void AssertAccessCounts(int expectedReads, int expectedWrites)
        {
            Assert.AreEqual(expectedWrites, _beforeWriteCount, "Writes");
            Assert.AreEqual(expectedWrites, _afterAccessWriteCount, "Writes");

            Assert.AreEqual(expectedReads, _afterAccessTotalCount - _afterAccessWriteCount, "Reads");
            Assert.AreEqual(expectedReads +  expectedWrites, _beforeAccessCount, "Reads");
        }
    }
}
