// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
#if NET6_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Nodes;
using JObject = System.Text.Json.Nodes.JsonObject;
using JToken = System.Text.Json.Nodes.JsonNode;
#else
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class CacheSerializationContract
    {
#if NET6_0_OR_GREATER
        private static readonly JsonSerializerOptions NeverIgnoreJsonOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };
#endif

        private static readonly IEnumerable<string> s_knownPropertyNames = new[] {
                StorageJsonValues.CredentialTypeAccessToken,
                StorageJsonValues.CredentialTypeRefreshToken,
                StorageJsonValues.CredentialTypeIdToken,
                StorageJsonValues.AccountRootKey,
                StorageJsonValues.AppMetadata};

        public Dictionary<string, MsalAccessTokenCacheItem> AccessTokens { get; set; } =
            new Dictionary<string, MsalAccessTokenCacheItem>();

        public Dictionary<string, MsalRefreshTokenCacheItem> RefreshTokens { get; set; } =
            new Dictionary<string, MsalRefreshTokenCacheItem>();

        public Dictionary<string, MsalIdTokenCacheItem> IdTokens { get; set; } =
            new Dictionary<string, MsalIdTokenCacheItem>();

        public Dictionary<string, MsalAccountCacheItem> Accounts { get; set; } =
            new Dictionary<string, MsalAccountCacheItem>();

        public Dictionary<string, MsalAppMetadataCacheItem> AppMetadata { get; set; } =
            new Dictionary<string, MsalAppMetadataCacheItem>();

        public IDictionary<string, JToken> UnknownNodes { get; }

        public CacheSerializationContract(IDictionary<string, JToken> unknownNodes)
        {
            UnknownNodes = unknownNodes ?? new Dictionary<string, JToken>();
        }

        internal static CacheSerializationContract FromJsonString(string json)
        {
#if NET6_0_OR_GREATER
            var root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            }).AsObject();
#else
            var root = JObject.Parse(json);
#endif
            var unknownNodes = ExtractUnknownNodes(root);

            var contract = new CacheSerializationContract(unknownNodes);

            // Access Tokens
            if (root.ContainsKey(StorageJsonValues.CredentialTypeAccessToken))
            {
#if NET6_0_OR_GREATER
                foreach (var token in root[StorageJsonValues.CredentialTypeAccessToken].AsObject())
                {
                    if (token.Value is JObject j)
#else
                foreach (var token in root[StorageJsonValues.CredentialTypeAccessToken].Values())
                {
                    if (token is JObject j)
#endif
                    {
                        var item = MsalAccessTokenCacheItem.FromJObject(j);
                        contract.AccessTokens[item.GetKey().ToString()] = item;
                    }
                }
            }

            // Refresh Tokens
            if (root.ContainsKey(StorageJsonValues.CredentialTypeRefreshToken))
            {
#if NET6_0_OR_GREATER
                foreach (var token in root[StorageJsonValues.CredentialTypeRefreshToken].AsObject())
                {
                    if (token.Value is JObject j)
#else
                foreach (var token in root[StorageJsonValues.CredentialTypeRefreshToken].Values())
                {
                    if (token is JObject j)
#endif
                    {
                        var item = MsalRefreshTokenCacheItem.FromJObject(j);
                        contract.RefreshTokens[item.GetKey().ToString()] = item;
                    }
                }
            }

            // Id Tokens
            if (root.ContainsKey(StorageJsonValues.CredentialTypeIdToken))
            {
#if NET6_0_OR_GREATER
                foreach (var token in root[StorageJsonValues.CredentialTypeIdToken].AsObject())
                {
                    if (token.Value is JObject j)
#else
                foreach (var token in root[StorageJsonValues.CredentialTypeIdToken].Values())
                {
                    if (token is JObject j)
#endif
                    {
                        var item = MsalIdTokenCacheItem.FromJObject(j);
                        contract.IdTokens[item.GetKey().ToString()] = item;
                    }
                }
            }

            // Accounts
            if (root.ContainsKey(StorageJsonValues.AccountRootKey))
            {
#if NET6_0_OR_GREATER
                foreach (var token in root[StorageJsonValues.AccountRootKey].AsObject())
                {
                    if (token.Value is JObject j)
#else
                foreach (var token in root[StorageJsonValues.AccountRootKey].Values())
                {
                    if (token is JObject j)
#endif
                    {
                        var item = MsalAccountCacheItem.FromJObject(j);
                        contract.Accounts[item.GetKey().ToString()] = item;
                    }
                }
            }

            // App Metadata
            if (root.ContainsKey(StorageJsonValues.AppMetadata))
            {
#if NET6_0_OR_GREATER
                foreach (var token in root[StorageJsonValues.AppMetadata].AsObject())
                {
                    if (token.Value is JObject j)
#else
                foreach (var token in root[StorageJsonValues.AppMetadata].Values())
                {
                    if (token is JObject j)
#endif
                    {
                        var item = MsalAppMetadataCacheItem.FromJObject(j);
                        contract.AppMetadata[item.GetKey().ToString()] = item;
                    }
                }
            }

            return contract;
        }

        private static IDictionary<string, JToken> ExtractUnknownNodes(JObject root)
        {
#if NET6_0_OR_GREATER
            return root
#else
            return (root as IDictionary<string, JToken>)
#endif
                .Where(kvp => !s_knownPropertyNames.Any(p => string.Equals(kvp.Key, p, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        internal string ToJsonString()
        {
            JObject root = new JObject();

            // Access Tokens
            var accessTokensRoot = new JObject();
            foreach (var kvp in AccessTokens)
            {
                accessTokensRoot[kvp.Key] = kvp.Value.ToJObject();
            }

            root[StorageJsonValues.CredentialTypeAccessToken] = accessTokensRoot;

            // Refresh Tokens
            var refreshTokensRoot = new JObject();
            foreach (var kvp in RefreshTokens)
            {
                refreshTokensRoot[kvp.Key] = kvp.Value.ToJObject();
            }

            root[StorageJsonValues.CredentialTypeRefreshToken] = refreshTokensRoot;

            // Id Tokens
            var idTokensRoot = new JObject();
            foreach (var kvp in IdTokens)
            {
                idTokensRoot[kvp.Key] = kvp.Value.ToJObject();
            }

            root[StorageJsonValues.CredentialTypeIdToken] = idTokensRoot;

            // Accounts
            var accountsRoot = new JObject();
            foreach (var kvp in Accounts)
            {
                accountsRoot[kvp.Key] = kvp.Value.ToJObject();
            }

            root[StorageJsonValues.AccountRootKey] = accountsRoot;

            // App Metadata
            var appMetadataRoot = new JObject();
            foreach (var kvp in AppMetadata)
            {
                appMetadataRoot[kvp.Key] = kvp.Value.ToJObject();
            }

            root[StorageJsonValues.AppMetadata] = appMetadataRoot;

            // Anything else
            foreach (var kvp in UnknownNodes)
            {
#if NET6_0_OR_GREATER
                root[kvp.Key] = kvp.Value != null ? JToken.Parse(kvp.Value.ToJsonString()) : null;
#else
                root[kvp.Key] = kvp.Value;
#endif
            }
#if NET6_0_OR_GREATER
            return root.ToJsonString(NeverIgnoreJsonOptions);
#else
            return JsonConvert.SerializeObject(
                root,
                Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include
                });
#endif
        }
    }
}
