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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Token cache class used by <see cref="AuthenticationContext"/> to store access and refresh tokens.
    /// </summary>
    public class TokenCache
    {
        /// <summary>
        /// Notification for certain token cache interactions during token acquisition.
        /// </summary>
        /// <param name="args">Arguments related to the cache item impacted</param>
        public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);

        internal readonly IDictionary<AdalTokenCacheKey, AdalResultWrapper> tokenCacheDictionary;

        // We do not want to return near expiry tokens, this is why we use this hard coded setting to refresh tokens which are close to expiration.
        private const int ExpirationMarginInMinutes = 5;

        private volatile bool hasStateChanged;

        private readonly Object cacheLock = new Object();

        static TokenCache()
        {
            ModuleInitializer.EnsureModuleInitialized();

            DefaultShared = new TokenCache
            {
                BeforeAccess = StorageDelegates.BeforeAccess,
                AfterAccess = StorageDelegates.AfterAccess
            };
        }

        internal TokenCacheAccessor tokenCacheAccessor = new TokenCacheAccessor();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TokenCache()
        {
            if (CoreLoggerBase.Default == null)
                CoreLoggerBase.Default = new AdalLogger(Guid.Empty);

            this.tokenCacheDictionary = new ConcurrentDictionary<AdalTokenCacheKey, AdalResultWrapper>();
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
        /// the flag after serializing and persisting the state of the cache.
        /// </summary>
        public bool HasStateChanged
        {
            get
            {
                lock (cacheLock)
                {
                    return this.hasStateChanged;
                }
            }

            set
            {
                lock (cacheLock)
                {
                    this.hasStateChanged = value;
                }
            }
        }

        /// <summary>
        /// Gets the nunmber of items in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                lock (cacheLock)
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs { TokenCache = this };
                    this.OnBeforeAccess(args);

                    var count = this.tokenCacheDictionary.Count;

                    this.OnAfterAccess(args);

                    return count;
                }
            }
        }

        /// <summary>
        /// Serializes current state of the cache as a blob. Caller application can persist the blob and update the state of the cache later by 
        /// passing that blob back in constructor or by calling method Deserialize.
        /// </summary>
        /// <returns>Current state of the cache as a blob</returns>
        public byte[] Serialize()
        {
            lock (cacheLock)
            {
                return AdalCacheOperations.Serialize(tokenCacheDictionary);
            }
        }

        /// <summary>
        /// Deserializes state of the cache. The state should be the blob received earlier by calling the method Serialize.
        /// </summary>
        /// <param name="state">State of the cache as a blob</param>
        public void Deserialize(byte[] state)
        {
            lock (cacheLock)
            {
                tokenCacheDictionary.Clear();
                foreach (var entry in AdalCacheOperations.Deserialize(state))
                {
                    tokenCacheDictionary.Add(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// Reads a copy of the list of all items in the cache. 
        /// </summary>
        /// <returns>The items in the cache</returns>
        public virtual IEnumerable<TokenCacheItem> ReadItems()
        {
            lock (cacheLock)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs { TokenCache = this };
                this.OnBeforeAccess(args);

                List<TokenCacheItem> items =
                    this.tokenCacheDictionary.Select(kvp => new TokenCacheItem(kvp.Key, kvp.Value.Result)).ToList();

                this.OnAfterAccess(args);

                return items;
            }
        }

        /// <summary>
        /// Deletes an item from the cache.
        /// </summary>
        /// <param name="item">The item to delete from the cache</param>
        public virtual void DeleteItem(TokenCacheItem item)
        {
            lock (cacheLock)
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

                AdalTokenCacheKey toRemoveKey = this.tokenCacheDictionary.Keys.FirstOrDefault(item.Match);
                if (toRemoveKey != null)
                {
                    this.tokenCacheDictionary.Remove(toRemoveKey);
                    string msg = "One item removed successfully";
                    CoreLoggerBase.Default.Info(msg);
                    CoreLoggerBase.Default.InfoPii(msg);
                }
                else
                {
                    string msg = "Item not Present in the Cache";
                    CoreLoggerBase.Default.Info(msg);
                    CoreLoggerBase.Default.InfoPii(msg);
                }

                this.HasStateChanged = true;
                this.OnAfterAccess(args);
            }
        }

        /// <summary>
        /// Clears the cache by deleting all the items. Note that if the cache is the default shared cache, clearing it would
        /// impact all the instances of <see cref="AuthenticationContext"/> which share that cache.
        /// </summary>
        public virtual void Clear()
        {
            lock (cacheLock)
            {
                ClearAdalCache();
                ClearMsalCache();
            }
        }

        internal void ClearAdalCache()
        {
            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs { TokenCache = this };
            this.OnBeforeAccess(args);
            this.OnBeforeWrite(args);
            string msg = String.Format(CultureInfo.CurrentCulture, "Clearing Cache :- {0} items to be removed",
                this.tokenCacheDictionary.Count);
            CoreLoggerBase.Default.Info(msg);
            CoreLoggerBase.Default.InfoPii(msg);
            this.tokenCacheDictionary.Clear();
            msg = "Successfully Cleared Cache";
            CoreLoggerBase.Default.Info(msg);
            CoreLoggerBase.Default.InfoPii(msg);
            this.HasStateChanged = true;
            this.OnAfterAccess(args);
        }

        internal void ClearMsalCache()
        {
            tokenCacheAccessor.Clear();
        }

        internal void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            lock (cacheLock)
            {
                if (AfterAccess != null)
                {
                    AfterAccess(args);
                }
            }
        }

        internal void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            lock (cacheLock)
            {
                if (BeforeAccess != null)
                {
                    BeforeAccess(args);
                }
            }
        }

        internal void OnBeforeWrite(TokenCacheNotificationArgs args)
        {
            lock (cacheLock)
            {
                if (BeforeWrite != null)
                {
                    BeforeWrite(args);
                }
            }
        }

        internal async Task<AdalResultWrapper> LoadFromCacheAsync(CacheQueryData cacheQueryData, RequestContext requestContext)
        {
            AdalResultWrapper resultEx = null;
            var aliasedHosts = await GetOrderedAliasesAsync(cacheQueryData.Authority, false, requestContext).ConfigureAwait(false);
            foreach (var aliasedHost in aliasedHosts)
            {
                cacheQueryData.Authority = ReplaceHost(cacheQueryData.Authority, aliasedHost);
                resultEx = LoadFromCacheCommon(cacheQueryData, requestContext);
                if (resultEx?.Result != null)
                {
                    break;
                }
            }

            return resultEx;
        }

        private static string GetHost(string uri)
        {
            // The following line serves as a validation for uri. Relevant exceptions will be thrown.
            new Uri(uri); //NOSONAR

            // Note: host is supposed to be case insensitive, and would be normalized to lowercase by: new Uri(uri).Host
            // but we would like to preserve its case to match a previously cached token
            return uri.Split('/')[2];
        }

        private static string ReplaceHost(string oldUri, string newHost)
        {
            if (string.IsNullOrEmpty(oldUri) || string.IsNullOrEmpty(newHost))
            {
                throw new ArgumentNullException();
            }

            return string.Format(CultureInfo.InvariantCulture, "https://{0}{1}", newHost, new Uri(oldUri).AbsolutePath);
        }

        internal static async Task<List<string>> GetOrderedAliasesAsync(string authority, bool validateAuthority, RequestContext requestContext)
        {
            var metadata = await InstanceDiscovery.GetMetadataEntryAsync(new Uri(authority), validateAuthority, requestContext).ConfigureAwait(false);
            var aliasedAuthorities = new List<string>(new string[] { metadata.PreferredCache, GetHost(authority) });
            aliasedAuthorities.AddRange(metadata.Aliases ?? Enumerable.Empty<string>());
            return aliasedAuthorities;
        }

        internal AdalResultWrapper LoadFromCacheCommon(CacheQueryData cacheQueryData, RequestContext requestContext)
        {
            lock (cacheLock)
            {
                var msg = "Looking up cache for a token...";
                requestContext.Logger.Verbose(msg);
                requestContext.Logger.VerbosePii(msg);

                AdalResultWrapper resultEx = null;
                KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>? kvp =
                    this.LoadSingleItemFromCache(cacheQueryData, requestContext);

                if (kvp.HasValue)
                {
                    AdalTokenCacheKey cacheKey = kvp.Value.Key;
                    resultEx = kvp.Value.Value.Clone();

                    bool tokenNearExpiry = (resultEx.Result.ExpiresOn <=
                                            DateTime.UtcNow + TimeSpan.FromMinutes(ExpirationMarginInMinutes));
                    bool tokenExtendedLifeTimeExpired = (resultEx.Result.ExtendedExpiresOn <=
                                                         DateTime.UtcNow);

                    //check for cross-tenant authority
                    if (!cacheKey.Authority.Equals(cacheQueryData.Authority, StringComparison.OrdinalIgnoreCase))
                    {
                        // this is a cross-tenant result. use RT only
                        resultEx.Result.AccessToken = null;

                        msg = "Cross Tenant refresh token was found in the cache";
                        requestContext.Logger.Info(msg);
                        requestContext.Logger.InfoPii(msg);
                    }
                    else if (tokenNearExpiry && !cacheQueryData.ExtendedLifeTimeEnabled)
                    {
                        resultEx.Result.AccessToken = null;

                        msg = "An expired or near expiry token was found in the cache";
                        requestContext.Logger.Info(msg);
                        requestContext.Logger.InfoPii(msg);
                    }
                    else if (!cacheKey.ResourceEquals(cacheQueryData.Resource))
                    {
                        requestContext.Logger.InfoPii(string.Format(CultureInfo.CurrentCulture,
                                "Multi resource refresh token for resource '{0}' will be used to acquire token for '{1}'",
                                cacheKey.Resource, cacheQueryData.Resource));
                        var newResultEx = new AdalResultWrapper
                        {
                            Result = new AdalResult(null, null, DateTimeOffset.MinValue),
                            RefreshToken = resultEx.RefreshToken,
                            ResourceInResponse = resultEx.ResourceInResponse
                        };

                        newResultEx.Result.UpdateTenantAndUserInfo(resultEx.Result.TenantId, resultEx.Result.IdToken,
                            resultEx.Result.UserInfo);
                        resultEx = newResultEx;
                    }
                    else if (!tokenExtendedLifeTimeExpired && cacheQueryData.ExtendedLifeTimeEnabled && tokenNearExpiry)
                    {
                        resultEx.Result.ExtendedLifeTimeToken = true;
                        resultEx.Result.ExpiresOn = resultEx.Result.ExtendedExpiresOn;

                        msg =
                            "The extendedLifeTime is enabled and a stale AT with extendedLifeTimeEnabled is returned.";
                        requestContext.Logger.Info(msg);
                        requestContext.Logger.InfoPii(msg);
                    }
                    else if (tokenExtendedLifeTimeExpired)
                    {
                        resultEx.Result.AccessToken = null;

                        msg = "The AT has expired its ExtendedLifeTime";
                        requestContext.Logger.Info(msg);
                        requestContext.Logger.InfoPii(msg);
                    }
                    else
                    {
                        msg = string.Format(CultureInfo.CurrentCulture, "{0} minutes left until token in cache expires",
                            (resultEx.Result.ExpiresOn - DateTime.UtcNow).TotalMinutes);
                        requestContext.Logger.Info(msg);
                        requestContext.Logger.InfoPii(msg);
                    }

                    if (resultEx.Result.AccessToken == null && resultEx.RefreshToken == null)
                    {
                        this.tokenCacheDictionary.Remove(cacheKey);

                        msg = "An old item was removed from the cache";
                        requestContext.Logger.Info(msg);
                        requestContext.Logger.InfoPii(msg);

                        this.HasStateChanged = true;
                        resultEx = null;
                    }

                    if (resultEx != null)
                    {
                        resultEx.Result.Authority = cacheKey.Authority;

                        msg = "A matching item (access token or refresh token or both) was found in the cache";
                        requestContext.Logger.Info(msg);
                        requestContext.Logger.InfoPii(msg);
                    }
                }
                else
                {
                    msg = "No matching token was found in the cache";
                    requestContext.Logger.Info(msg);
                    requestContext.Logger.InfoPii(msg);
                    if (cacheQueryData.SubjectType == TokenSubjectType.User)
                    {
                        msg = "Checking MSAL cache for user token cache";
                        requestContext.Logger.Info(msg);
                        requestContext.Logger.InfoPii(msg);
                        resultEx = CacheFallbackOperations.FindMsalEntryForAdal(tokenCacheAccessor,
                            cacheQueryData.Authority, cacheQueryData.ClientId, cacheQueryData.DisplayableId, requestContext);
                    }
                }

                return resultEx;
            }
        }

        internal async Task StoreToCacheAsync(AdalResultWrapper result, string authority, string resource, string clientId,
            TokenSubjectType subjectType, RequestContext requestContext)
        {
            var metadata = await InstanceDiscovery.GetMetadataEntryAsync(new Uri(authority), false, requestContext).ConfigureAwait(false);
            StoreToCacheCommon(result, ReplaceHost(authority, metadata.PreferredCache), resource, clientId, subjectType, requestContext);
        }

        internal void StoreToCacheCommon(AdalResultWrapper result, string authority, string resource, string clientId,
            TokenSubjectType subjectType, RequestContext requestContext)
        {
            lock (cacheLock)
            {
                var msg = "Storing token in the cache...";
                requestContext.Logger.Verbose(msg);
                requestContext.Logger.VerbosePii(msg);

                string uniqueId = (result.Result.UserInfo != null) ? result.Result.UserInfo.UniqueId : null;
                string displayableId = (result.Result.UserInfo != null) ? result.Result.UserInfo.DisplayableId : null;

                this.OnBeforeWrite(new TokenCacheNotificationArgs
                {
                    Resource = resource,
                    ClientId = clientId,
                    UniqueId = uniqueId,
                    DisplayableId = displayableId
                });

                AdalTokenCacheKey AdalTokenCacheKey = new AdalTokenCacheKey(authority, resource, clientId, subjectType,
                    result.Result.UserInfo);
                this.tokenCacheDictionary[AdalTokenCacheKey] = result;

                msg = "An item was stored in the cache";
                requestContext.Logger.Verbose(msg);
                requestContext.Logger.VerbosePii(msg);

                this.UpdateCachedMrrtRefreshTokens(result, clientId, subjectType);

                this.HasStateChanged = true;

                //store ADAL RT in MSAL cache for user tokens where authority is AAD
                if (subjectType == TokenSubjectType.User && Authenticator.DetectAuthorityType(authority) == Internal.Instance.AuthorityType.AAD)
                {
                    Identity.Core.IdToken idToken = Identity.Core.IdToken.Parse(result.Result.IdToken);
                    
                    CacheFallbackOperations.WriteMsalRefreshToken(tokenCacheAccessor, result, authority, clientId, displayableId,
                        result.Result.UserInfo.GivenName,
                        result.Result.UserInfo.FamilyName, idToken?.ObjectId);
                }
            }
        }

        private void UpdateCachedMrrtRefreshTokens(AdalResultWrapper result, string clientId,
            TokenSubjectType subjectType)
        {
            lock (cacheLock)
            {
                if (result.Result.UserInfo != null && result.IsMultipleResourceRefreshToken)
                {
                    //pass null for authority to update the token for all the tenants
                    List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> mrrtItems =
                        this.QueryCache(null, clientId, subjectType, result.Result.UserInfo.UniqueId,
                                result.Result.UserInfo.DisplayableId, null)
                            .Where(p => p.Value.IsMultipleResourceRefreshToken)
                            .ToList();

                    foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> mrrtItem in mrrtItems)
                    {
                        mrrtItem.Value.RefreshToken = result.RefreshToken;
                    }
                }
            }
        }

        private KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>? LoadSingleItemFromCache(
            CacheQueryData cacheQueryData, RequestContext requestContext)
        {
            lock (cacheLock)
            {
                // First identify all potential tokens.
                List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> items =
                    this.QueryCache(cacheQueryData.Authority, cacheQueryData.ClientId, cacheQueryData.SubjectType,
                        cacheQueryData.UniqueId, cacheQueryData.DisplayableId, cacheQueryData.AssertionHash);

                List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> resourceSpecificItems =
                    items.Where(p => p.Key.ResourceEquals(cacheQueryData.Resource)).ToList();
                int resourceValuesCount = resourceSpecificItems.Count;

                KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>? returnValue = null;
                switch (resourceValuesCount)
                {
                    case 1:
                        var msg = "An item matching the requested resource was found in the cache";
                        requestContext.Logger.Info(msg);
                        requestContext.Logger.InfoPii(msg);

                        returnValue = resourceSpecificItems.First();
                        break;
                    case 0:
                        {
                            // There are no resource specific tokens.  Choose any of the MRRT tokens if there are any.
                            List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> mrrtItems =
                                items.Where(p => p.Value.IsMultipleResourceRefreshToken).ToList();

                            if (mrrtItems.Any())
                            {
                                returnValue = mrrtItems.First();
                                msg =
                                    "A Multi Resource Refresh Token for a different resource was found which can be used";
                                requestContext.Logger.Info(msg);
                                requestContext.Logger.InfoPii(msg);
                            }
                        }
                        break;
                    default:
                        throw new AdalException(AdalError.MultipleTokensMatched);
                }

                // check for tokens issued to same client_id/user_id combination, but any tenant.
                // this check only applies to user tokens. client tokens should be ignored.
                if (returnValue == null && cacheQueryData.SubjectType != TokenSubjectType.Client)
                {
                    List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> itemsForAllTenants = this.QueryCache(
                        null, cacheQueryData.ClientId, cacheQueryData.SubjectType, cacheQueryData.UniqueId,
                        cacheQueryData.DisplayableId, cacheQueryData.AssertionHash);

                    List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> cloudSpecificItemsForAllTenants =
                        itemsForAllTenants.Where(item => IsSameCloud(item.Key.Authority, cacheQueryData.Authority)).ToList();

                    if (cloudSpecificItemsForAllTenants.Count != 0)
                    {
                        returnValue = cloudSpecificItemsForAllTenants.First();
                    }

                    // check if the token was issued by AAD
                    if (returnValue != null &&
                        Authenticator.DetectAuthorityType(returnValue.Value.Key.Authority) == Internal.Instance.AuthorityType.ADFS)
                    {
                        returnValue = null;
                    }
                }

                return returnValue;
            }
        }

        private static bool IsSameCloud(string authority, string authority1)
        {
            return new Uri(authority).Host.Equals(new Uri(authority1).Host);
        }

        /// <summary>
        /// Queries all values in the cache that meet the passed in values, plus the 
        /// authority value that this AuthorizationContext was created with.  In every case passing
        /// null results in a wildcard evaluation.
        /// </summary>
        private List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> QueryCache(string authority, string clientId,
            TokenSubjectType subjectType, string uniqueId, string displayableId, string assertionHash)
        {
            lock (cacheLock)
            {
                //if developer passes an assertion then assertionHash must be used to match the cache entry.
                //if UserAssertionHash in cache entry is null then it won't be a match.
                return this.tokenCacheDictionary.Where(
                        p =>
                            (string.IsNullOrWhiteSpace(authority) || p.Key.Authority == authority)
                            && (string.IsNullOrWhiteSpace(clientId) || p.Key.ClientIdEquals(clientId))
                            && (string.IsNullOrWhiteSpace(uniqueId) || p.Key.UniqueId == uniqueId)
                            && (string.IsNullOrWhiteSpace(displayableId) || p.Key.DisplayableIdEquals(displayableId))
                            && p.Key.TokenSubjectType == subjectType &&
                            (string.IsNullOrWhiteSpace(assertionHash) ||
                             assertionHash.Equals(p.Value.UserAssertionHash, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
        }
    }
}