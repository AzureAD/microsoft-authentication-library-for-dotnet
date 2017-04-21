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
using System.Globalization;
using System.Linq;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.OAuth2;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Token cache class used by ConfidentialClientApplication and PublicClientApplication to store access and refresh tokens.
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
        /// AfterAccess notification.
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
                        new AccessTokenCacheItem(requestParams.TenantUpdatedCanonicalAuthority, requestParams.ClientId,
                                response)
                            {UserAssertionHash = requestParams.UserAssertion?.AssertionHash};

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
                        AccessTokenCacheItem accessTokenItem =
                            JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(accessTokenString);
                        if (accessTokenItem.ClientId.Equals(ClientId) &&
                            accessTokenItem.Authority.Equals(requestParams.TenantUpdatedCanonicalAuthority) &&
                            accessTokenItem.ScopeSet.ScopeIntersects(accessTokenCacheItem.ScopeSet))
                        {
                            accessTokenItemList.Add(accessTokenItem);
                        }
                    }
#if DESKTOP || NETSTANDARD1_3
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
                        RefreshTokenCacheItem refreshTokenCacheItem = new RefreshTokenCacheItem(
                            requestParams.Authority.Host,
                            requestParams.ClientId,
                            response);
                        requestParams.RequestContext.Logger.Info("Saving RT in cache with key - " );
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
                AccessTokenCacheItem accessTokenCacheItem = null;
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = requestParam.User
                };

                OnBeforeAccess(args);
                //filtered by client id.
                ICollection<AccessTokenCacheItem> tokenCacheItems = GetAllAccessTokensForClient(requestParam.RequestContext);
                OnAfterAccess(args);

                // this is OBO flow. match the cache entry with assertion hash,
                // Authority, ScopeSet and client Id.
                if (requestParam.UserAssertion != null)
                {
                    tokenCacheItems =
                        tokenCacheItems.Where(
                                item =>
                                    !string.IsNullOrEmpty(item.UserAssertionHash) &&
                                    item.UserAssertionHash.Equals(requestParam.UserAssertion.AssertionHash))
                            .ToList();
                }
                else
                {
#if DESKTOP || NETSTANDARD1_3
// if there is no credential then it is user flow
// and not a client credential flow.
                    if (!requestParam.HasCredential)
#endif
                    {
                        //filter by identifier of the user instead
                        tokenCacheItems =
                            tokenCacheItems
                                .Where(item => item.GetUserIdentifier().Equals(requestParam.User?.Identifier))
                                .ToList();
                    }
                }

                //no match found after initial filtering
                if (!tokenCacheItems.Any())
                {
                    return null;
                }

                IEnumerable<AccessTokenCacheItem> filteredItems =
                    tokenCacheItems.Where(
                            item =>
                                item.ScopeSet.ScopeContains(requestParam.Scope))
                        .ToList();
                //no authority passed
                if (requestParam.Authority == null)
                {
                    //if only one cached token found
                    if (filteredItems.Count() == 1)
                    {
                        accessTokenCacheItem = filteredItems.First();
                        requestParam.Authority =
                            Authority.CreateAuthority(accessTokenCacheItem.Authority, requestParam.ValidateAuthority);
                    }
                    else if (filteredItems.Count() > 1)
                    {
                        //more than 1 match found
                        //TODO: log PII for authorities found.
                        throw new MsalClientException(MsalClientException.MultipleTokensMatchedError,
                            MsalErrorMessage.MultipleTokensMatched);
                    }
                    else
                    {
                        //no match found. check if there was a single authority used
                        IEnumerable<string> authorityList = tokenCacheItems.Select(tci => tci.Authority).Distinct();
                        if (authorityList.Count() > 1)
                        {
                            throw new MsalClientException(MsalClientException.MultipleTokensMatchedError,
                                "Multiple authorities found in the cache. Pass in authority in the API overload.");
                        }

                        requestParam.Authority =
                            Authority.CreateAuthority(authorityList.First(), requestParam.ValidateAuthority);
                    }
                }
                else
                {
                    //authority was passed in the API
                    filteredItems =
                        filteredItems.Where(
                                item =>
                                    item.Authority.Equals(requestParam.Authority.CanonicalAuthority))
                            .ToList();

                    //no match
                    if (!filteredItems.Any())
                    {
                        // TODO: log access token not found
                        return null;
                    }

                    //if only one cached token found
                    if (filteredItems.Count() == 1)
                    {
                        accessTokenCacheItem = filteredItems.First();
                    }
                    else
                    {
                        //more than 1 match found
                        //TODO: log PII for authorities found.
                        throw new MsalClientException(MsalClientException.MultipleTokensMatchedError,
                            MsalErrorMessage.MultipleTokensMatched);
                    }
                }

                if (accessTokenCacheItem != null && accessTokenCacheItem.ExpiresOn >
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
                if (requestParam.Authority == null)
                {
                    return null;
                }

                RefreshTokenCacheKey key = new RefreshTokenCacheKey(
                    requestParam.Authority.Host, requestParam.ClientId,
                    requestParam.User?.Identifier);
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = requestParam.User
                };

                OnBeforeAccess(args);
                RefreshTokenCacheItem refreshTokenCacheItem =
                    JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(
                        TokenCacheAccessor.GetRefreshToken(key.ToString()));
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

        internal void DeleteAccessToken(AccessTokenCacheItem accessTokenCacheItem)
        {
            lock (LockObject)
            {
                try
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        User = accessTokenCacheItem.User
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);
                    TokenCacheAccessor.DeleteAccessToken(accessTokenCacheItem.GetAccessTokenItemKey().ToString());
                    OnAfterAccess(args);
                }
                finally
                {
                    HasStateChanged = false;
                }
            }
        }

        internal ICollection<User> GetUsers(string environment, RequestContext requestContext)
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
                ICollection<RefreshTokenCacheItem> tokenCacheItems = GetAllRefreshTokensForClient(requestContext);
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

        internal ICollection<RefreshTokenCacheItem> GetAllRefreshTokensForClient(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<RefreshTokenCacheItem> allRefreshTokens = new List<RefreshTokenCacheItem>();
                foreach (var refreshTokenString in TokenCacheAccessor.GetAllRefreshTokensAsString())
                {
                    RefreshTokenCacheItem refreshTokenCacheItem =
                        JsonHelper.DeserializeFromJson<RefreshTokenCacheItem>(refreshTokenString);
                    if (refreshTokenCacheItem.ClientId.Equals(ClientId))
                    {
                        allRefreshTokens.Add(refreshTokenCacheItem);
                    }
                }

                return allRefreshTokens;
            }
        }

        internal ICollection<AccessTokenCacheItem> GetAllAccessTokensForClient(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<AccessTokenCacheItem> allAccessTokens = new List<AccessTokenCacheItem>();
                foreach (var accessTokenString in TokenCacheAccessor.GetAllAccessTokensAsString())
                {
                    AccessTokenCacheItem accessTokenCacheItem =
                        JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(accessTokenString);
                    if (accessTokenCacheItem.ClientId.Equals(ClientId))
                    {
                        allAccessTokens.Add(accessTokenCacheItem);
                    }
                }

                return allAccessTokens;
            }
        }

        internal void Remove(IUser user, RequestContext requestContext)
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
                    IList<RefreshTokenCacheItem> allRefreshTokens = GetAllRefreshTokensForClient(requestContext)
                        .Where(item => item.GetUserIdentifier().Equals(user.Identifier))
                        .ToList();
                    foreach (RefreshTokenCacheItem refreshTokenCacheItem in allRefreshTokens)
                    {
                        TokenCacheAccessor.DeleteRefreshToken(refreshTokenCacheItem.GetRefreshTokenItemKey()
                            .ToString());
                    }

                    IList<AccessTokenCacheItem> allAccessTokens = GetAllAccessTokensForClient(requestContext)
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

        internal ICollection<string> GetAllAccessTokenCacheItems(RequestContext requestContext)
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

        internal ICollection<string> GetAllRefreshTokenCacheItems(RequestContext requestContext)
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
                TokenCacheAccessor.SaveAccessToken(accessTokenCacheItem.GetAccessTokenItemKey().ToString(),
                    JsonHelper.SerializeToJson(accessTokenCacheItem));
            }
        }

        internal void AddRefreshTokenCacheItem(RefreshTokenCacheItem refreshTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                TokenCacheAccessor.SaveRefreshToken(refreshTokenCacheItem.GetRefreshTokenItemKey().ToString(),
                    JsonHelper.SerializeToJson(refreshTokenCacheItem));
            }
        }

        internal void ClearCache()
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

                    TokenCacheAccessor.Clear();

                    OnAfterAccess(args);
                }
                finally
                {
                    HasStateChanged = false;
                }
            }
        }

        /// <summary>
        /// Only used by dev test apps
        /// </summary>
        /// <param name="accessTokenCacheItem"></param>
        internal void SaveAccesTokenCacheItem(AccessTokenCacheItem accessTokenCacheItem)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = accessTokenCacheItem.User
                };

                try
                {
                    HasStateChanged = true;
                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    TokenCacheAccessor.SaveAccessToken(accessTokenCacheItem.GetAccessTokenItemKey().ToString(),
                        JsonHelper.SerializeToJson(accessTokenCacheItem));
                }
                finally
                {
                    OnAfterAccess(args);
                    HasStateChanged = false;
                }
            }
        }

        /// <summary>
        /// Only used by dev test apps
        /// </summary>
        /// <param name="refreshTokenCacheItem"></param>
        internal void SaveRefreshTokenCacheItem(RefreshTokenCacheItem refreshTokenCacheItem)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = refreshTokenCacheItem.User
                };

                try
                {
                    HasStateChanged = true;
                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    TokenCacheAccessor.SaveRefreshToken(refreshTokenCacheItem.GetRefreshTokenItemKey().ToString(),
                        JsonHelper.SerializeToJson(refreshTokenCacheItem));
                }
                finally
                {
                    OnAfterAccess(args);
                    HasStateChanged = false;
                }
            }
        }
    }
}