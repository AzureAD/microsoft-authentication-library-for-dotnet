// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.ManagedIdentity.V2.Cache
{
    internal sealed partial class InMemoryMtlsCertCache
    {
        private sealed class Bucket
        {
            private readonly List<MtlsCertCacheEntry> _entries = new List<MtlsCertCacheEntry>(2);
            private readonly object _gate = new object();

            public List<MtlsCertCacheEntry> Snapshot()
            {
                lock (_gate)
                { return new List<MtlsCertCacheEntry>(_entries); }
            }

            public void Add(MtlsCertCacheEntry entry)
            {
                lock (_gate)
                {
                    _entries.Add(entry);
                    // keep most recent first (NotBefore / NotAfter / CreatedAtUtc)
                    int i = _entries.Count - 1;
                    while (i > 0 && IsLess(_entries[i - 1], _entries[i]))
                    {
                        var tmp = _entries[i - 1];
                        _entries[i - 1] = _entries[i];
                        _entries[i] = tmp;
                        i--;
                    }
                    if (_entries.Count > 2)
                        _entries.RemoveRange(2, _entries.Count - 2);
                }
            }

            public void Remove(MtlsCertCacheEntry entry)
            {
                lock (_gate)
                { _entries.Remove(entry); }
            }

            public int Prune(DateTimeOffset nowUtc, TimeSpan skew)
            {
                lock (_gate)
                {
                    int before = _entries.Count;
                    for (int i = _entries.Count - 1; i >= 0; i--)
                    {
                        if (_entries[i].NotAfter <= (nowUtc + skew))
                            _entries.RemoveAt(i);
                    }
                    return before - _entries.Count;
                }
            }

            private static bool IsLess(MtlsCertCacheEntry a, MtlsCertCacheEntry b)
            {
                if (a.NotBefore != b.NotBefore)
                    return a.NotBefore < b.NotBefore;
                if (a.NotAfter != b.NotAfter)
                    return a.NotAfter < b.NotAfter;
                return a.CreatedAtUtc < b.CreatedAtUtc;
            }
        }
    }
}
