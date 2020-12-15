// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using System.Linq;
using Microsoft.Identity.Client.Http;
using System.Net;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal;

#if SUPPORTS_BROKER
using Microsoft.Identity.Client.Platforms.Features.WamBroker;
#endif

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

#if NET_CORE || NET5_WIN
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
#if DESKTOP
 [TestMethod]
        public void PlatformProxy_HttpClient_NetDesktop()
        {
            // Arrange
            var factory = PlatformProxyFactory.CreatePlatformProxy(null)
                .CreateDefaultHttpClientFactory();          

            // Assert
            Assert.IsTrue(factory is 
                Client.Platforms.net45.Http.NetDesktopHttpClientFactory);
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

#if NET5_WIN
            Assert.IsTrue(proxy.BrokerSupportsWamAccounts);
            Assert.IsTrue(proxy.CanBrokerSupportSilentAuth());
#endif

            Assert.IsTrue(proxy.IsSystemWebViewAvailable);
            Assert.AreSame(
                Constants.DefaultRedirectUri,
                proxy.GetDefaultRedirectUri("cid", false));

#if DESKTOP || NET5_WIN
            Assert.IsTrue(proxy.UseEmbeddedWebViewDefault);
            Assert.AreSame(
                Constants.NativeClientRedirectUri,
                proxy.GetDefaultRedirectUri("cid", true));

#else
            Assert.IsFalse(proxy.UseEmbeddedWebViewDefault);
             Assert.AreSame(
                Constants.LocalHostRedirectUri,
                proxy.GetDefaultRedirectUri("cid", true));
#endif
        }

    }
}
