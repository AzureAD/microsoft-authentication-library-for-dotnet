//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
#if ADAL_WINRT
using Windows.Storage;
#endif
using System.Collections.Generic;
using System.IO;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Delegate to be called before or after any library call accesses the token cache.
    /// </summary>
    /// <param name="e"></param>
    public delegate void TokenCacheAccessNotification(TokenCacheAccessArgs e);

    /// <summary>
    /// </summary>
#if ADAL_WINRT
    public sealed class TokenCache
#else
    public class TokenCache
#endif
    {
        private const double SchemaVersion = 1.0;
        
        private const string Delimiter = ":::";
        private const string LocalSettingsContainerName = "ActiveDirectoryAuthenticationLibrary";
        private const string LocalSettingsKey = "TokenCache";

        static TokenCache()
        {
            DefaultShared = new TokenCache();

#if ADAL_WINRT
            DefaultShared.BeforeAccess = DefaultTokenCache_BeforeAccess;
            DefaultShared.AfterAccess = DefaultTokenCache_AfterAccess;
#endif
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TokenCache()
        {
            this.TokenCacheStore = new Dictionary<TokenCacheKey, string>();
        }

        /// <summary>
        /// Constructor receiving state of the cache
        /// </summary>        
        public TokenCache([ReadOnlyArray] byte[] state)
            : this()
        {
            this.Deserialize(state);
        }

        /// <summary>
        /// Static token cache shared by all instances of AuthenticationContext which do not explicitly pass a cache instance during construction.
        /// </summary>
        public static TokenCache DefaultShared { get; private set; }

        /// <summary>
        /// Notification method called before any library method accesses the cache.
        /// </summary>
        public TokenCacheAccessNotification BeforeAccess { get; set; }

        /// <summary>
        /// Notification method called after any library method accesses the cache.
        /// </summary>
        public TokenCacheAccessNotification AfterAccess { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating whether cache state has changed. ADAL methods set this flag after any change. Caller application should reset 
        /// the flag after serlizaing and persisting the state of the cache.
        /// </summary>
        public bool HasStateChanged { get; set; }
        internal IDictionary<TokenCacheKey, string> TokenCacheStore { get; private set; }

        /// <summary>
        /// Serializes current state of the cache as a blob. Caller application can persist the blob and update the state of the cache later by 
        /// passing that blob back in constructor or by calling method Deserialize.
        /// </summary>
        /// <returns>Current state of the cache as a blob</returns>
        public byte[] Serialize()
        {
            using (Stream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(SchemaVersion);
                writer.Write(this.TokenCacheStore.Count);
                foreach (KeyValuePair<TokenCacheKey, string> kvp in this.TokenCacheStore)
                {
                    writer.Write(string.Format("{1}{0}{2}{0}{3}", Delimiter, kvp.Key.Authority, kvp.Key.Resource, kvp.Key.ClientId));
                    writer.Write(kvp.Value);
                }

                int length = (int)stream.Position;
                stream.Position = 0;
                BinaryReader reader = new BinaryReader(stream);
                return reader.ReadBytes(length);
            }
        }

        /// <summary>
        /// Deserializes state of the cache. The state should be the blob received earlier by calling the method Serialize.
        /// </summary>
        /// <param name="state">State of the cache as a blob</param>
        public void Deserialize([ReadOnlyArray] byte[] state)
        {
            if (state != null)
            {
                using (Stream stream = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(state);
                    writer.Flush();
                    stream.Position = 0;

                    BinaryReader reader = new BinaryReader(stream);
                    double schemaVersion = reader.ReadDouble();
                    if (Math.Abs(schemaVersion - SchemaVersion) > 0.001)
                    {
                        // The version of the serialized cache does not match the current schema
                        return;
                    }

                    this.TokenCacheStore.Clear();
                    int count = reader.ReadInt32();
                    for (int n = 0; n < count; n++)
                    {
                        string keyString = reader.ReadString();
                        string value = reader.ReadString();

                        string[] kvpElements = keyString.Split(new[] { Delimiter }, StringSplitOptions.None);
                        AuthenticationResult result = TokenCacheEncoding.DecodeCacheValue(value);
                        TokenCacheKey key = new TokenCacheKey
                                            {
                                                Authority = kvpElements[0],
                                                Resource = kvpElements[1],
                                                ClientId = kvpElements[2],
                                                ExpiresOn = result.ExpiresOn,
                                                IsMultipleResourceRefreshToken = result.IsMultipleResourceRefreshToken,
                                                TenantId = result.TenantId,
                                            };

                        if (result.UserInfo != null)
                        {
                            key.DisplayableId = result.UserInfo.DisplayableId;
                            key.FamilyName = result.UserInfo.FamilyName;
                            key.GivenName = result.UserInfo.GivenName;
                            key.IdentityProviderName = result.UserInfo.IdentityProvider;
                            key.UniqueId = result.UserInfo.UniqueId;
                        }

                        this.TokenCacheStore.Add(key, value);
                    }
                }
            }
        }

        /// <summary>
        /// Reads a copy of the list of all items in the cache. 
        /// </summary>
        /// <returns>The items in the cache</returns>
        public IEnumerable<TokenCacheItem> ReadItems()
        {
            List<TokenCacheItem> items = new List<TokenCacheItem>();
            foreach (KeyValuePair<TokenCacheKey, string> kvp in this.TokenCacheStore)
            {
                AuthenticationResult result = TokenCacheEncoding.DecodeCacheValue(kvp.Value);
                TokenCacheItem item = new TokenCacheItem
                    {
                        Authority = kvp.Key.Authority,
                        Resource = kvp.Key.Resource,
                        TenantId = kvp.Key.TenantId,
                        UniqueId = kvp.Key.UniqueId,                                    
                        ClientId = kvp.Key.ClientId,
                        DisplayableId = kvp.Key.DisplayableId,
                        ExpiresOn = kvp.Key.ExpiresOn,
                        FamilyName = kvp.Key.FamilyName,
                        GivenName = kvp.Key.GivenName,
                        IdentityProviderName = kvp.Key.IdentityProviderName,
                        IsMultipleResourceRefreshToken = kvp.Key.IsMultipleResourceRefreshToken,
                        AccessToken = result.AccessToken,
                        RefreshToken = result.RefreshToken
                    };

                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Deletes an item from the cache.
        /// </summary>
        /// <param name="item">The item to delete from the cache</param>
        public void DeleteItem(TokenCacheItem item)
        {
            List<TokenCacheKey> toRemoveKeys = new List<TokenCacheKey>();
            foreach (KeyValuePair<TokenCacheKey, string> kvp in this.TokenCacheStore)
            {
                AuthenticationResult result = TokenCacheEncoding.DecodeCacheValue(kvp.Value);
                if(item.Authority == kvp.Key.Authority &&
                    item.ClientId == kvp.Key.ClientId &&
                    item.DisplayableId == kvp.Key.DisplayableId &&
                    item.ExpiresOn == kvp.Key.ExpiresOn &&
                    item.FamilyName == kvp.Key.FamilyName &&
                    item.GivenName == kvp.Key.GivenName &&
                    item.IdentityProviderName == kvp.Key.IdentityProviderName &&
                    item.IsMultipleResourceRefreshToken == kvp.Key.IsMultipleResourceRefreshToken &&
                    item.AccessToken == result.AccessToken &&
                    item.RefreshToken == result.RefreshToken)
                {
                    toRemoveKeys.Add(kvp.Key);
                }               
            }

            foreach (TokenCacheKey key in toRemoveKeys)
            {
                this.TokenCacheStore.Remove(key);
            }

            this.HasStateChanged = true;
        }

        /// <summary>
        /// Clears the cache by deleting all the items
        /// </summary>
        public void ClearAll()
        {
            this.TokenCacheStore.Clear();
        }

        internal void OnAfterAccess(TokenCacheAccessArgs e)
        {
            if (AfterAccess != null)
            {
                AfterAccess(e);
            }
        }

        internal void OnBeforeAccess(TokenCacheAccessArgs e)
        {
            if (BeforeAccess != null)
            {
                BeforeAccess(e);
            }
        }

#if ADAL_WINRT
        private static void DefaultTokenCache_BeforeAccess(TokenCacheAccessArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
            if (localSettings.Containers[LocalSettingsContainerName].Values.ContainsKey(LocalSettingsKey))
            {
                try
                {
                    byte[] state = (byte[])localSettings.Containers[LocalSettingsContainerName].Values[LocalSettingsKey];
                    DefaultShared.Deserialize(CryptographyHelper.Decrypt(state));
                }
                catch
                {
                    // Ignore as the cache seems to be corrupt
                }
            }
        }
        private static void DefaultTokenCache_AfterAccess(TokenCacheAccessArgs e)
        {
            if (DefaultShared.HasStateChanged)
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
                localSettings.Containers[LocalSettingsContainerName].Values[LocalSettingsKey] = CryptographyHelper.Encrypt(DefaultShared.Serialize());
                DefaultShared.HasStateChanged = false;
            }
        }
#endif
    }
}