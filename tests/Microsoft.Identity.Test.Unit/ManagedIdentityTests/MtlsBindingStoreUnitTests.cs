// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class MtlsBindingStoreUnitTests
    {
        private const string Prefix = "CN=MSALTest-Store-";

        [TestInitialize]
        public void Init()
        {
            // Ensure per-process map is empty for each test
            Microsoft.Identity.Client.ManagedIdentity.ManagedIdentityClient.ResetSourceAndBindingForTest();

            // Also clear any leftover test certs with our prefix
            CertHelper.RemoveBySubjectPrefix(Prefix);
        }

        [TestCleanup]
        public void Cleanup()
        {
            CertHelper.RemoveBySubjectPrefix(Prefix);
        }

        // Tests that when multiple certificates with same subject are installed,
        // GetFreshestBySubject returns the newest (valid) certificate
        [TestMethod]
        public void InstallAndGetSubject_Then_GetFreshestBySubject_ReturnsLatest()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var older = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-30), DateTimeOffset.UtcNow.AddMinutes(30));
            var newer = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddHours(1));

            var s1 = MtlsBindingStore.InstallAndGetSubject(older);
            Assert.AreEqual(subject, s1);

            var s2 = MtlsBindingStore.InstallAndGetSubject(newer);
            Assert.AreEqual(subject, s2);

            var freshest = MtlsBindingStore.GetFreshestBySubject(subject);
            Assert.IsNotNull(freshest);
            Assert.AreEqual(newer.Thumbprint, freshest.Thumbprint);
        }

        // Verifies that the PruneOlder method correctly removes all certificates
        // except the one with the specified thumbprint
        [TestMethod]
        public void PruneOlder_KeepsOnlySpecifiedThumbprint()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var c1 = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-40), DateTimeOffset.UtcNow.AddMinutes(20));
            var c2 = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-20), DateTimeOffset.UtcNow.AddMinutes(40));
            var c3 = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddMinutes(60));

            MtlsBindingStore.InstallAndGetSubject(c1);
            MtlsBindingStore.InstallAndGetSubject(c2);
            MtlsBindingStore.InstallAndGetSubject(c3);

            MtlsBindingStore.PruneOlder(subject, c2.Thumbprint);

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var matches = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subject, false);
            Assert.AreEqual(1, matches.Count, "Prune should leave exactly one certificate.");
            Assert.AreEqual(c2.Thumbprint, matches[0].Thumbprint);
        }

        // Tests the certificate half-life calculation logic - certificates before and after
        // their validity period's half-life point are correctly identified
        [TestMethod]
        public void IsBeyondHalfLife_BeforeAndAfter()
        {
            var subjBefore = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var beforeHalf = CertHelper.CreateSelfSigned(subjBefore,
                DateTimeOffset.UtcNow.AddMinutes(-5),   // started 5 mins ago
                DateTimeOffset.UtcNow.AddHours(1));     // ends in 60 mins → half-life 27.5 mins ahead

            var subjAfter = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var afterHalf = CertHelper.CreateSelfSigned(subjAfter,
                DateTimeOffset.UtcNow.AddHours(-2),     // started 2h ago
                DateTimeOffset.UtcNow.AddMinutes(5));   // ends in 5 mins → half-life long past

            Assert.IsFalse(MtlsBindingStore.IsBeyondHalfLife(beforeHalf));
            Assert.IsTrue(MtlsBindingStore.IsBeyondHalfLife(afterHalf));
        }

        // When resolving by thumbprint AND subject, an exact thumbprint match wins
        // even if there are fresher certificates with the same subject
        [TestMethod]
        public void ResolveByThumbprintThenSubject_ExactMatchWins()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var exact = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            var fresher = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(2));

            MtlsBindingStore.InstallAndGetSubject(exact);
            MtlsBindingStore.InstallAndGetSubject(fresher);

            var resolved = MtlsBindingStore.ResolveByThumbprintThenSubject(exact.Thumbprint, subject, cleanupOlder: true, out var tp);
            Assert.IsNotNull(resolved);
            Assert.AreEqual(exact.Thumbprint, resolved.Thumbprint);
            Assert.AreEqual(exact.Thumbprint, tp);
        }

        // When a specified thumbprint cannot be found, the resolver falls back
        // to the freshest certificate with the specified subject
        [TestMethod]
        public void ResolveByThumbprintThenSubject_FallsBackToFreshestWhenThumbprintMissing()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var older = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(10));
            var newest = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));

            MtlsBindingStore.InstallAndGetSubject(older);
            MtlsBindingStore.InstallAndGetSubject(newest);

            // bogus thumbprint => choose freshest by subject
            var resolved = MtlsBindingStore.ResolveByThumbprintThenSubject("00DEADBEEF", subject, cleanupOlder: true, out var tp);
            Assert.IsNotNull(resolved);
            Assert.AreEqual(newest.Thumbprint, resolved.Thumbprint);
            Assert.AreEqual(newest.Thumbprint, tp);
        }

        // Verifies that resolver returns null when there are only expired certificates
        [TestMethod]
        public void ResolveByThumbprintThenSubject_ReturnsNullWhenNoValid()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var expired = CertHelper.CreateSelfSigned(subject,
                DateTimeOffset.UtcNow.AddHours(-4),
                DateTimeOffset.UtcNow.AddHours(-1)); // already expired

            MtlsBindingStore.InstallAndGetSubject(expired);

            var resolved = MtlsBindingStore.ResolveByThumbprintThenSubject(expired.Thumbprint, subject, cleanupOlder: true, out var _);
            Assert.IsNull(resolved, "No valid cert should resolve when only expired exists.");
        }

        // Certificates expired for more than 7 days should be automatically purged
        // during resolution operations
        [TestMethod]
        public void Resolve_Purges_ExpiredBeyond7Days()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            // expired 8 days ago
            var nb = DateTimeOffset.UtcNow.AddDays(-9);
            var na = DateTimeOffset.UtcNow.AddDays(-8);
            var stale = CertHelper.CreateSelfSigned(subject, nb, na);

            MtlsBindingStore.InstallAndGetSubject(stale);

            // Call resolve: expected to purge stale (>7 days) and return null
            var resolved = MtlsBindingStore.ResolveByThumbprintThenSubject(stale.Thumbprint, subject, cleanupOlder: true, out var _);
            // If your implementation purges inside Resolve..., this will be null and the cert should be deleted:
            Assert.IsNull(resolved);
            Assert.IsFalse(CertHelper.ExistsByThumbprint(stale.Thumbprint),
                "Stale certificate (>7 days expired) should be purged by ResolveByThumbprintThenSubject.");
        }

        // Tests that the cache properly separates Bearer and PoP tokens by identity,
        // allowing different certificate bindings for the same identity based on token type
        [TestMethod]
        public void Cache_Separates_Bearer_And_PoP_ByIdentity()
        {
            string identity = "id-" + Guid.NewGuid().ToString("N");
            string subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";

            // Two different certs (fresh) under the same subject
            var bearerCert = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            var popCert = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(2));

            // Install to store to keep the mapping realistic
            MtlsBindingStore.InstallAndGetSubject(bearerCert);
            MtlsBindingStore.InstallAndGetSubject(popCert);

            var bearerResp = CertHelper.MakeResp(bearerCert);
            var popResp = CertHelper.MakeResp(popCert);

            // Cache both mappings for the same identity, under different token types
            ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(identity, bearerResp, subject, bearerCert.Thumbprint, Constants.BearerTokenType);
            ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(identity, popResp, subject, popCert.Thumbprint, Constants.MtlsPoPTokenType);

            // Verify Bearer lookup returns the Bearer thumbprint
            Assert.IsTrue(ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(identity, Constants.BearerTokenType, out var outRespB, out var outSubjB, out var outTpB));
            Assert.AreEqual(bearerResp, outRespB);
            Assert.AreEqual(subject, outSubjB);
            Assert.AreEqual(bearerCert.Thumbprint, outTpB, ignoreCase: true);

            // Verify PoP lookup returns the PoP thumbprint
            Assert.IsTrue(ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(identity, Constants.MtlsPoPTokenType, out var outRespP, out var outSubjP, out var outTpP));
            Assert.AreEqual(popResp, outRespP);
            Assert.AreEqual(subject, outSubjP);
            Assert.AreEqual(popCert.Thumbprint, outTpP, ignoreCase: true);
        }

        // Verifies that PoP tokens can be retrieved from any identity with 
        // TryGetAnyImdsV2BindingMetadata but Bearer tokens cannot (by design)
        [TestMethod]
        public void TryGetAny_PoP_Returns_From_AnyIdentity_But_Bearer_DoesNot()
        {
            string identity1 = "id-" + Guid.NewGuid().ToString("N");
            string identity2 = "id-" + Guid.NewGuid().ToString("N");
            string subject1 = $"{Prefix}{Guid.NewGuid()}, DC=unit";

            var popCert1 = CertHelper.CreateSelfSigned(subject1, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(2));
            MtlsBindingStore.InstallAndGetSubject(popCert1);

            var popResp1 = CertHelper.MakeResp(popCert1);

            // Put a PoP mapping under identity1 only
            ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(identity1, popResp1, subject1, popCert1.Thumbprint, Constants.MtlsPoPTokenType);

            // Query "any" for PoP -> should succeed
            Assert.IsTrue(ImdsV2ManagedIdentitySource.TryGetAnyImdsV2BindingMetadata(Constants.MtlsPoPTokenType, out var anyResp, out var anySubject, out var anyTp));
            Assert.AreEqual(popResp1, anyResp);
            Assert.AreEqual(subject1, anySubject);
            Assert.AreEqual(popCert1.Thumbprint, anyTp, ignoreCase: true);

            // Query "any" for Bearer -> should fail (by design)
            Assert.IsFalse(ImdsV2ManagedIdentitySource.TryGetAnyImdsV2BindingMetadata(Constants.BearerTokenType, out _, out _, out _));

            // identity2 still has no direct PoP mapping
            Assert.IsFalse(ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(identity2, Constants.MtlsPoPTokenType, out _, out _, out _));
        }

        // Tests that during certificate rotation, the subject is set only once (first-wins)
        // while the thumbprint can be updated
        [TestMethod]
        public void Cache_Subject_Is_SetOnce_And_Thumbprint_Rotates()
        {
            string identity = "id-" + Guid.NewGuid().ToString("N");
            string subject1 = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            string subject2 = $"{Prefix}{Guid.NewGuid()}, DC=unit";

            var certV1 = CertHelper.CreateSelfSigned(subject1, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(15));
            var certV2 = CertHelper.CreateSelfSigned(subject2, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));

            MtlsBindingStore.InstallAndGetSubject(certV1);
            MtlsBindingStore.InstallAndGetSubject(certV2);

            var respV1 = CertHelper.MakeResp(certV1);
            var respV2 = CertHelper.MakeResp(certV2);

            // First cache write sets subject
            ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(identity, respV1, subject1, certV1.Thumbprint, Constants.MtlsPoPTokenType);

            // Second cache write (rotation) keeps subject1 (first-wins) but updates thumbprint
            ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(identity, respV2, subject2, certV2.Thumbprint, Constants.MtlsPoPTokenType);

            Assert.IsTrue(ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(identity, Constants.MtlsPoPTokenType, out var outResp, out var outSubject, out var outTp));
            Assert.AreEqual(respV2, outResp, "Response should reflect latest rotation payload.");
            Assert.AreEqual(subject1, outSubject, "Subject is set once (first write wins).");
            Assert.AreEqual(certV2.Thumbprint, outTp, ignoreCase: true, "Thumbprint should be latest after rotation.");
        }

        // Verifies that lookups fail appropriately for unknown identities
        // or incorrect token types
        [TestMethod]
        public void TryGet_ReturnsFalse_For_UnknownIdentity_Or_TokenType()
        {
            string identity = "id-" + Guid.NewGuid().ToString("N");
            string subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";

            var cert = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            MtlsBindingStore.InstallAndGetSubject(cert);

            var resp = CertHelper.MakeResp(cert);

            // Only cache a PoP mapping
            ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(identity, resp, subject, cert.Thumbprint, Constants.MtlsPoPTokenType);

            // Unknown identity
            Assert.IsFalse(ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata("id-missing", Constants.MtlsPoPTokenType, out _, out _, out _));

            // Wrong token type for the same identity
            Assert.IsFalse(ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(identity, Constants.BearerTokenType, out _, out _, out _));
        }

        // Tests that mappings for different identities remain separate and
        // don't interfere with each other
        [TestMethod]
        public void Mappings_Do_Not_Mix_Between_Identities()
        {
            string id1 = "id-" + Guid.NewGuid().ToString("N");
            string id2 = "id-" + Guid.NewGuid().ToString("N");
            string subj1 = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            string subj2 = $"{Prefix}{Guid.NewGuid()}, DC=unit";

            var cert1 = CertHelper.CreateSelfSigned(subj1, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            var cert2 = CertHelper.CreateSelfSigned(subj2, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));

            MtlsBindingStore.InstallAndGetSubject(cert1);
            MtlsBindingStore.InstallAndGetSubject(cert2);

            var resp1 = CertHelper.MakeResp(cert1, tenant: "t1", client: "c1");
            var resp2 = CertHelper.MakeResp(cert2, tenant: "t2", client: "c2");

            // id1 -> Bearer
            ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(id1, resp1, subj1, cert1.Thumbprint, Constants.BearerTokenType);

            // id2 -> PoP
            ImdsV2ManagedIdentitySource.CacheImdsV2BindingMetadata(id2, resp2, subj2, cert2.Thumbprint, Constants.MtlsPoPTokenType);

            // id1 Bearer OK, id1 PoP missing
            Assert.IsTrue(ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(id1, Constants.BearerTokenType, out var r1, out var s1, out var tp1));
            Assert.AreEqual(resp1, r1);
            Assert.AreEqual(subj1, s1);
            Assert.AreEqual(cert1.Thumbprint, tp1, ignoreCase: true);

            Assert.IsFalse(ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(id1, Constants.MtlsPoPTokenType, out _, out _, out _));

            // id2 PoP OK, id2 Bearer missing
            Assert.IsTrue(ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(id2, Constants.MtlsPoPTokenType, out var r2, out var s2, out var tp2));
            Assert.AreEqual(resp2, r2);
            Assert.AreEqual(subj2, s2);
            Assert.AreEqual(cert2.Thumbprint, tp2, ignoreCase: true);

            Assert.IsFalse(ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(id2, Constants.BearerTokenType, out _, out _, out _));
        }

        // A cert with a private key should be usable for signing
        [TestMethod]
        public void IsPrivateKeyUsable_ReturnsTrue_ForCertWithPrivateKey()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var certWithKey = CertHelper.CreateSelfSigned(
                subject,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddHours(1));

            // Install and fetch from store (sanity)
            MtlsBindingStore.InstallAndGetSubject(certWithKey);
            var fetched = MtlsBindingStore.GetFreshestBySubject(subject);
            Assert.IsNotNull(fetched, "Freshest certificate should be present in the store.");
            Assert.IsTrue(fetched.HasPrivateKey, "Sanity check: certificate should have a private key.");

            // Private-key probe should succeed
            Assert.IsTrue(MtlsBindingStore.IsPrivateKeyUsable(fetched),
                "Certificate with a usable private key should return true.");
        }

        // Same cert material but public-only (no private key) should be considered unusable
        [TestMethod]
        public void IsPrivateKeyUsable_ReturnsFalse_ForPublicOnlyCert()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var certWithKey = CertHelper.CreateSelfSigned(
                subject,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddHours(1));

            // Install the with-key version first
            MtlsBindingStore.InstallAndGetSubject(certWithKey);

            // Create a public-only view of the same certificate (no private key)
            var publicOnly = new X509Certificate2(certWithKey.Export(X509ContentType.Cert));

            // Replace in the store: InstallAndGetSubject de-dups by thumbprint, so this
            // will remove the with-key instance and leave the public-only instance.
            MtlsBindingStore.InstallAndGetSubject(publicOnly);

            var fetched = MtlsBindingStore.GetFreshestBySubject(subject);
            Assert.IsNotNull(fetched, "Public-only certificate should be present in the store.");
            Assert.IsFalse(fetched.HasPrivateKey, "Sanity check: fetched certificate should not have a private key.");

            // Private-key probe should fail
            Assert.IsFalse(MtlsBindingStore.IsPrivateKeyUsable(fetched),
                "Public-only certificate should return false for private key usability.");
        }

        // Optional: once detected as unusable, removal by thumbprint should clean up the store
        [TestMethod]
        public void RemoveByThumbprint_AfterUnusableKeyDetection_RemovesCert()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var certWithKey = CertHelper.CreateSelfSigned(
                subject,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddHours(1));

            // Install with private key first
            MtlsBindingStore.InstallAndGetSubject(certWithKey);

            // Replace with a public-only instance (same thumbprint)
            var publicOnly = new X509Certificate2(certWithKey.Export(X509ContentType.Cert));
            MtlsBindingStore.InstallAndGetSubject(publicOnly);

            var fetched = MtlsBindingStore.GetFreshestBySubject(subject);
            Assert.IsNotNull(fetched);
            Assert.IsFalse(MtlsBindingStore.IsPrivateKeyUsable(fetched),
                "Setup expects the certificate to be public-only (unusable).");

            // Remove and verify gone
            MtlsBindingStore.RemoveByThumbprint(fetched.Thumbprint);
            Assert.IsFalse(CertHelper.ExistsByThumbprint(fetched.Thumbprint),
                "Certificate should be removed from the store after removal by thumbprint.");
        }

        // Direct test for IsCurrentlyValid with current, expired, and future certificates
        [TestMethod]
        public void IsCurrentlyValid_ReturnsTrueOnlyForValidCertificates()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            
            var expired = CertHelper.CreateSelfSigned(
                subject, 
                DateTimeOffset.UtcNow.AddHours(-2), 
                DateTimeOffset.UtcNow.AddHours(-1)); // Already expired
            
            var valid = CertHelper.CreateSelfSigned(
                subject, 
                DateTimeOffset.UtcNow.AddHours(-1), 
                DateTimeOffset.UtcNow.AddHours(1)); // Currently valid
            
            var future = CertHelper.CreateSelfSigned(
                subject, 
                DateTimeOffset.UtcNow.AddHours(1), 
                DateTimeOffset.UtcNow.AddHours(2)); // Not valid yet (future start date)            
            Assert.IsFalse(MtlsBindingStore.IsCurrentlyValid(expired), "Expired certificate should not be valid");
            Assert.IsTrue(MtlsBindingStore.IsCurrentlyValid(valid), "Currently valid certificate should be valid");
            Assert.IsFalse(MtlsBindingStore.IsCurrentlyValid(future), "Future certificate should not be valid yet");
            Assert.IsFalse(MtlsBindingStore.IsCurrentlyValid(null), "Null certificate should not be valid");
        }

        // Test for RemoveAllBySubject
        [TestMethod]
        public void RemoveAllBySubject_RemovesAllCertificatesWithSubject()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var differentSubject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            
            // Create 3 certs with same subject and 1 with different subject
            var cert1 = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-40), DateTimeOffset.UtcNow.AddMinutes(20));
            var cert2 = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-20), DateTimeOffset.UtcNow.AddMinutes(40));
            var cert3 = CertHelper.CreateSelfSigned(subject, DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddMinutes(60));
            var otherCert = CertHelper.CreateSelfSigned(differentSubject, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(30));
            
            // Install all certs
            MtlsBindingStore.InstallAndGetSubject(cert1);
            MtlsBindingStore.InstallAndGetSubject(cert2);
            MtlsBindingStore.InstallAndGetSubject(cert3);
            MtlsBindingStore.InstallAndGetSubject(otherCert);
            
            // Remove all with matching subject
            MtlsBindingStore.RemoveAllBySubject(subject);
            
            // Verify all matching certs are gone
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var matches = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subject, false);
            var otherMatches = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, differentSubject, false);
            
            Assert.AreEqual(0, matches.Count, "All certificates with the target subject should be removed");
            Assert.AreEqual(1, otherMatches.Count, "Certificates with different subjects should not be affected");
            Assert.AreEqual(otherCert.Thumbprint, otherMatches[0].Thumbprint, "The unrelated certificate should remain intact");
        }

        // Test for PurgeExpiredBeyondWindow
        [TestMethod]
        public void PurgeExpiredBeyondWindow_RemovesOnlyStaleExpiredCerts()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var now = DateTimeOffset.UtcNow;
            
            // Create 3 certs with varying expiration dates
            var validCert = CertHelper.CreateSelfSigned(
                subject, now.AddDays(-10), now.AddDays(10)); // Valid
                
            var recentlyExpired = CertHelper.CreateSelfSigned(
                subject, now.AddDays(-20), now.AddDays(-3)); // Expired but within 7-day window
                
            var staleExpired = CertHelper.CreateSelfSigned(
                subject, now.AddDays(-30), now.AddDays(-10)); // Expired beyond 7-day window
            
            // Install all certs
            MtlsBindingStore.InstallAndGetSubject(validCert);
            MtlsBindingStore.InstallAndGetSubject(recentlyExpired);
            MtlsBindingStore.InstallAndGetSubject(staleExpired);
            
            // Run the purge operation
            MtlsBindingStore.PurgeExpiredBeyondWindow(subject);
            
            // Check what remains in the store
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var remainingCerts = store.Certificates
                .Find(X509FindType.FindBySubjectDistinguishedName, subject, false)
                .OfType<X509Certificate2>()
                .ToList();
            
            Assert.AreEqual(2, remainingCerts.Count, "Should keep valid and recently expired certs");
            
            var remainingThumbprints = remainingCerts.Select(c => c.Thumbprint).ToList();
            CollectionAssert.Contains(remainingThumbprints, validCert.Thumbprint, "Valid certificate should be kept");
            CollectionAssert.Contains(remainingThumbprints, recentlyExpired.Thumbprint, "Recently expired certificate should be kept");
            CollectionAssert.DoesNotContain(remainingThumbprints, staleExpired.Thumbprint, "Stale expired certificate should be removed");
        }

        // Test input validation on various methods
        [TestMethod]
        public void InputValidation_NullOrEmptyInputs()
        {
            // Test with null/empty subject
            Assert.IsNull(MtlsBindingStore.GetFreshestBySubject(null), "GetFreshestBySubject with null subject should return null");
            Assert.IsNull(MtlsBindingStore.GetFreshestBySubject(""), "GetFreshestBySubject with empty subject should return null");
            
            // Test with null certificate
            Assert.IsNull(MtlsBindingStore.InstallAndGetSubject(null), "InstallAndGetSubject with null cert should return null");
            
            // Verify RemoveAllBySubject with null/empty doesn't throw
            MtlsBindingStore.RemoveAllBySubject(null); // Should not throw
            MtlsBindingStore.RemoveAllBySubject(""); // Should not throw
            
            // Verify RemoveByThumbprint with null/empty doesn't throw
            MtlsBindingStore.RemoveByThumbprint(null); // Should not throw
            MtlsBindingStore.RemoveByThumbprint(""); // Should not throw
            
            // Test resolution with null/empty inputs
            string resolvedThumbprint;
            var result = MtlsBindingStore.ResolveByThumbprintThenSubject(null, null, false, out resolvedThumbprint);
            Assert.IsNull(result, "Resolution with null inputs should return null");
            Assert.IsNull(resolvedThumbprint, "Resolved thumbprint should be null with invalid inputs");
        }

        // Test boundary cases for certificate expiration and purging
        [TestMethod]
        public void ExpirationWindow_BoundaryCases()
        {
            var subject = $"{Prefix}{Guid.NewGuid()}, DC=unit";
            var now = DateTimeOffset.UtcNow;
            
            // Create certificates at various points around the expiration window boundary
            var justInsideWindow = CertHelper.CreateSelfSigned(
                subject, 
                now.AddDays(-14), 
                now.AddDays(-6.9)); // Expired 6.9 days ago (just inside 7-day window)
                
            var justOutsideWindow = CertHelper.CreateSelfSigned(
                subject, 
                now.AddDays(-14), 
                now.AddDays(-7.1)); // Expired 7.1 days ago (just outside 7-day window)
            
            MtlsBindingStore.InstallAndGetSubject(justInsideWindow);
            MtlsBindingStore.InstallAndGetSubject(justOutsideWindow);
            
            // Run purge and check what remains
            MtlsBindingStore.PurgeExpiredBeyondWindow(subject);
            
            // Check if justInsideWindow is kept and justOutsideWindow is purged
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            
            var insideCert = store.Certificates.Find(
                X509FindType.FindByThumbprint, justInsideWindow.Thumbprint, false);
                
            var outsideCert = store.Certificates.Find(
                X509FindType.FindByThumbprint, justOutsideWindow.Thumbprint, false);
            
            Assert.AreEqual(1, insideCert.Count, "Certificate just inside the purge window should be kept");
            Assert.AreEqual(0, outsideCert.Count, "Certificate just outside the purge window should be removed");
        }
    }
}
