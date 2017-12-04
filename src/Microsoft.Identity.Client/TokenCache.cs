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
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.OAuth2;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Internal.Telemetry;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Token cache class used by ConfidentialClientApplication and PublicClientApplication to store access and refresh tokens.
    /// </summary>
    public sealed class TokenCache
    {
        private const int DefaultExpirationBufferInMinutes = 5;

        internal readonly TelemetryTokenCacheAccessor TokenCacheAccessor = new TelemetryTokenCacheAccessor();

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
                    var msg = "Looking for scopes for the authority in the cache which intersect with " +
                              requestParams.Scope.AsSingleString();
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    IList<AccessTokenCacheItem> accessTokenItemList = new List<AccessTokenCacheItem>();
                    foreach (var accessTokenString in TokenCacheAccessor.GetAllAccessTokensAsString())
                    {
                        AccessTokenCacheItem accessTokenItem =
                            JsonHelper.DeserializeFromJson<AccessTokenCacheItem>(accessTokenString);
                        if (accessTokenItem.ClientId.Equals(ClientId) &&
                            accessTokenItem.Authority.Equals(requestParams.TenantUpdatedCanonicalAuthority) &&
                            accessTokenItem.ScopeSet.ScopeIntersects(accessTokenCacheItem.ScopeSet))
                        {
                            msg = "Intersecting scopes found - " + accessTokenItem.Scope;
                            requestParams.RequestContext.Logger.Verbose(msg);
                            requestParams.RequestContext.Logger.VerbosePii(msg);
                            accessTokenItemList.Add(accessTokenItem);
                        }
                    }

                    msg = "Intersecting scope entries count - " + accessTokenItemList.Count;
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);

                    if (!requestParams.IsClientCredentialRequest)
                    {
                        //filter by identifer of the user instead
                        accessTokenItemList =
                            accessTokenItemList.Where(
                                    item => item.GetUserIdentifier().Equals(accessTokenCacheItem.GetUserIdentifier()))
                                .ToList();
                        msg = "Matching entries after filtering by user - " + accessTokenItemList.Count;
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                    }

                    foreach (var cacheItem in accessTokenItemList)
                    {
                        TokenCacheAccessor.DeleteAccessToken(cacheItem.GetAccessTokenItemKey().ToString(), requestParams.RequestContext);
                    }

                    TokenCacheAccessor.SaveAccessToken(accessTokenCacheItem.GetAccessTokenItemKey().ToString(),
                        JsonHelper.SerializeToJson(accessTokenCacheItem), requestParams.RequestContext);

                    // if server returns the refresh token back, save it in the cache.
                    if (response.RefreshToken != null)
                    {
                        // create the refresh token cache item
                        RefreshTokenCacheItem refreshTokenCacheItem = new RefreshTokenCacheItem(
                            requestParams.Authority.Host,
                            requestParams.ClientId,
                            response);
                        msg = "Saving RT in cache...";
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                        TokenCacheAccessor.SaveRefreshToken(refreshTokenCacheItem.GetRefreshTokenItemKey().ToString(),
                            JsonHelper.SerializeToJson(refreshTokenCacheItem), requestParams.RequestContext);
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

        internal AccessTokenCacheItem FindAccessToken(AuthenticationRequestParameters requestParams)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheLookup) { TokenType = CacheEvent.TokenTypes.AT };
            Telemetry.GetInstance().StartEvent(requestParams.RequestContext.TelemetryRequestId, cacheEvent);
            try
            {
                return FindAccessTokenCommon(requestParams);
            }
            finally
            {
                Telemetry.GetInstance().StopEvent(requestParams.RequestContext.TelemetryRequestId, cacheEvent);
            }
        }

        private AccessTokenCacheItem FindAccessTokenCommon(AuthenticationRequestParameters requestParams)
        {
            lock (LockObject)
            {
                string msg = "Looking up access token in the cache..";
                requestParams.RequestContext.Logger.Info(msg);
                requestParams.RequestContext.Logger.InfoPii(msg);
                AccessTokenCacheItem accessTokenCacheItem = null;
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = requestParams.User
                };

                OnBeforeAccess(args);
                //filtered by client id.
                ICollection<AccessTokenCacheItem> tokenCacheItems = GetAllAccessTokensForClient(requestParams.RequestContext);
                OnAfterAccess(args);

                // this is OBO flow. match the cache entry with assertion hash,
                // Authority, ScopeSet and client Id.
                if (requestParams.UserAssertion != null)
                {
                    msg = "Filtering by user assertion...";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    tokenCacheItems =
                        tokenCacheItems.Where(
                                item =>
                                    !string.IsNullOrEmpty(item.UserAssertionHash) &&
                                    item.UserAssertionHash.Equals(requestParams.UserAssertion.AssertionHash))
                            .ToList();
                }
                else
                {
                    if (!requestParams.IsClientCredentialRequest)
                    {
                        msg = "Filtering by user identifier...";
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                        //filter by identifier of the user instead
                        tokenCacheItems =
                            tokenCacheItems
                                .Where(item => item.GetUserIdentifier().Equals(requestParams.User?.Identifier))
                                .ToList();
                    }
                }

                //no match found after initial filtering
                if (!tokenCacheItems.Any())
                {
                    msg = "No matching entry found for user or assertion";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    return null;
                }

                msg = "Matching entry count -" + tokenCacheItems.Count;
                requestParams.RequestContext.Logger.Info(msg);
                requestParams.RequestContext.Logger.InfoPii(msg);

                IEnumerable<AccessTokenCacheItem> filteredItems =
                    tokenCacheItems.Where(
                            item =>
                                item.ScopeSet.ScopeContains(requestParams.Scope))
                        .ToList();

                msg = "Matching entry count after filtering by scopes - " + filteredItems.Count();
                requestParams.RequestContext.Logger.Info(msg);
                requestParams.RequestContext.Logger.InfoPii(msg);
                //no authority passed
                if (requestParams.Authority == null)
                {
                    msg = "No authority provided..";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    //if only one cached token found
                    if (filteredItems.Count() == 1)
                    {
                        accessTokenCacheItem = filteredItems.First();
                        requestParams.Authority =
                            Authority.CreateAuthority(accessTokenCacheItem.Authority, requestParams.ValidateAuthority);

                        msg = "1 matching entry found.Authority may be used for refreshing access token.";
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                    }
                    else if (filteredItems.Count() > 1)
                    {
                        msg = "Multiple authorities found for same client_id, user and scopes";
                        requestParams.RequestContext.Logger.Error(msg);
                        requestParams.RequestContext.Logger.ErrorPii(msg + " :- " + filteredItems
                                .Select(tci => tci.Authority)
                                .AsSingleString());
                        throw new MsalClientException(MsalClientException.MultipleTokensMatchedError,
                            MsalErrorMessage.MultipleTokensMatched);
                    }
                    else
                    {
                        msg = "No tokens found for matching client_id, user and scopes.";
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);

                        msg = "Check if the tokens are for the same authority for given client_id and user.";
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                        //no match found. check if there was a single authority used
                        IEnumerable<string> authorityList = tokenCacheItems.Select(tci => tci.Authority).Distinct();
                        if (authorityList.Count() > 1)
                        {
                            msg = "Multiple authorities found for same client_id and user.";
                            requestParams.RequestContext.Logger.Error(msg);
                            requestParams.RequestContext.Logger.ErrorPii(msg + " :- " + authorityList.AsSingleString());

                            throw new MsalClientException(MsalClientException.MultipleTokensMatchedError,
                                "Multiple authorities found in the cache. Pass in authority in the API overload.");
                        }

                        msg = "Distinct Authority found. Use it for refresh token grant call";
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                        requestParams.Authority = Authority.CreateAuthority(authorityList.First(), requestParams.ValidateAuthority);
                    }
                }
                else
                {
                    msg = "Authority provided..";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    //authority was passed in the API
                    filteredItems =
                        filteredItems.Where(
                                item =>
                                    item.Authority.Equals(requestParams.Authority.CanonicalAuthority))
                            .ToList();

                    //no match
                    if (!filteredItems.Any())
                    {
                        msg = "No tokens found for matching authority, client_id, user and scopes.";
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                        return null;
                    }

                    //if only one cached token found
                    if (filteredItems.Count() == 1)
                    {
                        accessTokenCacheItem = filteredItems.First();
                    }
                    else
                    {
                        msg = "Multiple tokens found for matching authority, client_id, user and scopes.";
                        requestParams.RequestContext.Logger.Error(msg);
                        requestParams.RequestContext.Logger.ErrorPii(msg);
                        
                        throw new MsalClientException(MsalClientException.MultipleTokensMatchedError,
                            MsalErrorMessage.MultipleTokensMatched);
                    }
                }

                if (accessTokenCacheItem != null && accessTokenCacheItem.ExpiresOn >
                    DateTime.UtcNow + TimeSpan.FromMinutes(DefaultExpirationBufferInMinutes))
                {
                    msg = "Access token is not expired. Returning the found cache entry..";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    return accessTokenCacheItem;
                }

                if (accessTokenCacheItem != null)
                {
                    msg = "Access token has expired or about to expire. Current time (" + DateTime.UtcNow +
                          ") - Expiration Time (" + accessTokenCacheItem.ExpiresOn + ")";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                }

                return null;
            }
        }

        internal RefreshTokenCacheItem FindRefreshToken(AuthenticationRequestParameters requestParams)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheLookup) { TokenType = CacheEvent.TokenTypes.RT };
            Telemetry.GetInstance().StartEvent(requestParams.RequestContext.TelemetryRequestId, cacheEvent);
            try
            {
                return FindRefreshTokenCommon(requestParams);
            }
            finally
            {
                Telemetry.GetInstance().StopEvent(requestParams.RequestContext.TelemetryRequestId, cacheEvent);
            }
        }

        private RefreshTokenCacheItem FindRefreshTokenCommon(AuthenticationRequestParameters requestParam)
        {
            lock (LockObject)
            {
                var msg = "Looking up refresh token in the cache..";
                requestParam.RequestContext.Logger.Info(msg);
                requestParam.RequestContext.Logger.InfoPii(msg);
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

                msg = "Refresh token found in the cache? - " + (refreshTokenCacheItem != null);
                requestParam.RequestContext.Logger.Info(msg);
                requestParam.RequestContext.Logger.InfoPii(msg);
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
                var msg = "Removing user from cache..";
                requestContext.Logger.Info(msg);
                requestContext.Logger.InfoPii(msg);

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
                        TokenCacheAccessor.DeleteRefreshToken(refreshTokenCacheItem.GetRefreshTokenItemKey().ToString(), requestContext);
                    }

                    msg = "Deleted refresh token count - " + allRefreshTokens.Count;
                    requestContext.Logger.Info(msg);
                    requestContext.Logger.InfoPii(msg);
                    IList<AccessTokenCacheItem> allAccessTokens = GetAllAccessTokensForClient(requestContext)
                        .Where(item => item.GetUserIdentifier().Equals(user.Identifier))
                        .ToList();

                    foreach (AccessTokenCacheItem accessTokenCacheItem in allAccessTokens)
                    {
                        TokenCacheAccessor.DeleteAccessToken(accessTokenCacheItem.GetAccessTokenItemKey().ToString(), requestContext);
                    }

                    msg = "Deleted access token count - " + allAccessTokens.Count;
                    requestContext.Logger.Info(msg);
                    requestContext.Logger.InfoPii(msg);
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