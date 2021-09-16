// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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
        public int BeforeAccessCount { get; private set; } = 0;
        public int BeforeWriteCount { get; private set; } = 0;
        public int AfterAccessTotalCount { get; private set; } = 0;
        public int AfterAccessWriteCount { get; private set; } = 0;

        public TokenCacheNotificationArgs LastBeforeAccessNotificationArgs { get; private set; }
        public TokenCacheNotificationArgs LastBeforeWriteNotificationArgs { get; private set; }
        public TokenCacheNotificationArgs LastAfterAccessNotificationArgs { get; private set; }

        public TokenCacheAccessRecorder(TokenCache tokenCache)
        {
            _tokenCache = tokenCache;

            var existingBeforeAccessCallback = _tokenCache.BeforeAccess;
            _tokenCache.BeforeAccess = (args) =>
            {
                BeforeAccessCount++;
                LastBeforeAccessNotificationArgs = args;
                existingBeforeAccessCallback?.Invoke(args);
            };

            var existingBeforeWriteCallback = _tokenCache.BeforeWrite;
            _tokenCache.BeforeWrite = (args) =>
            {
                BeforeWriteCount++;
                LastBeforeWriteNotificationArgs = args;

                existingBeforeWriteCallback?.Invoke(args);
            };

            var existingAfterAccessCallback = _tokenCache.AfterAccess;
            _tokenCache.AfterAccess = (args) =>
            {
                AfterAccessTotalCount++;
                LastAfterAccessNotificationArgs = args;

                if (args.HasStateChanged)
                {
                    AfterAccessWriteCount++;
                }

                existingAfterAccessCallback?.Invoke(args);
            };

        }

        public void AssertAccessCounts(int expectedReads, int expectedWrites)
        {
            Assert.AreEqual(expectedWrites, BeforeWriteCount, "Writes");
            Assert.AreEqual(expectedWrites, AfterAccessWriteCount, "Writes");

            Assert.AreEqual(expectedReads, AfterAccessTotalCount - AfterAccessWriteCount, "Reads");
            Assert.AreEqual(expectedReads +  expectedWrites, BeforeAccessCount, "Reads");
        }

        public void WaitTo_AssertAcessCounts(int expectedReads, int expectedWrites, int maxTimeInMilliSec = 30000)
        {
            YieldTillSatisfied(() => BeforeWriteCount == expectedWrites && AfterAccessWriteCount == expectedWrites && AfterAccessTotalCount == (expectedReads + expectedWrites) && BeforeAccessCount == (expectedReads + expectedWrites), maxTimeInMilliSec);
            AssertAccessCounts(expectedReads, expectedWrites);
        }

        private bool YieldTillSatisfied(Func<bool> func, int maxTimeInMilliSec = 30000)
        {
            int iCount = maxTimeInMilliSec / 100;
            while (iCount > 0)
            {
                if (func())
                {
                    return true;
                }
                Thread.Yield();
                Thread.Sleep(100);
                iCount--;
            }

            return false;
        }
    }
}
