// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
#if SUPPORTS_SYSTEM_TEXT_JSON
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
#if SUPPORTS_SYSTEM_TEXT_JSON
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
#if SUPPORTS_SYSTEM_TEXT_JSON
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
                foreach (var elem in GetElement(root, StorageJsonValues.CredentialTypeAccessToken))
                {
                    if (elem != null)
                    {
                        var item = MsalAccessTokenCacheItem.FromJObject(elem);
                        contract.AccessTokens[item.GetKey().ToString()] = item;
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
                        contract.RefreshTokens[item.GetKey().ToString()] = item;
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
                        contract.IdTokens[item.GetKey().ToString()] = item;
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
                        contract.Accounts[item.GetKey().ToString()] = item;
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
                        contract.AppMetadata[item.GetKey().ToString()] = item;
                    }
                }
            }

            return contract;

            // private method for enumerating collection
#if SUPPORTS_SYSTEM_TEXT_JSON
            static IEnumerable<JsonObject> GetElement(JsonObject root, string key)
            {
                foreach (var token in root[key].AsObject())
                {
                    yield return token.Value as JObject;
                }
            }
#else
            static IEnumerable<JObject> GetElement(JObject root, string key)
            {
                foreach (var token in root[key].Values())
                {
                    yield return token as JObject;
                }
            }
#endif
        }

        private static IDictionary<string, JToken> ExtractUnknownNodes(JObject root)
        {
#if SUPPORTS_SYSTEM_TEXT_JSON
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
#if SUPPORTS_SYSTEM_TEXT_JSON
                root[kvp.Key] = kvp.Value != null ? JToken.Parse(kvp.Value.ToJsonString()) : null;
#else
                root[kvp.Key] = kvp.Value;
#endif
            }
#if SUPPORTS_SYSTEM_TEXT_JSON
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
