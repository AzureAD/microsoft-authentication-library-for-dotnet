// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.ManagedIdentity.V2.Cache;
using Microsoft.Identity.Client.ManagedIdentity.V2.Cache.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class InMemoryMtlsCertCacheTests
    {
        private static MtlsCertCacheEntry MakeEntry(DateTimeOffset nb, TimeSpan lifetime)
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=mi1, DC=tenant1", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            var cert = req.CreateSelfSigned(nb.UtcDateTime, (nb + lifetime).UtcDateTime);
            return new MtlsCertCacheEntry(cert, new object(), keyHandle: string.Empty, createdAtUtc: nb);
        }

        [TestMethod]
        public void TryGetLatest_PrefersNewest_NotBefore_Then_NotAfter()
        {
            var cache = InMemoryMtlsCertCache.Shared;
            cache.ClearForTest();

            var key = MiCacheKey.FromStrings(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "bearer");

            var t0 = DateTimeOffset.UtcNow;
            cache.Put(key, MakeEntry(t0.AddMinutes(-10), TimeSpan.FromHours(1)));
            cache.Put(key, MakeEntry(t0.AddMinutes(-5), TimeSpan.FromHours(1)));

            Assert.IsTrue(cache.TryGetLatest(key, t0, out var e));
            Assert.IsTrue(e.NotBefore > t0.AddMinutes(-10), "should pick newer one");
        }

        [TestMethod]
        public void ExpiredEntries_ArePrunedAndNotReturned()
        {
            var cache = InMemoryMtlsCertCache.Shared;
            cache.ClearForTest();

            var key = MiCacheKey.FromStrings(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "bearer");

            var now = DateTimeOffset.UtcNow;
            cache.Put(key, MakeEntry(now.AddHours(-3), TimeSpan.FromHours(1))); // expired

            Assert.IsFalse(cache.TryGetLatest(key, now, out _), "expired should not be returned");
        }
    }
}
