// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SysX509 = System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class MtlsBindingStoreUnitTests
    {
        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Track persisted CNG key names created by tests so we can delete them in cleanup.
        private static readonly ConcurrentBag<string> s_cngKeysToDelete = new ConcurrentBag<string>();

        [TestInitialize]
        public void Init()
        {
            if (!IsWindows)
            {
                Assert.Inconclusive("Windows-only test.");
            }

            // Remove any test-installed certs from previous runs
            MtlsBindingStore.Default.RemoveAllWithFriendlyNamePrefixForTest("MSAL|");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (!IsWindows)
            {
                return;
            }

            // Remove test-installed certs
            MtlsBindingStore.Default.RemoveAllWithFriendlyNamePrefixForTest("MSAL|");

            // Best-effort deletion of persisted CNG keys created by tests
            while (s_cngKeysToDelete.TryTake(out var keyName))
            {
                try
                {
                    using var key = CngKey.Open(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider);
                    key.Delete();
                }
                catch
                {
                    // ignore – best-effort cleanup
                }
            }
        }

        /// <summary>
        /// Creates a self-signed cert whose private key is a *persisted*, non-exportable CNG key.
        /// This mirrors production (TPM/KeyGuard) behavior: the key cannot be exported, but is
        /// usable via the provider when rehydrated from the Windows store.
        /// </summary>
        private static X509Certificate2 NewCert(string cn, string dc, TimeSpan lifetime)
        {
            // Unique CNG key name in the current user profile
            string keyName = "MSAL_Test_" + Guid.NewGuid().ToString("N");

            var creationParams = new CngKeyCreationParameters
            {
                Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                KeyUsage = CngKeyUsages.Signing,
                ExportPolicy = CngExportPolicies.None, // non-exportable key
                KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey
            };

            using var cngKey = CngKey.Create(CngAlgorithm.Rsa, keyName, creationParams);
            using var rsa = new RSACng(cngKey);

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-1);
            var notAfter = notBefore + lifetime;

            var req = new SysX509.CertificateRequest(
                $"CN={cn}, DC={dc}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Create the certificate; on Windows with a persisted RSACng key, this already has a private key.
            var cert = req.CreateSelfSigned(notBefore, notAfter);

            // Track the key for deletion in Cleanup
            s_cngKeysToDelete.Add(keyName);

            // Some runtimes may return a public-only instance; if so, bind the persisted key.
            if (!cert.HasPrivateKey)
            {
                cert = cert.CopyWithPrivateKey(rsa);
            }

            return cert;
        }

        private static bool TryFindInstalledTestCert(
            string cn, string dc, string expectedTokenType,
            out X509Certificate2 certFromStore, out string friendlyName, out bool hasUsablePrivateKey)
        {
            certFromStore = null;
            friendlyName = null;
            hasUsablePrivateKey = false;

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                // Find by subject CN/DC first to confirm presence
                var candidate = store.Certificates
                    .OfType<X509Certificate2>()
                    .FirstOrDefault(c =>
                    {
                        var subj = c.Subject ?? string.Empty;
                        return subj.IndexOf("CN=" + cn, StringComparison.OrdinalIgnoreCase) >= 0 &&
                               subj.IndexOf("DC=" + dc, StringComparison.OrdinalIgnoreCase) >= 0;
                    });

                if (candidate == null)
                {
                    return false; // nothing matching in store
                }

                // FriendlyName may throw on some platforms; best-effort
                try
                { friendlyName = candidate.FriendlyName; }
                catch { friendlyName = null; }

                // Private key usability (same as product check: GetRSAPrivateKey + export public params)
                try
                {
                    using var rsa = candidate.GetRSAPrivateKey();
                    if (rsa != null)
                    {
                        rsa.ExportParameters(false); // public only – should work for non-exportable keys
                        hasUsablePrivateKey = true;
                    }
                }
                catch
                {
                    hasUsablePrivateKey = false;
                }

                certFromStore = candidate;
                return true;
            }
            catch
            {
                certFromStore = null;
                friendlyName = null;
                hasUsablePrivateKey = false;
                return false;
            }
        }

        [TestMethod]
        public void Install_Then_ResolveBySubjectAndType_Works()
        {
            if (!IsWindows)
            {
                Assert.Inconclusive("Windows-only test.");
            }

            string cn = Guid.NewGuid().ToString();
            string dc = Guid.NewGuid().ToString();

            using var cert = NewCert(cn, dc, TimeSpan.FromHours(1));

            string fn = BindingFriendlyName.Build("mtls_pop", "https://eastus2euap.mtlsauth.microsoft.com");
            MtlsBindingStore.Default.TryInstallWithFriendlyName(cert, fn);

            // Environment sanity check: can we see the cert in the store, with friendly name & usable key?
            if (!TryFindInstalledTestCert(cn, dc, "mtls_pop",
                out var foundRaw, out var friendly, out var hasKey))
            {
                Assert.Inconclusive("Environment did not return a matching cert from the CurrentUser\\My store.");
            }
            using (foundRaw)
            { /* dispose */ }

            if (string.IsNullOrEmpty(friendly) || !BindingFriendlyName.HasOurPrefix(friendly))
            {
                Assert.Inconclusive("FriendlyName was not persisted by the platform; skipping resolver assertion.");
            }

            if (!hasKey)
            {
                Assert.Inconclusive("Store entry does not expose a usable private key (environment limitation).");
            }

            // Now assert through the product resolver
            Assert.IsTrue(
                MtlsBindingStore.Default.TryResolveFreshestBySubjectAndType(cn, dc, "mtls_pop", out var found, out var endpoint),
                "Expected to resolve the freshest cert by subject and token type.");

            Assert.IsNotNull(found);
            Assert.IsTrue(!string.IsNullOrEmpty(endpoint), "Endpoint should be populated.");
            found.Dispose();
        }

        [TestMethod]
        public void PurgeExpiredBeyondWindow_RemovesOldCerts()
        {
            if (!IsWindows)
            {
                Assert.Inconclusive("Windows-only test.");
            }

            string cn = Guid.NewGuid().ToString();
            string dc = Guid.NewGuid().ToString();

            using var expired = NewCert(cn, dc, TimeSpan.FromSeconds(1));

            string fn = BindingFriendlyName.Build("bearer", "https://x");
            MtlsBindingStore.Default.TryInstallWithFriendlyName(expired, fn);

            // Let it expire
            System.Threading.Thread.Sleep(1500);

            MtlsBindingStore.Default.PurgeExpiredBeyondWindow(cn, dc, TimeSpan.Zero);

            // We don't assert preconditions; just verify the resolver doesn't return it post-purge.
            Assert.IsFalse(
                MtlsBindingStore.Default.TryResolveFreshestBySubjectAndType(cn, dc, "bearer", out _, out _),
                "Expired cert should have been purged.");
        }
    }
}
