// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.Throttling
{
    [TestClass]
    public class ThrottlingCacheTests
    {
        private readonly ILoggerAdapter _logger = NSubstitute.Substitute.For<ILoggerAdapter>();

        private readonly MsalServiceException _ex1 = new MsalServiceException("code1", "msg1");
        private readonly MsalServiceException _ex2 = new MsalServiceException("code2", "msg2");

        [TestMethod]
        public async Task GetRemovesExpired_Async()
        {
            // Arrange
            ThrottlingCache cache = new ThrottlingCache();
            
            cache.AddAndCleanup("k1", new ThrottlingCacheEntry(_ex1, TimeSpan.FromMilliseconds(1)), _logger); // expired
            cache.AddAndCleanup("k2", new ThrottlingCacheEntry(_ex2, TimeSpan.FromMilliseconds(10000)), _logger);

            // Act
            await Task.Delay(1).ConfigureAwait(false);
            bool isFound1 = cache.TryGetOrRemoveExpired("k1", _logger, out MsalServiceException foundEx1);
            bool isFound2 = cache.TryGetOrRemoveExpired("k2", _logger, out MsalServiceException foundEx2);

            // Assert
            Assert.IsFalse(isFound1, "Should have been removed as it is expired");
            Assert.IsTrue(isFound2, "Should have been found as it is not expired");
            Assert.IsNull(foundEx1);
            Assert.AreSame(_ex2, foundEx2);
        }

        [TestMethod]
        public async Task TestCleanup_Async()
        {
            // Arrange
            ThrottlingCache cache = new ThrottlingCache(50);

            cache.AddAndCleanup("k1", new ThrottlingCacheEntry(_ex1, TimeSpan.FromMilliseconds(1)), _logger); // expired
            cache.AddAndCleanup("k2", new ThrottlingCacheEntry(_ex2, TimeSpan.FromMilliseconds(10000)), _logger);
            
            // Act - should trigger a cleanup
            await Task.Delay(50).ConfigureAwait(false);
            cache.AddAndCleanup("k3", new ThrottlingCacheEntry(_ex2, TimeSpan.FromMilliseconds(1000)), _logger);

            // Assert
            bool isFound1 = cache.TryGetOrRemoveExpired("k1", _logger, out MsalServiceException foundEx1);
            bool isFound2 = cache.TryGetOrRemoveExpired("k2", _logger, out MsalServiceException foundEx2);

            Assert.IsFalse(isFound1, "Should have been removed by cleanup");
            Assert.IsTrue(isFound2, "Should have been found as it is not expired");
            Assert.IsNull(foundEx1);
            Assert.AreSame(_ex2, foundEx2);
        }
    }
}
