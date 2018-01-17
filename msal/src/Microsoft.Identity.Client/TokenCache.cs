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
using Microsoft.Identity.Client.Internal.Requests;

using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.Telemetry;

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

        internal MsalAccessTokenCacheItem SaveAccessAndRefreshToken(AuthenticationRequestParameters requestParams,
            MsalTokenResponse response)
        {
            lock (LockObject)
            {
                try
                {
                    MsalRefreshTokenCacheItem msalRefreshTokenCacheItem = null;
                    // create the access token cache item
                    MsalAccessTokenCacheItem msalAccessTokenCacheItem =
                        new MsalAccessTokenCacheItem(requestParams.TenantUpdatedCanonicalAuthority, requestParams.ClientId,
                                response)
                            {UserAssertionHash = requestParams.UserAssertion?.AssertionHash};

                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        User = new User(msalAccessTokenCacheItem.GetUserIdentifier(),
                            msalAccessTokenCacheItem.IdToken?.PreferredUsername, msalAccessTokenCacheItem.IdToken?.Name,
                            msalAccessTokenCacheItem.IdToken?.Issuer)
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
                    IList<MsalAccessTokenCacheItem> accessTokenItemList = new List<MsalAccessTokenCacheItem>();
                    foreach (var accessTokenString in TokenCacheAccessor.GetAllAccessTokensAsString())
                    {
                        MsalAccessTokenCacheItem msalAccessTokenItem =
                            JsonHelper.DeserializeFromJson<MsalAccessTokenCacheItem>(accessTokenString);
                        if (msalAccessTokenItem.ClientId.Equals(ClientId) &&
                            msalAccessTokenItem.Authority.Equals(requestParams.TenantUpdatedCanonicalAuthority) &&
                            msalAccessTokenItem.ScopeSet.ScopeIntersects(msalAccessTokenCacheItem.ScopeSet))
                        {
                            msg = "Intersecting scopes found - " + msalAccessTokenItem.Scope;
                            requestParams.RequestContext.Logger.Verbose(msg);
                            requestParams.RequestContext.Logger.VerbosePii(msg);
                            accessTokenItemList.Add(msalAccessTokenItem);
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
                                    item => item.GetUserIdentifier().Equals(msalAccessTokenCacheItem.GetUserIdentifier()))
                                .ToList();
                        msg = "Matching entries after filtering by user - " + accessTokenItemList.Count;
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                    }

                    foreach (var cacheItem in accessTokenItemList)
                    {
                        TokenCacheAccessor.DeleteAccessToken(cacheItem.GetAccessTokenItemKey().ToString(), requestParams.RequestContext);
                    }

                    TokenCacheAccessor.SaveAccessToken(msalAccessTokenCacheItem.GetAccessTokenItemKey().ToString(),
                        JsonHelper.SerializeToJson(msalAccessTokenCacheItem), requestParams.RequestContext);

                    // if server returns the refresh token back, save it in the cache.
                    if (response.RefreshToken != null)
                    {
                        // create the refresh token cache item
                       msalRefreshTokenCacheItem = new MsalRefreshTokenCacheItem(
                            requestParams.Authority.Host,
                            requestParams.ClientId,
                            response);
                        msg = "Saving RT in cache...";
                        requestParams.RequestContext.Logger.Info(msg);
                        requestParams.RequestContext.Logger.InfoPii(msg);
                        TokenCacheAccessor.SaveRefreshToken(msalRefreshTokenCacheItem.GetRefreshTokenItemKey().ToString(),
                            JsonHelper.SerializeToJson(msalRefreshTokenCacheItem), requestParams.RequestContext);
                    }

                    OnAfterAccess(args);

                    //save RT in ADAL cache for public clients
                    if (!requestParams.IsClientCredentialRequest)
                    {
                        CacheFallbackOperations.WriteAdalRefreshToken(msalRefreshTokenCacheItem, requestParams.TenantUpdatedCanonicalAuthority, msalAccessTokenCacheItem.IdToken.ObjectId, response.Scope);
                    }

                    return msalAccessTokenCacheItem;
                }
                finally
                {
                    HasStateChanged = false;
                }
            }
        }

        internal MsalAccessTokenCacheItem FindAccessToken(AuthenticationRequestParameters requestParams)
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

        private MsalAccessTokenCacheItem FindAccessTokenCommon(AuthenticationRequestParameters requestParams)
        {
            lock (LockObject)
            {
                string msg = "Looking up access token in the cache..";
                requestParams.RequestContext.Logger.Info(msg);
                requestParams.RequestContext.Logger.InfoPii(msg);
                MsalAccessTokenCacheItem msalAccessTokenCacheItem = null;
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = requestParams.User
                };

                OnBeforeAccess(args);
                //filtered by client id.
                ICollection<MsalAccessTokenCacheItem> tokenCacheItems = GetAllAccessTokensForClient(requestParams.RequestContext);
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

                IEnumerable<MsalAccessTokenCacheItem> filteredItems =
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
                        msalAccessTokenCacheItem = filteredItems.First();
                        requestParams.Authority =
                            Authority.CreateAuthority(msalAccessTokenCacheItem.Authority, requestParams.ValidateAuthority);

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
                        msalAccessTokenCacheItem = filteredItems.First();
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

                if (msalAccessTokenCacheItem != null && msalAccessTokenCacheItem.ExpiresOn >
                    DateTime.UtcNow + TimeSpan.FromMinutes(DefaultExpirationBufferInMinutes))
                {
                    msg = "Access token is not expired. Returning the found cache entry..";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                    return msalAccessTokenCacheItem;
                }

                if (msalAccessTokenCacheItem != null)
                {
                    msg = "Access token has expired or about to expire. Current time (" + DateTime.UtcNow +
                          ") - Expiration Time (" + msalAccessTokenCacheItem.ExpiresOn + ")";
                    requestParams.RequestContext.Logger.Info(msg);
                    requestParams.RequestContext.Logger.InfoPii(msg);
                }

                return null;
            }
        }

        internal MsalRefreshTokenCacheItem FindRefreshToken(AuthenticationRequestParameters requestParams)
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

        private MsalRefreshTokenCacheItem FindRefreshTokenCommon(AuthenticationRequestParameters requestParam)
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

                MsalRefreshTokenCacheKey key = new MsalRefreshTokenCacheKey(
                    requestParam.Authority.Host, requestParam.ClientId,
                    requestParam.User?.Identifier);
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = requestParam.User
                };

                OnBeforeAccess(args);
                MsalRefreshTokenCacheItem msalRefreshTokenCacheItem =
                    JsonHelper.DeserializeFromJson<MsalRefreshTokenCacheItem>(
                        TokenCacheAccessor.GetRefreshToken(key.ToString()));
                OnAfterAccess(args);

                msg = "Refresh token found in the cache? - " + (msalRefreshTokenCacheItem != null);
                requestParam.RequestContext.Logger.Info(msg);
                requestParam.RequestContext.Logger.InfoPii(msg);

                if (msalRefreshTokenCacheItem != null)
                {
                    return msalRefreshTokenCacheItem;
                }

                requestParam.RequestContext.Logger.Info("Checking ADAL cache for matching RT");
                requestParam.RequestContext.Logger.InfoPii("Checking ADAL cache for matching RT");
                return CacheFallbackOperations.GetAdalEntryForMsal(requestParam.Authority.Host, requestParam.ClientId, requestParam.LoginHint, requestParam.User?.Identifier);
            }
        }

        internal void DeleteRefreshToken(MsalRefreshTokenCacheItem msalRefreshTokenCacheItem)
        {
            lock (LockObject)
            {
                try
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        User = new User(msalRefreshTokenCacheItem.GetUserIdentifier(),
                            msalRefreshTokenCacheItem.DisplayableId, msalRefreshTokenCacheItem.Name,
                            msalRefreshTokenCacheItem.IdentityProvider)
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);
                    TokenCacheAccessor.DeleteRefreshToken(msalRefreshTokenCacheItem.GetRefreshTokenItemKey().ToString());
                    OnAfterAccess(args);
                }
                finally
                {
                    HasStateChanged = false;
                }
            }
        }

        internal void DeleteAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            lock (LockObject)
            {
                try
                {
                    TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                    {
                        TokenCache = this,
                        ClientId = ClientId,
                        User = new User(msalAccessTokenCacheItem.GetUserIdentifier(),
                            msalAccessTokenCacheItem.IdToken?.PreferredUsername, msalAccessTokenCacheItem.IdToken?.Name,
                            msalAccessTokenCacheItem.IdToken?.Issuer)
                    };

                    OnBeforeAccess(args);
                    OnBeforeWrite(args);
                    TokenCacheAccessor.DeleteAccessToken(msalAccessTokenCacheItem.GetAccessTokenItemKey().ToString());
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
                ICollection<MsalRefreshTokenCacheItem> tokenCacheItems = GetAllRefreshTokensForClient(requestContext);
                OnAfterAccess(args);

                IDictionary<string, User> allUsers = new Dictionary<string, User>();
                foreach (MsalRefreshTokenCacheItem item in tokenCacheItems)
                {
                    if (environment.Equals(
                        item.Environment, StringComparison.OrdinalIgnoreCase))
                    {
                        User user = new User(item.GetUserIdentifier(),
                            item.DisplayableId, item.Name,
                            item.IdentityProvider);
                        allUsers[item.GetUserIdentifier()] = user;
                    }
                }

                if (allUsers.Count > 0)
                {
                    return allUsers.Values;
                }

                foreach (MsalRefreshTokenCacheItem item in CacheFallbackOperations.GetAllAdalUsersForMsal(environment, ClientId))
                {
                    //only return ADAL users if they have client info
                    if (!string.IsNullOrEmpty(item.RawClientInfo))
                    {
                        User user = new User(item.GetUserIdentifier(),
                            item.DisplayableId, item.Name,
                            item.IdentityProvider);
                        allUsers[item.GetUserIdentifier()] = user;
                    }
                }

                return allUsers.Values;
            }
        }

        internal ICollection<MsalRefreshTokenCacheItem> GetAllRefreshTokensForClient(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<MsalRefreshTokenCacheItem> allRefreshTokens = new List<MsalRefreshTokenCacheItem>();
                foreach (var refreshTokenString in TokenCacheAccessor.GetAllRefreshTokensAsString())
                {
                    MsalRefreshTokenCacheItem msalRefreshTokenCacheItem =
                        JsonHelper.DeserializeFromJson<MsalRefreshTokenCacheItem>(refreshTokenString);
                    if (msalRefreshTokenCacheItem.ClientId.Equals(ClientId))
                    {
                        allRefreshTokens.Add(msalRefreshTokenCacheItem);
                    }
                }

                return allRefreshTokens;
            }
        }

        internal ICollection<MsalAccessTokenCacheItem> GetAllAccessTokensForClient(RequestContext requestContext)
        {
            lock (LockObject)
            {
                ICollection<MsalAccessTokenCacheItem> allAccessTokens = new List<MsalAccessTokenCacheItem>();
                foreach (var accessTokenString in TokenCacheAccessor.GetAllAccessTokensAsString())
                {
                    MsalAccessTokenCacheItem msalAccessTokenCacheItem =
                        JsonHelper.DeserializeFromJson<MsalAccessTokenCacheItem>(accessTokenString);
                    if (msalAccessTokenCacheItem.ClientId.Equals(ClientId))
                    {
                        allAccessTokens.Add(msalAccessTokenCacheItem);
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
                    IList<MsalRefreshTokenCacheItem> allRefreshTokens = GetAllRefreshTokensForClient(requestContext)
                        .Where(item => item.GetUserIdentifier().Equals(user.Identifier))
                        .ToList();
                    foreach (MsalRefreshTokenCacheItem refreshTokenCacheItem in allRefreshTokens)
                    {
                        TokenCacheAccessor.DeleteRefreshToken(refreshTokenCacheItem.GetRefreshTokenItemKey().ToString(), requestContext);
                    }

                    msg = "Deleted refresh token count - " + allRefreshTokens.Count;
                    requestContext.Logger.Info(msg);
                    requestContext.Logger.InfoPii(msg);
                    IList<MsalAccessTokenCacheItem> allAccessTokens = GetAllAccessTokensForClient(requestContext)
                        .Where(item => item.GetUserIdentifier().Equals(user.Identifier))
                        .ToList();

                    foreach (MsalAccessTokenCacheItem accessTokenCacheItem in allAccessTokens)
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

        internal void AddAccessTokenCacheItem(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                TokenCacheAccessor.SaveAccessToken(msalAccessTokenCacheItem.GetAccessTokenItemKey().ToString(),
                    JsonHelper.SerializeToJson(msalAccessTokenCacheItem));
            }
        }

        internal void AddRefreshTokenCacheItem(MsalRefreshTokenCacheItem msalRefreshTokenCacheItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (LockObject)
            {
                TokenCacheAccessor.SaveRefreshToken(msalRefreshTokenCacheItem.GetRefreshTokenItemKey().ToString(),
                    JsonHelper.SerializeToJson(msalRefreshTokenCacheItem));
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
        /// <param name="msalAccessTokenCacheItem"></param>
        internal void SaveAccesTokenCacheItem(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = new User(msalAccessTokenCacheItem.GetUserIdentifier(),
                        msalAccessTokenCacheItem.IdToken?.PreferredUsername, msalAccessTokenCacheItem.IdToken?.Name,
                        msalAccessTokenCacheItem.IdToken?.Issuer)
                };

                try
                {
                    HasStateChanged = true;
                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    TokenCacheAccessor.SaveAccessToken(msalAccessTokenCacheItem.GetAccessTokenItemKey().ToString(),
                        JsonHelper.SerializeToJson(msalAccessTokenCacheItem));
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
        /// <param name="msalRefreshTokenCacheItem"></param>
        internal void SaveRefreshTokenCacheItem(MsalRefreshTokenCacheItem msalRefreshTokenCacheItem)
        {
            lock (LockObject)
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs
                {
                    TokenCache = this,
                    ClientId = ClientId,
                    User = new User(msalRefreshTokenCacheItem.GetUserIdentifier(),
                        msalRefreshTokenCacheItem.DisplayableId, msalRefreshTokenCacheItem.Name,
                        msalRefreshTokenCacheItem.IdentityProvider)
                };

                try
                {
                    HasStateChanged = true;
                    OnBeforeAccess(args);
                    OnBeforeWrite(args);

                    TokenCacheAccessor.SaveRefreshToken(msalRefreshTokenCacheItem.GetRefreshTokenItemKey().ToString(),
                        JsonHelper.SerializeToJson(msalRefreshTokenCacheItem));
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