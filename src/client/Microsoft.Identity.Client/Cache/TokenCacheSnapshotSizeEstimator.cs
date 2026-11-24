// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
#if !NET8_0_OR_GREATER
using System.Text.Json.Nodes;
#endif
using Microsoft.Identity.Client.Cache.Items;

namespace Microsoft.Identity.Client.Cache
{
    internal static class TokenCacheSnapshotSizeEstimator
    {
        // Calibrated against .NET 8 x64 inclusive heap graphs: snapshot (32), contract (64),
        // six dictionary shells (6 * 80), and shared comparers (96). Backing arrays are separate.
        private const long SnapshotOverhead = 672;
        // Record shells plus each Lazy<T>, factory delegate and LazyHelper (136 bytes).
        private const long AccessTokenOverhead = 256 + 136;
        private const long RefreshTokenOverhead = 120 + 136;
        private const long IdTokenOverhead = 112 + 2 * 136;
        private const long AccountOverhead = 144 + 136;
        private const long AppMetadataOverhead = 64 + 136;
        private const long CollectionOverhead = 128;
        private const long CollectionEntryOverhead = 128;

        internal static long Estimate(CacheSerializationContract state, int unknownNodesSourceSizeInBytes)
        {
            long size = SnapshotOverhead +
                DictionaryStorage(state.AccessTokens) + DictionaryStorage(state.RefreshTokens) +
                DictionaryStorage(state.IdTokens) + DictionaryStorage(state.Accounts) +
                DictionaryStorage(state.AppMetadata);
            bool retainsSourceDocument = false;
            if (state.UnknownNodes.Count > 0)
            {
                foreach (var node in state.UnknownNodes)
                {
                    size += CollectionEntryOverhead + String(node.Key);
                    retainsSourceDocument |= node.Value is not null;
#if !NET8_0_OR_GREATER
                    size += MaterializedUnknownNode(node.Value);
#endif
                }
            }
            if (retainsSourceDocument)
            {
                // Unknown nodes may retain the entire input document. Approximate its UTF-8 data
                // and metadata from the source length without traversing or serializing the nodes.
                size += 2L * unknownNodesSourceSizeInBytes;
            }
            foreach (var entry in state.AccessTokens)
            {
                var item = entry.Value;
                size += AccessTokenOverhead + Credential(item) +
                    String(item.CacheKey) + String(item.TenantId) + String(item.OboCacheKey) +
                    String(item.KeyId) + String(item.TokenType) + String(item.ScopeString) +
                    Dictionary(item.AdditionalCacheKeyComponents) + Dictionary(item.PersistedCacheParameters);
#if NET8_0_OR_GREATER
                // A zero-capacity request only reads existing capacity; it does not allocate or resize.
                int scopeCapacity = item.ScopeSet.EnsureCapacity(0);
#else
                long scopeSlots = item.ScopeSet.Count;
                if (!string.IsNullOrEmpty(item.ScopeString))
                {
                    scopeSlots = 1;
                    foreach (char c in item.ScopeString)
                    {
                        if (c == ' ')
                        {
                            scopeSlots++;
                        }
                    }
                }
                long scopeCapacity = EstimatedCapacity(scopeSlots);
#endif
                size += 64 + CollectionStorage(scopeCapacity, 16);
                foreach (string scope in item.ScopeSet)
                {
                    // A single unsplit scope can be the same string as ScopeString.
                    if (!ReferenceEquals(scope, item.ScopeString))
                    {
                        size += String(scope);
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
                size += RefreshTokenOverhead + Credential(item) +
                    String(item.CacheKey) + String(item.FamilyId) + String(item.OboCacheKey) +
                    Dictionary(item.AdditionalCacheKeyComponents);
            }

            foreach (var entry in state.IdTokens)
            {
                var item = entry.Value;
                size += IdTokenOverhead + Credential(item) + String(item.CacheKey) + String(item.TenantId);
            }

            foreach (var entry in state.Accounts)
            {
                var item = entry.Value;
                size += AccountOverhead + Common(item) + String(item.CacheKey) + String(item.TenantId) +
                    String(item.PreferredUsername) + String(item.Name) + String(item.GivenName) +
                    String(item.FamilyName) + String(item.LocalAccountId) + String(item.AccountSource) +
                    String(item.AuthorityType) + Dictionary(item.WamAccountIds);
            }

            foreach (var entry in state.AppMetadata)
            {
                var item = entry.Value;
                size += AppMetadataOverhead + String(item.ClientId) + String(item.Environment) +
                    String(item.FamilyId) + String(item.CacheKey) + String(item.AdditionalFieldsJson);
            }
            return size;
        }

#if !NET8_0_OR_GREATER
        private static long MaterializedUnknownNode(JsonNode node)
        {
            // Legacy snapshot cloning already materializes unknown objects and arrays.
            if (node is JsonObject obj)
            {
                long size = CollectionOverhead;
                foreach (var pair in obj)
                {
                    size += CollectionEntryOverhead + String(pair.Key) + MaterializedUnknownNode(pair.Value);
                }
                return size;
            }
            if (node is JsonArray array)
            {
                long size = CollectionOverhead;
                foreach (var item in array)
                {
                    size += CollectionEntryOverhead + MaterializedUnknownNode(item);
                }
                return size;
            }
            return 0;
        }
#endif

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

            long size = values is Dictionary<string, string> dictionary
                ? 80 + DictionaryStorage(dictionary)
                : values is SortedList<string, string> sorted
                    ? 64 + 2 * Align(24L + 8L * sorted.Capacity)
                    : CollectionOverhead + CollectionEntryOverhead * values.Count;
            foreach (var pair in values)
            {
                size += String(pair.Key) + String(pair.Value);
            }
            return size;
        }

        private static long DictionaryStorage<T>(Dictionary<string, T> values)
        {
#if NET8_0_OR_GREATER
            return CollectionStorage(values.EnsureCapacity(0), 24);
#else
            return CollectionStorage(EstimatedCapacity(values.Count), 24);
#endif
        }

#if !NET8_0_OR_GREATER
        private static long EstimatedCapacity(long count)
        {
            return count == 0 ? 0 : Math.Max(3, count + count / 2);
        }
#endif

        private static long CollectionStorage(long capacity, int entrySize)
        {
            return capacity == 0 ? 0 : Align(24 + 4 * capacity) + Align(24 + entrySize * capacity);
        }

        private static long Align(long size)
        {
            return (size + 7) & ~7L;
        }

        private static long String(string value)
        {
            // Empty strings are shared globally, not retained separately by each field.
            return string.IsNullOrEmpty(value) ? 0 : Align(22L + 2L * value.Length);
        }
    }
}
