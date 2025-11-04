// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute; // for ILoggerAdapter substitute

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class PersistentCertificateStoreUnitTests
    {
        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private static ILoggerAdapter Logger => Substitute.For<ILoggerAdapter>();

        [TestInitialize]
        public void ImdsV2Tests_Init()
        {
            // Clean persisted store so prior DataRows/runs don't leak into this test
            if (ImdsV2TestStoreCleaner.IsWindows)
            {
                // A broad sweep is simplest and safe for our fake endpoints/certs
                ImdsV2TestStoreCleaner.RemoveAllTestArtifacts();
            }
        }

        // --- helpers ---

        private static X509Certificate2 CreateSelfSignedWithKey(string subject, TimeSpan lifetime)
        {
            using var rsa = RSA.Create(2048);

            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                new X500DistinguishedName(subject),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            DateTimeOffset notBefore, notAfter;

            if (lifetime <= TimeSpan.Zero)
            {
                // produce an expired cert safely (notAfter < now, but still > notBefore)
                var now = DateTimeOffset.UtcNow;
                notBefore = now.AddDays(-2);
                notAfter = now.AddSeconds(-30);
            }
            else
            {
                notBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
                notAfter = notBefore.Add(lifetime);
            }

            using var ephemeral = req.CreateSelfSigned(notBefore, notAfter);

            // Re-import as PFX so the private key is persisted and usable across TFMs
            var pfx = ephemeral.Export(X509ContentType.Pfx, "");
            return new X509Certificate2(
                pfx,
                "",
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        private static void RemoveAliasFromStore(string alias)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            X509Certificate2[] items;
            try
            {
                items = new X509Certificate2[store.Certificates.Count];
                store.Certificates.CopyTo(items, 0);
            }
            catch
            {
                items = store.Certificates.Cast<X509Certificate2>().ToArray();
            }

            foreach (var c in items)
            {
                try
                {
                    if (FriendlyNameCodec.TryDecode(c.FriendlyName, out var a, out _)
                        && StringComparer.Ordinal.Equals(a, alias))
                    {
                        try
                        { store.Remove(c); }
                        catch { /* best-effort */ }
                    }
                }
                finally
                {
                    c.Dispose();
                }
            }
        }

        // Small polling helper to absorb store-write propagation timing
        private static bool WaitForFind(string alias, out CertificateCacheValue value, int retries = 10, int delayMs = 50)
        {
            for (int i = 0; i < retries; i++)
            {
                if (PersistentCertificateStore.TryFind(alias, out value, Logger))
                    return true;

                Thread.Sleep(delayMs);
            }

            value = default;
            return false;
        }

        // --- tests ---

        [TestMethod]
        public void TryPersist_Then_TryFind_HappyPath()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-happy-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantX";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                using var cert = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(3));

                PersistentCertificateStore.TryPersist(alias, cert, ep, clientId: "ignored", logger: Logger);

                // Verify we can find it (with a small retry to avoid timing flakes)
                Assert.IsTrue(WaitForFind(alias, out var value), "Persisted cert should be found.");
                Assert.IsNotNull(value.Certificate);
                Assert.AreEqual(ep, value.Endpoint);
                Assert.AreEqual(guid, value.ClientId);
                Assert.IsTrue(value.Certificate.HasPrivateKey);
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void TryPersist_NewestWins_SkipOlder()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-newest-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantY";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                using var older = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(2));
                using var newer = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(3));

                // Persist older first, then newer
                PersistentCertificateStore.TryPersist(alias, older, ep, "ignored", Logger);
                PersistentCertificateStore.TryPersist(alias, newer, ep, "ignored", Logger);

                // Selection should return the newer one (by NotAfter)
                Assert.IsTrue(WaitForFind(alias, out var value), "Expected to find persisted cert.");
                var delta = Math.Abs((value.Certificate.NotAfter - newer.NotAfter).TotalSeconds);
                Assert.IsTrue(delta <= 2, "Newest persisted cert should be selected.");
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void TryPersist_Skip_Add_When_NewerOrEqual_AlreadyPresent()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-skip-old-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantZ";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                using var newer = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(3));
                using var older = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(2));

                // Add newer first
                PersistentCertificateStore.TryPersist(alias, newer, ep, "ignored", Logger);

                // Attempt to add older (should be skipped)
                PersistentCertificateStore.TryPersist(alias, older, ep, "ignored", Logger);

                // TryFind returns the newer
                Assert.IsTrue(WaitForFind(alias, out var value), "Expected to find persisted cert.");
                var delta = Math.Abs((value.Certificate.NotAfter - newer.NotAfter).TotalSeconds);
                Assert.IsTrue(delta <= 2);
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void TryFind_Rejects_NonGuid_CN()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-nonguid-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenant1";

            try
            {
                using var cert = CreateSelfSignedWithKey("CN=Test", TimeSpan.FromDays(3));

                PersistentCertificateStore.TryPersist(alias, cert, ep, "ignored", Logger);

                // Should not return non-GUID CN entries
                Assert.IsFalse(PersistentCertificateStore.TryFind(alias, out _, Logger));
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void TryFind_Rejects_Short_Lifetime_Less_Than_24h()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-short-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenant2";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                using var shortLived = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromHours(23)); // < 24h

                PersistentCertificateStore.TryPersist(alias, shortLived, ep, "ignored", Logger);

                // Selection policy should reject it
                Assert.IsFalse(PersistentCertificateStore.TryFind(alias, out _, Logger));
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void PruneExpired_Removes_Only_Expired()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-prune-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenant3";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                // Expired cert (NotAfter in the past)
                var now = DateTimeOffset.UtcNow;
                using var expired = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromSeconds(-30));

                PersistentCertificateStore.TryPersist(alias, expired, ep, "ignored", Logger);

                // Ensure it is (potentially) present, then prune
                PersistentCertificateStore.TryPruneAliasOlderThan(alias, now, Logger);

                // Verify no entries remain for alias
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                var any = store.Certificates
                    .Cast<X509Certificate2>()
                    .Any(c => FriendlyNameCodec.TryDecode(c.FriendlyName, out var a, out _)
                           && StringComparer.Ordinal.Equals(a, alias));

                foreach (var c in store.Certificates)
                    c.Dispose();

                Assert.IsFalse(any, "Expired entries for alias should be pruned.");
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void TryPersist_Skips_When_Mutex_Busy_Then_Succeeds_After_Release()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-mutex-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenant4";
            var guid = Guid.NewGuid().ToString("D");

            using var cert = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(2));

            try
            {
                using var hold = new ManualResetEventSlim(false);
                using var done = new ManualResetEventSlim(false);

                // Hold the alias lock from a background thread for ~400ms
                var t = new Thread(() =>
                {
                    InterprocessLock.TryWithAliasLock(
                        alias,
                        timeout: TimeSpan.FromMilliseconds(250),
                        action: () =>
                        {
                            hold.Set();          // signal that lock is held
                            Thread.Sleep(400);   // hold lock for a bit
                        });
                    done.Set();
                });
                t.IsBackground = true;
                t.Start();

                // Wait until the lock is held
                Assert.IsTrue(hold.Wait(2000));

                // First persist should *skip* due to contention (best-effort)
                PersistentCertificateStore.TryPersist(alias, cert, ep, "ignored", Logger);

                // Verify not added yet
                Assert.IsFalse(PersistentCertificateStore.TryFind(alias, out _, Logger));

                // After lock released, try again => should persist
                Assert.IsTrue(done.Wait(5000));
                PersistentCertificateStore.TryPersist(alias, cert, ep, "ignored", Logger);

                Assert.IsTrue(WaitForFind(alias, out var v), "Expected to find after lock released.");
                Assert.AreEqual(ep, v.Endpoint);
                Assert.AreEqual(guid, v.ClientId);
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        #region Additional tests

        [TestMethod]
        public void TryPersist_DoesNotPersist_When_NoPrivateKey()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-nokey-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantX";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                // Create a cert WITH key, then strip the key by exporting only the public part
                using var withKey = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(2));
                using var pubOnly = new X509Certificate2(withKey.Export(X509ContentType.Cert)); // public-only
                Assert.IsFalse(pubOnly.HasPrivateKey, "Test setup must produce a public-only cert.");

                // Persist should no-op for no-private-key
                PersistentCertificateStore.TryPersist(alias, pubOnly, ep, "ignored", Logger);

                // Should not find anything for alias
                Assert.IsFalse(PersistentCertificateStore.TryFind(alias, out _, Logger));
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void TryFind_Boundary_Exactly24h_IsRejected()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-24h-exact-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantY";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                // Our CreateSelfSignedWithKey uses notBefore = now-2m, so lifetime of (24h + 2m)
                // yields NotAfter ≈ (now + 24h). That should be rejected by policy (<= 24h is insufficient).
                using var exactly24h = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromHours(24).Add(TimeSpan.FromMinutes(2)));

                PersistentCertificateStore.TryPersist(alias, exactly24h, ep, "ignored", Logger);
                Assert.IsFalse(PersistentCertificateStore.TryFind(alias, out _, Logger),
                    "Exactly-24h remaining should be rejected by policy.");
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void TryFind_Boundary_JustOver24h_IsAccepted()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-24h-plus-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantY";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                // 24h + 3m lifetime (with notBefore = now-2m) → NotAfter ≈ now + 24h + 1m → acceptable
                using var over24h = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromHours(24).Add(TimeSpan.FromMinutes(3)));

                PersistentCertificateStore.TryPersist(alias, over24h, ep, "ignored", Logger);
                Assert.IsTrue(PersistentCertificateStore.TryFind(alias, out var v, Logger),
                    "Slightly-over-24h remaining should be accepted.");
                Assert.AreEqual(ep, v.Endpoint);
                Assert.AreEqual(guid, v.ClientId);
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void TryFind_Returns_Newest_Endpoint_And_ClientId()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-newest-ep-" + Guid.NewGuid().ToString("N");
            var epOld = "https://fake_mtls/tenant/OLD";
            var epNew = "https://fake_mtls/tenant/NEW";
            var guidOld = Guid.NewGuid().ToString("D");
            var guidNew = Guid.NewGuid().ToString("D");

            try
            {
                using var older = CreateSelfSignedWithKey("CN=" + guidOld, TimeSpan.FromDays(2));
                using var newer = CreateSelfSignedWithKey("CN=" + guidNew, TimeSpan.FromDays(3));

                PersistentCertificateStore.TryPersist(alias, older, epOld, "ignored", Logger);
                PersistentCertificateStore.TryPersist(alias, newer, epNew, "ignored", Logger);

                Assert.IsTrue(PersistentCertificateStore.TryFind(alias, out var v, Logger), "Expected find for alias.");
                Assert.AreEqual(guidNew, v.ClientId, "ClientId must reflect the newest NotAfter entry.");
                Assert.AreEqual(epNew, v.Endpoint, "Endpoint must come from the newest NotAfter entry.");
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void TryFind_Isolated_Per_Alias_No_Cross_Talk()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias1 = "alias-a-" + Guid.NewGuid().ToString("N");
            var alias2 = "alias-b-" + Guid.NewGuid().ToString("N");
            var ep1 = "https://fake_mtls/tenantA";
            var ep2 = "https://fake_mtls/tenantB";
            var guid1 = Guid.NewGuid().ToString("D");
            var guid2 = Guid.NewGuid().ToString("D");

            try
            {
                using var c1 = CreateSelfSignedWithKey("CN=" + guid1, TimeSpan.FromDays(3));
                using var c2 = CreateSelfSignedWithKey("CN=" + guid2, TimeSpan.FromDays(3));

                PersistentCertificateStore.TryPersist(alias1, c1, ep1, "ignored", Logger);
                PersistentCertificateStore.TryPersist(alias2, c2, ep2, "ignored", Logger);

                Assert.IsTrue(PersistentCertificateStore.TryFind(alias1, out var v1, Logger));
                Assert.AreEqual(ep1, v1.Endpoint);
                Assert.AreEqual(guid1, v1.ClientId);

                Assert.IsTrue(PersistentCertificateStore.TryFind(alias2, out var v2, Logger));
                Assert.AreEqual(ep2, v2.Endpoint);
                Assert.AreEqual(guid2, v2.ClientId);
            }
            finally
            {
                RemoveAliasFromStore(alias1);
                RemoveAliasFromStore(alias2);
            }
        }

        [TestMethod]
        public void TryFind_Prefers_Newest_Among_Many()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "alias-many-" + Guid.NewGuid().ToString("N");
            var ep1 = "https://fake_mtls/ep1";
            var ep2 = "https://fake_mtls/ep2";
            var ep3 = "https://fake_mtls/ep3";
            var g1 = Guid.NewGuid().ToString("D");
            var g2 = Guid.NewGuid().ToString("D");
            var g3 = Guid.NewGuid().ToString("D");

            try
            {
                using var c1 = CreateSelfSignedWithKey("CN=" + g1, TimeSpan.FromDays(1));
                using var c2 = CreateSelfSignedWithKey("CN=" + g2, TimeSpan.FromDays(2));
                using var c3 = CreateSelfSignedWithKey("CN=" + g3, TimeSpan.FromDays(3)); // newest

                PersistentCertificateStore.TryPersist(alias, c1, ep1, "ignored", Logger);
                PersistentCertificateStore.TryPersist(alias, c2, ep2, "ignored", Logger);
                PersistentCertificateStore.TryPersist(alias, c3, ep3, "ignored", Logger);

                Assert.IsTrue(PersistentCertificateStore.TryFind(alias, out var v, Logger), "Expected find.");
                Assert.AreEqual(g3, v.ClientId);
                Assert.AreEqual(ep3, v.Endpoint);
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        #endregion
    }
}
