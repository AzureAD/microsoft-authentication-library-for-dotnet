// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// Decorates a token cache with counting logic. Delegates must be configured before this.
    /// </summary>
    public class TokenCacheAccessRecorder
    {
        private readonly TokenCache _tokenCache;
        /// <summary>
        /// before access and before write should be the same as they are both triggered before the cache access happens. after access is triggered at the end of cache access, so it will be triggered for both read and write operations. after access with state change will only be triggered for write operations. so we can use these 3 counters to validate the number of read and write operations that happened on the cache.
        /// </summary>
        public int BeforeAccessCount { get; private set; } = 0;
        /// <summary>
        /// before write should only be triggered for write operations, so it should be the same as after access with state change. if before write count is different than after access with state change count, it means that there is an issue with the cache implementation where before write is not properly configured or not being triggered for write operations.
        /// </summary>
        public int BeforeWriteCount { get; private set; } = 0;
        /// <summary>
        /// after access will be triggered for both read and write operations, so it should be equal to the sum of before write count and the number of read operations. if after access total count is different than the sum of before write count and the number of read operations, it means that there is an issue with the cache implementation where after access is not properly configured or not being triggered for cache accesses.
        /// </summary>
        public int AfterAccessTotalCount { get; private set; } = 0;
        /// <summary>
        /// access with state change will only be triggered for write operations, so it should be the same as before write count. if after access with state change count is different than before write count, it means that there is an issue with the cache implementation where after access is not properly configured or not being triggered for write operations.
        /// </summary>
        public int AfterAccessWriteCount { get; private set; } = 0;

        /// <summary>
        /// Gets the arguments from the most recent BeforeAccess notification event.
        /// </summary>
        public TokenCacheNotificationArgs LastBeforeAccessNotificationArgs { get; private set; }
        /// <summary>
        /// Gets the arguments from the most recent BeforeWrite notification event.
        /// </summary>
        public TokenCacheNotificationArgs LastBeforeWriteNotificationArgs { get; private set; }
        /// <summary>
        /// Gets the arguments from the most recent AfterAccess notification event.
        /// </summary>
        public TokenCacheNotificationArgs LastAfterAccessNotificationArgs { get; private set; }

        /// <summary>
        /// token cache access recorder that can be used to record the number of times the token cache is accessed for read and write operations. it also allows to capture the arguments of the notifications for further assertions. this is useful for validating the behavior of the token cache in different scenarios and ensuring that it is being accessed as expected.
        /// </summary>
        /// <param name="tokenCache"></param>
        /// <param name="assertLogic"></param>
        public TokenCacheAccessRecorder(TokenCache tokenCache, Action<TokenCacheNotificationArgs> assertLogic = null)
        {
            _tokenCache = tokenCache;

            if ((tokenCache as ITokenCacheInternal).Accessor.GetType() == typeof(AppAccessorWithPartitionAsserts) ||
                (tokenCache as ITokenCacheInternal).Accessor.GetType() == typeof(UserAccessorWithPartitionAsserts))
            {
                ValidationHelpers.AssertFail("[TEST FAILURE] This is test setup issue. You cannot use TokenCacheAccessRecorder and WithCachePartitioningAsserts at the same time");
            }

            var existingBeforeAccessCallback = _tokenCache.BeforeAccess;
            _tokenCache.BeforeAccess = (args) =>
            {
                assertLogic?.Invoke(args);
                BeforeAccessCount++;
                LastBeforeAccessNotificationArgs = args;
                existingBeforeAccessCallback?.Invoke(args);
            };

            var existingBeforeWriteCallback = _tokenCache.BeforeWrite;
            _tokenCache.BeforeWrite = (args) =>
            {
                assertLogic?.Invoke(args);
                BeforeWriteCount++;
                LastBeforeWriteNotificationArgs = args;

                existingBeforeWriteCallback?.Invoke(args);
            };

            var existingAfterAccessCallback = _tokenCache.AfterAccess;
            _tokenCache.AfterAccess = (args) =>
            {
                assertLogic?.Invoke(args);
                AfterAccessTotalCount++;
                LastAfterAccessNotificationArgs = args;

                if (args.HasStateChanged)
                {
                    AfterAccessWriteCount++;
                }

                existingAfterAccessCallback?.Invoke(args);
            };
        }

        /// <summary>
        /// asserts that the recorded access counts match the expected values for read and write operations. this method validates that the token cache is being accessed the expected number of times for both read and write operations, and that the before and after access notifications are being triggered correctly. if the counts do not match the expected values, it indicates a potential issue with the cache implementation or the test setup, such as missing or misconfigured notification callbacks.
        /// </summary>
        /// <param name="expectedReads"></param>
        /// <param name="expectedWrites"></param>
        public void AssertAccessCounts(int expectedReads, int expectedWrites)
        {
            ValidationHelpers.AssertAreEqual(expectedWrites, BeforeWriteCount, "Writes");
            ValidationHelpers.AssertAreEqual(expectedWrites, AfterAccessWriteCount, "Writes");

            ValidationHelpers.AssertAreEqual(expectedReads, AfterAccessTotalCount - AfterAccessWriteCount, "Reads");
            ValidationHelpers.AssertAreEqual(expectedReads +  expectedWrites, BeforeAccessCount, "Reads");
        }

        /// <summary>
        /// waits until the recorded access counts match the expected values for read and write operations, or until the specified maximum time is reached. this method repeatedly checks the access counts and yields execution until the expected values are observed or the timeout occurs. it is useful for scenarios where the token cache operations are asynchronous or delayed, ensuring that the assertions are made only after the expected access patterns have been completed.
        /// </summary>
        /// <param name="expectedReads"></param>
        /// <param name="expectedWrites"></param>
        /// <param name="maxTimeInMilliSec"></param>
        public void WaitTo_AssertAcessCounts(int expectedReads, int expectedWrites, int maxTimeInMilliSec = 30000)
        {
            TestCommon.YieldTillSatisfied(() => BeforeWriteCount == expectedWrites && AfterAccessWriteCount == expectedWrites && AfterAccessTotalCount == (expectedReads + expectedWrites) && BeforeAccessCount == (expectedReads + expectedWrites), maxTimeInMilliSec);
            AssertAccessCounts(expectedReads, expectedWrites);
        }
    }
}
