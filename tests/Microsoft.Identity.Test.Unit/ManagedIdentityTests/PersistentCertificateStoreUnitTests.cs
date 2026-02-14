// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Identity.Client;
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

        private IPersistentCertificateCache _cache;

        [TestInitialize]
        public void ImdsV2Tests_Init()
        {
            // Create the platform cache once per test run.
            // It's safe to instantiate on non-Windows; methods no-op internally.
            _cache = new WindowsPersistentCertificateCache();

            // Clean persisted store so prior DataRows/runs don't leak into this test
            if (ImdsV2TestStoreCleaner.IsWindows)
            {
                // A broad sweep is simplest and safe for our fake endpoints/certs
                ImdsV2TestStoreCleaner.RemoveAllTestArtifacts();
            }
        }

        private static void WindowsOnly()
        {
            if (!IsWindows)
            {
                Assert.Inconclusive("Windows-only");
            }
        }

        private static void NonWindowsOnly()
        {
            if (IsWindows)
            {
                Assert.Inconclusive("Non-Windows-only");
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

            foreach (var cert in items)
            {
                try
                {
                    if (MsiCertificateFriendlyNameEncoder.TryDecode(cert.FriendlyName, out var decodedAlias, out _)
                        && StringComparer.Ordinal.Equals(decodedAlias, alias))
                    {
                        try
                        { store.Remove(cert); }
                        catch { /* best-effort */ }
                    }
                }
                finally
                {
                    cert.Dispose();
                }
            }
        }

        // Small polling helper to absorb store-write propagation timing
        private bool WaitForFind(string alias, out CertificateCacheValue value, int retries = 10, int delayMs = 50)
        {
            for (int i = 0; i < retries; i++)
            {
                if (_cache.Read(alias, out value, Logger))
                    return true;

                Thread.Sleep(delayMs);
            }

            value = default;
            return false;
        }

        // --- tests ---

        [TestMethod]
        public void Write_Then_Read_HappyPath()
        {
            WindowsOnly();

            var alias = "alias-happy-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantX";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                using var cert = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(3));

                _cache.Write(alias, cert, ep, Logger);

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
        public void Write_NewestWins_SkipOlder()
        {
            WindowsOnly();

            var alias = "alias-newest-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantY";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                using var older = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(2));
                using var newer = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(3));

                // Persist older first, then newer
                _cache.Write(alias, older, ep, Logger);
                _cache.Write(alias, newer, ep, Logger);

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
        public void Write_Skip_Add_When_NewerOrEqual_AlreadyPresent()
        {
            WindowsOnly();

            var alias = "alias-skip-old-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantZ";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                using var newer = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(3));
                using var older = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(2));

                // Add newer first
                _cache.Write(alias, newer, ep, Logger);

                // Attempt to add older (should be skipped)
                _cache.Write(alias, older, ep, Logger);

                // Read returns the newer
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
        public void Read_Rejects_NonGuid_CN()
        {
            WindowsOnly();

            var alias = "alias-nonguid-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenant1";

            try
            {
                using var cert = CreateSelfSignedWithKey("CN=Test", TimeSpan.FromDays(3));

                _cache.Write(alias, cert, ep, Logger);

                // Should not return non-GUID CN entries
                Assert.IsFalse(_cache.Read(alias, out _, Logger));
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void Read_Rejects_Short_Lifetime_Less_Than_24h()
        {
            WindowsOnly();

            var alias = "alias-short-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenant2";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                using var shortLived = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromHours(23)); // < 24h

                _cache.Write(alias, shortLived, ep, Logger);

                // Selection policy should reject it
                Assert.IsFalse(_cache.Read(alias, out _, Logger));
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void Delete_Prunes_Expired_Only()
        {
            WindowsOnly();

            var alias = "alias-prune-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenant3";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                // Expired cert (NotAfter in the past)
                using var expired = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromSeconds(-30));

                _cache.Write(alias, expired, ep, Logger);

                // Ensure it is (potentially) present, then prune
                _cache.Delete(alias, Logger);

                // Verify no entries remain for alias
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                var any = store.Certificates
                    .Cast<X509Certificate2>()
                    .Any(c => MsiCertificateFriendlyNameEncoder.TryDecode(c.FriendlyName, out var a, out _)
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
        public void Write_Skips_When_Mutex_Busy_Then_Succeeds_After_Release()
        {
            WindowsOnly();

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
                        },
                        logVerbose: _ => { });
                    done.Set();
                });
                t.IsBackground = true;
                t.Start();

                // Wait until the lock is held
                Assert.IsTrue(hold.Wait(2000));

                // First write should *skip* due to contention (best-effort)
                _cache.Write(alias, cert, ep, Logger);

                // Verify not added yet
                Assert.IsFalse(_cache.Read(alias, out _, Logger));

                // After lock released, try again => should persist
                Assert.IsTrue(done.Wait(5000));
                _cache.Write(alias, cert, ep, Logger);

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
        public void Write_DoesNotPersist_When_NoPrivateKey()
        {
            WindowsOnly();

            var alias = "alias-nokey-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantX";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                // Create a cert WITH key, then strip the key by exporting only the public part
                using var withKey = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(2));
                using var pubOnly = new X509Certificate2(withKey.Export(X509ContentType.Cert)); // public-only
                Assert.IsFalse(pubOnly.HasPrivateKey, "Test setup must produce a public-only cert.");

                // Write should no-op from a usability standpoint (read won't return it)
                _cache.Write(alias, pubOnly, ep, Logger);

                // Should not find anything usable for alias
                Assert.IsFalse(_cache.Read(alias, out _, Logger));
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void Read_Boundary_Exactly24h_IsRejected()
        {
            WindowsOnly();

            var alias = "alias-24h-exact-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantY";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                // Our CreateSelfSignedWithKey uses notBefore = now-2m, so lifetime of (24h + 2m)
                // yields NotAfter ≈ (now + 24h). That should be rejected by policy (<= 24h is insufficient).
                using var exactly24h = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromHours(24).Add(TimeSpan.FromMinutes(2)));

                _cache.Write(alias, exactly24h, ep, Logger);
                Assert.IsFalse(_cache.Read(alias, out _, Logger),
                    "Exactly-24h remaining should be rejected by policy.");
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void Read_Boundary_JustOver24h_IsAccepted()
        {
            WindowsOnly();

            var alias = "alias-24h-plus-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/tenantY";
            var guid = Guid.NewGuid().ToString("D");

            try
            {
                // 24h + 3m lifetime (with notBefore = now-2m) → NotAfter ≈ now + 24h + 1m → acceptable
                using var over24h = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromHours(24).Add(TimeSpan.FromMinutes(3)));

                _cache.Write(alias, over24h, ep, Logger);
                Assert.IsTrue(_cache.Read(alias, out var v, Logger),
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
        public void Read_Returns_Newest_Endpoint_And_ClientId()
        {
            WindowsOnly();

            var alias = "alias-newest-ep-" + Guid.NewGuid().ToString("N");
            var epOld = "https://fake_mtls/tenant/OLD";
            var epNew = "https://fake_mtls/tenant/NEW";
            var guidOld = Guid.NewGuid().ToString("D");
            var guidNew = Guid.NewGuid().ToString("D");

            try
            {
                using var older = CreateSelfSignedWithKey("CN=" + guidOld, TimeSpan.FromDays(2));
                using var newer = CreateSelfSignedWithKey("CN=" + guidNew, TimeSpan.FromDays(3));

                _cache.Write(alias, older, epOld, Logger);
                _cache.Write(alias, newer, epNew, Logger);

                Assert.IsTrue(_cache.Read(alias, out var v, Logger), "Expected read for alias.");
                Assert.AreEqual(guidNew, v.ClientId, "ClientId must reflect the newest NotAfter entry.");
                Assert.AreEqual(epNew, v.Endpoint, "Endpoint must come from the newest NotAfter entry.");
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void Read_Isolated_Per_Alias_No_Cross_Talk()
        {
            WindowsOnly();

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

                _cache.Write(alias1, c1, ep1, Logger);
                _cache.Write(alias2, c2, ep2, Logger);

                Assert.IsTrue(_cache.Read(alias1, out var v1, Logger));
                Assert.AreEqual(ep1, v1.Endpoint);
                Assert.AreEqual(guid1, v1.ClientId);

                Assert.IsTrue(_cache.Read(alias2, out var v2, Logger));
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
        public void Read_Prefers_Newest_Among_Many()
        {
            WindowsOnly();

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

                _cache.Write(alias, c1, ep1, Logger);
                _cache.Write(alias, c2, ep2, Logger);
                _cache.Write(alias, c3, ep3, Logger);

                Assert.IsTrue(_cache.Read(alias, out var v, Logger), "Expected read.");
                Assert.AreEqual(g3, v.ClientId);
                Assert.AreEqual(ep3, v.Endpoint);
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void NonWindows_WindowsPersistentCertificateCache_IsNoOp()
        {
            NonWindowsOnly();

            var alias = "alias-nonwindows-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/nonwindows";
            var guid = Guid.NewGuid().ToString("D");

            using var cert = CreateSelfSignedWithKey("CN=" + guid, TimeSpan.FromDays(2));

            // On non-Windows, WindowsPersistentCertificateCache should behave as a no-op:
            // Write() and Read() return without touching any real store.
            _cache.Write(alias, cert, ep, Logger);

            Assert.IsFalse(_cache.Read(alias, out _, Logger),
                "On non-Windows the persistent cache should effectively be disabled.");
        }

        [TestMethod]
        public void Write_And_Read_Handle_Alias_EdgeCases()
        {
            WindowsOnly();

            var ep = "https://fake_mtls/alias-edge";
            using var cert = CreateSelfSignedWithKey("CN=" + Guid.NewGuid().ToString("D"),
                TimeSpan.FromDays(3));

            // Aliases that should be valid and round-trip through persistence
            string[] goodAliases =
            {
                new string('a', 2048),          // very long alias
                "alias-ümläüt-用户-🔐"         // unicode + special characters (no illegal delimiters)
            };

            foreach (var alias in goodAliases)
            {
                try
                {
                    _cache.Write(alias, cert, ep, Logger);

                    Assert.IsTrue(WaitForFind(alias, out var value),
                        $"Expected alias '{alias}' to be persisted.");
                    Assert.AreEqual(ep, value.Endpoint);
                }
                finally
                {
                    RemoveAliasFromStore(alias);
                }
            }

            // Aliases that should be rejected by the FriendlyName encoder and not persisted
            string[] badAliases =
            {
                null,
                string.Empty,
                "   ",
                "bad|alias"   // '|' is illegal for our FriendlyName grammar
            };

            foreach (var alias in badAliases)
            {
                _cache.Write(alias, cert, ep, Logger);
                Assert.IsFalse(_cache.Read(alias, out _, Logger),
                    $"Alias '{alias ?? "<null>"}' should not be persisted.");
            }
        }

        #endregion

        #region //MTLS specific tests 

        [TestMethod]
        public void DeleteAllForAlias_Removes_All_Certificates_For_Alias()
        {
            WindowsOnly();

            var alias = "alias-delall-" + Guid.NewGuid().ToString("N");
            var ep = "https://fake_mtls/delall";
            var logger = Logger;

            try
            {
                // Write 3 certs with increasing NotAfter so all 3 are added (policy only skips older/equal)
                using var c1 = CreateSelfSignedWithKey("CN=" + Guid.NewGuid().ToString("D"), TimeSpan.FromDays(2));
                using var c2 = CreateSelfSignedWithKey("CN=" + Guid.NewGuid().ToString("D"), TimeSpan.FromDays(3));
                using var c3 = CreateSelfSignedWithKey("CN=" + Guid.NewGuid().ToString("D"), TimeSpan.FromDays(4));

                _cache.Write(alias, c1, ep, logger);
                _cache.Write(alias, c2, ep, logger);
                _cache.Write(alias, c3, ep, logger);

                Assert.IsTrue(WaitForFind(alias, out _), "Expected at least one persisted entry.");
                Assert.IsTrue(CountAliasInStore(alias) >= 2, "Expected multiple certs persisted for alias.");

                // Act
                _cache.DeleteAllForAlias(alias, logger);

                // Assert: store should have 0 for alias
                Assert.IsTrue(WaitForAliasCount(alias, expected: 0), "Expected all certs for alias to be deleted.");
                Assert.IsFalse(_cache.Read(alias, out _, logger), "Read should return false after DeleteAllForAlias.");
            }
            finally
            {
                RemoveAliasFromStore(alias);
            }
        }

        [TestMethod]
        public void DeleteAllForAlias_Does_Not_Remove_Other_Aliases()
        {
            WindowsOnly();

            var alias1 = "alias-delall-a-" + Guid.NewGuid().ToString("N");
            var alias2 = "alias-delall-b-" + Guid.NewGuid().ToString("N");
            var ep1 = "https://fake_mtls/a";
            var ep2 = "https://fake_mtls/b";
            var logger = Logger;

            try
            {
                using var c1 = CreateSelfSignedWithKey("CN=" + Guid.NewGuid().ToString("D"), TimeSpan.FromDays(3));
                using var c2 = CreateSelfSignedWithKey("CN=" + Guid.NewGuid().ToString("D"), TimeSpan.FromDays(3));

                _cache.Write(alias1, c1, ep1, logger);
                _cache.Write(alias2, c2, ep2, logger);

                Assert.IsTrue(WaitForFind(alias1, out _));
                Assert.IsTrue(WaitForFind(alias2, out _));
                Assert.AreEqual(1, CountAliasInStore(alias1));
                Assert.AreEqual(1, CountAliasInStore(alias2));

                // Act
                _cache.DeleteAllForAlias(alias1, logger);

                // Assert
                Assert.IsTrue(WaitForAliasCount(alias1, expected: 0), "alias1 should be removed.");
                Assert.AreEqual(1, CountAliasInStore(alias2), "alias2 must remain.");
                Assert.IsTrue(_cache.Read(alias2, out var v2, logger), "Read(alias2) should still succeed.");

                // caller owns returned cert
                v2.Certificate.Dispose();
            }
            finally
            {
                RemoveAliasFromStore(alias1);
                RemoveAliasFromStore(alias2);
            }
        }

        [TestMethod]
        public void RemoveBadCert_Removes_From_Memory_And_Calls_Persistent_DeleteAll()
        {
            var memory = new InMemoryCertificateCache();
            var persisted = Substitute.For<IPersistentCertificateCache>();
            var logger = Substitute.For<ILoggerAdapter>();

            var cache = new MtlsBindingCache(memory, persisted);

            const string alias = "alias-remove-bad-cert";
            const string ep = "https://fake_mtls/ep";
            const string cid = "11111111-1111-1111-1111-111111111111";

            using var cert = CreateSelfSignedCert(TimeSpan.FromDays(2));
            memory.Set(alias, new CertificateCacheValue(cert, ep, cid));

            // Sanity: should be present
            Assert.IsTrue(memory.TryGet(alias, out var before));
            before.Certificate.Dispose();

            // Act
            cache.RemoveBadCert(alias, logger);

            // Assert: memory entry gone
            Assert.IsFalse(memory.TryGet(alias, out _), "Expected memory cache eviction.");

            // Assert: persistent delete-all invoked
            persisted.Received(1).DeleteAllForAlias(alias, logger);
        }

        [TestMethod]
        public void RemoveBadCert_Is_BestEffort_DoesNotThrow_When_Persistent_Throws()
        {
            var memory = new InMemoryCertificateCache();
            var persisted = Substitute.For<IPersistentCertificateCache>();
            var logger = Substitute.For<ILoggerAdapter>();

            persisted
                .When(p => p.DeleteAllForAlias(Arg.Any<string>(), Arg.Any<ILoggerAdapter>()))
                .Do(_ => throw new InvalidOperationException("boom"));

            var cache = new MtlsBindingCache(memory, persisted);

            // Should not throw
            cache.RemoveBadCert("alias", logger);
        }

        [TestMethod]
        public void IsSchanelFailure_ReturnsTrue_For_SocketException_10054_Chain()
        {
            // Build exception chain like your logs
            var sock = new SocketException(10054);
            var io = new IOException("Unable to write data to the transport connection: An existing connection was forcibly closed by the remote host.", sock);
            var http = new HttpRequestException("An error occurred while sending the request.", io);

            // ErrorCode must be managed_identity_unreachable_network for the catch filter,
            // but the private method only checks ToString() content.
            var msal = new MsalServiceException(MsalError.ManagedIdentityUnreachableNetwork, "An error occurred while sending the request.", http);

            // Invoke private static bool IsSchanelFailure(MsalServiceException ex)
            var mi = typeof(ImdsV2ManagedIdentitySource)
                .GetMethod("IsSchanelFailure", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(mi, "Could not find IsSchanelFailure via reflection.");

            var result = (bool)mi.Invoke(null, new object[] { msal });

            Assert.IsTrue(result, "Expected 10054 chain to be detected as SCHANNEL failure.");
        }

        private static X509Certificate2 CreateSelfSignedCert(TimeSpan lifetime, string subjectCn = "CN=RemoveBadCertTest")
        {
            using var rsa = RSA.Create(2048);
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                new X500DistinguishedName(subjectCn),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
            var notAfter = notBefore.Add(lifetime);
            return req.CreateSelfSigned(notBefore, notAfter);
        }

        private static int CountAliasInStore(string alias)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

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

            int count = 0;
            foreach (var cert in items)
            {
                try
                {
                    if (MsiCertificateFriendlyNameEncoder.TryDecode(cert.FriendlyName, out var decodedAlias, out _)
                        && StringComparer.Ordinal.Equals(decodedAlias, alias))
                    {
                        count++;
                    }
                }
                finally
                {
                    cert.Dispose();
                }
            }

            return count;
        }

        private static bool WaitForAliasCount(string alias, int expected, int retries = 20, int delayMs = 50)
        {
            for (int i = 0; i < retries; i++)
            {
                if (CountAliasInStore(alias) == expected)
                {
                    return true;
                }

                Thread.Sleep(delayMs);
            }

            return false;
        }

        #endregion
    }
}
