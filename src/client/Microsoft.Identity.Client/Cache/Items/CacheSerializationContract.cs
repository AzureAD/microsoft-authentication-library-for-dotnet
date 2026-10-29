// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Identity.Client.Internal;
using JObject = System.Text.Json.Nodes.JsonObject;
using JToken = System.Text.Json.Nodes.JsonNode;

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

        public IDictionary<string, JToken> UnknownNodes { get; }

        public CacheSerializationContract(IDictionary<string, JToken> unknownNodes)
        {
            UnknownNodes = unknownNodes ?? new Dictionary<string, JToken>();
        }

        internal static CacheSerializationContract FromJsonString(string json)
        {
            var root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            }).AsObject();
            RestoreSharedClientInfo(root);
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
        }

        private static void RestoreSharedClientInfo(JObject root)
        {
            // Other V3 producers store client_info only on an account or refresh token.
            // Recover missing provenance only from an exact, verified sibling identity.
            var groups = s_knownPropertyNames
                .Where(key => key != StorageJsonValues.AppMetadata && root.ContainsKey(key))
                .SelectMany(key => GetElement(root, key))
                .Where(item => item is not null)
                .GroupBy(item => (
                    HomeAccountId: item[StorageJsonKeys.HomeAccountId]?.GetValue<string>(),
                    Environment: item[StorageJsonKeys.Environment]?.GetValue<string>()?.ToLowerInvariant()));

            foreach (var group in groups)
            {
                if (string.IsNullOrEmpty(group.Key.HomeAccountId) ||
                    string.IsNullOrEmpty(group.Key.Environment) ||
                    group.Any(item => item.ContainsKey(StorageJsonKeys.SubjectKeyVersion)))
                {
                    continue;
                }

                var missingClientInfo = group
                    .Where(item => string.IsNullOrEmpty(item[StorageJsonKeys.ClientInfo]?.GetValue<string>()))
                    .ToList();
                if (missingClientInfo.Count == 0)
                {
                    continue;
                }

                string verifiedClientInfo = null;
                bool conflictingIdentity = false;
                foreach (var item in group)
                {
                    string rawClientInfo = item[StorageJsonKeys.ClientInfo]?.GetValue<string>();
                    if (string.IsNullOrEmpty(rawClientInfo))
                    {
                        continue;
                    }

                    var clientInfo = ClientInfo.CreateFromJson(rawClientInfo);
                    if (string.IsNullOrEmpty(clientInfo.UniqueObjectIdentifier) ||
                        string.IsNullOrEmpty(clientInfo.UniqueTenantIdentifier) ||
                        !string.Equals(group.Key.HomeAccountId, clientInfo.ToAccountIdentifier(), StringComparison.Ordinal))
                    {
                        conflictingIdentity = true;
                        break;
                    }

                    verifiedClientInfo = rawClientInfo;
                }

                if (verifiedClientInfo is not null && !conflictingIdentity)
                {
                    foreach (var item in missingClientInfo)
                    {
                        item[StorageJsonKeys.ClientInfo] = verifiedClientInfo;
                    }
                }
            }
        }

        private static IEnumerable<JObject> GetElement(JObject root, string key)
        {
            foreach (var token in root[key].AsObject())
            {
                yield return token.Value as JObject;
            }
        }

        private static IDictionary<string, JToken> ExtractUnknownNodes(JObject root)
        {
            return root
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
                root[kvp.Key] = kvp.Value != null ? JToken.Parse(kvp.Value.ToJsonString()) : null;
            }
            return root.ToJsonString(NeverIgnoreJsonOptions);
        }
    }
}
