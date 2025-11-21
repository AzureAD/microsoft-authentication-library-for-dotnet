// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    internal static class ImdsV2TestStoreCleaner
    {
        // Keep independent of product internals
        private const string FriendlyPrefix = "MSAL|";

        // DC (tenant) used by tests
        private const string TestTenantId = "751a212b-4003-416e-b600-e1f48e40db9f";

        // Subject CNs to clean unconditionally
        private static readonly string[] UnconditionalCnTrash = new[]
        {
            "UAMI-20Y",
            "SAMI-20Y",
            "Test"
        };

        // Subject CNs to clean only when DC == TestTenantId
        private static readonly string[] ConditionalCnTrash = new[]
        {
            "system_assigned_managed_identity",
            "d3adb33f-c0de-ed0c-c0de-deadb33fc0d3"
        };

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Remove all persisted entries that look like test artifacts:
        /// - FriendlyName starts with "MSAL|" and either has a fake endpoint or is expired-by-policy
        /// - Subject CN matches test CNs (UAMI-20Y, SAMI-20Y, Test)
        /// - Subject CN matches certain values AND subject DC equals the test tenant
        /// Best-effort, no-throw.
        /// </summary>
        public static void RemoveAllTestArtifacts()
        {
            if (!IsWindows)
                return;

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                var nowUtc = DateTime.UtcNow;

                // Snapshot once to safely enumerate while removing
                X509Certificate2[] items = SnapshotCertificates(store);

                foreach (var c in items)
                {
                    try
                    {
                        var fn = c.FriendlyName ?? string.Empty;

                        // Case 1: Our persisted entries with fake endpoints or expired-by-policy
                        if (fn.StartsWith(FriendlyPrefix, StringComparison.Ordinal))
                        {
                            bool looksFakeEp =
                                fn.IndexOf("|ep=http://", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                fn.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                fn.IndexOf("fake", StringComparison.OrdinalIgnoreCase) >= 0;

                            bool expiredByPolicy =
                                c.NotAfter.ToUniversalTime() <= nowUtc +
                                Microsoft.Identity.Client.ManagedIdentity.V2.CertificateCacheEntry.MinRemainingLifetime;

                            if (looksFakeEp || expiredByPolicy)
                            {
                                try
                                { store.Remove(c); }
                                catch { /* ignore */ }
                                continue;
                            }
                        }

                        // Case 2: Subject-based cleanup for test certs
                        var cn = GetCn(c);
                        var dc = GetDc(c);

                        if (MatchesSubjectTrash(cn, dc))
                        {
                            try
                            { store.Remove(c); }
                            catch { /* ignore */ }
                            continue;
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

        /// <summary>
        /// Remove all entries for a specific alias (cache key) based on FriendlyName.
        /// Best-effort, no-throw.
        /// </summary>
        public static void RemoveAlias(string alias)
        {
            if (!IsWindows || string.IsNullOrWhiteSpace(alias))
                return;

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                // Snapshot for safe removal
                X509Certificate2[] items = SnapshotCertificates(store);

                foreach (var c in items)
                {
                    try
                    {
                        var fn = c.FriendlyName ?? string.Empty;
                        if (fn.StartsWith(FriendlyPrefix, StringComparison.Ordinal) &&
                            fn.Contains("alias="))
                        {
                            try
                            { store.Remove(c); }
                            catch { /* ignore */ }
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

        // ---------------- helpers ----------------

        /// <summary>
        /// Takes a safe snapshot of the certificates in the given store so we can
        /// enumerate and remove without running into "collection modified" issues.
        /// </summary>
        private static X509Certificate2[] SnapshotCertificates(X509Store store)
        {
            try
            {
                var items = new X509Certificate2[store.Certificates.Count];
                store.Certificates.CopyTo(items, 0);
                return items;
            }
            catch
            {
                // Fallback for providers that don't like CopyTo while removing.
                return store.Certificates.Cast<X509Certificate2>().ToArray();
            }
        }

        /// <summary>
        /// Determines whether the given subject CN/DC pair should be treated as a test
        /// artifact and removed from the store.
        /// </summary>
        private static bool MatchesSubjectTrash(string cn, string dc)
        {
            if (string.IsNullOrEmpty(cn))
                return false;

            // Unconditional CNs
            foreach (var name in UnconditionalCnTrash)
            {
                if (string.Equals(cn, name, StringComparison.Ordinal))
                    return true;
            }

            // Conditional CNs (require test tenant DC)
            if (!string.IsNullOrEmpty(dc) &&
                string.Equals(dc, TestTenantId, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var name in ConditionalCnTrash)
                {
                    if (string.Equals(cn, name, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Extracts the subject common name (CN) from the certificate, using
        /// <see cref="X509Certificate2.GetNameInfo"/> when possible and falling back
        /// to manual DN parsing.
        /// </summary>
        private static string GetCn(X509Certificate2 cert)
        {
            try
            {
                var simple = cert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
                if (!string.IsNullOrEmpty(simple))
                    return simple;
            }
            catch
            {
                // fall through to manual parse
            }

            return ReadRdn(cert, "CN");
        }

        /// <summary>
        /// Extracts the subject DC (domain component) from the certificate, if present.
        /// </summary>
        private static string GetDc(X509Certificate2 cert)
        {
            return ReadRdn(cert, "DC");
        }

        /// <summary>
        /// Parses a specific RDN (e.g. "CN", "DC") out of the certificate subject.
        /// Returns <c>null</c> if the RDN is not present.
        /// </summary>
        private static string ReadRdn(X509Certificate2 cert, string rdn)
        {
            var dn = cert?.SubjectName?.Name ?? cert?.Subject ?? string.Empty;
            if (string.IsNullOrEmpty(dn))
                return null;

            // Simple, robust split: "CN=..., DC=..." etc.
            var parts = dn.Split(',');
            foreach (var part in parts)
            {
                var kv = part.Split('=');
                if (kv.Length == 2 &&
                    kv[0].Trim().Equals(rdn, StringComparison.OrdinalIgnoreCase))
                {
                    return kv[1].Trim().Trim('"');
                }
            }
            return null;
        }
    }
}
