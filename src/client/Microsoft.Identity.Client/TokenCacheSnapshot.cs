// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// An immutable, process-local snapshot of persisted MSAL token cache state.
    /// </summary>
    /// <remarks>
    /// Contains credentials. Do not log or expose this object. Mutable working cache
    /// entries are copied on capture and restore; this type is not a transport format.
    /// A snapshot can be restored into multiple caches of the same type, including concurrently.
    /// Use <see cref="SerializeMsalV3"/> when crossing process boundaries or persisting to storage.
    /// </remarks>
    public sealed class TokenCacheSnapshot
    {
        private readonly CacheSerializationContract _state;
        private readonly long _unknownNodesEstimatedSize;
        internal bool IsApplicationCache { get; }

        internal TokenCacheSnapshot(
            ITokenCacheAccessor accessor,
            IDictionary<string, JsonNode> unknownNodes,
            bool isApplicationCache)
        {
            IsApplicationCache = isApplicationCache;
            _state = new CacheSerializationContract(CloneUnknownNodes(
                unknownNodes, detachFromOriginalDocument: true, out _unknownNodesEstimatedSize));

            foreach (var item in accessor.GetAllAccessTokens())
            {
                var copy = item.CloneForSnapshot();
                _state.AccessTokens[copy.CacheKey] = copy;
            }
            foreach (var item in accessor.GetAllRefreshTokens())
            {
                var copy = item.CloneForSnapshot();
                _state.RefreshTokens[copy.CacheKey] = copy;
            }
            foreach (var item in accessor.GetAllIdTokens())
            {
                var copy = item.CloneForSnapshot();
                _state.IdTokens[copy.CacheKey] = copy;
            }
            foreach (var item in accessor.GetAllAccounts())
            {
                var copy = item.CloneForSnapshot();
                _state.Accounts[copy.CacheKey] = copy;
            }
            foreach (var item in accessor.GetAllAppMetadata())
            {
                var copy = item.CloneForSnapshot();
                _state.AppMetadata[copy.CacheKey] = copy;
            }
        }

        /// <summary>
        /// Serializes this snapshot in the existing MSAL V3 format for persistent storage.
        /// </summary>
        /// <returns>A newly allocated serialized payload.</returns>
        public byte[] SerializeMsalV3()
        {
            return _state.ToJsonString().ToByteArray();
        }

        /// <summary>
        /// Estimates the managed memory retained by this snapshot, in bytes.
        /// </summary>
        /// <remarks>
        /// This estimate is intended for cache admission and eviction accounting, not as a hard
        /// memory limit. Actual memory depends on the runtime, architecture, sharing, and cache
        /// contents. It excludes cache-provider overhead, temporary allocations, and working caches.
        /// The estimate may overcount or undercount actual memory and may change between MSAL versions.
        /// This method examines the snapshot's entries and scopes and does not serialize the cache.
        /// </remarks>
        /// <returns>An approximate size in bytes.</returns>
        public long EstimateSizeInBytes()
        {
            return TokenCacheSnapshotSizeEstimator.Estimate(_state, _unknownNodesEstimatedSize);
        }

        internal IDictionary<string, JsonNode> Restore(ITokenCacheAccessor accessor)
        {
            accessor.Clear();
            foreach (var item in _state.AccessTokens.Values)
            {
                accessor.SaveAccessToken(item.CloneForSnapshot());
            }
            foreach (var item in _state.RefreshTokens.Values)
            {
                accessor.SaveRefreshToken(item.CloneForSnapshot());
            }
            foreach (var item in _state.IdTokens.Values)
            {
                accessor.SaveIdToken(item.CloneForSnapshot());
            }
            foreach (var item in _state.Accounts.Values)
            {
                accessor.SaveAccount(item.CloneForSnapshot());
            }
            foreach (var item in _state.AppMetadata.Values)
            {
                accessor.SaveAppMetadata(item.CloneForSnapshot());
            }
            return CloneUnknownNodes(_state.UnknownNodes);
        }

        internal static IDictionary<string, JsonNode> CloneUnknownNodes(
            IDictionary<string, JsonNode> nodes,
            bool detachFromOriginalDocument = false)
        {
            return CloneUnknownNodes(nodes, detachFromOriginalDocument, out _);
        }

        private static IDictionary<string, JsonNode> CloneUnknownNodes(
            IDictionary<string, JsonNode> nodes,
            bool detachFromOriginalDocument,
            out long estimatedSize)
        {
            estimatedSize = 0;
            var copy = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
            if (nodes is not null)
            {
                foreach (var pair in nodes)
                {
                    // A JsonNode clone can retain the complete source document, not just this subtree.
                    JsonNode node;
                    if (detachFromOriginalDocument)
                    {
                        string json = pair.Value?.ToJsonString();
                        node = json is null ? null : JsonNode.Parse(json, pair.Value.Options);
                        estimatedSize += TokenCacheSnapshotSizeEstimator.EstimateUnknownNode(pair.Key, json);
                    }
                    else
                    {
                        node = CloneNode(pair.Value);
                    }
                    copy.Add(pair.Key, node);
                }
            }
            return copy;
        }

        private static JsonNode CloneNode(JsonNode node)
        {
#if NET8_0_OR_GREATER
            return node?.DeepClone();
#else
            // The legacy targets use System.Text.Json 6, before JsonNode.DeepClone.
            if (node is JsonObject obj)
            {
                var copy = new JsonObject(obj.Options);
                foreach (var pair in obj)
                {
                    copy.Add(pair.Key, CloneNode(pair.Value));
                }
                return copy;
            }
            if (node is JsonArray array)
            {
                var copy = new JsonArray(array.Options);
                foreach (var item in array)
                {
                    copy.Add(CloneNode(item));
                }
                return copy;
            }
            // Unknown fields enter the cache through the JSON parser, so scalar
            // nodes contain immutable JsonElements rather than application objects.
            return node is null ? null : JsonValue.Create(node.GetValue<JsonElement>().Clone());
#endif
        }
    }
}
