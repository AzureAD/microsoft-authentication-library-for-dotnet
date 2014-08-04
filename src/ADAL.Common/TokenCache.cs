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
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
#if ADAL_NET
    public class TokenCache
#else
    public sealed partial class TokenCache
#endif
    {
        internal delegate Task<AuthenticationResult> RefreshAccessTokenAsync(AuthenticationResult result, string resource, ClientKey clientKey, CallState callState);

        private const int SchemaVersion = 2;
        
        private const string Delimiter = ":::";
        private const string LocalSettingsContainerName = "ActiveDirectoryAuthenticationLibrary";

        internal readonly IDictionary<TokenCacheKey, AuthenticationResult> tokenCacheDictionary;

        // We do not want to return near expiry tokens, this is why we use this hard coded setting to refresh tokens which are close to expiration.
        private const int ExpirationMarginInMinutes = 5;

        static TokenCache()
        {
            DefaultShared = new TokenCache();

#if !ADAL_NET
            DefaultShared.BeforeAccess = DefaultTokenCache_BeforeAccess;
            DefaultShared.AfterAccess = DefaultTokenCache_AfterAccess;

            DefaultTokenCache_BeforeAccess(null);
#endif
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TokenCache()
        {
            this.tokenCacheDictionary = new ConcurrentDictionary<TokenCacheKey, AuthenticationResult>();
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

        /// <summary>
        /// Gets the nunmber of items in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                return this.tokenCacheDictionary.Count;
            }
        }

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
                writer.Write(this.tokenCacheDictionary.Count);
                foreach (KeyValuePair<TokenCacheKey, AuthenticationResult> kvp in this.tokenCacheDictionary)
                {
                    writer.Write(string.Format("{1}{0}{2}{0}{3}{0}{4}", Delimiter, kvp.Key.Authority, kvp.Key.Resource, kvp.Key.ClientId, (int)kvp.Key.TokenSubjectType));
                    writer.Write(kvp.Value.Serialize());
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
            if (state == null)
            {
                this.tokenCacheDictionary.Clear();
                return;
            }

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

                this.tokenCacheDictionary.Clear();
                int count = reader.ReadInt32();
                for (int n = 0; n < count; n++)
                {
                    string keyString = reader.ReadString();

                    string[] kvpElements = keyString.Split(new[] { Delimiter }, StringSplitOptions.None);
                    AuthenticationResult result = AuthenticationResult.Deserialize(reader.ReadString());
                    TokenCacheKey key = new TokenCacheKey(kvpElements[0], kvpElements[1], kvpElements[2], (TokenSubjectType)int.Parse(kvpElements[3]), result.UserInfo);

                    this.tokenCacheDictionary.Add(key, result);
                }
            }
        }

        /// <summary>
        /// Reads a copy of the list of all items in the cache. 
        /// </summary>
        /// <returns>The items in the cache</returns>
#if ADAL_NET
        public virtual IEnumerable<TokenCacheItem> ReadItems()
#else
        public IEnumerable<TokenCacheItem> ReadItems()
#endif
        {
            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs { TokenCache = this };
            this.OnBeforeAccess(args);

            List<TokenCacheItem> items = new List<TokenCacheItem>();
            foreach (KeyValuePair<TokenCacheKey, AuthenticationResult> kvp in this.tokenCacheDictionary)
            {
                items.Add(new TokenCacheItem(kvp.Key, kvp.Value));
            }

            this.OnAfterAccess(args);

            return items;
        }

        /// <summary>
        /// Deletes an item from the cache.
        /// </summary>
        /// <param name="item">The item to delete from the cache</param>
#if ADAL_NET
        public virtual void DeleteItem(TokenCacheItem item)
#else
        public void DeleteItem(TokenCacheItem item)
#endif
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    Resource = item.Resource,
                    ClientId = item.ClientId,
                    UniqueId = item.UniqueId,
                    DisplayableId = item.DisplayableId
                };

            this.OnBeforeAccess(args);
            this.OnBeforeWrite(args);

            TokenCacheKey toRemoveKey = this.tokenCacheDictionary.Keys.FirstOrDefault(item.Match);
            if (toRemoveKey != null)
            {
                this.tokenCacheDictionary.Remove(toRemoveKey);
            }

            this.HasStateChanged = true;
            this.OnAfterAccess(args);
        }

        /// <summary>
        /// Clears the cache by deleting all the items. Note that if the cache is the default shared cache, clearing it would
        /// impact all the instances of <see cref="AuthenticationContext"/> which share that cache.
        /// </summary>
#if ADAL_NET
        public virtual void Clear()
#else
        public void Clear()
#endif
        {
            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs { TokenCache = this };
            this.OnBeforeAccess(args);
            this.OnBeforeWrite(args);
            this.tokenCacheDictionary.Clear();
            this.HasStateChanged = true;
            this.OnAfterAccess(args);
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

        internal AuthenticationResult LoadFromCache(string authority, string resource, string clientId, TokenSubjectType subjectType, string uniqueId, string displayableId, CallState callState)
        {
            AuthenticationResult result = null;

            KeyValuePair<TokenCacheKey, AuthenticationResult>? kvp = this.LoadSingleItemFromCache(authority, resource, clientId, subjectType, uniqueId, displayableId);

            if (kvp.HasValue)
            {
                TokenCacheKey cacheKey = kvp.Value.Key;
                result = kvp.Value.Value;
                bool tokenMarginallyExpired = (result.ExpiresOn <= DateTime.UtcNow + TimeSpan.FromMinutes(ExpirationMarginInMinutes));

                if (tokenMarginallyExpired || !cacheKey.ResourceEquals(resource))
                {
                    result.AccessToken = null;
                }

                if (result.AccessToken == null && result.RefreshToken == null)
                {
                    this.tokenCacheDictionary.Remove(cacheKey);
                    this.HasStateChanged = true;
                    result = null;
                }

                if (result != null)
                {
                    Logger.Verbose(callState, "A matching token was found in the cache");
                }
            }

            return result;
        }

        internal void StoreToCache(AuthenticationResult result, string authority, string resource, string clientId, TokenSubjectType subjectType)
        {
            string uniqueId = (result.UserInfo != null) ? result.UserInfo.UniqueId : null;
            string displayableId = (result.UserInfo != null) ? result.UserInfo.DisplayableId : null;

            this.OnBeforeWrite(new TokenCacheNotificationArgs
            {
                Resource = resource,
                ClientId = clientId,
                UniqueId = uniqueId,
                DisplayableId = displayableId
            });

            TokenCacheKey tokenCacheKey = new TokenCacheKey(authority, resource, clientId, subjectType, result.UserInfo);
            this.tokenCacheDictionary[tokenCacheKey] = result;
            this.UpdateCachedMrrtRefreshTokens(result, authority, clientId, subjectType);

            this.HasStateChanged = true;
        }

        private void UpdateCachedMrrtRefreshTokens(AuthenticationResult result, string authority, string clientId, TokenSubjectType subjectType)
        {
            if (result.UserInfo != null)
            {
                List<KeyValuePair<TokenCacheKey, AuthenticationResult>> mrrtItems =
                    this.QueryCache(authority, clientId, subjectType, result.UserInfo.UniqueId, result.UserInfo.DisplayableId).Where(p => p.Value.IsMultipleResourceRefreshToken).ToList();

                foreach (KeyValuePair<TokenCacheKey, AuthenticationResult> mrrtItem in mrrtItems)
                {
                    mrrtItem.Value.RefreshToken = result.RefreshToken;
                }
            }
        }

        private KeyValuePair<TokenCacheKey, AuthenticationResult>? LoadSingleItemFromCache(string authority, string resource, string clientId, TokenSubjectType subjectType, string uniqueId, string displayableId)
        {
            // First identify all potential tokens.
            List<KeyValuePair<TokenCacheKey, AuthenticationResult>> items = this.QueryCache(authority, clientId, subjectType, uniqueId, displayableId);

            List<KeyValuePair<TokenCacheKey, AuthenticationResult>> resourceSpecificItems =
                items.Where(p => p.Key.ResourceEquals(resource)).ToList();

            int resourceValuesCount = resourceSpecificItems.Count();
            KeyValuePair<TokenCacheKey, AuthenticationResult>? returnValue = null;
            if (resourceValuesCount == 1)
            {
                returnValue = resourceSpecificItems.First();
            }
            else if (resourceValuesCount == 0)
            {
                // There are no resource specific tokens.  Choose any of the MRRT tokens if there are any.
                List<KeyValuePair<TokenCacheKey, AuthenticationResult>> mrrtItems =
                    items.Where(p => p.Value.IsMultipleResourceRefreshToken).ToList();

                if (mrrtItems.Any())
                {
                    returnValue = mrrtItems.First();
                }
            }
            else
            {
                // There is more than one resource specific token.  It is 
                // ambiguous which one to return so throw.
                throw new AdalException(AdalError.MultipleTokensMatched);
            }

            return returnValue;
        }

        /// <summary>
        /// Queries all values in the cache that meet the passed in values, plus the 
        /// authority value that this AuthorizationContext was created with.  In every case passing
        /// null results in a wildcard evaluation.
        /// </summary>
        private List<KeyValuePair<TokenCacheKey, AuthenticationResult>> QueryCache(string authority, string clientId, TokenSubjectType subjectType, string uniqueId, string displayableId)
        {
            return this.tokenCacheDictionary.Where(
                    p =>
                        p.Key.Authority == authority
                        && (string.IsNullOrWhiteSpace(clientId) || p.Key.ClientIdEquals(clientId))
                        && (string.IsNullOrWhiteSpace(uniqueId) || p.Key.UniqueId == uniqueId)
                        && (string.IsNullOrWhiteSpace(displayableId) || p.Key.DisplayableIdEquals(displayableId))
                        && p.Key.TokenSubjectType == subjectType).ToList();
        }
    }
}