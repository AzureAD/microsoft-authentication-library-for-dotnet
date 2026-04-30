// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
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
        public void TestHttpClientWithSameCallback_ReturnsCachedInstance()
        {
            // Arrange
            SimpleHttpClientFactory.ResetStaticStateForTest();
            var factory = new SimpleHttpClientFactory();
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> customCallback = (sender, cert, chain, errors) => true;

            // Act - same delegate instance passed on both calls
            HttpClient client1 = factory.GetHttpClient(customCallback);
            HttpClient client2 = factory.GetHttpClient(customCallback);

            // Assert - same delegate → same cached HttpClient (avoids socket exhaustion)
            Assert.IsNotNull(client1);
            Assert.AreSame(client1, client2);
        }

        [TestMethod]
        public void TestHttpClientWithDifferentCallbacks_ReturnsDifferentInstances()
        {
            // Arrange
            SimpleHttpClientFactory.ResetStaticStateForTest();
            var factory = new SimpleHttpClientFactory();
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> callback1 = (sender, cert, chain, errors) => true;
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> callback2 = (sender, cert, chain, errors) => false;

            // Act - different delegate instances passed
            HttpClient client1 = factory.GetHttpClient(callback1);
            HttpClient client2 = factory.GetHttpClient(callback2);

            // Assert - different callbacks → different HttpClient instances
            Assert.IsNotNull(client1);
            Assert.IsNotNull(client2);
            Assert.AreNotSame(client1, client2);
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

        [TestMethod]
        public void TestGetHttpClient_DoesNotLeakHttpClients()
        {
            // Arrange - reset static state so we start from a clean pool
            SimpleHttpClientFactory.ResetStaticStateForTest();
            var factory = new SimpleHttpClientFactory();

            // Act - call GetHttpClient multiple times with the same (default) key
            factory.GetHttpClient();
            factory.GetHttpClient();
            factory.GetHttpClient();

            int created = SimpleHttpClientFactory.HttpClientCreationCount;

            // Assert - CreateHttpClient should be called exactly once.
            // Before the fix, GetOrAdd(key, CreateHttpClient()) eagerly evaluates
            // CreateHttpClient() on every call, causing unnecessary throwaway
            // HttpClient/HttpClientHandler allocations.
            Assert.AreEqual(1, created,
                $"CreateHttpClient was called {created} times for 3 lookups. " +
                "Use GetOrAdd(key, factory_delegate) to avoid creating throwaway HttpClient instances.");
        }

        [TestMethod]
        public void TestGetHttpClientWithMtlsCert_DoesNotLeakHttpClients()
        {
            // Arrange - reset static state so we start from a clean pool
            SimpleHttpClientFactory.ResetStaticStateForTest();
            var factory = new SimpleHttpClientFactory();
            var cert = CertHelper.GetOrCreateTestCert();

            // Act - call GetHttpClient(cert) multiple times with the same certificate
            factory.GetHttpClient(cert);
            factory.GetHttpClient(cert);
            factory.GetHttpClient(cert);

            int created = SimpleHttpClientFactory.HttpClientCreationCount;

            // Assert - CreateMtlsHttpClient should be called exactly once for the same thumbprint key.
            // Repeated lookups with the same cert should return the cached instance without
            // creating throwaway HttpClient/HttpClientHandler allocations.
            Assert.AreEqual(1, created,
                $"CreateMtlsHttpClient was called {created} times for 3 lookups with the same certificate. " +
                "Use GetOrAdd(key, factory_delegate) to avoid creating throwaway HttpClient instances.");
        }

        [TestMethod]
        public async Task TestGetHttpClient_ConcurrentCalls_DoNotLeakHttpClients()
        {
            // Arrange - reset static state so we start from a clean pool
            SimpleHttpClientFactory.ResetStaticStateForTest();
            var factory = new SimpleHttpClientFactory();

            const int threadCount = 20;
            var tasks = new List<Task<HttpClient>>(threadCount);

            // Act - call GetHttpClient() concurrently from many threads at once
            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(() => factory.GetHttpClient()));
            }

            HttpClient[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

            int created = SimpleHttpClientFactory.HttpClientCreationCount;

            // Assert - all callers got a non-null client and the same cached instance.
            // With Lazy<HttpClient>(ExecutionAndPublication) only one HttpClient should
            // ever be constructed, regardless of how many threads raced on the same key.
            foreach (HttpClient client in results)
            {
                Assert.IsNotNull(client);
                Assert.AreSame(results[0], client, "All concurrent callers should receive the same cached HttpClient instance.");
            }

            Assert.AreEqual(1, created,
                $"CreateHttpClient was called {created} times across {threadCount} concurrent calls. " +
                "Lazy<HttpClient>(ExecutionAndPublication) should guarantee exactly one construction per key.");
        }

    }
}
