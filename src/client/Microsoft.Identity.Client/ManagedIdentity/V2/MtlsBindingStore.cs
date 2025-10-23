// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal sealed class MtlsBindingStore : ICertificateRepository
    {
        public static MtlsBindingStore Default { get; } = new MtlsBindingStore();

        private static readonly TimeSpan s_clockSkew = TimeSpan.FromMinutes(2);

        public void TryInstallWithFriendlyName(X509Certificate2 cert, string friendlyName)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));

            try
            {
                // Create an independent instance bound to the SAME private key
                X509Certificate2 toStore;
                using (var rsa = cert.GetRSAPrivateKey())
                {
                    if (rsa == null)
                        return; // no usable key; skip store write
                    var rawB64 = Convert.ToBase64String(cert.RawData);
                    toStore = CommonCryptographyManager.AttachPrivateKeyToCert(rawB64, rsa);
                }

                try
                { toStore.FriendlyName = friendlyName; }
                catch { /* best-effort */ }

                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(toStore); // add the copy — NOT the cached instance
                }
            }
            catch
            {
                // Best-effort — do not fail the caller
            }
        }

        public bool TryResolveFreshestBySubjectAndType(
            string subjectCn, string subjectDc, string tokenType,
            out X509Certificate2 freshest, out string mtlsEndpoint)
        {
            freshest = null;
            mtlsEndpoint = null;

            try
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var now = DateTimeOffset.UtcNow;

                    var candidates = new List<(X509Certificate2 cert, DateTimeOffset nb, DateTimeOffset na, string endpoint)>();

                    foreach (var cert in store.Certificates)
                    {
                        if (!SubjectMatches(cert, subjectCn, subjectDc))
                            continue;

                        string fn = null;
                        try
                        { fn = cert.FriendlyName; }
                        catch { fn = null; }

                        if (string.IsNullOrEmpty(fn) || !BindingFriendlyName.HasOurPrefix(fn))
                            continue;

                        if (!BindingFriendlyName.TryParse(fn, out var fnType, out var endpoint))
                            continue;

                        if (!string.Equals(fnType, tokenType, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var nb = new DateTimeOffset(cert.NotBefore.ToUniversalTime());
                        var na = new DateTimeOffset(cert.NotAfter.ToUniversalTime());

                        // Current validity window
                        if (na <= (now + s_clockSkew))
                            continue;

                        // Private key usability
                        if (!IsPrivateKeyUsable(cert))
                            continue;

                        candidates.Add((cert, nb, na, endpoint));
                    }

                    if (candidates.Count == 0)
                        return false;

                    // Choose the newest by NotBefore, then longer NotAfter
                    candidates.Sort((a, b) =>
                    {
                        int c = a.nb.CompareTo(b.nb);
                        if (c != 0)
                            return -c; // desc
                        c = a.na.CompareTo(b.na);
                        return -c;
                    });

                    freshest = candidates[0].cert;
                    mtlsEndpoint = candidates[0].endpoint;
                    return true;
                }
            }
            catch
            {
                return false; // rehydrate not available on this platform or store inaccessible
            }
        }

        public void PurgeExpiredBeyondWindow(string subjectCn, string subjectDc, TimeSpan grace)
        {
            try
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);
                    var coll = store.Certificates;
                    var now = DateTimeOffset.UtcNow;

                    for (int i = coll.Count - 1; i >= 0; i--)
                    {
                        var c = coll[i];
                        if (!SubjectMatches(c, subjectCn, subjectDc))
                            continue;

                        var na = new DateTimeOffset(c.NotAfter.ToUniversalTime());
                        if (na + grace < now)
                        {
                            try
                            { store.Remove(c); }
                            catch { /* ignore */ }
                        }
                    }
                }
            }
            catch { /* best-effort */ }
        }

        public void RemoveAllWithFriendlyNamePrefixForTest(string friendlyNamePrefix)
        {
            try
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);
                    var coll = store.Certificates;

                    for (int i = coll.Count - 1; i >= 0; i--)
                    {
                        string fn = null;
                        try
                        { fn = coll[i].FriendlyName; }
                        catch { /* ignore */ }
                        if (!string.IsNullOrEmpty(fn) && fn.StartsWith(friendlyNamePrefix, StringComparison.Ordinal))
                        {
                            try
                            { store.Remove(coll[i]); }
                            catch { /* ignore */ }
                        }
                    }
                }
            }
            catch { /* test hook */ }
        }

        // ---- helpers ----

        private static bool SubjectMatches(X509Certificate2 cert, string cn, string dc)
        {
            try
            {
                var subj = cert.Subject;
                if (string.IsNullOrEmpty(subj))
                    return false;

                // Robust-enough check for "CN=<cn>" and "DC=<dc>"
                bool hasCn = subj.IndexOf("CN=" + cn, StringComparison.OrdinalIgnoreCase) >= 0;
                bool hasDc = subj.IndexOf("DC=" + dc, StringComparison.OrdinalIgnoreCase) >= 0;
                return hasCn && hasDc;
            }
            catch { return false; }
        }

        internal static bool IsPrivateKeyUsable(X509Certificate2 cert)
        {
            try
            {
                // Prefer RSA; if ECDSA certs are introduced, add ECDsa fallback.
                using (var rsa = cert.GetRSAPrivateKey())
                {
                    if (rsa == null)
                        return false;

                    // Non-exportable KeyGuard keys still allow reading public params
                    // and cryptographic operations; this is a light sanity check.
                    rsa.ExportParameters(false);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
