// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                proxy.CreateTokenCacheAccessor(null),
                proxy.CreateTokenCacheAccessor(null));

            Assert.AreSame(
                proxy.CryptographyManager,
                proxy.CryptographyManager);
        }

        [TestMethod]
        public void PlatformProxy_HttpClient()
        {
            // Arrange
            var proxy = PlatformProxyFactory.CreatePlatformProxy(null);
            var factory1 = proxy.CreateDefaultHttpClientFactory();
            var factory2 = proxy.CreateDefaultHttpClientFactory();

            // Act
            var client1 = factory1.GetHttpClient();
            var client2 = factory1.GetHttpClient();
            var client3 = factory2.GetHttpClient();

            // Assert
            Assert.AreNotSame(factory1, factory2, "HttpClient factory does not need to be static");

            Assert.AreSame(
               client1, client2, "On NetDesktop and NetCore, the HttpClient should be static");
            Assert.AreSame(
               client2, client3, "On NetDesktop and NetCore, the HttpClient should be static");
            Assert.AreEqual("application/json",
                client1.DefaultRequestHeaders.Accept.Single().MediaType);
            Assert.AreEqual(HttpClientConfig.MaxResponseContentBufferSizeInBytes,
                client1.MaxResponseContentBufferSize);
        }

#if NET_CORE || NETFRAMEWORK
        [TestMethod]
        public void PlatformProxy_HttpClient_NetCore()
        {
            // Arrange
            var factory = PlatformProxyFactory.CreatePlatformProxy(null)
                .CreateDefaultHttpClientFactory();

            // Act
            var client1 = factory.GetHttpClient();
            var client2 = factory.GetHttpClient();

            // Assert
            Assert.IsTrue(factory is SimpleHttpClientFactory);
            Assert.AreSame(client1, client2, "On NetDesktop and NetCore, the HttpClient should be static");

        }
#endif

        [TestMethod]
        public void PlatformProxy_HttpClient_DoesNotSetGlobalProperties()
        {
            // Arrange
            int originalDnsTimeout = ServicePointManager.DnsRefreshTimeout;
            int originalConnLimit = ServicePointManager.DefaultConnectionLimit;

            try
            {
                int newDnsTimeout = 1001;
                int newConnLimit = 42;

                ServicePointManager.DnsRefreshTimeout = newDnsTimeout;
                ServicePointManager.DefaultConnectionLimit = newConnLimit;

                // Act
                var factory = PlatformProxyFactory.CreatePlatformProxy(null)
                    .CreateDefaultHttpClientFactory();
                _ = factory.GetHttpClient();

                // Assert - the factory does not override these global properties
                Assert.AreEqual(newDnsTimeout, ServicePointManager.DnsRefreshTimeout);
                Assert.AreEqual(newConnLimit, ServicePointManager.DefaultConnectionLimit);

            }
            finally
            {
                ServicePointManager.DnsRefreshTimeout = originalDnsTimeout;
                ServicePointManager.DefaultConnectionLimit = originalConnLimit;
            }
        }

        [TestMethod]
        public void FlagDefaultsTest()
        {
            var proxy = PlatformProxyFactory.CreatePlatformProxy(null);

            Assert.IsTrue(proxy.BrokerSupportsWamAccounts);
            Assert.IsTrue(proxy.CanBrokerSupportSilentAuth());

            Assert.AreEqual(
                Constants.DefaultRedirectUri,
                proxy.GetDefaultRedirectUri("cid", false));

#if NETFRAMEWORK
   Assert.AreEqual(
          Constants.NativeClientRedirectUri,
          proxy.GetDefaultRedirectUri("cid", true));
#else
 Assert.AreEqual(
          Constants.LocalHostRedirectUri,
          proxy.GetDefaultRedirectUri("cid", true));
#endif


        }

    }
}
