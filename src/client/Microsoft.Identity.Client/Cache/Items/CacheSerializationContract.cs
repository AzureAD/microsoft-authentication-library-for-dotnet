// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using JObject = System.Text.Json.Nodes.JsonObject;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class CacheSerializationContract
    {
        private static readonly JsonSerializerOptions NeverIgnoreJsonOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

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

        public IDictionary<string, JsonElement> UnknownNodes { get; }

        public CacheSerializationContract(IDictionary<string, JsonElement> unknownNodes)
        {
            UnknownNodes = unknownNodes ?? new Dictionary<string, JsonElement>();
        }

        internal static CacheSerializationContract FromJsonString(string json)
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            });
            var rootElement = document.RootElement.Clone();
            var root = JsonObject.Create(rootElement);
            var unknownNodes = ExtractUnknownNodes(rootElement);

            var contract = new CacheSerializationContract(unknownNodes);

            // Access Tokens
            if (root.ContainsKey(StorageJsonValues.CredentialTypeAccessToken))
            {
                foreach (var elem in GetElement(root, StorageJsonValues.CredentialTypeAccessToken))
                {
                    if (elem != null)
                    {
                        var item = MsalAccessTokenCacheItem.FromJObject(elem);
                        contract.AccessTokens[item.CacheKey] = item;
                    }
                }
            }

            // Refresh Tokens
            if (root.ContainsKey(StorageJsonValues.CredentialTypeRefreshToken))
            {
                foreach (var elem in GetElement(root, StorageJsonValues.CredentialTypeRefreshToken))
                {
                    if (elem != null)
                    {
                        var item = MsalRefreshTokenCacheItem.FromJObject(elem);
                        contract.RefreshTokens[item.CacheKey] = item;
                    }
                }
            }

            // Id Tokens
            if (root.ContainsKey(StorageJsonValues.CredentialTypeIdToken))
            {
                foreach (var elem in GetElement(root, StorageJsonValues.CredentialTypeIdToken))
                {
                    if (elem != null)
                    {
                        var item = MsalIdTokenCacheItem.FromJObject(elem);
                        contract.IdTokens[item.CacheKey] = item;
                    }
                }
            }

            // Accounts
            if (root.ContainsKey(StorageJsonValues.AccountRootKey))
            {
                foreach (var elem in GetElement(root, StorageJsonValues.AccountRootKey))
                {
                    if (elem != null)
                    {
                        var item = MsalAccountCacheItem.FromJObject(elem);
                        contract.Accounts[item.CacheKey] = item;
                    }
                }
            }

            // App Metadata
            if (root.ContainsKey(StorageJsonValues.AppMetadata))
            {
                foreach (var elem in GetElement(root, StorageJsonValues.AppMetadata))
                {
                    if (elem != null)
                    {
                        var item = MsalAppMetadataCacheItem.FromJObject(elem);
                        contract.AppMetadata[item.CacheKey] = item;
                    }
                }
            }

            return contract;

            // private method for enumerating collection
            static IEnumerable<JsonObject> GetElement(JsonObject root, string key)
            {
                foreach (var token in root[key].AsObject())
                {
                    yield return token.Value as JObject;
                }
            }
        }

        private static IDictionary<string, JsonElement> ExtractUnknownNodes(JsonElement root)
        {
            return root.EnumerateObject()
                .Where(property => !s_knownPropertyNames.Any(p => string.Equals(property.Name, p, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(property => property.Name, property => property.Value);
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
                root[kvp.Key] = JsonNode.Parse(kvp.Value.GetRawText(), documentOptions: new JsonDocumentOptions
                {
                    AllowTrailingCommas = true
                });
            }
            return root.ToJsonString(NeverIgnoreJsonOptions);
        }
    }
}
