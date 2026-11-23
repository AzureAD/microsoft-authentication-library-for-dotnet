// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;

namespace Microsoft.Identity.Client.Cache
{
    internal static class TokenCacheSnapshotSizeEstimator
    {
        // Rounded 1.5 KiB heuristic budgets, sanity-checked against synthetic retained-memory
        // measurements, not derived from CLR layouts or intended as upper bounds.
        // SnapshotOverhead covers the snapshot, serialization contract, and six dictionary shells.
        private const long SnapshotOverhead = 1536;
        // EntryOverhead covers the record's fields/header, lazy iOS-key wrapper/delegate,
        // and a share of the containing dictionary's backing arrays and spare capacity.
        // Variable-length strings and nested collection contents are charged separately below,
        // deliberately overcounting some shared strings.
        private const long EntryOverhead = 1536;
        private const long CollectionOverhead = 128;
        private const long CollectionEntryOverhead = 128;

        internal static long Estimate(CacheSerializationContract state, long unknownNodesSize)
        {
            long size = SnapshotOverhead + unknownNodesSize;
            foreach (var entry in state.AccessTokens)
            {
                var item = entry.Value;
                size += EntryOverhead + Credential(item) +
                    String(item.CacheKey) + String(item.TenantId) + String(item.OboCacheKey) +
                    String(item.KeyId) + String(item.TokenType) + String(item.ScopeString) +
                    Dictionary(item.AdditionalCacheKeyComponents) + Dictionary(item.PersistedCacheParameters);
                foreach (string scope in item.ScopeSet)
                {
                    size += CollectionEntryOverhead + String(scope);
                }
                if (!string.IsNullOrEmpty(item.ScopeString))
                {
                    long scopeSlots = 1;
                    foreach (char c in item.ScopeString)
                    {
                        if (c == ' ')
                        {
                            scopeSlots++;
                        }
                    }
                    // Some runtimes can preallocate the set from the split scope count,
                    // even when many scopes are duplicates.
                    if (scopeSlots > item.ScopeSet.Count)
                    {
                        size += CollectionEntryOverhead * (scopeSlots - item.ScopeSet.Count);
                    }
                }
                if (item.AdditionalCacheKeyComponents is not null)
                {
                    // InitCacheKey also retains a separate generated extension string in _extraKeyParts.
                    // The full CacheKey size is a conservative proxy for that string, not a second
                    // CacheKey object; it overcounts the non-extension parts without allocating here.
                    size += String(item.CacheKey);
                }
            }

            foreach (var entry in state.RefreshTokens)
            {
                var item = entry.Value;
                size += EntryOverhead + Credential(item) +
                    String(item.CacheKey) + String(item.FamilyId) + String(item.OboCacheKey) +
                    Dictionary(item.AdditionalCacheKeyComponents);
            }

            foreach (var entry in state.IdTokens)
            {
                var item = entry.Value;
                size += EntryOverhead + Credential(item) + String(item.CacheKey) + String(item.TenantId);
            }

            foreach (var entry in state.Accounts)
            {
                var item = entry.Value;
                size += EntryOverhead + Common(item) + String(item.CacheKey) + String(item.TenantId) +
                    String(item.PreferredUsername) + String(item.Name) + String(item.GivenName) +
                    String(item.FamilyName) + String(item.LocalAccountId) + String(item.AccountSource) +
                    String(item.AuthorityType) + Dictionary(item.WamAccountIds);
            }

            foreach (var entry in state.AppMetadata)
            {
                var item = entry.Value;
                size += EntryOverhead + String(item.ClientId) + String(item.Environment) +
                    String(item.FamilyId) + String(item.CacheKey) + String(item.AdditionalFieldsJson);
            }
            return size;
        }

        internal static long EstimateUnknownNode(string key, string json)
        {
            long size = CollectionEntryOverhead + String(key);
            if (json is null)
            {
                return size;
            }

            // Count structure outside strings without materializing the JsonNode tree. This also
            // reserves space for lazy nodes that may be materialized by legacy snapshot restore.
            long units = 1;
            bool inString = false;
            bool escaped = false;
            foreach (char c in json)
            {
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                }
                else if (c == '"')
                {
                    inString = true;
                }
                else if (c == '{' || c == '[' || c == ':' || c == ',')
                {
                    units++;
                }
            }

            return size + CollectionOverhead + 6L * json.Length + CollectionEntryOverhead * units;
        }

        private static long Common(MsalCacheItemBase item)
        {
            return String(item.HomeAccountId) + String(item.Environment) +
                String(item.RawClientInfo) + String(item.AdditionalFieldsJson);
        }

        private static long Credential(MsalCredentialCacheItemBase item)
        {
            return Common(item) + String(item.CredentialType) + String(item.ClientId) + String(item.Secret);
        }

        private static long Dictionary(IDictionary<string, string> values)
        {
            if (values is null)
            {
                return 0;
            }

            long size = CollectionOverhead;
            foreach (var pair in values)
            {
                size += CollectionEntryOverhead + String(pair.Key) + String(pair.Value);
            }
            return size;
        }

        private static long String(string value)
        {
            return value is null ? 0 : 32L + 2L * value.Length;
        }
    }
}
