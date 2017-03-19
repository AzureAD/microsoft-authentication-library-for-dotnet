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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.OAuth2;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Token cache class used by <see cref="ConfidentialClientApplication"/> and <see cref="PublicClientApplication"/> to store access and refresh tokens.
    /// </summary>
    public sealed class TokenCache
    {
        private const int DefaultExpirationBufferInMinutes = 5;

        internal readonly TokenCacheAccessor TokenCacheAccessor = new TokenCacheAccessor();

        /// <summary>
        /// Notification for certain token cache interactions during token acquisition.
        /// </summary>
        /// <param name="args">Arguments related to the cache item impacted</param>
        public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);

        internal readonly object LockObject = new object();
        private volatile bool hasStateChanged;

        internal string ClientId { get; set; }

        /// <summary>
        /// Notification method called before any library method accesses the cache.
        /// </summary>
        internal TokenCacheNotification BeforeAccess { get; set; }

        /// <summary>
        /// Notification method called before any library method writes to the cache. This notification can be used to reload
        /// the cache state from a row in database and lock that row. That database row can then be unlocked in
        /// <see cref="AfterAccess" /> notification.
        /// </summary>
        internal TokenCacheNotification BeforeWrite { get; set; }

        /// <summary>
        /// Notification method called after any library method accesses the cache.
        /// </summary>
        internal TokenCacheNotification AfterAccess { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating whether cache state has changed. MSAL methods set this flag after any change.
        /// Caller application should reset
        /// the flag after serializing and persisting the state of the cache.
        /// </summary>
        public bool HasStateChanged
        {
            get { return hasStateChanged; }

            set { hasStateChanged = value; }
        }
        
        internal void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            AfterAccess?.Invoke(args);
        }

        internal void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            BeforeAccess?.Invoke(args);
        }

        internal void OnBeforeWrite(TokenCacheNotificationArgs args)
        {
            BeforeWrite?.Invoke(args);
        }

        internal AccessTokenCacheItem SaveAccessAndRefreshToken(AuthenticationRequestParameters requestParams, TokenResponse response)
        {
            lock (LockObject)
            {
                // create the access token cache item
                AccessTokenCacheItem accessTokenCacheItem =
                    new AccessTokenCacheItem(requestParams.Authority.CanonicalAuthority, requestParams.ClientId,
                        response);
                accessTokenCacheItem.UserAssertionHash = requestParams.UserAssertion?.AssertionHash;

                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = accessTokenCacheItem.User
                };

                HasStateChanged = true;
                OnBeforeAccess(args);
                OnBeforeWrite(args);

                //delete all cache entries with intersecting scopes.
                //this should not happen but we have this as a safe guard
                //against multiple matches.
                IList<AccessTokenCacheItem> accessTokenItemList = new List<AccessTokenCacheItem>();
                AccessTokenCacheItem atItem = null;
                foreach (var accessTokenString in TokenCacheAccessor.GetAllAccessTokensAsString())
                {
                    atItem = JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(accessTokenString);
                    if (atItem.ClientId.Equals(ClientId) && atItem.Authority.Equals(requestParams.Authority.CanonicalAuthority) &&
                                    atItem.Scope.ScopeIntersects(accessTokenCacheItem.Scope))
                    {
                        accessTokenItemList.Add(atItem);
                    }
                }
#if NET45 || NETSTANDARD1_3
                // if there is no credential then it is user flow
                // and not a client credential flow.
                if (!requestParams.HasCredential)
#endif
                {
                    //filter by home_oid of the user instead
                    accessTokenItemList =
                        accessTokenItemList.Where(item => item.HomeObjectId.Equals(accessTokenCacheItem.User?.HomeObjectId))
                            .ToList();
                }

                foreach (var cacheItem in accessTokenItemList)
                {
                    TokenCacheAccessor.DeleteAccessToken(cacheItem.GetTokenCacheKey().ToString());
                }

                TokenCacheAccessor.SaveAccessToken(accessTokenCacheItem.GetTokenCacheKey().ToString(), JsonHelper.SerializeToJson(accessTokenCacheItem));

                // if server returns the refresh token back, save it in the cache.
                if (response.RefreshToken != null)
                {
                    // create the refresh token cache item
                    RefreshTokenCacheItem refreshTokenCacheItem = new RefreshTokenCacheItem(null, requestParams.ClientId,
                        response);
                    TokenCacheAccessor.SaveRefreshToken(refreshTokenCacheItem.GetTokenCacheKey().ToString(), JsonHelper.SerializeToJson(refreshTokenCacheItem));
                }

                OnAfterAccess(args);

                return accessTokenCacheItem;
            }
        }

        internal AccessTokenCacheItem FindAccessToken(AuthenticationRequestParameters requestParam)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = requestParam.User
                };

                OnBeforeAccess(args);
                ICollection<AccessTokenCacheItem> tokenCacheItems = GetAllAccessTokens();

                OnAfterAccess(args);

                //first filter the list by authority, client id and scopes
                tokenCacheItems =
                    tokenCacheItems.Where(
                        item =>
                            item.Authority.Equals(requestParam.Authority.CanonicalAuthority) &&
                            item.ClientId.Equals(requestParam.ClientId) &&
                            item.Scope.ScopeContains(requestParam.Scope))
                        .ToList();
                
                    // this is OBO flow. match the cache entry with assertion hash,
                    // Authority, Scope and client Id.
                    if (requestParam.UserAssertion != null)
                    {
                        tokenCacheItems =
                            tokenCacheItems.Where(
                                item =>
                                    !string.IsNullOrEmpty(item.UserAssertionHash) &&
                                    item.UserAssertionHash.Equals(requestParam.UserAssertion.AssertionHash)).ToList();
                    }
                    else
                    {
#if NET45 || NETSTANDARD1_3
                    // if there is no credential then it is user flow
                    // and not a client credential flow.
                    if (!requestParam.HasCredential)
#endif
                        {
                            //filter by home_oid of the user instead
                            tokenCacheItems =
                                tokenCacheItems.Where(item => item.HomeObjectId.Equals(requestParam.User?.HomeObjectId))
                                    .ToList();
                        }
                    }

                if (tokenCacheItems.Count == 0)
                {
                    // TODO: log access token not found
                    return null;
                }

                // TODO: If user is not provided for silent request, and there is only one item found in the cache. Should we return it?
                if (tokenCacheItems.Count > 1)
                {
                    // TODO: log there are multiple access tokens found, don't know which one to use.
                    return null;
                }

                // Access token lookup needs to be a strict match. In the JSON response from token endpoint, server only returns the scope
                // the developer requires the token for. We store the token separately for considerations i.e. MFA.
                AccessTokenCacheItem accessTokenCacheItem = tokenCacheItems.First();
                if (accessTokenCacheItem.ExpiresOn >
                    DateTime.UtcNow + TimeSpan.FromMinutes(DefaultExpirationBufferInMinutes))
                {
                    return accessTokenCacheItem;
                }

                //TODO: log the access token found is expired.
                return null;
            }
        }

        internal RefreshTokenCacheItem FindRefreshToken(AuthenticationRequestParameters requestParam)
        {
            lock (LockObject)
            {
                TokenCacheKey key = new TokenCacheKey(null, null, requestParam.ClientId, requestParam.User?.HomeObjectId);
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = requestParam.User
                };

                OnBeforeAccess(args);
                RefreshTokenCacheItem rtItem = JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(TokenCacheAccessor.GetRefreshToken(key.ToString()));
                OnAfterAccess(args);
                return rtItem;
            }
        }

        internal void DeleteRefreshToken(RefreshTokenCacheItem rtItem)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = rtItem.User
                };

                OnBeforeAccess(args);
                OnBeforeWrite(args);
                TokenCacheAccessor.DeleteRefreshToken(rtItem.GetTokenCacheKey().ToString());
                OnAfterAccess(args);
            }
        }

        internal ICollection<User> GetUsers(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = null
                };

                OnBeforeAccess(args);
                ICollection<RefreshTokenCacheItem> tokenCacheItems = GetAllRefreshTokens();
                OnAfterAccess(args);

                IDictionary<string, User> allUsers = new Dictionary<string, User>();
                foreach (RefreshTokenCacheItem item in tokenCacheItems)
                {
                    User user = new User(item.User);
                    allUsers[item.HomeObjectId] = user;
                }

                return allUsers.Values;
            }
        }

        internal ICollection<RefreshTokenCacheItem> GetAllRefreshTokens()
        {
            lock (LockObject)
            {
                ICollection<RefreshTokenCacheItem> allRefreshTokens = new List<RefreshTokenCacheItem>();
                RefreshTokenCacheItem rtItem = null;
                foreach (var refreshTokenString in TokenCacheAccessor.GetAllRefreshTokensAsString())
                {
                    rtItem = JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(refreshTokenString);
                    if (rtItem.ClientId.Equals(ClientId))
                    {
                        allRefreshTokens.Add(rtItem);
                    }
                }

                return allRefreshTokens;
            }
        }
        
        internal ICollection<AccessTokenCacheItem> GetAllAccessTokens()
        {
            lock (LockObject)
            {
                ICollection<AccessTokenCacheItem> allAccessTokens = new List<AccessTokenCacheItem>();
                AccessTokenCacheItem atItem = null;
                foreach (var accessTokenString in TokenCacheAccessor.GetAllAccessTokensAsString())
                {
                    atItem = JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(accessTokenString);
                    if (atItem.ClientId.Equals(ClientId))
                    {
                        allAccessTokens.Add(atItem);
                    }
                }

                return allAccessTokens;
            }
        }
        
        internal void Remove(User user)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = null
                };

                OnBeforeAccess(args);
                OnBeforeWrite(args);
                IList<RefreshTokenCacheItem> allRefreshTokens = GetAllRefreshTokens()
                        .Where(item => item.HomeObjectId.Equals(user.HomeObjectId))
                        .ToList();
                foreach (var rtItem in allRefreshTokens)
                {
                    TokenCacheAccessor.DeleteRefreshToken(rtItem.GetTokenCacheKey().ToString());
                }

                IList<AccessTokenCacheItem> allAccessTokens = GetAllAccessTokens()
                        .Where(item => item.HomeObjectId.Equals(user.HomeObjectId)).ToList();

                foreach (var atItem in allAccessTokens)
                {
                    TokenCacheAccessor.DeleteAccessToken(atItem.GetTokenCacheKey().ToString());
                }

                OnAfterAccess(args);
            }
        }

        internal ICollection<string> GetAllAccessTokenCacheItems()
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                ICollection<string> allTokens =
                    TokenCacheAccessor.GetAllAccessTokensAsString();
                return allTokens;
            }
        }

        internal ICollection<string> GetAllRefreshTokenCacheItems()
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                ICollection<string> allTokens =
                    TokenCacheAccessor.GetAllRefreshTokensAsString();
                return allTokens;
            }
        }

        internal void AddAccessTokenCacheItem(AccessTokenCacheItem accessTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                TokenCacheAccessor.SaveAccessToken(accessTokenCacheItem.GetTokenCacheKey().ToString(), JsonHelper.SerializeToJson(accessTokenCacheItem));
            }
        }

        internal void AddRefreshTokenCacheItem(RefreshTokenCacheItem refreshTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                TokenCacheAccessor.SaveRefreshToken(refreshTokenCacheItem.GetTokenCacheKey().ToString(), JsonHelper.SerializeToJson(refreshTokenCacheItem));
            }
        }
    }
}