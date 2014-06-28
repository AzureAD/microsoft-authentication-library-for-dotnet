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
using System.Collections.Concurrent;
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
    /// Notification for certain token cache interactions during token acquisition.
    /// </summary>
    /// <param name="args"></param>
    public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);

    /// <summary>
    /// Token cache class used by <see cref="AuthenticationContext"/> to store access and refresh tokens.
    /// </summary>
#if ADAL_WINRT
    public sealed class TokenCache
#else
    public class TokenCache
#endif
    {
        private const int SchemaVersion = 1;
        
        private const string Delimiter = ":::";
        private const string LocalSettingsContainerName = "ActiveDirectoryAuthenticationLibrary";

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
            this.TokenCacheStore = new ConcurrentDictionary<TokenCacheKey, string>();
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
        public TokenCacheNotification BeforeAccess { get; set; }


        /// <summary>
        /// Notification method called before any library method writes to the cache. This notification can be used to reload
        /// the cache state from a row in database and lock that row. That database row can then be unlocked in <see cref="AfterAccess"/> notification.
        /// </summary>
        public TokenCacheNotification BeforeWrite { get; set; }

        /// <summary>
        /// Notification method called after any library method accesses the cache.
        /// </summary>
        public TokenCacheNotification AfterAccess { get; set; }

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
                    writer.Write(string.Format("{1}{0}{2}{0}{3}{0}{4}", Delimiter, kvp.Key.Authority, kvp.Key.Resource, kvp.Key.ClientId, (int)kvp.Key.SubjectType));
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
                    int schemaVersion = reader.ReadInt32();
                    if (schemaVersion != SchemaVersion)
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
                                                SubjectType = (TokenSubjectType)int.Parse(kvpElements[3]),
                                                ExpiresOn = result.ExpiresOn,
                                                IsMultipleResourceRefreshToken = result.IsMultipleResourceRefreshToken,
                                            };

                        if (result.UserInfo != null)
                        {
                            key.DisplayableId = result.UserInfo.DisplayableId;
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
                        TenantId = result.TenantId,
                        UniqueId = kvp.Key.UniqueId,                                    
                        ClientId = kvp.Key.ClientId,
                        DisplayableId = kvp.Key.DisplayableId,
                        ExpiresOn = kvp.Key.ExpiresOn,
                        FamilyName = (result.UserInfo != null) ? result.UserInfo.FamilyName : null,
                        GivenName = (result.UserInfo != null) ? result.UserInfo.GivenName : null,
                        IdentityProvider = (result.UserInfo != null) ? result.UserInfo.IdentityProvider : null,
                        IsMultipleResourceRefreshToken = kvp.Key.IsMultipleResourceRefreshToken,
                        AccessToken = result.AccessToken,
                        RefreshToken = result.RefreshToken,
                        IdToken = result.IdToken
                        // We do not add SubjectType to TokenCacheItem
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
                    (result.UserInfo == null || item.FamilyName == result.UserInfo.FamilyName) &&
                    (result.UserInfo == null || item.GivenName == result.UserInfo.GivenName) &&
                    (result.UserInfo == null || item.IdentityProvider == result.UserInfo.IdentityProvider) &&
                    item.IsMultipleResourceRefreshToken == kvp.Key.IsMultipleResourceRefreshToken &&
                    item.AccessToken == result.AccessToken &&
                    item.RefreshToken == result.RefreshToken &&
                    item.IdToken == result.IdToken)
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
        /// Clears the cache by deleting all the items. Note that if the cache is the default shared cache, clearing it would
        /// impact all the instances of <see cref="AuthenticationContext"/> which share that cache.
        /// </summary>
        public void Clear()
        {
            this.TokenCacheStore.Clear();
        }

        internal void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            if (AfterAccess != null)
            {
                AfterAccess(args);
            }
        }

        internal void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            if (BeforeAccess != null)
            {
                BeforeAccess(args);
            }
        }

        internal void OnBeforeWrite(TokenCacheNotificationArgs args)
        {
            if (BeforeWrite != null)
            {
                BeforeWrite(args);
            }
        }

#if ADAL_WINRT
        private static void DefaultTokenCache_BeforeAccess(TokenCacheNotificationArgs args)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
            try
            {
                byte[] state = LocalSettingsHelper.GetCacheValue(localSettings.Containers[LocalSettingsContainerName].Values);
                DefaultShared.Deserialize(state);
            }
            catch
            {
                // Ignore as the cache seems to be corrupt
            }
        }
        private static void DefaultTokenCache_AfterAccess(TokenCacheNotificationArgs args)
        {
            if (DefaultShared.HasStateChanged)
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
                LocalSettingsHelper.SetCacheValue(localSettings.Containers[LocalSettingsContainerName].Values, DefaultShared.Serialize());
                DefaultShared.HasStateChanged = false;
            }
        }
#endif
    }
}