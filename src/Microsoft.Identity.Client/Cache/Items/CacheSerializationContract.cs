// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class CacheSerializationContract
    {
        private static readonly IEnumerable<string> KnownPropertyNames = new[] {
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

        public CacheSerializationContract(IDictionary<string, JToken> unkownNodes)
        {
            UnknownNodes = unkownNodes ?? new Dictionary<string, JToken>();
        }

        internal static CacheSerializationContract FromJsonString(string json)
        {
            JObject root = JObject.Parse(json);
            var unkownNodes = ExtractUnkownNodes(root);

            var contract = new CacheSerializationContract(unkownNodes);

            // Access Tokens
            if (root.ContainsKey(StorageJsonValues.CredentialTypeAccessToken))
            {
                foreach (var token in root[StorageJsonValues.CredentialTypeAccessToken]
                    .Values())
                {
                    if (token is JObject j)
                    {
                        var item = MsalAccessTokenCacheItem.FromJObject(j);
                        contract.AccessTokens[item.GetKey().ToString()] = item;
                    }
                }
            }

            // Refresh Tokens
            if (root.ContainsKey(StorageJsonValues.CredentialTypeRefreshToken))
            {
                foreach (var token in root[StorageJsonValues.CredentialTypeRefreshToken]
                    .Values())
                {
                    if (token is JObject j)
                    {
                        var item = MsalRefreshTokenCacheItem.FromJObject(j);
                        contract.RefreshTokens[item.GetKey().ToString()] = item;
                    }
                }
            }

            // Id Tokens
            if (root.ContainsKey(StorageJsonValues.CredentialTypeIdToken))
            {
                foreach (var token in root[StorageJsonValues.CredentialTypeIdToken]
                    .Values())
                {
                    if (token is JObject j)
                    {
                        var item = MsalIdTokenCacheItem.FromJObject(j);
                        contract.IdTokens[item.GetKey().ToString()] = item;
                    }
                }
            }

            // Accounts
            if (root.ContainsKey(StorageJsonValues.AccountRootKey))
            {
                foreach (var token in root[StorageJsonValues.AccountRootKey]
                    .Values())
                {
                    if (token is JObject j)
                    {
                        var item = MsalAccountCacheItem.FromJObject(j);
                        contract.Accounts[item.GetKey().ToString()] = item;
                    }
                }
            }

            // App Metadata
            if (root.ContainsKey(StorageJsonValues.AppMetadata))
            {
                foreach (var token in root[StorageJsonValues.AppMetadata]
                    .Values())
                {
                    if (token is JObject j)
                    {
                        var item = MsalAppMetadataCacheItem.FromJObject(j);
                        contract.AppMetadata[item.GetKey().ToString()] = item;
                    }
                }
            }

            return contract;
        }
        private static IDictionary<string, JToken> ExtractUnkownNodes(JObject root)
        {
            return (root as IDictionary<string, JToken>)
                .Where(kvp => !KnownPropertyNames.Any(p => string.Equals(kvp.Key, p, StringComparison.OrdinalIgnoreCase)))
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
                root[kvp.Key] = kvp.Value;
            }

            return root.ToString();
        }
    }
}
