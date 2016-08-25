//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Instance;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    public class TokenCache
    {
        /// <summary>
        /// Notification for certain token cache interactions during token acquisition.
        /// </summary>
        /// <param name="args">Arguments related to the cache item impacted</param>
        public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);

        private const int SchemaVersion = 1;
        private const string Delimiter = ":::";
        private readonly object lockObject = new object();
        internal readonly IDictionary<TokenCacheKey, AuthenticationResultEx> tokenCacheDictionary;
        private volatile bool hasStateChanged;

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
        /// Default constructor.
        /// </summary>
        public TokenCache()
        {
            this.tokenCacheDictionary = new ConcurrentDictionary<TokenCacheKey, AuthenticationResultEx>();
        }

        /// <summary>
        /// Constructor receiving state of the cache
        /// </summary>
        public TokenCache(byte[] state)
            : this()
        {
            this.Deserialize(state);
        }

        /// <summary>
        /// Static user token cache shared by all instances of application which do not explicitly pass a cache instance
        /// during construction.
        /// </summary>
        public static TokenCache DefaultSharedUserTokenCache { get; internal set; }

        /// <summary>
        /// Static client token cache shared by all instances of ConfidentialClientApplication which do not explicitly pass a
        /// cache instance
        /// during construction.
        /// </summary>
        public static TokenCache DefaultSharedAppTokenCache { get; internal set; }

        /// <summary>
        /// Notification method called before any library method accesses the cache.
        /// </summary>
        public TokenCacheNotification BeforeAccess { get; set; }

        /// <summary>
        /// Notification method called before any library method writes to the cache. This notification can be used to reload
        /// the cache state from a row in database and lock that row. That database row can then be unlocked in
        /// <see cref="AfterAccess" /> notification.
        /// </summary>
        public TokenCacheNotification BeforeWrite { get; set; }

        /// <summary>
        /// Notification method called after any library method accesses the cache.
        /// </summary>
        public TokenCacheNotification AfterAccess { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating whether cache state has changed. ADAL methods set this flag after any change.
        /// Caller application should reset
        /// the flag after serializing and persisting the state of the cache.
        /// </summary>
        public bool HasStateChanged
        {
            get { return this.hasStateChanged; }

            set { this.hasStateChanged = value; }
        }

        /// <summary>
        /// Gets the nunmber of items in the cache.
        /// </summary>
        public int Count
        {
            get { return this.tokenCacheDictionary.Count; }
        }

        /// <summary>
        /// Serializes current state of the cache as a blob. Caller application can persist the blob and update the state of
        /// the cache later by
        /// passing that blob back in constructor or by calling method Deserialize.
        /// </summary>
        /// <returns>Current state of the cache as a blob</returns>
        public byte[] Serialize()
        {
            lock (lockObject)
            {
                using (Stream stream = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(SchemaVersion);
                    PlatformPlugin.Logger.Information(null,
                        string.Format(CultureInfo.InvariantCulture, "Serializing token cache with {0} items.",
                            this.tokenCacheDictionary.Count));
                    writer.Write(this.tokenCacheDictionary.Count);
                    foreach (KeyValuePair<TokenCacheKey, AuthenticationResultEx> kvp in this.tokenCacheDictionary)
                    {
                        writer.Write(string.Format(CultureInfo.InvariantCulture, "{1}{0}{2}{0}{3}{0}{4}", Delimiter,
                            kvp.Key.Authority,
                            kvp.Key.Scope.AsSingleString(), kvp.Key.ClientId, kvp.Key.Policy));
                        writer.Write(kvp.Value.Serialize());
                    }

                    int length = (int) stream.Position;
                    stream.Position = 0;
                    BinaryReader reader = new BinaryReader(stream);
                    return reader.ReadBytes(length);
                }
            }
        }

        /// <summary>
        /// Deserializes state of the cache. The state should be the blob received earlier by calling the method Serialize.
        /// </summary>
        /// <param name="state">State of the cache as a blob</param>
        public void Deserialize(byte[] state)
        {
            lock (lockObject)
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

                        string[] kvpElements = keyString.Split(new[] {Delimiter}, StringSplitOptions.None);
                        AuthenticationResultEx resultEx = AuthenticationResultEx.Deserialize(reader.ReadString());

                        TokenCacheKey key = new TokenCacheKey(kvpElements[0],
                            kvpElements[1].AsSet(), kvpElements[2], resultEx.Result.User, kvpElements[3]);

                        this.tokenCacheDictionary.Add(key, resultEx);
                    }

                    PlatformPlugin.Logger.Information(null,
                        string.Format(CultureInfo.InvariantCulture, "Deserialized {0} items to token cache.", count));
                }
            }
        }

        /// <summary>
        /// Reads a copy of the list of all items in the cache.
        /// </summary>
        /// <returns>The items in the cache</returns>
        public IEnumerable<TokenCacheItem> ReadItems(string clientId)
        {
            lock (lockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs {TokenCache = this};
                this.OnBeforeAccess(args);

                List<TokenCacheItem> items = ReadItemsFromCache(clientId);

                this.OnAfterAccess(args);

                return items;
            }
        }

        /// <summary>
        /// Deletes an item from the cache.
        /// </summary>
        /// <param name="item">The item to delete from the cache</param>
        internal void DeleteItem(TokenCacheItem item)
        {
            lock (lockObject)
            {
                if (item == null)
                {
                    throw new ArgumentNullException("item");
                }

                PlatformPlugin.Logger.Information(null, "Deleting token in the cache");

                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    Scope = item.Scope.AsArray(),
                    ClientId = item.ClientId,
                    User = item.User,
                    Policy = item.Policy
                };

                this.OnBeforeAccess(args);
                this.OnBeforeWrite(args);

                DeleteItemFromCache(item);

                this.HasStateChanged = true;
                this.OnAfterAccess(args);
            }
        }

        /// <summary>
        /// Clears the cache by deleting all the items. Note that if the cache is the default shared cache, clearing it would
        /// impact all the instances of <see cref="PublicClientApplication" /> which share that cache.
        /// </summary>
        public virtual void Clear(string clientId)
        {
            lock (lockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs {TokenCache = this};
                this.OnBeforeAccess(args);
                this.OnBeforeWrite(args);

                foreach (var item in this.ReadItemsFromCache(clientId))
                {
                    this.DeleteItemFromCache(item);
                }

                this.HasStateChanged = true;
                this.OnAfterAccess(args);
            }
        }

        internal IEnumerable<string> GetUniqueIdsFromCache(string clientId)
        {
            IEnumerable<TokenCacheItem> allItems = this.ReadItems(clientId);
            return allItems.Select(item => item.UniqueId).Distinct();
        }

        internal IEnumerable<string> GetHomeObjectIdsFromCache(string clientId)
        {
            IEnumerable<TokenCacheItem> allItems = this.ReadItems(clientId);
            return allItems.Select(item => item.HomeObjectId).Distinct();
        }

        internal IEnumerable<User> GetUsers(string clientId)
        {
            lock (lockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs {TokenCache = this};
                this.OnBeforeAccess(args);
                this.OnBeforeWrite(args);

                List<User> users = new List<User>();
                IEnumerable<string> homeOids = this.GetHomeObjectIdsFromCache(clientId);
                foreach (string homeOid in homeOids)
                {
                    User localUser =
                        this.ReadItems(clientId)
                            .First(item => !string.IsNullOrEmpty(item.HomeObjectId) && item.HomeObjectId.Equals(homeOid))
                            .User;
                    localUser.ClientId = clientId;
                    localUser.TokenCache = this;
                    users.Add(localUser);
                }

                this.HasStateChanged = true;
                this.OnAfterAccess(args);

                return users;
            }
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

        internal virtual AuthenticationResultEx LoadFromCache(string authority, HashSet<string> scope, string clientId,
            User user, string policy, CallState callState)
        {
            lock (lockObject)
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
                                            DateTime.UtcNow + TimeSpan.FromMinutes(Constant.ExpirationMarginInMinutes));
                    if (!cacheKey.ScopeContains(scope) ||
                        (!Authority.IsTenantLess(authority) && !authority.Equals(cacheKey.Authority)) ||
                        !clientId.Equals(cacheKey.ClientId))
                    {
                        //requested scope are not a subset or authority does not match (cross-tenant RT) or client id is not same (FoCI).
                        PlatformPlugin.Logger.Verbose(callState,
                            string.Format(CultureInfo.InvariantCulture,
                                "Refresh token for scope '{0}' will be used to acquire token for '{1}'",
                                cacheKey.Scope.AsSingleString(),
                                scope.AsSingleString()));

                        resultEx = CreateResultExFromCacheResultEx(cacheKey, resultEx);
                    }
                    else if (tokenNearExpiry)
                    {
                        resultEx.Result.Token = null;
                        PlatformPlugin.Logger.Verbose(callState,
                            "An expired or near expiry token was found in the cache");
                    }
                    else
                    {
                        PlatformPlugin.Logger.Verbose(callState,
                            string.Format(CultureInfo.InvariantCulture, "{0} minutes left until token in cache expires",
                                (resultEx.Result.ExpiresOn - DateTime.UtcNow).TotalMinutes));
                    }

                    // client credential tokens do not have associated refresh tokens.
                    if (resultEx.Result.Token == null && resultEx.RefreshToken == null)
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
        }

        private AuthenticationResultEx CreateResultExFromCacheResultEx(TokenCacheKey key,
            AuthenticationResultEx resultEx)
        {
            var newResultEx = new AuthenticationResultEx
            {
                Result = new AuthenticationResult(null, null, DateTimeOffset.MinValue)
                {
                    ScopeSet = new HashSet<string>(resultEx.Result.ScopeSet.ToArray())
                },
                RefreshToken = resultEx.RefreshToken,
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

        internal void StoreToCache(AuthenticationResultEx resultEx, string authority, string clientId,
            string policy, bool restrictToSingleUser, CallState callState)
        {
            lock (lockObject)
            {
                PlatformPlugin.Logger.Verbose(callState, "Storing token in the cache...");

                //single user mode cannot allow more than 1 unique id in the cache including null
                if (restrictToSingleUser &&
                    (resultEx.Result.User == null || string.IsNullOrEmpty(resultEx.Result.User.UniqueId) ||
                     !this.GetUniqueIdsFromCache(clientId).Contains(resultEx.Result.User.UniqueId)))
                {
                    throw new MsalException(MsalError.InvalidCacheOperation,
                        "Cannot add more than 1 user with a different unique id when RestrictToSingleUser is set to TRUE.");
                }

                this.OnBeforeWrite(new TokenCacheNotificationArgs
                {
                    Scope = resultEx.Result.Scope,
                    ClientId = clientId,
                    User = resultEx.Result.User,
                    Policy = policy
                });

                TokenCacheKey tokenCacheKey = new TokenCacheKey(authority, resultEx.Result.ScopeSet, clientId,
                    resultEx.Result.User, policy);
                // First identify all potential tokens.
                List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> items = this.QueryCache(authority, clientId,
                    resultEx.Result.User, policy);
                List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> itemsToRemove =
                    items.Where(p => p.Key.ScopeIntersects(resultEx.Result.ScopeSet)).ToList();

                if (!itemsToRemove.Any())
                {
                    this.tokenCacheDictionary[tokenCacheKey] = resultEx;
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

                    this.tokenCacheDictionary[tokenCacheKey] = resultEx;
                    PlatformPlugin.Logger.Verbose(callState, "An item was updated in the cache");
                }

                this.UpdateCachedRefreshTokens(resultEx, authority, clientId, policy);
                this.HasStateChanged = true;
            }
        }

        private void UpdateCachedRefreshTokens(AuthenticationResultEx result, string authority, string clientId,
            string policy)
        {
            lock (lockObject)
            {
                if (result.Result.User != null && result.IsMultipleScopeRefreshToken)
                {
                    List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> mrrtItems =
                        this.QueryCache(authority, clientId, result.Result.User.UniqueId,
                            result.Result.User.DisplayableId, result.Result.User.HomeObjectId, policy)
                            .Where(p => p.Value.IsMultipleScopeRefreshToken)
                            .ToList();

                    foreach (KeyValuePair<TokenCacheKey, AuthenticationResultEx> mrrtItem in mrrtItems)
                    {
                        mrrtItem.Value.RefreshToken = result.RefreshToken;
                    }
                }
            }
        }

        internal KeyValuePair<TokenCacheKey, AuthenticationResultEx>? LoadSingleItemFromCache(string authority,
            HashSet<string> scope, string clientId, User user,
            string policy, CallState callState)
        {
            lock (lockObject)
            {
                string uniqueId = null;
                string displayableId = null;
                string rootId = null;

                if (user != null)
                {
                    uniqueId = user.UniqueId;
                    displayableId = user.DisplayableId;
                    rootId = user.HomeObjectId;
                }

                if (uniqueId == null && displayableId == null && rootId == null)
                {
                    PlatformPlugin.Logger.Information(null, "No user information provided.");
                    // if authority is common and there are multiple unique ids in the cache
                    // then throw MultipleTokensMatched because code cannot guess which user
                    // is requested by the developer.
                    if (Authority.IsTenantLess(authority) && this.GetUniqueIdsFromCache(clientId).Count() > 1)
                    {
                        throw new MsalException(MsalError.MultipleTokensMatched);
                    }
                }

                if (Authority.IsTenantLess(authority))
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
        }

        private List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> QueryCache(string authority, string clientId,
            User user, string policy)
        {
            return this.QueryCache(
                authority, clientId, user?.UniqueId, user?.DisplayableId, user?.HomeObjectId, policy);
        }

        /// <summary>
        /// Queries all values in the cache that meet the passed in values, plus the
        /// authority value that this AuthorizationContext was created with.  In every case passing
        /// null results in a wildcard evaluation.
        /// </summary>
        private List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> QueryCache(string authority, string clientId,
            string uniqueId, string displayableId, string rootId, string policy)
        {
            return this.tokenCacheDictionary.Where(
                p =>
                    (string.IsNullOrWhiteSpace(authority) || p.Key.Equals(p.Key.Authority, authority))
                    && (string.IsNullOrWhiteSpace(clientId) || p.Key.Equals(p.Key.ClientId, clientId))
                    && (string.IsNullOrWhiteSpace(uniqueId) || p.Key.Equals(p.Key.UniqueId, uniqueId))
                    && (string.IsNullOrWhiteSpace(displayableId) || p.Key.Equals(p.Key.DisplayableId, displayableId))
                    && (string.IsNullOrWhiteSpace(rootId) || p.Key.Equals(p.Key.HomeObjectId, rootId))
                    && (string.IsNullOrWhiteSpace(policy) || p.Key.Equals(p.Key.Policy, policy))).ToList();
        }

        private List<TokenCacheItem> ReadItemsFromCache(string clientId)
        {
            return this.tokenCacheDictionary.Where(kvp => kvp.Key.ClientId.Equals(clientId))
                .Select(kvp => new TokenCacheItem(kvp.Key, kvp.Value.Result))
                .ToList();
        }

        private void DeleteItemFromCache(TokenCacheItem item)
        {
            TokenCacheKey toRemoveKey = this.tokenCacheDictionary.Keys.FirstOrDefault(item.Match);
            if (toRemoveKey != null)
            {
                this.tokenCacheDictionary.Remove(toRemoveKey);
            }
        }
    }
}