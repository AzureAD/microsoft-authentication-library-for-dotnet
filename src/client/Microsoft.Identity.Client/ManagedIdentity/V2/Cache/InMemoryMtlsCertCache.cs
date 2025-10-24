// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ManagedIdentity.V2.Cache.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.ManagedIdentity.V2.Cache
{
    internal sealed partial class InMemoryMtlsCertCache : IMtlsCertCache
    {
        public static InMemoryMtlsCertCache Shared { get; } = new InMemoryMtlsCertCache();

        private static readonly TimeSpan s_clockSkew = TimeSpan.FromMinutes(2);
        private readonly ConcurrentDictionary<MiCacheKey, Bucket> _buckets =
            new ConcurrentDictionary<MiCacheKey, Bucket>();

        public bool TryGetLatest(MiCacheKey key, DateTimeOffset nowUtc, out MtlsCertCacheEntry entry)
        {
            entry = null;
            if (!_buckets.TryGetValue(key, out var bucket))
                return false;

            bucket.Prune(nowUtc, s_clockSkew);

            var list = bucket.Snapshot();
            
            MtlsCertCacheEntry best = null;

            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (best == null)
                { best = e; continue; }
                if (e.NotBefore > best.NotBefore)
                { best = e; continue; }
                if (e.NotBefore < best.NotBefore)
                    continue;
                if (e.NotAfter > best.NotAfter)
                { best = e; continue; }
                if (e.NotAfter < best.NotAfter)
                    continue;
                if (e.CreatedAtUtc > best.CreatedAtUtc)
                    best = e;
            }
            if (best == null)
                return false;

            if (best.NotAfter <= (nowUtc + s_clockSkew))
            {
                bucket.Remove(best);
                return false;
            }

            // Drop entries whose private key is not usable (e.g., after reboot)
            try
            {
                if (!MtlsBindingStore.IsPrivateKeyUsable(best.Certificate))
                {
                    bucket.Remove(best);
                    return false;
                }
            }
            catch
            {
                bucket.Remove(best);
                return false;
            }

            entry = best;
            return true;
        }

        public void Put(MiCacheKey key, MtlsCertCacheEntry entry)
        {
            var bucket = _buckets.GetOrAdd(key, _ => new Bucket());
            bucket.Add(entry);
        }

        public int Prune(DateTimeOffset nowUtc)
        {
            int removed = 0;
            foreach (var kv in _buckets)
            {
                removed += kv.Value.Prune(nowUtc, s_clockSkew);
            }
            return removed;
        }

        internal bool TryGetLatestBySubject(
            string subjectCn,
            string subjectDc,
            string tokenType,                // kept for signature symmetry; not used here
            DateTimeOffset now,
            out MtlsCertCacheEntry entry)
        {
            entry = null;
            MtlsCertCacheEntry best = null;
            DateTimeOffset bestNotBefore = DateTimeOffset.MinValue;

            // Iterate all in-memory entries; a linear scan is OK on rare misses.
            foreach (var bucket in _buckets.Values)
            {
                // If you have a lock-protected list, expose a Snapshot() that returns a copy/array.
                foreach (var e in bucket.Snapshot())
                {
                    var cert = e.Certificate;
                    if (cert == null)
                        continue;

                    // Validity check
                    if (cert.NotAfter.ToUniversalTime() <= now.UtcDateTime)
                        continue;

                    // Private key usability check: drop unusable entries
                    try
                    {
                        if (!MtlsBindingStore.IsPrivateKeyUsable(cert))
                        {
                            // Best-effort removal to keep cache clean; safe due to internal locking
                            bucket.Remove(e);
                            continue;
                        }
                    }
                    catch
                    {
                        bucket.Remove(e);
                        continue;
                    }

                    // Subject CN/DC match
                    var subject = cert.Subject;
                    if (string.IsNullOrEmpty(subject))
                        continue;

                    bool cnOk = subject.IndexOf("CN=" + subjectCn, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool dcOk = subject.IndexOf("DC=" + subjectDc, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!cnOk || !dcOk)
                        continue;

                    // Pick freshest by NotBefore
                    var nbUtc = cert.NotBefore.ToUniversalTime();
                    if (nbUtc > bestNotBefore.UtcDateTime)
                    {
                        best = e;
                        bestNotBefore = new DateTimeOffset(nbUtc, TimeSpan.Zero);
                    }
                }
            }

            if (best != null)
            {
                entry = best;
                return true;
            }

            return false;
        }

        // ---- TEST HOOK ----
        // Clears all per-key buckets. Safe for tests; not meant for production code paths.
        internal void ClearForTest()
        {
            _buckets.Clear();
        }
    }
}
