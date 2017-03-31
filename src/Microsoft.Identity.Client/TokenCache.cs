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
        private volatile bool _hasStateChanged;

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
        /// Gets or sets the flag indicating whether cache state has changed.
        /// MSAL methods set this flag after any change.
        /// Caller application should reset the flag after serializing and persisting the state of the cache.
        /// </summary>
        public bool HasStateChanged
        {
            get { return _hasStateChanged; }
            set { _hasStateChanged = value; }
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
            HasStateChanged = true;
            BeforeWrite?.Invoke(args);
        }

        internal AccessTokenCacheItem SaveAccessAndRefreshToken(AuthenticationRequestParameters requestParams,
            TokenResponse response)
        {
            lock (LockObject)
            {
                try
                {
                    // create the access token cache item
                    AccessTokenCacheItem accessTokenCacheItem =
                        new AccessTokenCacheItem(requestParams.Authority.CanonicalAuthority, requestParams.ClientId,
                            response)
                        { UserAssertionHash = requestParams.UserAssertion?.AssertionHash };

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
                    foreach (var accessTokenString in TokenCacheAccessor.GetAllAccessTokensAsString())
                    {
                        AccessTokenCacheItem accessTokenItem = JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(accessTokenString);
                        if (accessTokenItem.ClientId.Equals(ClientId) &&
                            accessTokenItem.Authority.Equals(requestParams.Authority.CanonicalAuthority) &&
                            accessTokenItem.ScopeSet.ScopeIntersects(accessTokenCacheItem.ScopeSet))
                        {
                            accessTokenItemList.Add(accessTokenItem);
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
                            accessTokenItemList.Where(
                                    item => item.GetUserIdentifier().Equals(accessTokenCacheItem.GetUserIdentifier()))
                                .ToList();
                    }

                    foreach (var cacheItem in accessTokenItemList)
                    {
                        TokenCacheAccessor.DeleteAccessToken(cacheItem.GetAccessTokenItemKey().ToString());
                    }

                    TokenCacheAccessor.SaveAccessToken(accessTokenCacheItem.GetAccessTokenItemKey().ToString(),
                        JsonHelper.SerializeToJson(accessTokenCacheItem));

                    // if server returns the refresh token back, save it in the cache.
                    if (response.RefreshToken != null)
                    {
                        // create the refresh token cache item
                        RefreshTokenCacheItem refreshTokenCacheItem = new RefreshTokenCacheItem(new Uri(requestParams.Authority.CanonicalAuthority).Host, 
                            requestParams.ClientId,
                            response);
                        TokenCacheAccessor.SaveRefreshToken(refreshTokenCacheItem.GetRefreshTokenItemKey().ToString(),
                            JsonHelper.SerializeToJson(refreshTokenCacheItem));
                    }
                    OnAfterAccess(args);

                    return accessTokenCacheItem;
                }
                finally
                {
                    HasStateChanged = false;
                }
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
                ICollection<AccessTokenCacheItem> tokenCacheItems = GetAllAccessTokensForClient();

                OnAfterAccess(args);

                //first filter the list by authority, client id and scopes
                tokenCacheItems =
                    tokenCacheItems.Where(
                        item =>
                            item.Authority.Equals(requestParam.Authority.CanonicalAuthority) &&
                            item.ClientId.Equals(requestParam.ClientId) &&
                            item.ScopeSet.ScopeContains(requestParam.Scope))
                        .ToList();

                // this is OBO flow. match the cache entry with assertion hash,
                // Authority, ScopeSet and client Id.
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
                            tokenCacheItems.Where(item => item.GetUserIdentifier().Equals(requestParam.User?.Identifier))
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
                RefreshTokenCacheKey key = new RefreshTokenCacheKey(
                    new Uri(requestParam.Authority.CanonicalAuthority).Host, requestParam.ClientId,
                    requestParam.User?.Identifier);
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = requestParam.User
                };

                OnBeforeAccess(args);
                RefreshTokenCacheItem refreshTokenCacheItem = JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(TokenCacheAccessor.GetRefreshToken(key.ToString()));
                OnAfterAccess(args);
                return refreshTokenCacheItem;
            }
        }

        internal void DeleteRefreshToken(RefreshTokenCacheItem refreshTokenCacheItem)
        {
            lock (LockObject)
            {
                try
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        User = refreshTokenCacheItem.User
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);
                    TokenCacheAccessor.DeleteRefreshToken(refreshTokenCacheItem.GetRefreshTokenItemKey().ToString());
                    OnAfterAccess(args);
                }
                finally
                {
                    HasStateChanged = false;
                }
            }
        }

        internal ICollection<User> GetUsers(string environment)
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
                ICollection<RefreshTokenCacheItem> tokenCacheItems = GetAllRefreshTokensForClient();
                OnAfterAccess(args);

                IDictionary<string, User> allUsers = new Dictionary<string, User>();
                foreach (RefreshTokenCacheItem item in tokenCacheItems)
                {
                    if (environment.Equals(
                        item.Environment, StringComparison.OrdinalIgnoreCase))
                    {
                        User user = new User(item.User);
                        allUsers[item.GetUserIdentifier()] = user;
                    }
                }

                return allUsers.Values;
            }
        }

        internal ICollection<RefreshTokenCacheItem> GetAllRefreshTokensForClient()
        {
            lock (LockObject)
            {
                ICollection<RefreshTokenCacheItem> allRefreshTokens = new List<RefreshTokenCacheItem>();
                foreach (var refreshTokenString in TokenCacheAccessor.GetAllRefreshTokensAsString())
                {
                    RefreshTokenCacheItem refreshTokenCacheItem = JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(refreshTokenString);
                    if (refreshTokenCacheItem.ClientId.Equals(ClientId))
                    {
                        allRefreshTokens.Add(refreshTokenCacheItem);
                    }
                }

                return allRefreshTokens;
            }
        }

        internal ICollection<AccessTokenCacheItem> GetAllAccessTokensForClient()
        {
            lock (LockObject)
            {
                ICollection<AccessTokenCacheItem> allAccessTokens = new List<AccessTokenCacheItem>();
                foreach (var accessTokenString in TokenCacheAccessor.GetAllAccessTokensAsString())
                {
                    AccessTokenCacheItem accessTokenCacheItem = JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(accessTokenString);
                    if (accessTokenCacheItem.ClientId.Equals(ClientId))
                    {
                        allAccessTokens.Add(accessTokenCacheItem);
                    }
                }

                return allAccessTokens;
            }
        }

        internal void Remove(User user)
        {
            lock (LockObject)
            {
                try
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        User = null
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);
                    IList<RefreshTokenCacheItem> allRefreshTokens = GetAllRefreshTokensForClient()
                        .Where(item => item.GetUserIdentifier().Equals(user.Identifier))
                        .ToList();
                    foreach (RefreshTokenCacheItem refreshTokenCacheItem in allRefreshTokens)
                    {
                        TokenCacheAccessor.DeleteRefreshToken(refreshTokenCacheItem.GetRefreshTokenItemKey().ToString());
                    }

                    IList<AccessTokenCacheItem> allAccessTokens = GetAllAccessTokensForClient()
                        .Where(item => item.GetUserIdentifier().Equals(user.Identifier))
                        .ToList();

                    foreach (AccessTokenCacheItem accessTokenCacheItem in allAccessTokens)
                    {
                        TokenCacheAccessor.DeleteAccessToken(accessTokenCacheItem.GetAccessTokenItemKey().ToString());
                    }

                    OnAfterAccess(args);
                }
                finally
                {
                    HasStateChanged = false;
                }
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
                TokenCacheAccessor.SaveAccessToken(accessTokenCacheItem.GetAccessTokenItemKey().ToString(), JsonHelper.SerializeToJson(accessTokenCacheItem));
            }
        }

        internal void AddRefreshTokenCacheItem(RefreshTokenCacheItem refreshTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                TokenCacheAccessor.SaveRefreshToken(refreshTokenCacheItem.GetRefreshTokenItemKey().ToString(), JsonHelper.SerializeToJson(refreshTokenCacheItem));
            }
        }
    }
}