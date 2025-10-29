// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class InMemoryCertificateCacheTests
    {
        private static X509Certificate2 CreateSelfSignedCert(TimeSpan lifetime, string subjectCn = "CN=CacheTest")
        {
            using var rsa = RSA.Create(2048);
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                new X500DistinguishedName(subjectCn),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Give NotBefore a small headroom to avoid clock skew flakes
            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
            var notAfter = notBefore.Add(lifetime);
            return req.CreateSelfSigned(notBefore, notAfter);
        }

        [TestMethod]
        public void TryGet_EmptyCache_ReturnsFalse()
        {
            var cache = new InMemoryCertificateCache();
            var ok = cache.TryGet("key-1", out _);
            Assert.IsFalse(ok);
        }

        [TestMethod]
        public void Set_Then_TryGet_Hit_And_ReturnsClone()
        {
            var cache = new InMemoryCertificateCache();
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(2));
            const string key = "key-hit-1";
            const string ep = "https://mtls.endpoint";
            const string cid = "11111111-1111-1111-1111-111111111111";

            cache.Set(key, cert, ep, cid);

            var ok = cache.TryGet(key, out var value);
            Assert.IsTrue(ok);
            Assert.IsNotNull(value.Certificate);
            try
            {
                Assert.AreEqual(ep, value.Endpoint);
                Assert.AreEqual(cid, value.ClientId);

                // Verify clone: instance is different but same thumbprint
                Assert.AreNotSame(cert, value.Certificate);
                Assert.AreEqual(cert.Thumbprint, value.Certificate.Thumbprint, ignoreCase: true);
            }
            finally
            {
                // Caller owns the clone returned by TryGet
                value.Certificate.Dispose();
            }
        }

        [TestMethod]
        public void Set_Skips_When_LessThan_MinLifetime()
        {
            var cache = new InMemoryCertificateCache();

            // Certificate lifetime shorter than product threshold (24h)
            using var shortCert = CreateSelfSignedCert(TimeSpan.FromHours(1));
            cache.Set("short-key", shortCert, "https://mtls", "client-guid");

            var ok = cache.TryGet("short-key", out _);
            Assert.IsFalse(ok, "Cache should skip certs with remaining lifetime < 24h.");
        }

        [TestMethod]
        public void Set_SameKey_Replaces_Previous()
        {
            var cache = new InMemoryCertificateCache();
            using var certA = CreateSelfSignedCert(TimeSpan.FromDays(3), "CN=A");
            using var certB = CreateSelfSignedCert(TimeSpan.FromDays(3), "CN=B");

            const string key = "replace-key";
            cache.Set(key, certA, "https://ep", "cid");
            cache.Set(key, certB, "https://ep", "cid");

            var ok = cache.TryGet(key, out var v);
            Assert.IsTrue(ok);
            try
            {
                Assert.AreEqual(certB.Thumbprint, v.Certificate.Thumbprint, ignoreCase: true,
                    "Newest certificate should be returned after REPLACE.");
            }
            finally
            {
                v.Certificate.Dispose();
            }
        }

        [TestMethod]
        public void Remove_Removes_Entry()
        {
            var cache = new InMemoryCertificateCache();
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(2));

            cache.Set("k1", cert, "https://ep", "cid");
            var removed = cache.Remove("k1");
            Assert.IsTrue(removed);

            var ok = cache.TryGet("k1", out _);
            Assert.IsFalse(ok);
        }

        [TestMethod]
        public void Clear_Removes_All()
        {
            var cache = new InMemoryCertificateCache();
            using var c1 = CreateSelfSignedCert(TimeSpan.FromDays(2));
            using var c2 = CreateSelfSignedCert(TimeSpan.FromDays(2));

            cache.Set("k1", c1, "https://ep1", "cid1");
            cache.Set("k2", c2, "https://ep2", "cid2");

            cache.Clear();

            Assert.IsFalse(cache.TryGet("k1", out _));
            Assert.IsFalse(cache.TryGet("k2", out _));
        }

        [TestMethod]
        public void Validate_Arguments()
        {
            var cache = new InMemoryCertificateCache();
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(2));

            // TryGet
            Assert.ThrowsException<ArgumentException>(() => cache.TryGet("  ", out _));
            Assert.ThrowsException<ArgumentException>(() => cache.TryGet(null, out _));

            // Set
            Assert.ThrowsException<ArgumentException>(() => cache.Set(" ", cert, "ep", "cid"));
            Assert.ThrowsException<ArgumentNullException>(() => cache.Set("k", null, "ep", "cid"));
            Assert.ThrowsException<ArgumentException>(() => cache.Set("k", cert, " ", "cid"));
            Assert.ThrowsException<ArgumentException>(() => cache.Set("k", cert, "ep", " "));

            // Remove
            Assert.ThrowsException<ArgumentException>(() => cache.Remove(""));
            Assert.ThrowsException<ArgumentException>(() => cache.Remove(null));
        }

        [TestMethod]
        public void Dispose_Prevents_Use()
        {
            var cache = new InMemoryCertificateCache();
            cache.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(() => cache.TryGet("k", out _));
            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(2));
            Assert.ThrowsException<ObjectDisposedException>(() => cache.Set("k", cert, "ep", "cid"));
            Assert.ThrowsException<ObjectDisposedException>(() => cache.Remove("k"));
            Assert.ThrowsException<ObjectDisposedException>(() => cache.Clear());
        }
    }

    [TestClass]
    public class CertificateCacheEntryTests
    {
        private static X509Certificate2 MakeCert(TimeSpan lifetime)
        {
            using var rsa = RSA.Create(2048);
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                new X500DistinguishedName("CN=EntryTest"),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
            var notAfter = notBefore.Add(lifetime);
            return req.CreateSelfSigned(notBefore, notAfter);
        }

        [TestMethod]
        public void IsExpiredUtc_Boundary()
        {
            using var cert = MakeCert(TimeSpan.FromHours(2));
            var notAfterUtc = cert.NotAfter.ToUniversalTime();

            var entry = new CertificateCacheEntry(
                certificate: new X509Certificate2(cert),
                notAfterUtc: new DateTimeOffset(notAfterUtc),
                endpoint: "https://ep",
                clientId: "cid");

            // Before notAfter -> not expired
            Assert.IsFalse(entry.IsExpiredUtc(DateTimeOffset.UtcNow));

            // After notAfter -> expired
            var later = new DateTimeOffset(notAfterUtc).AddMinutes(1);
            Assert.IsTrue(entry.IsExpiredUtc(later));
        }

        [TestMethod]
        public void Dispose_IsIdempotent_SetsFlag()
        {
            using var cert = MakeCert(TimeSpan.FromDays(2));
            var entry = new CertificateCacheEntry(
                certificate: new X509Certificate2(cert),
                notAfterUtc: DateTimeOffset.UtcNow.AddDays(2),
                endpoint: "https://ep",
                clientId: "cid");

            Assert.IsFalse(entry.IsDisposed);
            entry.Dispose();
            Assert.IsTrue(entry.IsDisposed);

            // No throw on second dispose
            entry.Dispose();
            Assert.IsTrue(entry.IsDisposed);
        }
    }

    [TestClass]
    public class CertificateCacheValueTests
    {
        private static X509Certificate2 MakeCert()
        {
            using var rsa = RSA.Create(2048);
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                new X500DistinguishedName("CN=ValTest"),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-1);
            var notAfter = notBefore.AddDays(2);
            return req.CreateSelfSigned(notBefore, notAfter);
        }

        [TestMethod]
        public void Ctor_Throws_On_Nulls()
        {
            // certificate null
            Assert.ThrowsException<ArgumentNullException>(() =>
                new CertificateCacheValue(null, "ep", "cid"));

            using var cert = MakeCert();

            // endpoint null
            Assert.ThrowsException<ArgumentNullException>(() =>
                new CertificateCacheValue(cert, null, "cid"));

            // clientId null
            Assert.ThrowsException<ArgumentNullException>(() =>
                new CertificateCacheValue(cert, "ep", null));
        }

        [TestMethod]
        public void Properties_Are_Immutable_And_Preserved()
        {
            using var cert = MakeCert();

            var value = new CertificateCacheValue(cert, "https://ep", "cid");
            Assert.AreEqual("https://ep", value.Endpoint);
            Assert.AreEqual("cid", value.ClientId);
            Assert.AreEqual(cert.Thumbprint, value.Certificate.Thumbprint, ignoreCase: true);

            // Caller should dispose when done
            value.Certificate.Dispose();
        }
    }
}
