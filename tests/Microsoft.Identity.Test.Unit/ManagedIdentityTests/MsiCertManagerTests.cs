// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.ManagedIdentity.V2.Cache;
using Microsoft.Identity.Client.ManagedIdentity.V2.Cache.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class MsiCertManagerTests
    {
        private sealed class FakeRepo : ICertificateRepository
        {
            public int InstallCount;
            public X509Certificate2 RehydrateCert;
            public string RehydrateEndpoint;
            public bool TryResolveFreshestBySubjectAndType(string cn, string dc, string tokenType,
                out X509Certificate2 cert, out string endpoint)
            {
                cert = RehydrateCert;
                endpoint = RehydrateEndpoint;
                return cert != null && !string.IsNullOrEmpty(endpoint);
            }

            public void TryInstallWithFriendlyName(X509Certificate2 cert, string friendlyName)
            {
                InstallCount++;

                // Simulate the store now containing this cert and being discoverable by rehydrate.
                RehydrateCert = cert;

                // FriendlyName format: "MSAL|1|token_type|b64url(endpoint)"
                try
                {
                    var parts = (friendlyName ?? "").Split('|');
                    if (parts.Length >= 4)
                    {
                        RehydrateEndpoint = Base64UrlDecode(parts[3]);
                    }
                }
                catch
                {
                    // test fake: ignore
                }
            }

            private static string Base64UrlDecode(string s)
            {
                if (string.IsNullOrEmpty(s))
                    return "";
                string b64 = s.Replace('-', '+').Replace('_', '/');
                switch (b64.Length % 4)
                {
                    case 2:
                        b64 += "==";
                        break;
                    case 3:
                        b64 += "=";
                        break;
                }
                return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
            }

            public void PurgeExpiredBeyondWindow(string cn, string dc, TimeSpan g) { }
            public void RemoveAllWithFriendlyNamePrefixForTest(string p) { }
        }

        private static (string raw, RSA rsa, X509Certificate2 withKey) MakeSelfSigned(string cn, string dc, DateTimeOffset nb, TimeSpan lifetime)
        {
            var rsa = RSA.Create(2048);
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                $"CN={cn}, DC={dc}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            var cert = req.CreateSelfSigned(nb.UtcDateTime, (nb + lifetime).UtcDateTime);
            var raw = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
            return (raw, rsa, cert);
        }

        [TestInitialize]
        public void Init()
        {
            InMemoryMtlsCertCache.Shared.ClearForTest();
            ImdsV2BindingCache.Shared.ClearForTest();
            MsiCertManager.ResetStoreEnsureFlagsForTest();
        }

        [TestMethod]
        public async Task MemoryHit_ReturnsCached_AndInstallsStoreOnce()
        {
            var repo = new FakeRepo();
            var mgr = new MsiCertManager(InMemoryMtlsCertCache.Shared, ImdsV2BindingCache.Shared, repo);

            string tenant = Guid.NewGuid().ToString();
            string mi = Guid.NewGuid().ToString();
            string token = "bearer";
            string idKey = "app-1";

            var (raw, rsa, withKey) = MakeSelfSigned(mi, tenant, DateTimeOffset.UtcNow.AddMinutes(-1), TimeSpan.FromHours(1));
            var resp = new CertificateRequestResponse
            {
                ClientId = mi,
                TenantId = tenant,
                MtlsAuthenticationEndpoint = "https://e2e/token",
                Certificate = raw
            };

            // First call -> mint
            int mintCalls = 0;
            var (cert, r) = await mgr.GetOrMintBindingAsync(
                idKey, tenant, mi, token,
                async ct => { mintCalls++; await Task.Yield(); return (resp, withKey.GetRSAPrivateKey()); },
                CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(1, mintCalls);
            Assert.AreEqual("https://e2e/token", r.MtlsAuthenticationEndpoint);

            // Second call -> memory hit, and we will attempt 1-time store ensure
            var (cert2, r2) = await mgr.GetOrMintBindingAsync(
                idKey, tenant, mi, token,
                async ct => { mintCalls++; await Task.Yield(); return (resp, withKey.GetRSAPrivateKey()); },
                CancellationToken.None).ConfigureAwait(false);

            Assert.AreSame(cert, cert2);
            Assert.AreEqual(1, mintCalls, "no re-mint on memory hit");
            Assert.AreEqual(1, repo.InstallCount, "store install called once");
        }

        [TestMethod]
        public async Task StoreRehydrate_SkipsMint_AndCaches()
        {
            var repo = new FakeRepo();
            var mgr = new MsiCertManager(InMemoryMtlsCertCache.Shared, ImdsV2BindingCache.Shared, repo);

            string tenant = Guid.NewGuid().ToString();
            string mi = Guid.NewGuid().ToString();
            string token = "mtls_pop";
            string idKey = "app-2";

            // Pretend store has a valid entry
            var (raw, rsa, withKey) = MakeSelfSigned(mi, tenant, DateTimeOffset.UtcNow.AddMinutes(-1), TimeSpan.FromHours(1));
            repo.RehydrateCert = withKey;
            repo.RehydrateEndpoint = "https://rehydrate/token";

            int mintCalls = 0;
            var (cert, resp) = await mgr.GetOrMintBindingAsync(
                idKey, tenant, mi, token,
                async ct => { mintCalls++; Assert.Fail("mint should not be called"); await Task.Yield(); return (null, (RSA)null); },
                CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual("https://rehydrate/token", resp.MtlsAuthenticationEndpoint);
            Assert.AreEqual(0, mintCalls);
        }

        [TestMethod]
        public async Task ConcurrentMint_Collapses_ToSingleCall()
        {
            var repo = new FakeRepo();
            var mgr = new MsiCertManager(InMemoryMtlsCertCache.Shared, ImdsV2BindingCache.Shared, repo);

            string tenant = Guid.NewGuid().ToString();
            string mi = Guid.NewGuid().ToString();
            string token = "bearer";
            string idKey = "app-3";

            var (raw, rsa, withKey) = MakeSelfSigned(mi, tenant, DateTimeOffset.UtcNow.AddMinutes(-1), TimeSpan.FromHours(1));
            var resp = new CertificateRequestResponse
            {
                ClientId = mi,
                TenantId = tenant,
                MtlsAuthenticationEndpoint = "https://e2e/token",
                Certificate = raw
            };

            int mintCalls = 0;
            var tasks = Enumerable.Range(0, 8).Select(_ =>
                mgr.GetOrMintBindingAsync(idKey, tenant, mi, token, async ct => { Interlocked.Increment(ref mintCalls); await Task.Yield(); return (resp, rsa); }, CancellationToken.None));

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            Assert.AreEqual(1, mintCalls, "exactly one mint under contention");
            Assert.IsTrue(results.Select(r => r.cert.Thumbprint).Distinct().Count() == 1);
        }
    }
}
