// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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
        public void RedirectConfigurationUsesSeparateHttpClientPool()
        {
            // Arrange
            var factory = new SimpleHttpClientFactory();
            var redirectControlFactory = (IMsalWsTrustHttpClientFactory)factory;

            // Act
            HttpClient defaultClient = factory.GetHttpClient();
            HttpClient noRedirectClient = redirectControlFactory.GetHttpClient(
                useDefaultCredentials: true);
            HttpClient secondNoRedirectClient = redirectControlFactory.GetHttpClient(
                useDefaultCredentials: true);
            HttpClient noRedirectNoCredentialsClient = redirectControlFactory.GetHttpClient(
                useDefaultCredentials: false);

            // Assert
            Assert.AreNotSame(defaultClient, noRedirectClient);
            Assert.AreSame(noRedirectClient, secondNoRedirectClient);
            Assert.AreNotSame(noRedirectClient, noRedirectNoCredentialsClient);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void WsTrustUsesCapableCustomFactory(bool useDefaultCredentials)
        {
            // Arrange
            var factory = Substitute.For<IMsalWsTrustHttpClientFactory>();
            using var client = new HttpClient();
            factory.GetHttpClient(useDefaultCredentials).Returns(client);
            var logger = Substitute.For<ILoggerAdapter>();
            var manager = new HttpManager(factory, disableInternalRetries: true);

            // Act
            HttpClient selectedClient = manager.GetHttpClient(
                null, null, allowAutoRedirect: false, useDefaultCredentials, logger);

            // Assert
            Assert.AreSame(client, selectedClient);
            factory.Received(1).GetHttpClient(useDefaultCredentials);
            factory.DidNotReceive().GetHttpClient();
            logger.DidNotReceive().Log(LogLevel.Warning, string.Empty, MsalErrorMessage.CustomHttpClientFactoryWsTrustFallback);
        }

        [TestMethod]
        public void WsTrustBypassesUnsupportedFactoryAndCachesSafeClients()
        {
            // Arrange
            var factory = Substitute.For<IMsalMtlsHttpClientFactory, IMsalSFHttpClientFactory>();
            using var customClient = new HttpClient();
            factory.GetHttpClient().Returns(customClient);
            factory.GetHttpClient(Arg.Any<X509Certificate2>()).Returns(customClient);
            var logger = Substitute.For<ILoggerAdapter>();
            var manager = new HttpManager(factory, disableInternalRetries: true);

            // Act
            HttpClient authenticatedClient = manager.GetHttpClient(
                null, null, allowAutoRedirect: false, useDefaultCredentials: true, logger);
            HttpClient anonymousClient = manager.GetHttpClient(
                null, null, allowAutoRedirect: false, useDefaultCredentials: false, logger);
            HttpClient repeatedAuthenticatedClient = manager.GetHttpClient(
                null, null, allowAutoRedirect: false, useDefaultCredentials: true, logger);
            HttpClient repeatedAnonymousClient = manager.GetHttpClient(
                null, null, allowAutoRedirect: false, useDefaultCredentials: false, logger);

            // Assert
            factory.DidNotReceiveWithAnyArgs().GetHttpClient(default(X509Certificate2));
            factory.DidNotReceive().GetHttpClient();
            ((IMsalSFHttpClientFactory)factory).DidNotReceiveWithAnyArgs().GetHttpClient(
                default(Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>));
            Assert.AreNotSame(customClient, authenticatedClient);
            Assert.AreNotSame(customClient, anonymousClient);
            Assert.AreNotSame(authenticatedClient, anonymousClient);
            Assert.AreSame(authenticatedClient, repeatedAuthenticatedClient);
            Assert.AreSame(anonymousClient, repeatedAnonymousClient);
            AssertWsTrustHandlerPolicy(authenticatedClient, useDefaultCredentials: true);
            AssertWsTrustHandlerPolicy(anonymousClient, useDefaultCredentials: false);
            logger.Received(4).Log(LogLevel.Warning, string.Empty, MsalErrorMessage.CustomHttpClientFactoryWsTrustFallback);
        }

        [TestMethod]
        public void WsTrustCapabilityDoesNotReplaceOtherCustomFactoryMethods()
        {
            // Arrange
            var factory = Substitute.For<IMsalWsTrustHttpClientFactory, IMsalMtlsHttpClientFactory, IMsalSFHttpClientFactory>();
            using var regularClient = new HttpClient();
            using var mtlsClient = new HttpClient();
            using var serviceFabricClient = new HttpClient();
            var mtlsFactory = (IMsalMtlsHttpClientFactory)factory;
            var serviceFabricFactory = (IMsalSFHttpClientFactory)factory;
            var certificate = CertHelper.GetOrCreateTestCert();
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validation = (_, _, _, _) => true;
            mtlsFactory.GetHttpClient(null).Returns(regularClient);
            mtlsFactory.GetHttpClient(certificate).Returns(mtlsClient);
            serviceFabricFactory.GetHttpClient(validation).Returns(serviceFabricClient);
            var logger = Substitute.For<ILoggerAdapter>();
            var manager = new HttpManager(factory, disableInternalRetries: true);

            // Act
            HttpClient selectedRegularClient = manager.GetHttpClient(
                null, null, allowAutoRedirect: true, useDefaultCredentials: true, logger);
            HttpClient selectedMtlsClient = manager.GetHttpClient(
                certificate, null, allowAutoRedirect: true, useDefaultCredentials: true, logger);
            HttpClient selectedServiceFabricClient = manager.GetHttpClient(
                null, validation, allowAutoRedirect: true, useDefaultCredentials: true, logger);

            // Assert
            Assert.AreSame(regularClient, selectedRegularClient);
            Assert.AreSame(mtlsClient, selectedMtlsClient);
            Assert.AreSame(serviceFabricClient, selectedServiceFabricClient);
            mtlsFactory.Received(1).GetHttpClient(null);
            mtlsFactory.Received(1).GetHttpClient(certificate);
            serviceFabricFactory.Received(1).GetHttpClient(validation);
            factory.DidNotReceiveWithAnyArgs().GetHttpClient(default(bool));
            logger.DidNotReceive().Log(LogLevel.Warning, string.Empty, MsalErrorMessage.CustomHttpClientFactoryWsTrustFallback);
        }

        [TestMethod]
        public void RegularRequestsStillUseUnsupportedCustomFactoryAfterWsTrustFallback()
        {
            // Arrange
            var factory = Substitute.For<IMsalHttpClientFactory>();
            using var client = new HttpClient();
            factory.GetHttpClient().Returns(client);
            var logger = Substitute.For<ILoggerAdapter>();
            var manager = new HttpManager(factory, disableInternalRetries: true);

            // Act
            manager.GetHttpClient(null, null, allowAutoRedirect: false, useDefaultCredentials: true, logger);
            HttpClient selectedClient = manager.GetHttpClient(
                null, null, allowAutoRedirect: true, useDefaultCredentials: true, logger);

            // Assert
            Assert.AreSame(client, selectedClient);
            factory.Received(1).GetHttpClient();
        }

        private static void AssertWsTrustHandlerPolicy(HttpClient client, bool useDefaultCredentials)
        {
            FieldInfo handlerField = typeof(HttpMessageInvoker)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(field => field.FieldType == typeof(HttpMessageHandler));
            var handler = handlerField.GetValue(client) as HttpClientHandler;
            Assert.IsNotNull(handler);
            Assert.IsFalse(handler.AllowAutoRedirect);
            Assert.AreEqual(useDefaultCredentials, handler.UseDefaultCredentials);
            if (!useDefaultCredentials)
            {
                Assert.IsNull(handler.Credentials);
            }
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

            // Act
            HttpClient mtlsClient = factory.GetHttpClient(cert);
            HttpClient handlerClient = factory.GetHttpClient((sender, cert, chain, errors) => true);

            // Assert
            Assert.IsNotNull(mtlsClient);
            Assert.IsNotNull(handlerClient);
            Assert.AreNotSame(mtlsClient, handlerClient); // Should be different instances
        }

        [TestMethod]
        public void TestMtlsHttpClientIsReusedForSameCertificateInstance()
        {
            // Arrange
            var factory = new SimpleHttpClientFactory();
            var certificate = CertHelper.GetOrCreateTestCert();

            // Act
            HttpClient firstClient = factory.GetHttpClient(certificate);
            HttpClient secondClient = factory.GetHttpClient(certificate);

            // Assert
            Assert.AreSame(firstClient, secondClient);
        }

        [TestMethod]
        public void TestMtlsHttpClientIsNotReusedForDifferentCertificateInstancesWithSameThumbprint()
        {
            // Arrange
            var factory = new SimpleHttpClientFactory();
            var firstCertificate = CertHelper.GetOrCreateTestCert();
            using var secondCertificate = new X509Certificate2(firstCertificate.Export(X509ContentType.Cert));
            Assert.AreEqual(firstCertificate.Thumbprint, secondCertificate.Thumbprint);

            // Act
            HttpClient firstClient = factory.GetHttpClient(firstCertificate);
            HttpClient secondClient = factory.GetHttpClient(secondCertificate);

            // Assert
            HttpClientHandler firstHandler = GetHttpClientHandler(firstClient);
            HttpClientHandler secondHandler = GetHttpClientHandler(secondClient);
            Assert.AreNotSame(firstClient, secondClient);
            Assert.AreSame(firstCertificate, firstHandler.ClientCertificates[0]);
            Assert.AreSame(secondCertificate, secondHandler.ClientCertificates[0]);
            Assert.AreSame(firstClient, factory.GetHttpClient(firstCertificate));
            Assert.AreSame(secondClient, factory.GetHttpClient(secondCertificate));
        }

        [TestMethod]
        public void TestMtlsHttpClientIsCreatedOnceForConcurrentAccess()
        {
            // Arrange
            var certificate = CertHelper.GetOrCreateTestCert();
            using var creationStarted = new ManualResetEventSlim();
            using var releaseCreation = new ManualResetEventSlim();
            int creationCount = 0;

            var cache = new ConditionalWeakTable<X509Certificate2, Lazy<HttpClient>>();
            HttpClient CreateClient(X509Certificate2 cert)
            {
                Interlocked.Increment(ref creationCount);
                creationStarted.Set();
                releaseCreation.Wait();
                return new HttpClient();
            }

            Task<HttpClient>[] clientTasks = Enumerable.Range(0, 20)
                .Select(_ => Task.Run(() =>
                    SimpleHttpClientFactory.GetOrCreateMtlsHttpClient(
                        cache,
                        certificate,
                        CreateClient)))
                .ToArray();

            // Act
            Assert.IsTrue(creationStarted.Wait(TimeSpan.FromSeconds(5)));
            releaseCreation.Set();
            Task.WaitAll(clientTasks);
            using HttpClient sharedClient = clientTasks[0].Result;

            // Assert
            Assert.AreEqual(1, creationCount);
            Assert.IsTrue(clientTasks.All(task => ReferenceEquals(sharedClient, task.Result)));
        }

        [TestMethod]
        public void TestMtlsHttpClientCreationFailureIsCachedPerCertificateInstance()
        {
            // Arrange
            var firstCertificate = CertHelper.GetOrCreateTestCert();
            using var secondCertificate = new X509Certificate2(firstCertificate.Export(X509ContentType.Cert));
            int creationCount = 0;
            var cache = new ConditionalWeakTable<X509Certificate2, Lazy<HttpClient>>();
            HttpClient CreateClient(X509Certificate2 cert)
            {
                Interlocked.Increment(ref creationCount);
                throw new InvalidOperationException("Test client creation failure.");
            }

            // Act
            InvalidOperationException firstException = Assert.Throws<InvalidOperationException>(
                () => SimpleHttpClientFactory.GetOrCreateMtlsHttpClient(
                    cache,
                    firstCertificate,
                    CreateClient));
            InvalidOperationException repeatedException = Assert.Throws<InvalidOperationException>(
                () => SimpleHttpClientFactory.GetOrCreateMtlsHttpClient(
                    cache,
                    firstCertificate,
                    CreateClient));
            InvalidOperationException secondCertificateException = Assert.Throws<InvalidOperationException>(
                () => SimpleHttpClientFactory.GetOrCreateMtlsHttpClient(
                    cache,
                    secondCertificate,
                    CreateClient));

            // Assert
            Assert.AreEqual("Test client creation failure.", firstException.Message);
            Assert.AreEqual(firstException.Message, repeatedException.Message);
            Assert.AreEqual(firstException.Message, secondCertificateException.Message);
            Assert.AreEqual(2, creationCount);
        }

        private static HttpClientHandler GetHttpClientHandler(HttpClient client)
        {
            FieldInfo handlerField = typeof(HttpMessageInvoker)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(field => field.FieldType == typeof(HttpMessageHandler));

            return (HttpClientHandler)handlerField.GetValue(client);
        }

    }
}
