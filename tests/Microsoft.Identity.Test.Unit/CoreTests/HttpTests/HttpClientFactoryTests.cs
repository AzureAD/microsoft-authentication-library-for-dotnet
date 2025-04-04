// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.HttpTests
{
    [TestClass]
    public class HttpClientFactoryTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            // You might need to add a method to clear the HttpClient cache in SimpleHttpClientFactory
        }

        [TestMethod]
        public void TestGetHttpClientWithCustomHandler()
        {
            // Arrange
            var factory = new SimpleHttpClientFactory();
            var customHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true
            };

            // Act
            HttpClient client = factory.GetHttpClient(customHandler);

            // Assert
            Assert.IsNotNull(client);
            // You might want to test that the client was properly cached and reused
        }

        [TestMethod]
        public void TestGetHttpClientWithNullHandler()
        {
            // Arrange
            var factory = new SimpleHttpClientFactory();

            // Act
            HttpClient client = factory.GetHttpClient((HttpClientHandler)null);

            // Assert
            Assert.IsNotNull(client);
            // This should return the default client (non-MTLS)
        }

        [TestMethod]
        public void TestHttpClientCacheReuse()
        {
            // Arrange
            var factory = new SimpleHttpClientFactory();
            var customHandler = new HttpClientHandler();

            // Act
            HttpClient client1 = factory.GetHttpClient(customHandler);
            HttpClient client2 = factory.GetHttpClient(customHandler);

            // Assert
            Assert.IsNotNull(client1);
            Assert.IsNotNull(client2);
            Assert.AreSame(client1, client2); // Should be the same instance
        }

        [TestMethod]
        public void TestHttpClientWithMtlsCertificateAndCustomHandler()
        {
            // Arrange
            var factory = new SimpleHttpClientFactory();
            var cert = CertHelper.GetOrCreateTestCert();
            var customHandler = new HttpClientHandler();

            // Act
            HttpClient mtlsClient = factory.GetHttpClient(cert);
            HttpClient handlerClient = factory.GetHttpClient(customHandler);

            // Assert
            Assert.IsNotNull(mtlsClient);
            Assert.IsNotNull(handlerClient);
            Assert.AreNotSame(mtlsClient, handlerClient); // Should be different instances
        }

    }
}
