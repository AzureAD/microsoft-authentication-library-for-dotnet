// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class PlatformProxyFactoryTests
    {
        [TestMethod]
        public void PlatformProxyFactoryDoesNotCacheTheProxy()
        {
            // Act
            var proxy1 = PlatformProxyFactory.CreatePlatformProxy(null);
            var proxy2 = PlatformProxyFactory.CreatePlatformProxy(null);

            // Assert
            Assert.IsFalse(proxy1 == proxy2);
        }

        [TestMethod]
        public void PlatformProxyFactoryReturnsInstances()
        {
            // Arrange
            var proxy = PlatformProxyFactory.CreatePlatformProxy(null);

            // Act and Assert
            Assert.AreNotSame(
                proxy.CreateLegacyCachePersistence(),
                proxy.CreateLegacyCachePersistence());

            Assert.AreNotSame(
                proxy.CreateTokenCacheAccessor(),
                proxy.CreateTokenCacheAccessor());

            Assert.AreSame(
                proxy.CryptographyManager,
                proxy.CryptographyManager);

        }

        [TestMethod]
        public void GetEnvRetrievesAValue()
        {
            try
            {
                // Arrange
                Environment.SetEnvironmentVariable("proxy_foo", "bar");

                var proxy = PlatformProxyFactory.CreatePlatformProxy(null);

                // Act
                string actualValue = proxy.GetEnvironmentVariable("proxy_foo");
                string actualEmptyValue = proxy.GetEnvironmentVariable("no_such_env_var_exists");


                // Assert
                Assert.AreEqual("bar", actualValue);
                Assert.IsNull(actualEmptyValue);
            }
            finally
            {
                Environment.SetEnvironmentVariable("proxy_foo", "");
            }
        }

        [TestMethod]
        public void GetEnvThrowsArgNullEx()
        {

            AssertException.Throws<ArgumentNullException>(
                () =>
                PlatformProxyFactory.CreatePlatformProxy(null).GetEnvironmentVariable(""));
        }


    }
}
