// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Tests for <see cref="WindowsCngKeyOperations.PurgeManagedIdentityCertificates"/>.
    /// The purge sweeps <c>CurrentUser\My</c> and removes every certificate whose issuer
    /// contains <see cref="WindowsCngKeyOperations.ManagedIdentityIssuerCnFragment"/>.
    /// It runs after a fresh KeyGuard key is minted so that persisted IMDSv2 binding
    /// certs (which are bound by container name to the now-replaced key) are not left
    /// behind to fail the next mTLS handshake.
    /// </summary>
    [TestClass]
    public class WindowsCngKeyOperationsPurgeUnitTests
    {
        // Discriminator we plant in each test cert's Subject so cleanup can find leftovers
        // from a failed run without touching unrelated certs in the developer's store.
        private const string TestSubjectDiscriminatorPrefix = "MSAL-Purge-Test-";

        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private static ILoggerAdapter Logger => Substitute.For<ILoggerAdapter>();

        [TestInitialize]
        public void Init()
        {
            // Reuse the existing broad sweep so prior test runs don't leak state.
            if (ImdsV2TestStoreCleaner.IsWindows)
            {
                ImdsV2TestStoreCleaner.RemoveAllTestArtifacts();
            }

            // Also remove any leftover purge-test certs from a previous failed run.
            RemoveAllPurgeTestArtifacts();
        }

        [TestCleanup]
        public void Cleanup()
        {
            RemoveAllPurgeTestArtifacts();
        }

        private static void WindowsOnly()
        {
            if (!IsWindows)
            {
                Assert.Inconclusive("Windows-only");
            }
        }

        [TestMethod]
        public void PurgeManagedIdentityCertificates_RemovesCertWithMatchingIssuer()
        {
            WindowsOnly();

            // Arrange
            string discriminator = TestSubjectDiscriminatorPrefix + Guid.NewGuid().ToString("N");
            string subject =
                "CN=" + WindowsCngKeyOperations.ManagedIdentityIssuerCnFragment +
                ", OU=" + discriminator;

            string plantedThumbprint;
            using (var cert = CreateSelfSignedWithKey(subject, TimeSpan.FromDays(2)))
            {
                plantedThumbprint = cert.Thumbprint;
                AddToCurrentUserMyStore(cert);
            }

            Assert.IsTrue(
                IsInCurrentUserMyStore(plantedThumbprint),
                "Test setup precondition: planted cert must be present in CurrentUser\\My before purge.");

            // Act
            WindowsCngKeyOperations.PurgeManagedIdentityCertificates(Logger);

            // Assert
            Assert.IsFalse(
                IsInCurrentUserMyStore(plantedThumbprint),
                "Purge should remove certs whose Issuer contains the managed identity issuer CN.");
        }

        [TestMethod]
        public void PurgeManagedIdentityCertificates_LeavesCertWithNonMatchingIssuer()
        {
            WindowsOnly();

            // Arrange
            string discriminator = TestSubjectDiscriminatorPrefix + Guid.NewGuid().ToString("N");
            // Subject/Issuer that does NOT contain the managed identity issuer fragment.
            string subject = "CN=unrelated.example.test, OU=" + discriminator;

            string plantedThumbprint;
            using (var cert = CreateSelfSignedWithKey(subject, TimeSpan.FromDays(2)))
            {
                plantedThumbprint = cert.Thumbprint;
                AddToCurrentUserMyStore(cert);
            }

            Assert.IsTrue(
                IsInCurrentUserMyStore(plantedThumbprint),
                "Test setup precondition: planted cert must be present in CurrentUser\\My before purge.");

            try
            {
                // Act
                WindowsCngKeyOperations.PurgeManagedIdentityCertificates(Logger);

                // Assert
                Assert.IsTrue(
                    IsInCurrentUserMyStore(plantedThumbprint),
                    "Purge must not remove certs whose Issuer does not contain the managed identity issuer CN.");
            }
            finally
            {
                RemoveByThumbprintFromCurrentUserMyStore(plantedThumbprint);
            }
        }

        [TestMethod]
        public void PurgeManagedIdentityCertificates_MatchIsCaseInsensitive()
        {
            WindowsOnly();

            // Arrange
            string discriminator = TestSubjectDiscriminatorPrefix + Guid.NewGuid().ToString("N");
            // Uppercase the issuer fragment to ensure the match is OrdinalIgnoreCase.
            string subject =
                "CN=" + WindowsCngKeyOperations.ManagedIdentityIssuerCnFragment.ToUpperInvariant() +
                ", OU=" + discriminator;

            string plantedThumbprint;
            using (var cert = CreateSelfSignedWithKey(subject, TimeSpan.FromDays(2)))
            {
                plantedThumbprint = cert.Thumbprint;
                AddToCurrentUserMyStore(cert);
            }

            Assert.IsTrue(
                IsInCurrentUserMyStore(plantedThumbprint),
                "Test setup precondition: planted cert must be present in CurrentUser\\My before purge.");

            // Act
            WindowsCngKeyOperations.PurgeManagedIdentityCertificates(Logger);

            // Assert
            Assert.IsFalse(
                IsInCurrentUserMyStore(plantedThumbprint),
                "Purge issuer match should be case-insensitive.");
        }

        [TestMethod]
        public void PurgeManagedIdentityCertificates_OnlyRemovesMatching_LeavesOtherCertsAlone()
        {
            WindowsOnly();

            // Arrange: plant one matching and one non-matching cert
            string matchDiscriminator = TestSubjectDiscriminatorPrefix + Guid.NewGuid().ToString("N");
            string nonMatchDiscriminator = TestSubjectDiscriminatorPrefix + Guid.NewGuid().ToString("N");

            string matchingSubject =
                "CN=" + WindowsCngKeyOperations.ManagedIdentityIssuerCnFragment +
                ", OU=" + matchDiscriminator;
            string nonMatchingSubject = "CN=unrelated.example.test, OU=" + nonMatchDiscriminator;

            string matchingThumb;
            string nonMatchingThumb;

            using (var matching = CreateSelfSignedWithKey(matchingSubject, TimeSpan.FromDays(2)))
            using (var nonMatching = CreateSelfSignedWithKey(nonMatchingSubject, TimeSpan.FromDays(2)))
            {
                matchingThumb = matching.Thumbprint;
                nonMatchingThumb = nonMatching.Thumbprint;

                AddToCurrentUserMyStore(matching);
                AddToCurrentUserMyStore(nonMatching);
            }

            Assert.IsTrue(IsInCurrentUserMyStore(matchingThumb), "Matching cert must be planted.");
            Assert.IsTrue(IsInCurrentUserMyStore(nonMatchingThumb), "Non-matching cert must be planted.");

            try
            {
                // Act
                WindowsCngKeyOperations.PurgeManagedIdentityCertificates(Logger);

                // Assert
                Assert.IsFalse(IsInCurrentUserMyStore(matchingThumb), "Matching cert should be purged.");
                Assert.IsTrue(IsInCurrentUserMyStore(nonMatchingThumb), "Non-matching cert should survive.");
            }
            finally
            {
                RemoveByThumbprintFromCurrentUserMyStore(nonMatchingThumb);
            }
        }

        // ---------------- helpers ----------------

        /// <summary>
        /// Creates a self-signed RSA cert with a persistable private key.
        /// Mirrors the pattern used by <c>PersistentCertificateStoreUnitTests.CreateSelfSignedWithKey</c>.
        /// </summary>
        private static X509Certificate2 CreateSelfSignedWithKey(string subject, TimeSpan lifetime)
        {
            using var rsa = RSA.Create(2048);

            var req = new CertificateRequest(
                new X500DistinguishedName(subject),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
            DateTimeOffset notAfter = notBefore.Add(lifetime);

            using var ephemeral = req.CreateSelfSigned(notBefore, notAfter);

            // Re-import as PFX so the private key is persisted and the store will accept it.
            var pfx = ephemeral.Export(X509ContentType.Pfx, "");
            return new X509Certificate2(
                pfx,
                "",
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        private static void AddToCurrentUserMyStore(X509Certificate2 cert)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
        }

        private static bool IsInCurrentUserMyStore(string thumbprint)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

            foreach (X509Certificate2 c in store.Certificates)
            {
                try
                {
                    if (string.Equals(c.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                finally
                {
                    c.Dispose();
                }
            }

            return false;
        }

        private static void RemoveByThumbprintFromCurrentUserMyStore(string thumbprint)
        {
            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                X509Certificate2[] snapshot;
                try
                {
                    snapshot = new X509Certificate2[store.Certificates.Count];
                    store.Certificates.CopyTo(snapshot, 0);
                }
                catch
                {
                    snapshot = store.Certificates.Cast<X509Certificate2>().ToArray();
                }

                foreach (var c in snapshot)
                {
                    try
                    {
                        if (string.Equals(c.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
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
            catch
            {
                // best-effort cleanup
            }
        }

        /// <summary>
        /// Removes any leftover purge-test certificates from <c>CurrentUser\My</c>.
        /// Matches our unique Subject OU discriminator to avoid touching unrelated certs.
        /// Best-effort, no-throw.
        /// </summary>
        private static void RemoveAllPurgeTestArtifacts()
        {
            if (!IsWindows)
            {
                return;
            }

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                X509Certificate2[] snapshot;
                try
                {
                    snapshot = new X509Certificate2[store.Certificates.Count];
                    store.Certificates.CopyTo(snapshot, 0);
                }
                catch
                {
                    snapshot = store.Certificates.Cast<X509Certificate2>().ToArray();
                }

                foreach (var c in snapshot)
                {
                    try
                    {
                        string subject = c.Subject ?? string.Empty;
                        if (subject.IndexOf(TestSubjectDiscriminatorPrefix, StringComparison.Ordinal) >= 0)
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
            catch
            {
                // best-effort
            }
        }
    }
}
