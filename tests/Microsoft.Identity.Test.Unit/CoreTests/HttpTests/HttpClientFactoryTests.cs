// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.HttpTests
{
    [TestClass]
    public class HttpClientFactoryTests : TestBase
    {

        [TestMethod]
        public void TestGetHttpClientWithCustomCallback()
        {
            // Arrange
            var factory = new SimpleHttpClientFactory();

            // Act
            HttpClient client = factory.GetHttpClient((sender, cert, chain, errors) => true);

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void TestGetHttpClientWithNoCallback()
        {
            // Arrange
            var factory = new SimpleHttpClientFactory();

            // Act
            HttpClient client = factory.GetHttpClient();

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void TestHttpClientIsNotCached()
        {
            // Arrange
            var factory = new SimpleHttpClientFactory();
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> customCallback = (sender, cert, chain, errors) => true;

            // Act
            HttpClient client1 = factory.GetHttpClient(customCallback);
            HttpClient client2 = factory.GetHttpClient(customCallback);

            // Assert
            Assert.IsNotNull(client1);
            Assert.IsNotNull(client2);
            Assert.AreNotSame(client1, client2); // A new instance should be created each time to ensure callback is applied
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
            HttpClient handlerClient = factory.GetHttpClient((sender, cert, chain, errors) => true);

            // Assert
            Assert.IsNotNull(mtlsClient);
            Assert.IsNotNull(handlerClient);
            Assert.AreNotSame(mtlsClient, handlerClient); // Should be different instances
        }

    }
}
