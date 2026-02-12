// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class MtlsBindingCacheTests
    {
        private static ILoggerAdapter Logger => Substitute.For<ILoggerAdapter>();

        private static X509Certificate2 CreateSelfSignedCert(
            TimeSpan lifetime,
            string subjectCn = "CN=CacheTest")
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                new X500DistinguishedName(subjectCn),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
            var notAfter = notBefore.Add(lifetime);
            return req.CreateSelfSigned(notBefore, notAfter);
        }

        [TestMethod]
        public async Task GetOrCreateAsync_MemoryCacheHit_ValidKey_ReturnsWithoutFactory()
        {
            // Arrange
            var memory = new InMemoryCertificateCache();
            var persisted = new NoOpPersistentCertificateCache();
            var cache = new MtlsBindingCache(memory, persisted);

            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(2));
            const string key = "valid-key";
            const string ep = "https://mtls.endpoint";
            const string cid = "11111111-1111-1111-1111-111111111111";

            memory.Set(key, new CertificateCacheValue(cert, ep, cid));

            int factoryCalls = 0;

            // Act
            var result = await cache.GetOrCreateAsync(
                key,
                () =>
                {
                    factoryCalls++;
                    return Task.FromResult(new MtlsBindingInfo(cert, ep, cid));
                },
                CancellationToken.None,
                Logger).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(ep, result.Endpoint);
            Assert.AreEqual(cid, result.ClientId);
            Assert.AreEqual(0, factoryCalls, "Factory should not be called on valid cache hit.");

            result.Certificate.Dispose();
        }

        [TestMethod]
        public async Task GetOrCreateAsync_MemoryCacheHit_InvalidKey_EvictsAndMintsNew()
        {
            // Arrange
            var memory = new InMemoryCertificateCache();
            var persisted = new NoOpPersistentCertificateCache();
            var cache = new MtlsBindingCache(memory, persisted);

            // Create a public-only cert (no private key) to simulate inaccessible key
            using var fullCert = CreateSelfSignedCert(TimeSpan.FromDays(2));
            byte[] publicOnly = fullCert.Export(X509ContentType.Cert);
            using var pubCert = new X509Certificate2(publicOnly);

            const string key = "invalid-key";
            const string ep = "https://mtls.endpoint";
            const string cid = "22222222-2222-2222-2222-222222222222";

            memory.Set(key, new CertificateCacheValue(pubCert, ep, cid));

            using var freshCert = CreateSelfSignedCert(TimeSpan.FromDays(2), "CN=Fresh");
            const string freshEp = "https://mtls.fresh";
            const string freshCid = "33333333-3333-3333-3333-333333333333";
            int factoryCalls = 0;

            // Act
            var result = await cache.GetOrCreateAsync(
                key,
                () =>
                {
                    factoryCalls++;
                    return Task.FromResult(new MtlsBindingInfo(freshCert, freshEp, freshCid));
                },
                CancellationToken.None,
                Logger).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(freshEp, result.Endpoint);
            Assert.AreEqual(freshCid, result.ClientId);
            Assert.AreEqual(1, factoryCalls, "Factory should be called once after evicting invalid cert.");
        }

        [TestMethod]
        public async Task GetOrCreateAsync_PersistedCacheHit_InvalidKey_MintsNew()
        {
            // Arrange
            var memory = new InMemoryCertificateCache();

            // Create a mock persistent cache that returns a public-only cert
            var persisted = Substitute.For<IPersistentCertificateCache>();
            using var fullCert = CreateSelfSignedCert(TimeSpan.FromDays(2));
            byte[] publicOnly = fullCert.Export(X509ContentType.Cert);
            var pubCert = new X509Certificate2(publicOnly);

            const string key = "persist-invalid";
            const string ep = "https://mtls.endpoint";
            const string cid = "44444444-4444-4444-4444-444444444444";

            persisted.Read(key, out Arg.Any<CertificateCacheValue>(), Arg.Any<ILoggerAdapter>())
                .Returns(x =>
                {
                    x[1] = new CertificateCacheValue(pubCert, ep, cid);
                    return true;
                });

            var cache = new MtlsBindingCache(memory, persisted);

            using var freshCert = CreateSelfSignedCert(TimeSpan.FromDays(2), "CN=Fresh");
            const string freshEp = "https://mtls.fresh";
            const string freshCid = "55555555-5555-5555-5555-555555555555";
            int factoryCalls = 0;

            // Act
            var result = await cache.GetOrCreateAsync(
                key,
                () =>
                {
                    factoryCalls++;
                    return Task.FromResult(new MtlsBindingInfo(freshCert, freshEp, freshCid));
                },
                CancellationToken.None,
                Logger).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(freshEp, result.Endpoint);
            Assert.AreEqual(freshCid, result.ClientId);
            Assert.AreEqual(1, factoryCalls, "Factory should be called when persisted cert has inaccessible key.");
        }

        [TestMethod]
        public async Task GetOrCreateAsync_CacheMiss_CallsFactory()
        {
            // Arrange
            var memory = new InMemoryCertificateCache();
            var persisted = new NoOpPersistentCertificateCache();
            var cache = new MtlsBindingCache(memory, persisted);

            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(2));
            const string ep = "https://mtls.endpoint";
            const string cid = "66666666-6666-6666-6666-666666666666";
            int factoryCalls = 0;

            // Act
            var result = await cache.GetOrCreateAsync(
                "miss-key",
                () =>
                {
                    factoryCalls++;
                    return Task.FromResult(new MtlsBindingInfo(cert, ep, cid));
                },
                CancellationToken.None,
                Logger).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, factoryCalls, "Factory should be called on cache miss.");
            Assert.AreEqual(ep, result.Endpoint);
        }
    }
}
