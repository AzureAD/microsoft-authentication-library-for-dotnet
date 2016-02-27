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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    /// <summary>
    ///     accessToken cache class used by <see cref="AuthenticationContext" /> to store access and refresh tokens.
    /// </summary>
    public class TokenCache
    {
        /// <summary>
        ///     Notification for certain token cache interactions during token acquisition.
        /// </summary>
        /// <param name="args">Arguments related to the cache item impacted</param>
        public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);

        private const int SchemaVersion = 1;
        private const string Delimiter = ":::";
        // We do not want to return near expiry tokens, this is why we use this hard coded setting to refresh tokens which are close to expiration.
        private const int ExpirationMarginInMinutes = 5;
        internal readonly IDictionary<TokenCacheKey, AuthenticationResultEx> tokenCacheDictionary;
        private volatile bool hasStateChanged;
        private bool _restrictToSingleUser;

        static TokenCache()
        {
            DefaultSharedUserTokenCache = new TokenCache
            {
                BeforeAccess = PlatformPlugin.TokenCachePlugin.BeforeAccess,
                AfterAccess = PlatformPlugin.TokenCachePlugin.AfterAccess
            };


            DefaultSharedAppTokenCache = new TokenCache
            {
                BeforeAccess = PlatformPlugin.TokenCachePlugin.BeforeAccess,
                AfterAccess = PlatformPlugin.TokenCachePlugin.AfterAccess
            };
        }

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public TokenCache()
        {
            this.tokenCacheDictionary = new ConcurrentDictionary<TokenCacheKey, AuthenticationResultEx>();
        }

        /// <summary>
        ///     Constructor receiving state of the cache
        /// </summary>
        public TokenCache(byte[] state)
            : this()
        {
            this.Deserialize(state);
        }

        /// <summary>
        ///     Static user token cache shared by all instances of application which do not explicitly pass a cache instance
        ///     during construction.
        /// </summary>
        public static TokenCache DefaultSharedUserTokenCache { get; private set; }


        /// <summary>
        ///     Static client token cache shared by all instances of ConfidentialClientApplication which do not explicitly pass a cache instance
        ///     during construction.
        /// </summary>
        public static TokenCache DefaultSharedAppTokenCache { get; private set; }

        /// <summary>
        ///     Notification method called before any library method accesses the cache.
        /// </summary>
        public TokenCacheNotification BeforeAccess { get; set; }

        /// <summary>
        ///     Notification method called before any library method writes to the cache. This notification can be used to reload
        ///     the cache state from a row in database and lock that row. That database row can then be unlocked in
        ///     <see cref="AfterAccess" /> notification.
        /// </summary>
        public TokenCacheNotification BeforeWrite { get; set; }

        /// <summary>
        ///     Notification method called after any library method accesses the cache.
        /// </summary>
        public TokenCacheNotification AfterAccess { get; set; }

        /// <summary>
        ///     Gets or sets the flag indicating whether cache state has changed. ADAL methods set this flag after any change.
        ///     Caller application should reset
        ///     the flag after serializing and persisting the state of the cache.
        /// </summary>
        public bool HasStateChanged
        {
            get { return this.hasStateChanged; }

            set { this.hasStateChanged = value; }
        }

        /// <summary>
        ///     Gets the nunmber of items in the cache.
        /// </summary>
        public int Count
        {
            get { return this.tokenCacheDictionary.Count; }
        }

        /// <summary>
        ///     Serializes current state of the cache as a blob. Caller application can persist the blob and update the state of
        ///     the cache later by
        ///     passing that blob back in constructor or by calling method Deserialize.
        /// </summary>
        /// <returns>Current state of the cache as a blob</returns>
        public byte[] Serialize()
        {
            using (Stream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(SchemaVersion);
                PlatformPlugin.Logger.Information(null,
                    string.Format("Serializing token cache with {0} items.", this.tokenCacheDictionary.Count));
                writer.Write(this.tokenCacheDictionary.Count);
                foreach (KeyValuePair<TokenCacheKey, AuthenticationResultEx> kvp in this.tokenCacheDictionary)
                {
                    writer.Write(string.Format("{1}{0}{2}{0}{3}{0}{4}", Delimiter, kvp.Key.Authority,
                        kvp.Key.Scope.CreateSingleStringFromSet(), kvp.Key.ClientId, kvp.Key.Policy));
                    writer.Write(kvp.Value.Serialize());
                }

                int length = (int)stream.Position;
                stream.Position = 0;
                BinaryReader reader = new BinaryReader(stream);
                return reader.ReadBytes(length);
            }
        }

        /// <summary>
        ///     Deserializes state of the cache. The state should be the blob received earlier by calling the method Serialize.
        /// </summary>
        /// <param name="state">State of the cache as a blob</param>
        public void Deserialize(byte[] state)
        {
            if (state == null || state.Length == 0)
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
                    PlatformPlugin.Logger.Warning(null,
                        "The version of the persistent state of the cache does not match the current schema, so skipping deserialization.");
                    return;
                }

                this.tokenCacheDictionary.Clear();
                int count = reader.ReadInt32();
                for (int n = 0; n < count; n++)
                {
                    string keyString = reader.ReadString();

                    string[] kvpElements = keyString.Split(new[] { Delimiter }, StringSplitOptions.None);
                    AuthenticationResultEx resultEx = AuthenticationResultEx.Deserialize(reader.ReadString());

                    TokenCacheKey key = new TokenCacheKey(kvpElements[0],
                        kvpElements[1].CreateSetFromSingleString(), kvpElements[2], resultEx.Result.User, kvpElements[3]);

                    this.tokenCacheDictionary.Add(key, resultEx);
                }

                PlatformPlugin.Logger.Information(null, string.Format("Deserialized {0} items to token cache.", count));
            }
        }

        /// <summary>
        ///     Reads a copy of the list of all items in the cache.
        /// </summary>
        /// <returns>The items in the cache</returns>
        public IEnumerable<TokenCacheItem> ReadItems(string clientId)
        {
            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs { TokenCache = this };
            this.OnBeforeAccess(args);

            List<TokenCacheItem> items =
                this.tokenCacheDictionary.Where(kvp=>kvp.Key.ClientId.Equals(clientId)).Select(kvp => new TokenCacheItem(kvp.Key, kvp.Value.Result)).ToList();

            this.OnAfterAccess(args);

            return items;
        }

        /// <summary>
        ///     Deletes an item from the cache.
        /// </summary>
        /// <param name="item">The item to delete from the cache</param>
        internal void DeleteItem(TokenCacheItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            PlatformPlugin.Logger.Information(null, "Deleting token in the cache");

            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
            {
                TokenCache = this,
                Scope = item.Scope,
                ClientId = item.ClientId,
                User = item.User,
                Policy = item.Policy
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
        ///     Clears the cache by deleting all the items. Note that if the cache is the default shared cache, clearing it would
        ///     impact all the instances of <see cref="AuthenticationContext" /> which share that cache.
        /// </summary>
        public virtual void Clear(string clientId)
        {
            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs { TokenCache = this };
            this.OnBeforeAccess(args);
            this.OnBeforeWrite(args);
            
            foreach(var item in this.ReadItems(clientId))
            {
                this.DeleteItem(item);
            }

            this.HasStateChanged = true;
            this.OnAfterAccess(args);
        }

        internal IEnumerable<string> GetUniqueIdsFromCache(string clientId)
        {
            IEnumerable<TokenCacheItem> allItems = this.ReadItems(clientId);
            return allItems.Select(item => item.UniqueId).Distinct();
        }

        internal IEnumerable<User> GetUsers(string clientId)
        {
            List<User> users = new List<User>();
            IEnumerable<string> uniqueIds = this.GetUniqueIdsFromCache(clientId);
            foreach (string uniqueId in uniqueIds)
            {
                users.Add(
                    this.ReadItems(clientId)
                        .First(item => !string.IsNullOrEmpty(item.UniqueId) && item.UniqueId.Equals(uniqueId))
                        .User);
            }

            return users;
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

        internal virtual AuthenticationResultEx LoadFromCache(string authority, HashSet<string> scope, string clientId, User user, string policy, CallState callState)
        {
            PlatformPlugin.Logger.Verbose(callState, "Looking up cache for a token...");
            AuthenticationResultEx resultEx = null;

            //get either a matching token or an MRRT supported RT
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? kvp = this.LoadSingleItemFromCache(authority, scope,
                clientId, user, policy, callState);
            TokenCacheKey cacheKey = null;
            if (kvp.HasValue)
            {
                cacheKey = kvp.Value.Key;
                resultEx = kvp.Value.Value;
                bool tokenNearExpiry = (resultEx.Result.ExpiresOn <=
                                        DateTime.UtcNow + TimeSpan.FromMinutes(ExpirationMarginInMinutes));
                if (!cacheKey.ScopeContains(scope) || (!IsAuthorityCommon(authority) && !authority.Equals(cacheKey.Authority)) || !clientId.Equals(cacheKey.ClientId))
                {
                    //requested scope are not a subset or authority does not match (cross-tenant RT) or client id is not same (FoCI).
                    PlatformPlugin.Logger.Verbose(callState,
                        string.Format("Refresh token for scope '{0}' will be used to acquire token for '{1}'",
                            cacheKey.Scope.CreateSingleStringFromSet(),
                            scope.CreateSingleStringFromSet()));
                    
                    resultEx = CreateResultExFromCacheResultEx(cacheKey, resultEx);
                }
                else if(tokenNearExpiry)
                {
                    resultEx.Result.AccessToken = null;
                    PlatformPlugin.Logger.Verbose(callState, "An expired or near expiry token was found in the cache");
                }
                else
                {
                    PlatformPlugin.Logger.Verbose(callState,
                        string.Format("{0} minutes left until token in cache expires",
                            (resultEx.Result.ExpiresOn - DateTime.UtcNow).TotalMinutes));
                }

                // client credential tokens do not have associated refresh tokens.
                if (resultEx.Result.AccessToken == null && resultEx.RefreshToken == null)
                {
                    this.tokenCacheDictionary.Remove(cacheKey);
                    PlatformPlugin.Logger.Information(callState, "An old item was removed from the cache");
                    this.HasStateChanged = true;
                    resultEx = null;
                }

                if (resultEx != null)
                {
                    PlatformPlugin.Logger.Information(callState,
                        "A matching item (access token or refresh token or both) was found in the cache");
                }
            }

            return resultEx;
        }


        private AuthenticationResultEx CreateResultExFromCacheResultEx(TokenCacheKey key, AuthenticationResultEx resultEx)
        {
            var newResultEx = new AuthenticationResultEx
            {
                Result = new AuthenticationResult(null, null, DateTimeOffset.MinValue),
                RefreshToken = resultEx.RefreshToken,
                ScopeInResponse = resultEx.ScopeInResponse
            };

            newResultEx.Result.UpdateTenantAndUser(resultEx.Result.TenantId, resultEx.Result.IdToken,
                resultEx.Result.User);

            if (newResultEx.Result.User != null)
            {
                newResultEx.Result.User.Authority = key.Authority;
                newResultEx.Result.User.ClientId = key.ClientId;
                newResultEx.Result.User.TokenCache = this;
            }

            return newResultEx;
        }


        internal void StoreToCache(AuthenticationResultEx result, string authority, string clientId,
            string policy, bool restrictToSingleUser, CallState callState)
        {
            PlatformPlugin.Logger.Verbose(callState, "Storing token in the cache...");

            //single user mode cannot allow more than 1 unique id in the cache including null
            if (restrictToSingleUser && (result.Result.User == null || string.IsNullOrEmpty(result.Result.User.UniqueId) ||
                !this.GetUniqueIdsFromCache(clientId).Contains(result.Result.User.UniqueId)))
            {
                throw new MsalException(
                    "Cannot add more than 1 user with a different unique id when RestrictToSingleUser is set to TRUE.");
            }

            this.OnBeforeWrite(new TokenCacheNotificationArgs
            {
                Scope = result.ScopeInResponse,
                ClientId = clientId,
                User = result.Result.User,
                Policy = policy
            });

            TokenCacheKey tokenCacheKey = new TokenCacheKey(authority, result.ScopeInResponse, clientId,
                result.Result.User, policy);
            // First identify all potential tokens.
            List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> items = this.QueryCache(authority, clientId, result.Result.User, policy);
            List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> itemsToRemove =
                items.Where(p => p.Key.ScopeIntersects(result.ScopeInResponse)).ToList();

            if (!itemsToRemove.Any())
            {
                this.tokenCacheDictionary[tokenCacheKey] = result;
                PlatformPlugin.Logger.Verbose(callState, "An item was stored in the cache");
            }
            else
            {
                //remove all intersections
                PlatformPlugin.Logger.Verbose(callState, "Items to remove - " + itemsToRemove.Count);
                foreach (var itemToRemove in itemsToRemove)
                {
                    this.tokenCacheDictionary.Remove(itemToRemove);
                }

                this.tokenCacheDictionary[tokenCacheKey] = result;
                PlatformPlugin.Logger.Verbose(callState, "An item was updated in the cache");
            }

            this.UpdateCachedRefreshTokens(result, authority, clientId, policy);
            this.HasStateChanged = true;
        }

        private void UpdateCachedRefreshTokens(AuthenticationResultEx result, string authority, string clientId, string policy)
        {
            if (result.Result.User != null && result.IsMultipleScopeRefreshToken)
            {
                List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> mrrtItems =
                    this.QueryCache(authority, clientId, result.Result.User.UniqueId,
                        result.Result.User.DisplayableId, result.Result.User.RootId, policy)
                        .Where(p => p.Value.IsMultipleScopeRefreshToken)
                        .ToList();

                foreach (KeyValuePair<TokenCacheKey, AuthenticationResultEx> mrrtItem in mrrtItems)
                {
                    mrrtItem.Value.RefreshToken = result.RefreshToken;
                }
            }
        }

        internal KeyValuePair<TokenCacheKey, AuthenticationResultEx>? LoadSingleItemFromCache(string authority,
            HashSet<string> scope, string clientId, User user,
            string policy, CallState callState)
        {
            string uniqueId = null;
            string displayableId = null;
            string rootId = null;

            if (user == null)
            {
                PlatformPlugin.Logger.Information(null, "No user information provided.");
                // if authority is common and there are multiple unique ids in the cache
                // then throw MultipleTokensMatched because code cannot guess which user
                // is requested by the developer.
                if (IsAuthorityCommon(authority) && this.GetUniqueIdsFromCache(clientId).Count() > 1)
                {
                    throw new MsalException(MsalError.MultipleTokensMatched);
                }
            }
            else
            {
                uniqueId = user.UniqueId;
                displayableId = user.DisplayableId;
                rootId = user.RootId;
            }

            if (IsAuthorityCommon(authority))
            {
                authority = null; //ignore authority
            }
            
            // First identify all potential tokens.
            List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> items = this.QueryCache(authority, clientId,
                uniqueId, displayableId, rootId, policy);

            //using ScopeContains because user could be accessing a subset of the scope.
            List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> scopeSpecificItems =
                items.Where(p => p.Key.ScopeContains(scope)).ToList();

            int scopeValuesCount = scopeSpecificItems.Count();
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? returnValue = null;
            switch (scopeValuesCount)
            {
                case 1:
                    PlatformPlugin.Logger.Information(callState,
                        "An item matching the requested scope set was found in the cache");
                    returnValue = scopeSpecificItems.First();
                    break;
                case 0:
                {
                        //look for intersecting scope first
                    scopeSpecificItems =
                        items.Where(p => p.Key.ScopeIntersects(scope)).ToList();
                    if (!scopeSpecificItems.Any())
                    {
                        //Choose any of the MRRT tokens if there are any.
                        scopeSpecificItems =
                            items.Where(p => p.Value.IsMultipleScopeRefreshToken).ToList();

                        if (scopeSpecificItems.Any())
                        {
                            returnValue = scopeSpecificItems.First();
                            PlatformPlugin.Logger.Information(callState,
                                "A Multi Scope Refresh Token for a different scope was found which can be used");
                        }
                    }
                    else
                        {
                            returnValue = scopeSpecificItems.First(); //return intersecting scopes
                        }
                }
                    break;
                default:
                    throw new MsalException(MsalError.MultipleTokensMatched);
            }

            if (user != null)
            {
                // check for tokens issued to same client_id/user_id combination, but any tenant.
                // cross tenant should not be used when there is no user provided, unless we are running in 
                // RestrictToSingleUser mode.
                if (returnValue == null)
                {
                    List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> itemsForAllTenants = this.QueryCache(
                        null, clientId, null, null, rootId, policy);
                    if (itemsForAllTenants.Count > 0)
                    {
                        returnValue = itemsForAllTenants.First();
                    }
                }

                // look for family of client id
                if (returnValue == null)
                {
                    // set authority and client id to null.
                    List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> itemsForFamily =
                        this.QueryCache(null, null, null, null, rootId, policy)
                            .Where(kvp => kvp.Value.Result != null && kvp.Value.Result.FamilyId != null).ToList();
                    if (itemsForFamily.Count > 0)
                    {
                        returnValue = itemsForFamily.First();
                    }
                }
            }

            return returnValue;
        }


        private bool IsAuthorityCommon(string authority)
        {
            return authority.ToLower().EndsWith("/common/");
        }

        private List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> QueryCache(string authority, string clientId,
            User user, string policy)
        {
            return this.QueryCache(
                authority, clientId, user?.UniqueId, user?.DisplayableId, user?.RootId, policy);
        }

        /// <summary>
        ///     Queries all values in the cache that meet the passed in values, plus the
        ///     authority value that this AuthorizationContext was created with.  In every case passing
        ///     null results in a wildcard evaluation.
        /// </summary>
        private List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> QueryCache(string authority, string clientId, string uniqueId, string displayableId, string rootId, string policy)
        {
            return this.tokenCacheDictionary.Where(
                p =>
                    (string.IsNullOrWhiteSpace(authority) || p.Key.Equals(p.Key.Authority, authority))
                    && (string.IsNullOrWhiteSpace(clientId) || p.Key.Equals(p.Key.ClientId, clientId))
                    && (string.IsNullOrWhiteSpace(uniqueId) || p.Key.Equals(p.Key.UniqueId, uniqueId))
                    && (string.IsNullOrWhiteSpace(displayableId) || p.Key.Equals(p.Key.DisplayableId, displayableId))
                    && (string.IsNullOrWhiteSpace(rootId) || p.Key.Equals(p.Key.RootId, rootId))
                    && (string.IsNullOrWhiteSpace(policy) || p.Key.Equals(p.Key.Policy, policy))).ToList();
        }
    }
}